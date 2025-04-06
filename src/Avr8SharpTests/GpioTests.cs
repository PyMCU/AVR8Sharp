using AVR8Sharp.Peripherals;
namespace AVr8SharpTests;

[TestFixture]
public class Gpio
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

	[Test (Description = "Should invoke the listeners when the port is written to")]
	public void PortWrite ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var port = new AvrIoPort (cpu, AvrIoPort.PortBConfig);

		cpu.WriteData (DDRB, 0x0f);

		port.AddListener ((value, oldValue) => {
			Assert.That (value, Is.EqualTo (0x55));
		});

		cpu.WriteData (PORTB, 0x55);

		Assert.That (cpu.Data[0x23], Is.EqualTo (0x5));
	}

	[Test (Description = "Should invoke the listeners when DDR changes (issue #28)")]
	public void DdrWrite ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var port = new AvrIoPort (cpu, AvrIoPort.PortBConfig);

		var counter = 0;

		cpu.WriteData (PORTB, 0x55);

		port.AddListener ((value, oldValue) => {
			Assert.That (value, Is.EqualTo (0x55));
		});

		cpu.WriteData (DDRB, 0xf0);
	}

	[Test (Description = "Should invoke the listeners when pullup register enabled (issue #62)")]
	public void Pullup ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var port = new AvrIoPort (cpu, AvrIoPort.PortBConfig);

		var counter = 0;

		port.AddListener ((value, oldValue) => {
			Assert.That (value, counter == 0 ? Is.EqualTo (0x55) : Is.EqualTo (0));
			counter++;
		});

		cpu.WriteData (PORTB, 0x55);
	}

	[Test (Description = "Should toggle the pin when writing to the PIN register")]
	public void PinToggle ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var port = new AvrIoPort (cpu, AvrIoPort.PortBConfig);

		var calledCorrectly = false;

		port.AddListener ((value, oldValue) => {
			calledCorrectly |= value == 0x54 && oldValue == 0x55;
		});

		cpu.WriteData (DDRB, 0x0f);
		cpu.WriteData (PORTB, 0x55);
		cpu.WriteData (PINB, 0x01);

        Assert.Multiple(() =>
        {
            Assert.That(cpu.Data[PINB], Is.EqualTo(0x4));
            Assert.That(calledCorrectly, Is.True);
        });
    }

	[Test (Description = "Should only affect one pin when writing to PIN using SBI (issue #103)")]
	public void PinToggleSbi ()
	{

		var program = Utils.AsmProgram (@$"
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
");

		var cpu = new AVR8Sharp.Cpu.Cpu (program.Program);
		var portD = new AvrIoPort (cpu, AvrIoPort.PortDConfig);
		var runner = new TestProgramRunner (cpu);

		var calledCorrectly = false;

		portD.AddListener ((value, oldValue) => {
			calledCorrectly |= value == 0x48 && oldValue == 0;
		});

		runner.RunInstructions (3);
		Assert.That (cpu.Data[PORTD], Is.EqualTo (0x48));

		var calledCorrectly2 = false;
		portD.AddListener ((value, oldValue) => {
			calledCorrectly2 |= value == 0x08 && oldValue == 0x48;
		});

		runner.RunInstructions (1);
        Assert.Multiple(() =>
        {
            Assert.That(cpu.Data[PORTD], Is.EqualTo(0x8));
            Assert.That(calledCorrectly, Is.True);
            Assert.That(calledCorrectly2, Is.True);
        });
    }

	[Test (Description = "Should update the PIN register on output compare (OCR) match (issue #102)")]
	public void PinToggleOcrMatch ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var port = new AvrIoPort (cpu, AvrIoPort.PortBConfig);

		cpu.WriteData (DDRB, 1 << 1);

		port.TimerOverridePin (1, PinOverrideMode.Set);
		Assert.Multiple (() => {
			Assert.That (port.GetPinState (1), Is.EqualTo (AVR8Sharp.Peripherals.PinState.High));
			Assert.That (cpu.Data[PINB], Is.EqualTo (1 << 1));
		});

		port.TimerOverridePin (1, PinOverrideMode.Clear);
		Assert.Multiple (() => {
			Assert.That (port.GetPinState (1), Is.EqualTo (AVR8Sharp.Peripherals.PinState.Low));
			Assert.That (cpu.Data[PINB], Is.EqualTo (0));
		});
	}

	[Test (Description = "Should remove the given listener")]
	public void RemoveListener ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var port = new AvrIoPort (cpu, AvrIoPort.PortBConfig);

		var counter = 0;

		var listener = new System.Action<byte, byte> ((_, _) => {
			counter++;
		});

		port.AddListener (listener);

		cpu.WriteData (DDRB, 0x0f);

		port.RemoveListener (listener);

		cpu.WriteData (PORTB, 0x99);

		Assert.That (counter, Is.EqualTo (1));
	}

	[TestFixture]
	public class PinState
	{
		[Test (Description = "Should return PinState.High when the pin set to output and HIGH")]
		public void PinStateHigh ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var port = new AvrIoPort (cpu, AvrIoPort.PortBConfig);

			cpu.WriteData (DDRB, 0x1);
			cpu.WriteData (PORTB, 0x1);

			Assert.That (port.GetPinState (PB0), Is.EqualTo (AVR8Sharp.Peripherals.PinState.High));
		}

		[Test (Description = "Should return PinState.Low when the pin set to output and LOW")]
		public void PinStateLow ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var port = new AvrIoPort (cpu, AvrIoPort.PortBConfig);

			cpu.WriteData (DDRB, 0x8);
			cpu.WriteData (PORTB, 0xf7);

			Assert.That (port.GetPinState (PB3), Is.EqualTo (AVR8Sharp.Peripherals.PinState.Low));
		}

		[Test (Description = "Should return PinState.Input by default (reset state)")]
		public void PinStateInput ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var port = new AvrIoPort (cpu, AvrIoPort.PortBConfig);

			Assert.That (port.GetPinState (PB1), Is.EqualTo (AVR8Sharp.Peripherals.PinState.Input));
		}

		[Test (Description = "Should return PinState.InputPullUp when the pin is set to input with pullup")]
		public void PinStateInputPullUp ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var port = new AvrIoPort (cpu, AvrIoPort.PortBConfig);

			cpu.WriteData (DDRB, 0);
			cpu.WriteData (PORTB, 0x2);

			Assert.That (port.GetPinState (PB1), Is.EqualTo (AVR8Sharp.Peripherals.PinState.InputPullup));
		}

		[Test (Description = "Should reflect the current port state when called inside a listener")]
		public void GetPinStateInListener ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var port = new AvrIoPort (cpu, AvrIoPort.PortBConfig);

			var listener = new System.Action<byte, byte> ((value, oldValue) => {
				Assert.That (port.GetPinState (PB0), Is.EqualTo (AVR8Sharp.Peripherals.PinState.High));
			});

			Assert.That (port.GetPinState (PB0), Is.EqualTo (AVR8Sharp.Peripherals.PinState.Input));
			cpu.WriteData (DDRB, 0x01);
			port.AddListener (listener);
			cpu.WriteData (PORTB, 0x01);
		}

		[Test (Description = "Should reflect the current port state when called inside a listener after DDR change")]
		public void GetPinStateInListenerAfterDdrChange ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var port = new AvrIoPort (cpu, AvrIoPort.PortBConfig);

			var listener = new System.Action<byte, byte> ((_, _) => {
				Assert.That (port.GetPinState (PB0), Is.EqualTo (AVR8Sharp.Peripherals.PinState.Low));
			});

			Assert.That (port.GetPinState (PB0), Is.EqualTo (AVR8Sharp.Peripherals.PinState.Input));
			port.AddListener (listener);
			cpu.WriteData (DDRB, 0x01);
		}
	}

	[TestFixture]
	public class SetPin
	{
		[Test (Description = "Should set the value of the given pin")]
		public void SetPinValue ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var port = new AvrIoPort (cpu, AvrIoPort.PortBConfig);

			cpu.WriteData(DDRB, 0);
			port.SetPinValue (PB4, true);
			Assert.That (cpu.Data[0x23], Is.EqualTo (0x10));

			port.SetPinValue (PB4, false);
			Assert.That (cpu.Data[0x23], Is.EqualTo (0));
		}

		[Test (Description = "Should only update PIN register when pin in Input mode")]
		public void SetPinValueInput ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var port = new AvrIoPort (cpu, AvrIoPort.PortBConfig);

			cpu.WriteData (DDRB, 0x10);
			cpu.WriteData (PORTB, 0x0);

			port.SetPinValue (PB4, true);

			Assert.That (cpu.Data[PINB], Is.EqualTo (0x0));

			cpu.WriteData (DDRB, 0x0);

			Assert.That (cpu.Data[PINB], Is.EqualTo (0x10));
		}
	}

	[TestFixture]
	public class ExternalInterrupts
	{
		[Test (Description = "Should generate INT0 interrupt on rising edge")]
		public void Int0RisingEdge ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var port = new AvrIoPort (cpu, AvrIoPort.PortDConfig);
			
			cpu.WriteData (EIMSK, 1 << INT0);
			cpu.WriteData (EICRA, (1 << ISC01) | (1 << ISC00));
			
			Assert.That (cpu.Data[EIFR], Is.EqualTo (0));
			port.SetPinValue (PD2, true);
			Assert.That (cpu.Data[EIFR], Is.EqualTo (1 << INT0));
			
			cpu.Data[SREG] = 0x80; // SREG: I------- (enable interrupts)
			cpu.Tick ();
			
			Assert.Multiple (() => {
				Assert.That (cpu.PC, Is.EqualTo (PC_INT_INT0));
				Assert.That (cpu.Cycles, Is.EqualTo (2));
				Assert.That (cpu.Data[EIFR], Is.EqualTo (0));
			});
			
			port.SetPinValue (PD2, false);
			Assert.That (cpu.Data[EIFR], Is.EqualTo (0));
		}

		[Test (Description = "Should generate INT0 interrupt on falling edge")]
		public void Int0FallingEdge ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var port = new AvrIoPort (cpu, AvrIoPort.PortDConfig);

			cpu.WriteData (EIMSK, 1 << INT0);
			cpu.WriteData (EICRA, 1 << ISC01);
			
			Assert.That (cpu.Data[EIFR], Is.EqualTo (0));
			port.SetPinValue (PD2, true);
			Assert.That (cpu.Data[EIFR], Is.EqualTo (0));
			port.SetPinValue (PD2, false);
			Assert.That (cpu.Data[EIFR], Is.EqualTo (1 << INT0));
			
			cpu.Data[SREG] = 0x80; // SREG: I------- (enable interrupts)
			cpu.Tick ();
			
			Assert.Multiple (() => {
				Assert.That (cpu.PC, Is.EqualTo (PC_INT_INT0));
				Assert.That (cpu.Cycles, Is.EqualTo (2));
				Assert.That (cpu.Data[EIFR], Is.EqualTo (0));
			});
		}
		
		[Test (Description = "Should generate INT0 interrupt on level change")]
		public void Int0LevelChange ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var port = new AvrIoPort (cpu, AvrIoPort.PortDConfig);

			cpu.WriteData (EIMSK, 1 << INT0);
			cpu.WriteData (EICRA, 1 << ISC00);
			
			Assert.That (cpu.Data[EIFR], Is.EqualTo (0));
			port.SetPinValue (PD2, true);
			Assert.That (cpu.Data[EIFR], Is.EqualTo (1 << INT0));
			cpu.WriteData (EIFR, 1 << INT0);
			Assert.That (cpu.Data[EIFR], Is.EqualTo (0));
			port.SetPinValue (PD2, false);
			Assert.That (cpu.Data[EIFR], Is.EqualTo (1 << INT0));
		}
		
		[Test (Description = "Should a sticky INT0 interrupt while the pin level is low")]
		public void Int0StickyLow ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var port = new AvrIoPort (cpu, AvrIoPort.PortDConfig);
			
			cpu.WriteData (EIMSK, 1 << INT0);
			cpu.WriteData (EICRA, 0);
			
			Assert.That (cpu.Data[EIFR], Is.EqualTo (0));
			
			port.SetPinValue (PD2, true);
			Assert.That (cpu.Data[EIFR], Is.EqualTo (0));
			
			port.SetPinValue (PD2, false);
			Assert.That (cpu.Data[EIFR], Is.EqualTo (1 << INT0));
			
			// This is a sticky interrupt, verify we can't clear the flag:
			cpu.WriteData (EIFR, 1 << INT0);
			Assert.That (cpu.Data[EIFR], Is.EqualTo (1 << INT0));
			
			cpu.Data[SREG] = 0x80; // SREG: I------- (enable interrupts)
			cpu.Tick ();
			Assert.Multiple (() => {
				Assert.That (cpu.PC, Is.EqualTo (PC_INT_INT0));
				Assert.That (cpu.Cycles, Is.EqualTo (2));
			});
			
			// Flag shouldn't be cleared, as the interrupt is sticky
			Assert.That (cpu.Data[EIFR], Is.EqualTo (1 << INT0));
			
			// But it will be cleared as soon as the pin goes high.
			port.SetPinValue (PD2, true);
			Assert.That (cpu.Data[EIFR], Is.EqualTo (0));
		}
	}

	[TestFixture]
	public class PinChangeInterrupts
	{
		[Test (Description = "Should generate a pin change interrupt when PB3 (PCINT3) goes high")]
		public void PinChangeHigh ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var port = new AvrIoPort (cpu, AvrIoPort.PortBConfig);
			
			cpu.WriteData (PCICR, 1 << PCIE0);
			cpu.WriteData (PCMSK0, 1 << PCINT3);
			
			port.SetPinValue (PB3, true);
			Assert.That (cpu.Data[PCIFR], Is.EqualTo (1 << PCIE0));
			
			cpu.Data[SREG] = 0x80; // SREG: I------- (enable interrupts)
			cpu.Tick ();
			
			Assert.Multiple (() => {
				Assert.That (cpu.PC, Is.EqualTo (PC_INT_PCINT0));
				Assert.That (cpu.Cycles, Is.EqualTo(2));
				Assert.That (cpu.Data[PCIFR], Is.EqualTo (0));
			});
		}
		
		[Test (Description = "Should generate a pin change interrupt when PB3 (PCINT3) goes low")]
		public void PinChangeLow ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var port = new AvrIoPort (cpu, AvrIoPort.PortBConfig);
			
			port.SetPinValue (PB3, true);
			cpu.WriteData (PCICR, 1 << PCIE0);
			cpu.WriteData (PCMSK0, 1 << PCINT3);
			Assert.That (cpu.Data[PCIFR], Is.EqualTo (0));
			
			port.SetPinValue (PB3, false);
			Assert.That (cpu.Data[PCIFR], Is.EqualTo (1 << PCIE0));
			
			cpu.Data[SREG] = 0x80; // SREG: I------- (enable interrupts)
			cpu.Tick ();
			
			Assert.Multiple (() => {
				Assert.That (cpu.PC, Is.EqualTo (PC_INT_PCINT0));
				Assert.That (cpu.Cycles, Is.EqualTo(2));
				Assert.That (cpu.Data[PCIFR], Is.EqualTo (0));
			});
		}
		
		[Test (Description = "Should clear the interrupt flag when writing to PCIFR")]
		public void ClearFlag ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var port = new AvrIoPort (cpu, AvrIoPort.PortBConfig);
			
			cpu.WriteData (PCICR, 1 << PCIE0);
			cpu.WriteData (PCMSK0, 1 << PCINT3);
			
			port.SetPinValue (PB3, true);
			Assert.That (cpu.Data[PCIFR], Is.EqualTo (1 << PCIE0));
			
			cpu.WriteData (PCIFR, 1 << PCIE0);
			Assert.That (cpu.Data[PCIFR], Is.EqualTo (0));
		}
	}
}
