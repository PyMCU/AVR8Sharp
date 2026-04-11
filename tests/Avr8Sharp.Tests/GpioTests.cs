using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

[TestFixture]
public class Gpio : AvrTestBase
{
	// CPU registers
	const int SREG = 95;

	// GPIO registers
	const int PINB = 0x23;
	const int DDRB = 0x24;
	const int PORTB = 0x25;
	const int PIND = 0x29;
	const int DDRD = 0x2a;
	const int PORTD = 0x2b;
	const int EIFR = 0x3c;
	const int EIMSK = 0x3d;
	const int PCICR = 0x68;
	const int EICRA = 0x69;
	const int PCIFR = 0x3b;
	const int PCMSK0 = 0x6b;

	// Register bit names
	const int INT0 = 0;
	const int ISC00 = 0;
	const int ISC01 = 1;
	const int PCIE0 = 0;
	const int PCINT3 = 3;

	// Pin names
	const int PB0 = 0;
	const int PB1 = 1;
	const int PB3 = 3;
	const int PB4 = 4;
	const int PD2 = 2;

	// Interrupt vector addresses
	const int PC_INT_INT0 = 2;
	const int PC_INT_PCINT0 = 6;

	private AvrIoPort portB;
	private AvrIoPort portC;
	private AvrIoPort portD;

	protected override void SetupPeripherals()
	{
		portB = new AvrIoPort (Cpu, AvrIoPort.PortBConfig);
		portC = new AvrIoPort (Cpu, AvrIoPort.PortCConfig);
		portD = new AvrIoPort (Cpu, AvrIoPort.PortDConfig);
	}


	[Test (Description = "Should invoke the listeners when the port is written to")]
	public void PortWrite ()
	{
		Cpu.WriteData (DDRB, 0x0f);

		portB.AddListener ((value, oldValue) => {
			Assert.That (value, Is.EqualTo (0x55));
		});

		Cpu.WriteData (PORTB, 0x55);

		Assert.That (Cpu.Mmio.Data[0x23], Is.EqualTo (0x5));
	}

	[Test (Description = "Should invoke the listeners when DDR changes (issue #28)")]
	public void DdrWrite ()
	{
		var counter = 0;

		Cpu.WriteData (PORTB, 0x55);

		portB.AddListener ((value, oldValue) => {
			Assert.That (value, Is.EqualTo (0x55));
		});

		Cpu.WriteData (DDRB, 0xf0);
	}

	[Test (Description = "Should invoke the listeners when pullup register enabled (issue #62)")]
	public void Pullup ()
	{
		var counter = 0;

		portB.AddListener ((value, oldValue) => {
			Assert.That (value, counter == 0 ? Is.EqualTo (0x55) : Is.EqualTo (0));
			counter++;
		});

		Cpu.WriteData (PORTB, 0x55);
	}

	[Test (Description = "Should toggle the pin when writing to the PIN register")]
	public void PinToggle ()
	{
		var calledCorrectly = false;

		portB.AddListener ((value, oldValue) => {
			calledCorrectly |= value == 0x54 && oldValue == 0x55;
		});

		Cpu.WriteData (DDRB, 0x0f);
		Cpu.WriteData (PORTB, 0x55);
		Cpu.WriteData (PINB, 0x01);

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Mmio.Data[PINB], Is.EqualTo(0x4));
            Assert.That(calledCorrectly, Is.True);
        });
    }

	[Test (Description = "Should only affect one pin when writing to PIN using SBI (issue #103)")]
	public void PinToggleSbi ()
	{
		var program = new AsmProgram (@$"
			; register addresses
		    _REPLACE DDRD, {DDRD - 0x20}
			_REPLACE PIND, {PIND - 0x20}
			_REPLACE PORTD, {PORTD - 0x20}

		    ; Setup
		    ldi r24, 0x48
		    out DDRD, r24
		    out PORTD, r24

		    ; Now toggle pin 6 with SBI
		    sbi PIND, 6

		    break
").Compile();

		Cpu.LoadProgram(program.Program);
		var runner = new TestProgramRunner (Cpu);

		var calledCorrectly = false;

		portD.AddListener ((value, oldValue) => {
			calledCorrectly |= value == 0x48 && oldValue == 0;
		});

		runner.RunInstructions (3);
		Assert.That (Cpu.Mmio.Data[PORTD], Is.EqualTo (0x48));

		var calledCorrectly2 = false;
		portD.AddListener ((value, oldValue) => {
			calledCorrectly2 |= value == 0x08 && oldValue == 0x48;
		});

		runner.RunInstructions (1);
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Mmio.Data[PORTD], Is.EqualTo(0x8));
            Assert.That(calledCorrectly, Is.True);
            Assert.That(calledCorrectly2, Is.True);
        });
    }

	[Test (Description = "Should update the PIN register on output compare (OCR) match (issue #102)")]
	public void PinToggleOcrMatch ()
	{
		Cpu.WriteData (DDRB, 1 << 1);

		portB.TimerOverridePin (1, PinOverrideMode.Set);
		Assert.Multiple (() => {
			Assert.That (portB.GetPinState (1), Is.EqualTo (AVR8Sharp.Core.Peripherals.PinState.High));
			Assert.That (Cpu.Mmio.Data[PINB], Is.EqualTo (1 << 1));
		});

		portB.TimerOverridePin (1, PinOverrideMode.Clear);
		Assert.Multiple (() => {
			Assert.That (portB.GetPinState (1), Is.EqualTo (AVR8Sharp.Core.Peripherals.PinState.Low));
			Assert.That (Cpu.Mmio.Data[PINB], Is.EqualTo (0));
		});
	}

	[Test (Description = "Should remove the given listener")]
	public void RemoveListener ()
	{
		var counter = 0;

		var listener = new Action<byte, byte> ((_, _) => {
			counter++;
		});

		portB.AddListener (listener);

		Cpu.WriteData (DDRB, 0x0f);

		portB.RemoveListener (listener);

		Cpu.WriteData (PORTB, 0x99);

		Assert.That (counter, Is.EqualTo (1));
	}

	[TestFixture]
	public class PinState : AvrTestBase
	{
		private AvrIoPort portB;

		protected override void SetupPeripherals()
		{
			portB = new AvrIoPort (Cpu, AvrIoPort.PortBConfig);
		}

		[Test (Description = "Should return PinState.High when the pin set to output and HIGH")]
		public void PinStateHigh ()
		{
			Cpu.WriteData (DDRB, 0x1);
			Cpu.WriteData (PORTB, 0x1);

			Assert.That (portB.GetPinState (PB0), Is.EqualTo (AVR8Sharp.Core.Peripherals.PinState.High));
		}

		[Test (Description = "Should return PinState.Low when the pin set to output and LOW")]
		public void PinStateLow ()
		{
			Cpu.WriteData (DDRB, 0x8);
			Cpu.WriteData (PORTB, 0xf7);

			Assert.That (portB.GetPinState (PB3), Is.EqualTo (AVR8Sharp.Core.Peripherals.PinState.Low));
		}

		[Test (Description = "Should return PinState.Input by default (reset state)")]
		public void PinStateInput ()
		{
			Assert.That (portB.GetPinState (PB1), Is.EqualTo (AVR8Sharp.Core.Peripherals.PinState.Input));
		}

		[Test (Description = "Should return PinState.InputPullUp when the pin is set to input with pullup")]
		public void PinStateInputPullUp ()
		{
			Cpu.WriteData (DDRB, 0);
			Cpu.WriteData (PORTB, 0x2);

			Assert.That (portB.GetPinState (PB1), Is.EqualTo (AVR8Sharp.Core.Peripherals.PinState.InputPullup));
		}

		[Test (Description = "Should reflect the current port state when called inside a listener")]
		public void GetPinStateInListener ()
		{
			var listener = new System.Action<byte, byte> ((value, oldValue) => {
				Assert.That (portB.GetPinState (PB0), Is.EqualTo (AVR8Sharp.Core.Peripherals.PinState.High));
			});

			Assert.That (portB.GetPinState (PB0), Is.EqualTo (AVR8Sharp.Core.Peripherals.PinState.Input));
			Cpu.WriteData (DDRB, 0x01);
			portB.AddListener (listener);
			Cpu.WriteData (PORTB, 0x01);
		}

		[Test (Description = "Should reflect the current port state when called inside a listener after DDR change")]
		public void GetPinStateInListenerAfterDdrChange ()
		{
			var listener = new System.Action<byte, byte> ((_, _) => {
				Assert.That (portB.GetPinState (PB0), Is.EqualTo (AVR8Sharp.Core.Peripherals.PinState.Low));
			});

			Assert.That (portB.GetPinState (PB0), Is.EqualTo (AVR8Sharp.Core.Peripherals.PinState.Input));
			portB.AddListener (listener);
			Cpu.WriteData (DDRB, 0x01);
		}
	}

	[TestFixture]
	public class SetPin : AvrTestBase
	{
		private AvrIoPort portB;

		protected override void SetupPeripherals()
		{
			portB = new AvrIoPort (Cpu, AvrIoPort.PortBConfig);
		}

		[Test (Description = "Should set the value of the given pin")]
		public void SetPinValue ()
		{
			Cpu.WriteData(DDRB, 0);
			portB.SetPinValue (PB4, true);
			Assert.That (Cpu.Mmio.Data[0x23], Is.EqualTo (0x10));

			portB.SetPinValue (PB4, false);
			Assert.That (Cpu.Mmio.Data[0x23], Is.EqualTo (0));
		}

		[Test (Description = "Should only update PIN register when pin in Input mode")]
		public void SetPinValueInput ()
		{
			Cpu.WriteData (DDRB, 0x10);
			Cpu.WriteData (PORTB, 0x0);

			portB.SetPinValue (PB4, true);

			Assert.That (Cpu.Mmio.Data[PINB], Is.EqualTo (0x0));

			Cpu.WriteData (DDRB, 0x0);

			Assert.That (Cpu.Mmio.Data[PINB], Is.EqualTo (0x10));
		}
	}

	[TestFixture]
	public class ExternalInterrupts : AvrTestBase
	{
		private AvrIoPort portD;

		protected override void SetupPeripherals()
		{
			portD = new AvrIoPort (Cpu, AvrIoPort.PortDConfig);
		}

		[Test (Description = "Should generate INT0 interrupt on rising edge")]
		public void Int0RisingEdge ()
		{
			Cpu.WriteData (EIMSK, 1 << INT0);
			Cpu.WriteData (EICRA, (1 << ISC01) | (1 << ISC00));
			
			Assert.That (Cpu.Mmio.Data[EIFR], Is.EqualTo (0));
			portD.SetPinValue (PD2, true);
			Assert.That (Cpu.Mmio.Data[EIFR], Is.EqualTo (1 << INT0));
			
			Cpu.Mmio.Data[SREG] = 0x80; // SREG: I------- (enable interrupts)
			Cpu.Tick ();
			
			Assert.Multiple (() => {
				Assert.That (Cpu.Pc, Is.EqualTo (PC_INT_INT0));
				Assert.That (Cpu.Cycles, Is.EqualTo (3)); // 3 cycles from DoAvrInterrupt (4 total incl. instruction)
				Assert.That (Cpu.Mmio.Data[EIFR], Is.EqualTo (0));
			});
			
			portD.SetPinValue (PD2, false);
			Assert.That (Cpu.Mmio.Data[EIFR], Is.EqualTo (0));
		}

		[Test (Description = "Should generate INT0 interrupt on falling edge")]
		public void Int0FallingEdge ()
		{
			Cpu.WriteData (EIMSK, 1 << INT0);
			Cpu.WriteData (EICRA, 1 << ISC01);
			
			Assert.That (Cpu.Mmio.Data[EIFR], Is.EqualTo (0));
			portD.SetPinValue (PD2, true);
			Assert.That (Cpu.Mmio.Data[EIFR], Is.EqualTo (0));
			portD.SetPinValue (PD2, false);
			Assert.That (Cpu.Mmio.Data[EIFR], Is.EqualTo (1 << INT0));
			
			Cpu.Mmio.Data[SREG] = 0x80; // SREG: I------- (enable interrupts)
			Cpu.Tick ();
			
			Assert.Multiple (() => {
				Assert.That (Cpu.Pc, Is.EqualTo (PC_INT_INT0));
				Assert.That (Cpu.Cycles, Is.EqualTo (3)); // 3 cycles from DoAvrInterrupt (4 total incl. instruction)
				Assert.That (Cpu.Mmio.Data[EIFR], Is.EqualTo (0));
			});
		}
		
		[Test (Description = "Should generate INT0 interrupt on level change")]
		public void Int0LevelChange ()
		{
			Cpu.WriteData (EIMSK, 1 << INT0);
			Cpu.WriteData (EICRA, 1 << ISC00);
			
			Assert.That (Cpu.Mmio.Data[EIFR], Is.EqualTo (0));
			portD.SetPinValue (PD2, true);
			Assert.That (Cpu.Mmio.Data[EIFR], Is.EqualTo (1 << INT0));
			Cpu.WriteData (EIFR, 1 << INT0);
			Assert.That (Cpu.Mmio.Data[EIFR], Is.EqualTo (0));
			portD.SetPinValue (PD2, false);
			Assert.That (Cpu.Mmio.Data[EIFR], Is.EqualTo (1 << INT0));
		}
		
		[Test (Description = "Should a sticky INT0 interrupt while the pin level is low")]
		public void Int0StickyLow ()
		{
			Cpu.WriteData (EIMSK, 1 << INT0);
			Cpu.WriteData (EICRA, 0);
			
			Assert.That (Cpu.Mmio.Data[EIFR], Is.EqualTo (0));
			
			portD.SetPinValue (PD2, true);
			Assert.That (Cpu.Mmio.Data[EIFR], Is.EqualTo (0));
			
			portD.SetPinValue (PD2, false);
			Assert.That (Cpu.Mmio.Data[EIFR], Is.EqualTo (1 << INT0));
			
			// This is a sticky interrupt, verify we can't clear the flag:
			Cpu.WriteData (EIFR, 1 << INT0);
			Assert.That (Cpu.Mmio.Data[EIFR], Is.EqualTo (1 << INT0));
			
			Cpu.Mmio.Data[SREG] = 0x80; // SREG: I------- (enable interrupts)
			Cpu.Tick ();
			Assert.Multiple (() => {
				Assert.That (Cpu.Pc, Is.EqualTo (PC_INT_INT0));
				Assert.That (Cpu.Cycles, Is.EqualTo (3)); // 3 cycles from DoAvrInterrupt (4 total incl. instruction)
			});
			
			// Flag shouldn't be cleared, as the interrupt is sticky
			Assert.That (Cpu.Mmio.Data[EIFR], Is.EqualTo (1 << INT0));
			
			// But it will be cleared as soon as the pin goes high.
			portD.SetPinValue (PD2, true);
			Assert.That (Cpu.Mmio.Data[EIFR], Is.EqualTo (0));
		}
	}

	[TestFixture]
	public class PinChangeInterrupts : AvrTestBase
	{
		private AvrIoPort portB;

		protected override void SetupPeripherals()
		{
			portB = new AvrIoPort (Cpu, AvrIoPort.PortBConfig);
		}

		[Test (Description = "Should generate a pin change interrupt when PB3 (PCINT3) goes high")]
		public void PinChangeHigh ()
		{
			Cpu.WriteData (PCICR, 1 << PCIE0);
			Cpu.WriteData (PCMSK0, 1 << PCINT3);
			
			portB.SetPinValue (PB3, true);
			Assert.That (Cpu.Mmio.Data[PCIFR], Is.EqualTo (1 << PCIE0));
			
			Cpu.Mmio.Data[SREG] = 0x80; // SREG: I------- (enable interrupts)
			Cpu.Tick ();
			
			Assert.Multiple (() => {
				Assert.That (Cpu.Pc, Is.EqualTo (PC_INT_PCINT0));
				Assert.That (Cpu.Cycles, Is.EqualTo(3)); // 3 cycles from DoAvrInterrupt (4 total incl. instruction)
				Assert.That (Cpu.Mmio.Data[PCIFR], Is.EqualTo (0));
			});
		}
		
		[Test (Description = "Should generate a pin change interrupt when PB3 (PCINT3) goes low")]
		public void PinChangeLow ()
		{
			portB.SetPinValue (PB3, true);
			Cpu.WriteData (PCICR, 1 << PCIE0);
			Cpu.WriteData (PCMSK0, 1 << PCINT3);
			Assert.That (Cpu.Mmio.Data[PCIFR], Is.EqualTo (0));
			
			portB.SetPinValue (PB3, false);
			Assert.That (Cpu.Mmio.Data[PCIFR], Is.EqualTo (1 << PCIE0));
			
			Cpu.Mmio.Data[SREG] = 0x80; // SREG: I------- (enable interrupts)
			Cpu.Tick ();
			
			Assert.Multiple (() => {
				Assert.That (Cpu.Pc, Is.EqualTo (PC_INT_PCINT0));
				Assert.That (Cpu.Cycles, Is.EqualTo(3)); // 3 cycles from DoAvrInterrupt (4 total incl. instruction)
				Assert.That (Cpu.Mmio.Data[PCIFR], Is.EqualTo (0));
			});
		}
		
		[Test (Description = "Should clear the interrupt flag when writing to PCIFR")]
		public void ClearFlag ()
		{
			Cpu.WriteData (PCICR, 1 << PCIE0);
			Cpu.WriteData (PCMSK0, 1 << PCINT3);

			portB.SetPinValue (PB3, true);
			Assert.That (Cpu.Mmio.Data[PCIFR], Is.EqualTo (1 << PCIE0));

			Cpu.WriteData (PCIFR, 1 << PCIE0);
			Assert.That (Cpu.Mmio.Data[PCIFR], Is.EqualTo (0));
		}
	}

	[TestFixture]
	public class PcmskIsolation : AvrTestBase
	{
		private AvrIoPort portB;
		private AvrIoPort portC;

		protected override void SetupPeripherals()
		{
			portB = new AvrIoPort (Cpu, AvrIoPort.PortBConfig);
			portC = new AvrIoPort (Cpu, AvrIoPort.PortCConfig);
		}

		// PCMSK addresses
		const int PCMSK0 = 0x6b; // Port B  (PCINT0 group)
		const int PCMSK1 = 0x6c; // Port C  (PCINT1 group)
		const int PCICR  = 0x68;
		const int PCIFR  = 0x3b;

		[Test (Description = "Writing PCMSK0 must not affect PortC PCINT1 interrupt enable state")]
		public void WritePcmsk0_DoesNotAffectPortC ()
		{
			// Regression for bug: PCMSK write previously called UpdateInterruptEnable on ALL
			// GPIO ports using the PCMSK VALUE (not PCICR), which could erroneously clear
			// or queue other port groups' interrupts.

			// Enable both PCINT groups in PCICR
			Cpu.WriteData (PCICR, 0x03); // PCIE0 | PCIE1

			// Manually raise the PCINT1 (Port C) flag in PCIFR so the interrupt is pending
			Cpu.Mmio.Data[PCIFR] = 0x02; // PCINT1 flag bit

			// Now write PCMSK0 — old code passed this VALUE (0x08) to UpdateInterruptEnable for
			// ALL ports; since 0x08 & PCIE1_mask=2 = 0, it would ClearInterrupt(portC_pcint).
			Cpu.WriteData (PCMSK0, 1 << 3); // PCINT3

			// PCIFR bit for PCINT1 must still be set — writing PCMSK0 must not touch PCINT1
			Assert.That (Cpu.Mmio.Data[PCIFR] & 0x02, Is.EqualTo(0x02),
				"Writing PCMSK0 must not clear the PCINT1 flag in PCIFR");
		}

		[Test (Description = "Writing PCMSK0 only re-evaluates Port B interrupt — Port C PCINT1 state unchanged")]
		public void WritePcmsk0_PortB_InterruptStaysArmed ()
		{
			// Enable PCINT1 group
			Cpu.WriteData (PCICR, 0x02); // PCIE1

			// Trigger a Port C pin change to arm the PCINT1 interrupt
			Cpu.WriteData (PCMSK1, 0x01); // PCINT8
			portC.SetPinValue (0, true);
			Assert.That (Cpu.Mmio.Data[PCIFR] & 0x02, Is.EqualTo(0x02), "PCINT1 should be pending");

			// Write PCMSK0 (Port B mask) — must not affect PCINT1
			Cpu.WriteData (PCMSK0, 0xFF);

			// PCINT1 must still be pending
			Assert.That (Cpu.Mmio.Data[PCIFR] & 0x02, Is.EqualTo(0x02),
				"PCINT1 must remain armed after unrelated PCMSK0 write");
		}
	}
}
