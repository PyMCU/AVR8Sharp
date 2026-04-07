using AVR8Sharp.Core.Peripherals;
namespace Avr8Sharp.Tests;

[TestFixture]
public class Timer
{
	// CPU registers
	const int R1 = 1;
	const int R17 = 17;
	const int R18 = 18;
	const int R19 = 19;
	const int R20 = 20;
	const int R21 = 21;
	const int R22 = 22;
	const int SREG = 95;

	// Timer 0 Registers
	const int TIFR0 = 0x35;
	const int TCCR0A = 0x44;
	const int TCCR0B = 0x45;
	const int TCNT0 = 0x46;
	const int OCR0A = 0x47;
	const int OCR0B = 0x48;
	const int TIMSK0 = 0x6e;
	const int TIMSK1 = 0x6f;

	// Timer 1 Registers
	const int TIFR1 = 0x36;
	const int TCCR1A = 0x80;
	const int TCCR1B = 0x81;
	const int TCCR1C = 0x82;
	const int TCNT1 = 0x84;
	const int TCNT1H = 0x85;
	const int ICR1 = 0x86;
	const int ICR1H = 0x87;
	const int OCR1A = 0x88;
	const int OCR1AH = 0x89;
	const int OCR1B = 0x8a;
	const int OCR1C = 0x8c;
	const int OCR1CH = 0x8d;

	// Timer 2 Registers
	const int TCCR2B = 0xb1;
	const int TCNT2 = 0xb2;

	// Register bit names
	const int TOV0 = 1;
	const int TOV1 = 1;
	const int OCIE0A = 2;
	const int OCIE0B = 4;
	const int TOIE0 = 1;
	const int OCF0A = 2;
	const int OCF0B = 4;
	const int OCF1A = 1 << 1;
	const int OCF1B = 1 << 2;
	const int OCF1C = 1 << 3;
	const int WGM00 = 1;
	const int WGM10 = 1;
	const int WGM01 = 2;
	const int WGM11 = 2;
	const int WGM12 = 8;
	const int WGM13 = 16;
	const int CS00 = 1;
	const int CS01 = 2;
	const int CS02 = 4;
	const int CS10 = 1;
	const int CS21 = 2;
	const int CS22 = 4;
	const int COM0B1 = 1 << 5;
	const int COM1C0 = 1 << 2;
	const int FOC0B = 1 << 6;
	const int FOC1C = 1 << 5;

	const int T0 = 4; // PD4 on ATmega328p

	// opcodes
	const int nopOpCode = 0;
	
	[Test (Description = "Should update timer every tick when prescaler is 1")]
	public void Timer0Prescaler1 ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		cpu.Cycles = 1;
		cpu.Tick();
		cpu.Cycles = 2;
		cpu.Tick();
		var tcnt = cpu.ReadData(TCNT0);
		Assert.That(tcnt, Is.EqualTo(1));
	}
	
	[Test (Description = "Should update timer every 64 ticks when prescaler is 3")]
	public void Timer0Prescaler3 ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCCR0B, CS01 | CS00); // Set prescaler to 64
		cpu.Cycles = 1;
		cpu.Tick();
		cpu.Cycles = 1 + 64;
		cpu.Tick();
		var tcnt = cpu.ReadData(TCNT0);
		Assert.That(tcnt, Is.EqualTo(1));
	}
	
	[Test (Description = "Should not update timer if it has been disabled")]
	public void Timer0Disabled ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCCR0B, 0); // No prescaler (disabled)
		cpu.Cycles = 1;
		cpu.Tick();
		cpu.Cycles = 100000;
		cpu.Tick();
		var tcnt = cpu.ReadData(TCNT0);
		Assert.That(tcnt, Is.EqualTo(0)); // TCNT should stay 0
	}
	
	[Test (Description = "Should set the TOV flag when timer wraps above TOP value")]
	public void Timer0Overflow ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCNT0, 0xff);
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		
		cpu.Cycles = 1;
		cpu.Tick();
        Assert.Multiple(() =>
        {
            Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(0xff));
            Assert.That(cpu.ReadData(TIFR0) & TOV0, Is.Zero);
        });

        cpu.Cycles++;
		cpu.Tick();
		
		Assert.Multiple(() =>
		{
			Assert.That(cpu.ReadData(TCNT0), Is.Zero);
			Assert.That(cpu.ReadData(TIFR0) & TOV0, Is.EqualTo(TOV0));
		});
	}
	
	[Test (Description = "Should set the TOV if timer overflows past TOP without reaching TOP")]
	public void Timer0Overflow2 ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCNT0, 0xfe);
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		
		cpu.Cycles = 1;
		cpu.Tick();
		Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(0xfe));
		cpu.Cycles += 4;
		cpu.Tick();
		Assert.Multiple(() =>
		{
			Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(0x2));
			Assert.That(cpu.ReadData(TIFR0) & TOV0, Is.EqualTo(TOV0));
		});
	}
	
	[Test (Description = "Should clear the TOV flag when writing 1 to the TOV bit, and not trigger the interrupt")]
	public void Timer0ClearOverflow ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCNT0, 0xff);
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		
		cpu.Cycles = 1;
		cpu.Tick();
		cpu.Cycles = 2;
		cpu.Tick();
		Assert.That(cpu.ReadData(TIFR0) & TOV0, Is.EqualTo(TOV0));
		cpu.WriteData(TIFR0, TOV0);
		Assert.That(cpu.ReadData(TIFR0) & TOV0, Is.Zero);
	}
	
	[Test (Description = "Should set TOV if timer overflows in FAST PWM mode")]
	public void Timer0FastPwmOverflow ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCNT0, 0xff);
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		cpu.Cycles = 1;
		cpu.Tick();
		cpu.WriteData(OCR0A, 0x7f);
		cpu.WriteData(TCCR0A, WGM01 | WGM00); // WGM: Fast PWM
		cpu.Cycles = 2;
		cpu.Tick();
		var tcnt = cpu.ReadData(TCNT0);
		Assert.That(tcnt, Is.Zero);
		Assert.That(cpu.ReadData(TIFR0) & TOV0, Is.EqualTo(TOV0));
	}
	
	[Test (Description = "Should generate an overflow interrupt if timer overflows and interrupts enabled")]
	public void Timer0OverflowInterrupt ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCNT0, 0xff);
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		cpu.Cycles = 1;
		cpu.Tick();
		cpu.WriteData(TIMSK0, TOIE0);
		cpu.WriteData(SREG, 0x80); // SREG: I-------
		cpu.Cycles = 2;
		cpu.Tick();
		var tcnt = cpu.ReadData(TCNT0);
        Assert.Multiple(() =>
        {
            Assert.That(tcnt, Is.EqualTo(3)); // TCNT = 3 (one tick + 3 interrupt-dispatch cycles, AVR spec)
            Assert.That(cpu.ReadData(TIFR0) & TOV0, Is.Zero);
            Assert.That(cpu.Pc, Is.EqualTo(0x20));
            Assert.That(cpu.Cycles, Is.EqualTo(5)); // cycles: 2 (tick) + 3 (DoAvrInterrupt) = 5
        });
    }
	
	[Test (Description = "Should support overriding TIFR/TOV and TIMSK/TOIE bits (issue #64)")]
	public void Timer0Override ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		
		// The following values correspond ATtiny85 config:
		var newConfiguration = AvrTimer.Timer0Config.CreateNew (tov: 2, ocfa: 2, ocfb: 8, toie: 2, ociea: 16, ocieb: 8);
		var timer = new AvrTimer (cpu, newConfiguration);
		
		cpu.WriteData(TCNT0, 0xff);
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		cpu.Cycles = 1;
		cpu.Tick();
		cpu.WriteData(TIMSK0, 2);
		cpu.WriteData(SREG, 0x80); // SREG: I-------
		cpu.Cycles = 2;
		cpu.Tick();
		var tcnt = cpu.ReadData(TCNT0);
		Assert.Multiple(() =>
		{
			Assert.That(tcnt, Is.EqualTo(3)); // TCNT = 3 (one tick + 3 interrupt-dispatch cycles, AVR spec)
			Assert.That(cpu.ReadData(TIFR0) & 2, Is.Zero);
			Assert.That(cpu.Pc, Is.EqualTo(0x20));
			Assert.That(cpu.Cycles, Is.EqualTo(5)); // cycles: 2 (tick) + 3 (DoAvrInterrupt) = 5
		});
	}
	
	[Test (Description = "Should not generate an overflow interrupt when global interrupts disabled")]
	public void Timer0OverflowInterruptDisabled ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCNT0, 0xff);
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		cpu.Cycles = 1;
		cpu.Tick();
		cpu.Mmio.Data[TIMSK0] = TOIE0;
		cpu.Mmio.Data[SREG] = 0x0; // SREG: --------
		cpu.Cycles = 2;
		cpu.Tick();
		Assert.Multiple(() =>
		{
			Assert.That(cpu.ReadData(TIFR0) & TOV0, Is.EqualTo(TOV0));
			Assert.That(cpu.Pc, Is.Zero);
			Assert.That(cpu.Cycles, Is.EqualTo(2));
		});
	}
	
	[Test (Description = "Should not generate an overflow interrupt when TOIE0 is clear")]
	public void Timer0OverflowInterruptDisabled2 ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCNT0, 0xff);
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		cpu.Cycles = 1;
		cpu.Tick();
		cpu.Mmio.Data[TIMSK0] = 0;
		cpu.Mmio.Data[SREG] = 0x80; // SREG: I-------
		cpu.Cycles = 2;
		cpu.Tick();
		Assert.Multiple(() =>
		{
			Assert.That(cpu.Mmio.Data[TIFR0] & TOV0, Is.EqualTo(TOV0));
			Assert.That(cpu.Pc, Is.Zero);
			Assert.That(cpu.Cycles, Is.EqualTo(2));
		});
	}
	
	[Test (Description = "Should set OCF0A/B flags when OCRA/B == 0 and the timer equals to OCRA (issue #74)")]
	public void Timer0OutputCompareMatch ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCNT0, 0xff);
		cpu.WriteData(OCR0A, 0x0);
		cpu.WriteData(OCR0B, 0x0);
		cpu.WriteData(TCCR0A, 0x0); // WGM: Normal
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		cpu.Cycles = 1;
		cpu.Tick();
		cpu.Cycles = 2;
		cpu.Tick();
		Assert.Multiple(() =>
		{
			Assert.That(cpu.ReadData (TCNT0), Is.Zero);
			Assert.That(cpu.Mmio.Data[TIFR0] & (OCF0A | OCF0B), Is.EqualTo(OCF0A | OCF0B));
			Assert.That(cpu.Pc, Is.Zero);
			Assert.That(cpu.Cycles, Is.EqualTo(2));
		});
	}
	
	[Test (Description = "Should set the OCF1A flag when OCR1A == 120 and the timer overflowed past 120 in WGM mode 15 (issue #94)")]
	public void Timer1OutputCompareMatch ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer1Config);
		
		cpu.WriteData(TCNT1, 118);
		cpu.WriteData(OCR1A, 120);
		cpu.WriteData(OCR1C, 4); // To avoid getting the OCF1B flag set
		cpu.WriteData(TCCR1A, WGM10 | WGM11); // WGM: Fast PWM	
		cpu.WriteData(TCCR1B, WGM12 | WGM13 | CS10); // Set prescaler to 1
		cpu.Cycles = 1;
		cpu.Tick();
		cpu.Cycles = 5;
		cpu.Tick();
		Assert.Multiple(() =>
		{
			Assert.That(cpu.ReadData(TCNT1), Is.EqualTo(1));
			Assert.That(cpu.Mmio.Data[TIFR1] & (OCF1A | OCF1C), Is.EqualTo(OCF1A));
			Assert.That(cpu.Pc, Is.Zero);
			Assert.That(cpu.Cycles, Is.EqualTo(5));
		});
	}
	
	[Test (Description = "Should set OCF0A flag when timer equals OCRA")]
	public void Timer0OutputCompareMatch2 ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCNT0, 0x10);
		cpu.WriteData(OCR0A, 0x11);
		cpu.WriteData(TCCR0A, 0x0); // WGM: Normal
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		cpu.Cycles = 1;
		cpu.Tick();
		cpu.Cycles = 2;
		cpu.Tick();
		Assert.Multiple(() =>
		{
			Assert.That(cpu.ReadData(TIFR0), Is.EqualTo(OCF0A));
			Assert.That(cpu.Pc, Is.Zero);
			Assert.That(cpu.Cycles, Is.EqualTo(2));
		});
	}
	
	[Test (Description = "Should reset the counter in CTC mode if it equals to OCRA")]
	public void Timer0OutputCompareMatch3 ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCNT0, 0x10);
		cpu.WriteData(OCR0A, 0x11);
		cpu.WriteData(TCCR0A, WGM01); // WGM: CTC
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		cpu.Cycles = 1;
		cpu.Tick();
		cpu.Cycles = 3;
		cpu.Tick();
		Assert.Multiple(() =>
		{
			Assert.That(cpu.ReadData(TCNT0), Is.Zero);
			Assert.That(cpu.Pc, Is.Zero);
			Assert.That(cpu.Cycles, Is.EqualTo(3));
		});
	}
	
	[Test (Description = "Should not set the TOV bit when TOP < MAX in CTC mode (issue #75)")]
	public void Timer0OutputCompareMatch4 ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCNT0, 0x1e);
		cpu.WriteData(OCR0A, 0x1f);
		cpu.WriteData(TCCR0A, WGM01); // WGM: CTC
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		cpu.Cycles = 1;
		cpu.Tick();
		cpu.Cycles++;
		cpu.Tick();
		cpu.Cycles++;
		cpu.Tick();
		var tcnt = cpu.ReadData(TCNT0);
		Assert.Multiple(() =>
		{
			Assert.That(tcnt, Is.Zero);
			Assert.That(cpu.Mmio.Data[TIFR0] & TOV0, Is.Zero); // TOV0 clear
		});
	}
	
	[Test (Description = "Should set the TOV bit when TOP == MAX in CTC mode (issue #75)")]
	public void Timer0OutputCompareMatch5 ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCNT0, 0xfe);
		cpu.WriteData(OCR0A, 0xff);
		cpu.WriteData(TCCR0A, WGM01); // WGM: CTC
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		cpu.Cycles = 1;
		cpu.Tick();
		
		cpu.Cycles++;
		cpu.Tick();
		Assert.Multiple(() =>
		{
			Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(0xff));
			Assert.That(cpu.Mmio.Data[TIFR0] & TOV0, Is.Zero); // TOV clear
		});
		
		cpu.Cycles++;
		cpu.Tick();
		Assert.Multiple(() =>
		{
			Assert.That(cpu.ReadData(TCNT0), Is.Zero);
			Assert.That(cpu.Mmio.Data[TIFR0] & TOV0, Is.EqualTo(TOV0)); // TOV set
		});
	}
	
	[Test (Description = "Should not set the TOV bit twice on overflow (issue #80)")]
	public void Timer0OutputCompareMatch6 ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCNT0, 0xfe);
		cpu.WriteData(OCR0A, 0xff);
		cpu.WriteData(TCCR0A, WGM01); // WGM: CTC
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		
		cpu.Cycles = 1;
		cpu.Tick();
		
		cpu.Cycles++;
		cpu.Tick();
		Assert.Multiple(() =>
		{
			Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(0xff));
			Assert.That(cpu.Mmio.Data[TIFR0] & TOV0, Is.Zero); // TOV clear
		});
		
		cpu.Cycles++;
		cpu.Tick();
		Assert.Multiple(() =>
		{
			Assert.That(cpu.ReadData(TCNT0), Is.Zero);
			Assert.That(cpu.Mmio.Data[TIFR0] & TOV0, Is.EqualTo(TOV0)); // TOV set
		});
	}
	
	[Test (Description = "Should set OCF0B flag when timer equals OCRB")]
	public void Timer0OutputCompareMatch7 ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCNT0, 0x10);
		cpu.WriteData(OCR0B, 0x11);
		cpu.WriteData(TCCR0A, 0x0); // WGM: Normal
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		cpu.Cycles = 1;
		cpu.Tick();
		cpu.Cycles = 2;
		cpu.Tick();
		Assert.Multiple(() =>
		{
			Assert.That(cpu.ReadData(TIFR0), Is.EqualTo(OCF0B));
			Assert.That(cpu.Pc, Is.Zero);
			Assert.That(cpu.Cycles, Is.EqualTo(2));
		});
	}
	
	[Test (Description = "Should generate Timer Compare A interrupt when TCNT0 == TCNTA")]
	public void Timer0OutputCompareMatchInterruptA ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCNT0, 0x20);
		cpu.WriteData(OCR0A, 0x21);
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		cpu.WriteData(TIMSK0, OCIE0A);
		cpu.WriteData(SREG, 0x80); // SREG: I-------
		cpu.Cycles = 1;
		cpu.Tick();
		cpu.Cycles = 2;
		cpu.Tick();
		var tcnt = cpu.ReadData(TCNT0);
		Assert.Multiple(() =>
		{
			Assert.That(tcnt, Is.EqualTo(0x24)); // TCNT = 0x24 (one tick + 3 interrupt-dispatch cycles, AVR spec)
			Assert.That(cpu.ReadData(TIFR0) & OCF0A, Is.Zero);
			Assert.That(cpu.Pc, Is.EqualTo(0x1c));
			Assert.That(cpu.Cycles, Is.EqualTo(5)); // cycles: 2 (tick) + 3 (DoAvrInterrupt) = 5
		});
	}
	
	[Test (Description = "Should not generate Timer Compare A interrupt when OCIEA is disabled")]
	public void Timer0OutputCompareMatchInterruptA2 ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCNT0, 0x20);
		cpu.WriteData(OCR0A, 0x21);
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		cpu.WriteData(TIMSK0, 0);
		cpu.WriteData(SREG, 0x80); // SREG: I-------
		cpu.Cycles = 1;
		cpu.Tick();
		cpu.Cycles = 2;
		cpu.Tick();
		Assert.Multiple(() =>
		{
			Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(0x21));
			Assert.That(cpu.Pc, Is.Zero);
			Assert.That(cpu.Cycles, Is.EqualTo(2));
		});
	}
	
	[Test (Description = "Should generate Timer Compare B interrupt when TCNT0 == TCNTB")]
	public void Timer0OutputCompareMatchInterruptB ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCNT0, 0x20);
		cpu.WriteData(OCR0B, 0x21);
		cpu.WriteData(TCCR0B, CS00); // Set prescaler to 1
		cpu.WriteData(TIMSK0, OCIE0B);
		cpu.WriteData(SREG, 0x80); // SREG: I-------
		cpu.Cycles = 1;
		cpu.Tick();
		cpu.Cycles = 2;
		cpu.Tick();
		var tcnt = cpu.ReadData(TCNT0);
		Assert.Multiple(() =>
		{
			Assert.That(tcnt, Is.EqualTo(0x24)); // TCNT = 0x24 (one tick + 3 interrupt-dispatch cycles, AVR spec)
			Assert.That(cpu.ReadData(TIFR0) & OCF0B, Is.Zero);
			Assert.That(cpu.Pc, Is.EqualTo(0x1e));
			Assert.That(cpu.Cycles, Is.EqualTo(5)); // cycles: 2 (tick) + 3 (DoAvrInterrupt) = 5
		});
	}
	
	[Test (Description = "Should not increment TCNT on the same cycle of TCNT write (issue #36)")]
	public void Timer0WriteTcnt ()
	{
		var program = Utils.AsmProgram (@"
		LDI r16, 0x1    ; TCCR0B = 1 << CS00;
        OUT 0x25, r16
		LDI r16, 0x30   ; TCNT0 <- 0x30
		OUT 0x26, r16
		NOP
		IN r17, 0x26    ; r17 <- TCNT
");
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		var runner = new TestProgramRunner (cpu);
		
		runner.RunInstructions (program.InstructionCount);
		
		Assert.That(cpu.Mmio.Data[R17], Is.EqualTo(0x31));
	}
	
	[Test (Description = "Timer2 should count every 256 ticks when prescaler is 6 (issue #5)")]
	public void Timer2Prescaler6 ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer2Config);
		
		cpu.WriteData(TCCR2B, CS22 | CS21); // Set prescaler to 256
		cpu.Cycles = 1;
		cpu.Tick();
		
		cpu.Cycles = 1 + 511;
		cpu.Tick();
		Assert.That(cpu.ReadData(TCNT2), Is.EqualTo(1));
		
		cpu.Cycles = 1 + 512;
		cpu.Tick();
		Assert.That(cpu.ReadData(TCNT2), Is.EqualTo(2));
	}

	[Test (Description = "Should update TCNT as it is being read by a 2-cycle instruction (issue #40)")]
	public void Timer0ReadTcnt ()
	{
		var program = Utils.AsmProgram (@"
		LDI r16, 0x1      ; TCCR0B = 1 << CS00
		OUT 0x25, r16
		LDI r16, 0x0      ; TCNT0 <- 0
		OUT 0x26, r16
		NOP
		LDS r1, 0x46      ; r1 <- TCNT0 (2 cycles)
");
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		var runner = new TestProgramRunner (cpu);
		
		runner.RunInstructions (program.InstructionCount);
		
		Assert.That(cpu.Mmio.Data[R1], Is.EqualTo(2));
	}
	
	[Test (Description = "Should not start counting before the prescaler is first set (issue #41)")]
	public void Timer0Prescaler0 ()
	{
		var program = Utils.AsmProgram (@"
		NOP
		NOP
	    NOP
		NOP
		LDI r16, 0x1    ; TCCR2B = 1 << CS20;
		STS 0xb1, r16   ; Should start counting after this line
		NOP
		LDS r17, 0xb2   ; TCNT should equal 2 at this point
");
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
		var timer = new AvrTimer (cpu, AvrTimer.Timer2Config);
		var runner = new TestProgramRunner (cpu);
		
		runner.RunInstructions (program.InstructionCount);
		
		Assert.That(cpu.Mmio.Data[R17], Is.EqualTo(2));
	}

	[Test (Description = "Should not keep counting for one more instruction when the timer is disabled (issue #72)")]
	public void Timer0Disabled2 ()
	{
		var program = Utils.AsmProgram (@"
		EOR r1, r1      ; r1 = 0;
		LDI r16, 0x1    ; TCCR2B = 1 << CS20;
		STS 0xb1, r16   ; Should start counting after this instruction,
		STS 0xb1, r1    ; and stop counting *after* this one.
		NOP
		LDS r17, 0xb2   ; TCNT2 should equal 2 at this point (not counting the NOP)
	");
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
		var timer = new AvrTimer (cpu, AvrTimer.Timer2Config);
		var runner = new TestProgramRunner (cpu);
		
		runner.RunInstructions (program.InstructionCount);
		
		Assert.That(cpu.ReadData(R17), Is.EqualTo(2));
	}
	
	[Test (Description = "Should clear OC0B pin when writing 1 to FOC0B")]
	public void Timer0ClearOc0b ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
		var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
		
		cpu.WriteData(TCCR0A, COM0B1);
		
		// Listen to Port B's internal callback
		var portD = new AvrIoPort(cpu, AvrIoPort.PortDConfig) {
			TimerOverridePin = (pin, mode) => {
				Assert.That(pin, Is.EqualTo(5));
				Assert.That(mode, Is.EqualTo(PinOverrideMode.Clear));
			}
		};
		
		cpu.WriteData(TCCR0B, FOC0B);
	}

	[TestFixture]
	public class FastPwm
	{
		[Test (Description = "Should set OC0A on Compare Match, clear on Bottom (issue #78)")]
		public void Timer0FastPwmMode1 ()
		{
			var program = Utils.AsmProgram (@"
        LDI r16, 0xfc   ; TCNT0 = 0xfc;
        OUT 0x26, r16
        LDI r16, 0xfe   ; OCR0A = 0xfe;
        OUT 0x27, r16  
        ; WGM: Fast PWM, enable OC0A mode 3 (set on Compare Match, clear on Bottom)
        LDI r16, 0xc3   ; TCCR0A = (1 << COM0A1) | (1 << COM0A0) | (1 << WGM01) | (1 << WGM00);
        OUT 0x24, r16
        LDI r16, 0x1    ; TCCR0B = 1 << CS00;
        OUT 0x25, r16

        NOP             ; TCNT is now 0xfd
      beforeMatch: 
        NOP             ; TCNT is now 0xfe (Compare Match)
      afterMatch:
        NOP             ; TCNT is now 0xff
      beforeBottom:     
        NOP             ; TCNT is now 0x00 (BOTTOM)
      afterBottom:
        NOP
");
			
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
			var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
			
			// Listen to Port D's internal callback
			var portD = new AvrIoPort(cpu, AvrIoPort.PortDConfig) {
				TimerOverridePin = (pin, mode) => {
                    Assert.Multiple(() =>
                    {
                        Assert.That(pin, Is.EqualTo(6));
                        Assert.That(mode, Is.EqualTo(PinOverrideMode.Enable));
                    });
                }
			};
			
			var runner = new TestProgramRunner (cpu);
			
			runner.RunToAddress (program.Labels["beforeMatch"]);
			Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(0xfd));
			
			portD.TimerOverridePin = (pin, mode) => {
				Assert.Multiple(() =>
				{
					Assert.That(pin, Is.EqualTo(6));
					Assert.That(mode, Is.EqualTo(PinOverrideMode.Set));
				});
			};
			
			runner.RunToAddress (program.Labels["afterMatch"]);
			Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(0xfe));
			
			portD.TimerOverridePin = (pin, mode) => {
				Assert.Fail("Should not set OC0A on BOTTOM");
			};
			
			runner.RunToAddress (program.Labels["beforeBottom"]);
			Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(0xff));
			
			portD.TimerOverridePin = (pin, mode) => {
				Assert.Multiple(() =>
				{
					Assert.That(pin, Is.EqualTo(6));
					Assert.That(mode, Is.EqualTo(PinOverrideMode.Clear));
				});
			};
			
			runner.RunToAddress (program.Labels["afterBottom"]);
			Assert.That(cpu.ReadData(TCNT0), Is.Zero);
		}

		[Test (Description = "Should toggle OC0A on Compare Match when COM0An = 1 (issue #78)")]
		public void Timer0FastPwmMode2 ()
		{
			var program = Utils.AsmProgram (@"
        LDI r16, 0xfc   ; TCNT0 = 0xfc;
        OUT 0x26, r16
        LDI r16, 0xfe   ; OCR0A = 0xfe;
        OUT 0x27, r16  
        ; WGM: Fast PWM, enable OC0A mode 1 (Toggle)
        LDI r16, 0x43   ; TCCR0A = (1 << COM0A0) | (1 << WGM01) | (1 << WGM00);
        OUT 0x24, r16
        LDI r16, 0x09   ; TCCR0B = (1 << WGM02) | (1 << CS00);
        OUT 0x25, r16

        NOP             ; TCNT is now 0xfd
      beforeMatch: 
        NOP             ; TCNT is now 0xfe (Compare Match, TOP)
      afterMatch:
        NOP             ; TCNT is now 0
      afterOverflow:
        NOP
");
			
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
			var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
			
			// Listen to Port D's internal callback
			var portD = new AvrIoPort(cpu, AvrIoPort.PortDConfig) {
				TimerOverridePin = (pin, mode) => {
					Assert.Multiple(() =>
					{
						Assert.That(pin, Is.EqualTo(6));
						Assert.That(mode, Is.EqualTo(PinOverrideMode.Enable));
					});
				}
			};
			
			var runner = new TestProgramRunner (cpu);
			
			runner.RunToAddress (program.Labels["beforeMatch"]);
			Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(0xfd));
			
			portD.TimerOverridePin = (pin, mode) => {
				Assert.Multiple(() =>
				{
					Assert.That(pin, Is.EqualTo(6));
					Assert.That(mode, Is.EqualTo(PinOverrideMode.Toggle));
				});
			};
			
			runner.RunToAddress (program.Labels["afterMatch"]);
			Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(0xfe));
			
			portD.TimerOverridePin = (pin, mode) => {
				Assert.Fail("Should not toggle OC0A on BOTTOM");
			};
			
			runner.RunToAddress (program.Labels["afterOverflow"]);
			Assert.That(cpu.ReadData(TCNT0), Is.Zero);
		}
		
		[Test (Description = "Should leave OC0A disconnected when COM0An = 1 and WGM02 = 0 (issue #78)")]
		public void Timer0FastPwmMode3 ()
		{
			var program = Utils.AsmProgram (@"
        LDI r16, 0xfc   ; TCNT0 = 0xfc;
        OUT 0x26, r16
        LDI r16, 0xfe   ; OCR0A = 0xfe;
        OUT 0x27, r16  
        ; WGM: Fast PWM mode 7, enable OC0A mode 1 (Toggle)
        LDI r16, 0x43   ; TCCR0A = (1 << COM0A0) | (1 << WGM01) | (1 << WGM00);
        OUT 0x24, r16
        LDI r16, 0x09   ; TCCR0B = (1 << WGM02) | (1 << CS00);
        OUT 0x25, r16
        
      beforeClearWGM02:
        LDI r16, 0x01   ; TCCR0B = (1 << CS00);
        OUT 0x25, r16

      afterClearWGM02:
        NOP
");
			
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
			var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
			
			// Listen to Port D's internal callback
			var portD = new AvrIoPort(cpu, AvrIoPort.PortDConfig) {
				TimerOverridePin = (pin, mode) => {
					Assert.Multiple(() =>
					{
						Assert.That(pin, Is.EqualTo(6));
						Assert.That(mode, Is.EqualTo(PinOverrideMode.Enable));
					});
				}
			};
			
			var runner = new TestProgramRunner (cpu);
			
			runner.RunToAddress (program.Labels["beforeClearWGM02"]);
			
			portD.TimerOverridePin = (pin, mode) => {
				Assert.Multiple(() =>
				{
					Assert.That(pin, Is.EqualTo(6));
					Assert.That(mode, Is.EqualTo(PinOverrideMode.None));
				});
			};
			
			runner.RunToAddress (program.Labels["afterClearWGM02"]);
			
		}
	}

	[TestFixture]
	public class PhaseCorrectPwm
	{
		[Test (Description = "Should count up to TOP, down to 0, and then set TOV flag")]
		public void Timer0PhaseCorrectPwm ()
		{
			var program = Utils.AsmProgram (@"
        LDI r16, 0x3   ; OCR0A = 0x3;   // <- TOP value
        OUT 0x27, r16  
        ; Set waveform generation mode (WGM) to PWM, Phase Correct, top OCR0A
        LDI r16, 0x1   ; TCCR0A = 1 << WGM00;
        OUT 0x24, r16  
        LDI r16, 0x9   ; TCCR0B = (1 << WGM02) | (1 << CS00);
        OUT 0x25, r16  
        LDI r16, 0x2   ; TCNT0 = 0x2;
        OUT 0x26, r16

        IN r17, 0x26   ; TCNT0 will be 2
        IN r18, 0x26   ; TCNT0 will be 3
        IN r19, 0x26   ; TCNT0 will be 2
        IN r20, 0x26   ; TCNT0 will be 1
        IN r21, 0x26   ; TCNT0 will be 0
        IN r22, 0x26   ; TCNT0 will be 1 (end of test)
");
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
			var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
			
			var runner = new TestProgramRunner (cpu);
			
			runner.RunInstructions (program.InstructionCount);
			
			Assert.That(cpu.ReadData(R17), Is.EqualTo(2));
			Assert.That(cpu.ReadData(R18), Is.EqualTo(3));
			Assert.That(cpu.ReadData(R19), Is.EqualTo(2));
			Assert.That(cpu.ReadData(R20), Is.EqualTo(1));
			Assert.That(cpu.ReadData(R21), Is.Zero);
			Assert.That(cpu.ReadData(R22), Is.EqualTo(1));
			Assert.That(cpu.ReadData(TIFR0) & TOV0, Is.EqualTo(TOV0));
		}

		[Test (Description = "Should clear OC0A when TCNT0=OCR0A and counting up")]
		public void Timer0PhaseCorrectPwm2 ()
		{
			var program = Utils.AsmProgram (@"
        LDI r16, 0xfe   ; OCR0A = 0xfe;   // <- TOP value
        OUT 0x27, r16  
        ; Set waveform generation mode (WGM) to PWM, Phase Correct
        LDI r16, 0x81   ; TCCR0A = (1 << COM0A1) | (1 << WGM00);
        OUT 0x24, r16  
        LDI r16, 0x1   ; TCCR0B = (1 << CS00);
        OUT 0x25, r16  
        LDI r16, 0xfd   ; TCNT0 = 0xfd;
        OUT 0x26, r16  

        NOP   ; TCNT0 will be 0xfe
        NOP   ; TCNT0 will be 0xff
        NOP   ; TCNT0 will be 0xfe again (end of test)
");
			
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
			var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
			
			// Listen to Port D's internal callback
			var portD = new AvrIoPort(cpu, AvrIoPort.PortDConfig) {
				TimerOverridePin = (pin, mode) => {
					Assert.Multiple(() =>
					{
						Assert.That(pin, Is.EqualTo(6));
						Assert.That(mode, Is.EqualTo(PinOverrideMode.Enable));
					});
				}
			};
			
			var nopCount = cpu.ProgramMemory.Count (i => i == nopOpCode);
			var runner = new TestProgramRunner (cpu);
			
			runner.RunInstructions (program.InstructionCount - nopCount);
			
			Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(0xfd));
			
			portD.TimerOverridePin = (pin, mode) => {
				Assert.Multiple(() =>
				{
					Assert.That(pin, Is.EqualTo(6));
					Assert.That(mode, Is.EqualTo(PinOverrideMode.Clear));
				});
			};
			
			runner.RunInstructions (1);
			Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(0xfe));

			portD.TimerOverridePin = (pin, mode) => {
				Assert.Fail ("Should not set OC0A on BOTTOM");
			};
			
			runner.RunInstructions (1);
			Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(0xff));
			
			portD.TimerOverridePin = (pin, mode) => {
				Assert.Multiple(() =>
				{
					Assert.That(pin, Is.EqualTo(6));
					Assert.That(mode, Is.EqualTo(PinOverrideMode.Set));
				});
			};
			
			runner.RunInstructions (1);
			
			Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(0xfe));
		}

		[Test (Description = "Should toggle OC0A when TCNT0=OCR0A and COM0An=1 (issue #78)")]
		public void Timer0PhaseCorrectPwm3 ()
		{
			var program = Utils.AsmProgram (@"
        LDI r16, 0xfe   ; OCR0A = 0xfe;   // <- TOP value
        OUT 0x27, r16  
        ; Set waveform generation mode (WGM) to PWM, Phase Correct (mode 5)
        LDI r16, 0x41   ; TCCR0A = (1 << COM0A0) | (1 << WGM00);
        OUT 0x24, r16  
        LDI r16, 0x09   ; TCCR0B = (1 << WGM02) | (1 << CS00);
        OUT 0x25, r16  
        LDI r16, 0xfd   ; TCNT0 = 0xfd;
        OUT 0x26, r16  

      beforeMatch:
        NOP             ; TCNT0 will be 0xfe
      afterMatch:
        NOP
");
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
			var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
			
			// Listen to Port D's internal callback
			var portD = new AvrIoPort(cpu, AvrIoPort.PortDConfig) {
				TimerOverridePin = (pin, mode) => {
					Assert.Multiple(() =>
					{
						Assert.That(pin, Is.EqualTo(6));
						Assert.That(mode, Is.EqualTo(PinOverrideMode.Enable));
					});
				}
			};
			
			var runner = new TestProgramRunner (cpu);
			
			runner.RunToAddress (program.Labels["beforeMatch"]);
			
			Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(0xfd));
			
			portD.TimerOverridePin = (pin, mode) => {
				Assert.Multiple(() =>
				{
					Assert.That(pin, Is.EqualTo(6));
					Assert.That(mode, Is.EqualTo(PinOverrideMode.Toggle));
				});
			};
			
			runner.RunToAddress (program.Labels["afterMatch"]);
			
			Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(0xfe));
		}

		[Test (Description = "Should leave OC0A disconnected TCNT0=OCR0A and COM0An=1 in WGM mode 1 (issue #78)")]
		public void Timer0PhaseCorrectPwm4 ()
		{
			var program = Utils.AsmProgram (@"
        LDI r16, 0xfe   ; OCR0A = 0xfe;   // <- TOP value
        OUT 0x27, r16  
        ; Set waveform generation mode (WGM) to PWM, Phase Correct (mode 1)
        LDI r16, 0x41   ; TCCR0A = (1 << COM0A0) | (1 << WGM00);
        OUT 0x24, r16  
        LDI r16, 0x01   ; TCCR0B = (1 << CS00);
        OUT 0x25, r16  
        LDI r16, 0xfd   ; TCNT0 = 0xfd;
        OUT 0x26, r16  
");
			
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
			var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
			
			// Listen to Port D's internal callback
			new AvrIoPort(cpu, AvrIoPort.PortDConfig) {
				TimerOverridePin = (pin, mode) => {
					Assert.Fail("Should not set OC0A");
				}
			};
			
			var runner = new TestProgramRunner (cpu);
			
			runner.RunInstructions (program.InstructionCount);
		}

		[Test (Description = "Should not miss Compare Match when executing multi-cycle instruction (issue #79)")]
		public void Timer0PhaseCorrectPwm5 ()
		{
			var program = Utils.AsmProgram (@"
        LDI r16, 0x10   ; OCR0A = 0x10;   // <- TOP value
        OUT 0x27, r16  
        ; Set waveform generation mode (WGM) to normal, enable OC0A (Set on match)
        LDI r16, 0xc0   ; TCCR0A = (1 << COM0A1) | (1 << COM0A0);
        OUT 0x24, r16  
        LDI r16, 0x1    ; TCCR0B = (1 << CS00);
        OUT 0x25, r16  
        LDI r16, 0xf    ; TCNT0 = 0xf;
        OUT 0x26, r16  
        RJMP 1          ; TCNT0 will be 0x11 (RJMP takes 2 cycles)
");
			
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
			var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);

			var calls = new List<KeyValuePair<int, PinOverrideMode>> ();
			
			// Listen to Port D's internal callback
			var portD = new AvrIoPort(cpu, AvrIoPort.PortDConfig) {
				TimerOverridePin = (pin, mode) => {
					calls.Add(new KeyValuePair<int, PinOverrideMode>(pin, mode));
				}
			};
			
			var runner = new TestProgramRunner (cpu);
			
			runner.RunInstructions (program.InstructionCount);
			
            Assert.Multiple(() =>
            {
	            Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(0x11));
	            
                Assert.That(calls[0].Key, Is.EqualTo(6));
                Assert.That(calls[0].Value, Is.EqualTo(PinOverrideMode.Enable));

                // Verify that Compare Match has occured and set the OC0A pin (PD6 on ATmega328p)
                Assert.That(calls[1].Key, Is.EqualTo(6));
                Assert.That(calls[1].Value, Is.EqualTo(PinOverrideMode.Set));
            });
        }

		[Test (Description = "Should only update OCR0A when TCNT0=TOP in PWM Phase Correct mode (issue #76)")]
		public void Timer0PhaseCorrectPwm6 ()
		{
			var program = Utils.AsmProgram (@"
        LDI r16, 0x4   ; OCR0A = 0x4;
        OUT 0x27, r16  
        ; Set waveform generation mode (WGM) to PWM, Phase Correct
        LDI r16, 0x01   ; TCCR0A = (1 << WGM00);
        OUT 0x24, r16  
        LDI r16, 0x09   ; TCCR0B = (1 << WGM02) | (1 << CS00);
        OUT 0x25, r16  
        LDI r16, 0x0   ; TCNT0 = 0x0;
        OUT 0x26, r16  

        LDI r16, 0x2    ; OCR0A = 0x2; // TCNT0 should read 0x0
        OUT 0x27, r16   ; // TCNT0 should read 0x1
        NOP             ; // TCNT0 should read 0x2
        NOP             ; // TCNT0 should read 0x3
        IN r17, 0x26    ; R17 = TCNT;  // TCNT0 should read 0x4 (that's old OCR0A / TOP)
        NOP             ; // TCNT0 should read 0x3
        NOP             ; // TCNT0 should read 0x2
        NOP             ; // TCNT0 should read 0x1
        NOP             ; // TCNT0 should read 0x0
        NOP             ; // TCNT0 should read 0x1
        NOP             ; // TCNT0 should read 0x2
        IN r18, 0x26    ; R18 = TCNT; // TCNT0 should read 0x1
");
			
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
			var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
			
			var runner = new TestProgramRunner (cpu);
			
			runner.RunInstructions (program.InstructionCount);
			
			Assert.That(cpu.ReadData(R17), Is.EqualTo(0x4));
			Assert.That(cpu.ReadData(R18), Is.EqualTo(0x1));
		}

		[Test (Description = "Should update OCR0A when TCNT0=TOP and TOP=0 in PWM Phase Correct mode (issue #119)")]
		public void Timer0PhaseCorrectPwm7 ()
		{
			var program = Utils.AsmProgram (@"
        ; Set waveform generation mode (WGM) to PWM, Phase Correct
        LDI r16, 0x01   ; TCCR0A = (1 << WGM00);
        OUT 0x24, r16
        LDI r16, 0x09   ; TCCR0B = (1 << WGM02) | (1 << CS00);
        OUT 0x25, r16
        LDI r16, 0x0   ; TCNT0 = 0x0;
        OUT 0x26, r16

        IN r17, 0x26    ; R17 = TCNT; // TCNT0 should read 0x0
        IN r18, 0x26    ; R18 = TCNT; // TCNT0 should read 0x0
        LDI r16, 0x2    ; OCR0A = 0x2; // TCNT0 should read 0x0
        OUT 0x27, r16   ; // TCNT0 should read 0x1
        NOP             ; // TCNT0 should read 0x2
        IN r19, 0x26    ; R19 = TCNT; // TCNT0 should read 0x1
");
			
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
			var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
			
			var runner = new TestProgramRunner (cpu);
			
			runner.RunInstructions (program.InstructionCount);
			
			Assert.That(cpu.ReadData(R17), Is.Zero);
			Assert.That(cpu.ReadData(R18), Is.Zero);
			Assert.That(cpu.ReadData(R19), Is.EqualTo(1));
		}

		[Test (Description = "Should not overrun when TOP < current value in Phase Correct mode (issue #119)")]
		public void Timer0PhaseCorrectPwm8 ()
		{
			var program = Utils.AsmProgram (@"
        ; Set waveform generation mode (WGM) to PWM, Phase Correct
        LDI r16, 0x01   ; TCCR0A = (1 << WGM00);
        OUT 0x24, r16
        LDI r16, 0x09   ; TCCR0B = (1 << WGM02) | (1 << CS00);
        OUT 0x25, r16
        LDI r16, 0xff   ; TCNT0 = 0xff;
        OUT 0x26, r16

        IN r17, 0x26    ; R17 = TCNT; // TCNT0 should read 255
");
			
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
			var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
			
			var runner = new TestProgramRunner (cpu);
			
			runner.RunInstructions (program.InstructionCount);
			
            Assert.Multiple(() =>
            {
                Assert.That(cpu.ReadData(R17), Is.EqualTo(0xff));
                Assert.That(timer.DebugTCNT, Is.Zero);
            });
        }
	}

	[TestFixture]
	public class SixteenBitsTimers
	{
		[Test (Description = "Should increment 16-bit TCNT by 1")]
		public void Timer1Increment ()
		{
			
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config);
			
			cpu.WriteData(TCNT1H, 0x22); // TCNT1 <- 0x2233
			cpu.WriteData(TCNT1, 0x33); // ...
			
			var timerLow = cpu.ReadData(TCNT1);
			var timerHigh = cpu.ReadData(TCNT1H);
			
			Assert.That((timerHigh << 8) | timerLow, Is.EqualTo(0x2233));
			
			cpu.WriteData(TCCR1A, 0x0); // WGM: Normal
			cpu.WriteData(TCCR1B, CS10); // Set prescaler to 1
			
			cpu.Cycles = 1;
			cpu.Tick();
			cpu.Cycles = 2;
			cpu.Tick();
			
			cpu.ReadData (TCNT1);
			
			Assert.That(cpu.Mmio.DataView.GetUint16(TCNT1, true), Is.EqualTo(0x2234)); // TCNT1 should increment
		}

		[Test (Description = "Should set OCF0A flag when timer equals OCRA (16 bit mode)")]
		public void Timer1OutputCompareMatchA ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config);
			
			cpu.WriteData(TCNT1H, 0x10); // TCNT1 <- 0x10ee
			cpu.WriteData(TCNT1, 0xee); // ...
			cpu.WriteData(OCR1AH, 0x10); // OCR1 <- 0x10ef
			cpu.WriteData(OCR1A, 0xef); // ...
			cpu.WriteData(TCCR1A, 0x0); // WGM: Normal
			cpu.WriteData(TCCR1B, CS10); // Set prescaler to 1
			
			cpu.Cycles = 1;
			cpu.Tick();
			cpu.Cycles = 2;
			cpu.Tick();
            Assert.Multiple(() =>
            {
                Assert.That(cpu.ReadData(TIFR1) & OCF1A, Is.EqualTo(OCF1A)); // TIFR1 should have OCF1A bit on
                Assert.That(cpu.Pc, Is.Zero);
                Assert.That(cpu.Cycles, Is.EqualTo(2));
            });
        }
		
		[Test (Description = "Should set OCF1C flag when timer equals OCRC")]
		public void Timer1OutputCompareMatchC ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			const int OCR1C = 0x8c;
			const int OCR1CH = 0x8d;
			const int OCF1C = 1 << 3;
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config.CreateNew (
				ocrc: OCR1C,
				ocfc: OCF1C
			));
			
			cpu.WriteData(TCNT1H, 0);
			cpu.WriteData(TCNT1, 0x10);
			cpu.WriteData(OCR1C, 0x11);
			cpu.WriteData(OCR1CH, 0x11);
			cpu.WriteData(TCCR1A, 0x0); // WGM: (Normal)
			cpu.WriteData(TCCR1B, CS00); // Set prescaler to 1
			
			cpu.Cycles = 1;
			cpu.Tick();
			cpu.Cycles = 2;
			cpu.Tick();
			Assert.Multiple(() =>
			{
				Assert.That(cpu.ReadData(TIFR1), Is.EqualTo(OCF1C));
				Assert.That(cpu.Pc, Is.Zero);
				Assert.That(cpu.Cycles, Is.EqualTo(2));
			});
		}
		
		[Test (Description = "Should generate an overflow interrupt if timer overflows and interrupts enabled")]
		public void Timer1OverflowInterrupt ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config);
			
			cpu.WriteData(TCCR1A, 0x3); // TCCR1A <- WGM10 | WGM11 (Fast PWM, 10-bit)
			cpu.WriteData(TCCR1B, 0x9); // TCCR1B <- WGM12 | CS10
			cpu.WriteData(TIMSK1, 0x1); // TIMSK1: TOIE1
			
			cpu.Mmio.Data[SREG] = 0x80; // SREG: I-------
			cpu.WriteData(TCNT1H, 0x3); // TCNT1 <- 0x3ff
			cpu.Cycles = 1;
			cpu.Tick();
			cpu.WriteData(TCNT1, 0xff); // ...
			cpu.Cycles++; // This cycle shouldn't be counted
			cpu.Tick();
			cpu.Cycles++;
			cpu.Tick(); // This is where we cause the overflow
			cpu.ReadData(TCNT1); // Refresh TCNT1
			Assert.Multiple(() =>
			{
				Assert.That(cpu.Mmio.DataView.GetUint16(TCNT1, true), Is.EqualTo(3)); // TCNT = 3 (3 interrupt-dispatch cycles)
				Assert.That(cpu.ReadData(TIFR1) & TOV1, Is.Zero);
				Assert.That(cpu.Pc, Is.EqualTo(0x1a));
				Assert.That(cpu.Cycles, Is.EqualTo(6)); // cycles: 3 (pre-interrupt) + 3 (DoAvrInterrupt) = 6
			});
		}
		
		[Test (Description = "Should reset the timer once it reaches ICR value in mode 12")]
		public void Timer1IcrReset ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config);
			
			cpu.WriteData(TCNT1H, 0x50); // TCNT1 <- 0x500f
			cpu.WriteData(TCNT1, 0x0f); // ...
			cpu.WriteData(ICR1H, 0x50); // ICR1 <- 0x5010
			cpu.WriteData(ICR1, 0x10); // ...
			cpu.WriteData(TCCR1B, WGM13 | WGM12 | CS10); // Set prescaler to 1, WGM: CTC
			cpu.Cycles = 1;
			cpu.Tick();
			cpu.Cycles = 3; // 2 cycles should increment timer twice, beyond ICR1
			cpu.Tick();
			cpu.ReadData(TCNT1); // Refresh TCNT1
			Assert.Multiple(() =>
			{
				Assert.That(cpu.Mmio.DataView.GetUint16(TCNT1, true), Is.Zero); // TCNT should be 0
				Assert.That(cpu.ReadData(TIFR1) & TOV1, Is.Zero);
				Assert.That(cpu.Cycles, Is.EqualTo(3));
			});
		}

		[Test (Description = "Should not update the high byte of TCNT if written after the low byte (issue #37)")]
		public void Timer1HighByteUpdate ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config);
			
			cpu.WriteData(TCNT1, 0x22);
			cpu.WriteData(TCNT1H, 0x55);
			cpu.Cycles = 1;
			cpu.Tick();
			
			var timerLow = cpu.ReadData(TCNT1);
			var timerHigh = cpu.ReadData(TCNT1H);
			
			Assert.That((timerHigh << 8) | timerLow, Is.EqualTo(0x22));
		}
		
		[Test (Description = "Reading from TCNT1H before TCNT1L should return old value (issue #37)")]
		public void Timer1HighByteReadBeforeLowByte ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config);
			
			cpu.WriteData(TCNT1H, 0xff);
			cpu.WriteData(TCNT1, 0xff);
			cpu.WriteData(TCCR1B, WGM12 | CS10); // Set prescaler to 1, WGM: CTC
			
			cpu.Cycles = 1;
			cpu.Tick();
			cpu.Cycles = 2;
			cpu.Tick();
			
			// We read the high byte before the low byte, so the high byte should still have
			// the previous value:
			var timerHigh = cpu.ReadData(TCNT1H);
			var timerLow = cpu.ReadData(TCNT1);
			
			Assert.That((timerHigh << 8) | timerLow, Is.EqualTo(0xff00));
		}
		
		[Test (Description = "Should toggle OC1B on Compare Match")]
		public void Timer1ToggleOnCompareMatchB ()
		{
			var program = Utils.AsmProgram (@"
        ; Set waveform generation mode (WGM) to Normal, top 0xFFFF
        LDI r16, 0x10   ; TCCR1A = (1 << COM1B0);
        STS 0x80, r16  
        LDI r16, 0x1    ; TCCR1B = (1 << CS00);
        STS 0x81, r16  
        LDI r16, 0x0    ; OCR1BH = 0x0;
        STS 0x8B, r16  
        LDI r16, 0x4a   ; OCR1BL = 0x4a;
        STS 0x8A, r16  
        LDI r16, 0x0    ; TCNT1H = 0x0;
        STS 0x85, r16  
        LDI r16, 0x49   ; TCNT1L = 0x49;
        STS 0x84, r16  

        NOP   ; TCNT1 will be 0x49
        NOP   ; TCNT1 will be 0x4a
");
			
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config);
			
			// Listen to Port D's internal callback
			var portD = new AvrIoPort(cpu, AvrIoPort.PortDConfig) {
				TimerOverridePin = (pin, mode) => {
					Assert.Multiple(() =>
					{
						Assert.That(pin, Is.EqualTo(2));
						Assert.That(mode, Is.EqualTo(PinOverrideMode.Enable));
					});
				}
			};
			
			var nopCount = cpu.ProgramMemory.Count (i => i == nopOpCode);
			
			var runner = new TestProgramRunner (cpu);
			
			runner.RunInstructions (program.InstructionCount - nopCount);
			
			Assert.That(cpu.ReadData(TCNT1), Is.EqualTo(0x49));
			
			portD.TimerOverridePin = (pin, mode) => {
				Assert.Multiple(() =>
				{
					Assert.That(pin, Is.EqualTo(2));
					Assert.That(mode, Is.EqualTo(PinOverrideMode.Toggle));
				});
			};
			
			runner.RunInstructions (1);
			Assert.That(cpu.ReadData(TCNT1), Is.EqualTo(0x4a));
		}

		[Test (Description = "Should toggle OC1C on Compare Match")]
		public void Timer1ToggleOnCompareMatchC ()
		{
			var program = Utils.AsmProgram (@$"
        ; Set waveform generation mode (WGM) to Normal, top 0xFFFF
        LDI r16, 0x04   ; TCCR1A = (1 << COM1C0);
        STS {TCCR1A}, r16  
        LDI r16, 0x1    ; TCCR1B = (1 << CS00);
        STS {TCCR1B}, r16  
        LDI r16, 0x0    ; OCR1CH = 0x0;
        STS {OCR1CH}, r16  
        LDI r16, 0x4a   ; OCR1C = 0x4a;
        STS {OCR1C}, r16  
        LDI r16, 0x0    ; TCNT1H = 0x0;
        STS {TCNT1H}, r16  
        LDI r16, 0x49   ; TCNT1 = 0x49;
        STS {TCNT1}, r16  

        NOP   ; TCNT1 will be 0x49
        NOP   ; TCNT1 will be 0x4a
");
			
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config.CreateNew (
				ocrc: OCR1C,
				ocfc: OCF1C,
				comparatorPortC: AvrIoPort.PortBConfig.PORT,
				comparatorPinC: 3
			));
			
			// Listen to Port B's internal callback
			var portB = new AvrIoPort(cpu, AvrIoPort.PortBConfig) {
				TimerOverridePin = (pin, mode) => {
					Assert.Multiple(() =>
					{
						Assert.That(pin, Is.EqualTo(3));
						Assert.That(mode, Is.EqualTo(PinOverrideMode.Enable));
					});
				}
			};
			
			var nopCount = cpu.ProgramMemory.Count (i => i == nopOpCode);
			
			var runner = new TestProgramRunner (cpu);
			
			runner.RunInstructions (program.InstructionCount - nopCount);
			
			Assert.That(cpu.ReadData(TCNT1), Is.EqualTo(0x49));
			
			portB.TimerOverridePin = (pin, mode) => {
				Assert.Multiple(() =>
				{
					Assert.That(pin, Is.EqualTo(3));
					Assert.That(mode, Is.EqualTo(PinOverrideMode.Toggle));
				});
			};
			
			runner.RunInstructions (1);
			
			Assert.That(cpu.ReadData(TCNT1), Is.EqualTo(0x4a));
		}

		[Test (Description = "Should toggle OC1C on when writing 1 to FOC1C")]
		public void Timer1ToggleOnForceOutputCompareC ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config.CreateNew (
				ocrc: OCR1C,
				ocfc: OCF1C,
				comparatorPortC: AvrIoPort.PortBConfig.PORT,
				comparatorPinC: 3
			));
			
			cpu.WriteData(TCCR1A, COM1C0);
			
			// Listen to Port B's internal callback
			var portB = new AvrIoPort(cpu, AvrIoPort.PortBConfig) {
				TimerOverridePin = (pin, mode) => {
					Assert.Multiple(() =>
					{
						Assert.That(pin, Is.EqualTo(3));
						Assert.That(mode, Is.EqualTo(PinOverrideMode.Toggle));
					});
				}
			};
			
			cpu.WriteData(TCCR1C, FOC1C);
		}
		
		[Test (Description = "Should not toggle OC1C on when writing 1 to FOC1C in PWM mode")]
		public void Timer1ToggleOnForceOutputCompareCPwm ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config.CreateNew (
				ocrc: OCR1C,
				ocfc: OCF1C,
				comparatorPortC: AvrIoPort.PortBConfig.PORT,
				comparatorPinC: 3
			));
			
			cpu.WriteData(TCCR1A, COM1C0 | WGM11);
			
			// Listen to Port B's internal callback
			var portB = new AvrIoPort(cpu, AvrIoPort.PortBConfig) {
				TimerOverridePin = (pin, mode) => {
					Assert.Fail("Should not toggle OC1C");
				}
			};
			
			cpu.WriteData(TCCR1C, FOC1C);
		}

		[Test (Description = "Should only update OCR1A when TCNT1=BOTTOM in PWM Phase/Frequency Correct mode (issue #76)")]
		public void Timer1PhaseCorrectPwm1 ()
		{
			var program = Utils.AsmProgram (@"
        LDI r16, 0x0    ; OCR1AH = 0x0;
        STS 0x89, r16  
        LDI r16, 0x4   ; OCR1AL = 0x4;
        STS 0x88, r16  
        ; Set waveform generation mode (WGM) to PWM Phase/Frequency Correct mode (9)
        LDI r16, 0x01   ; TCCR1A = (1 << WGM10);
        STS 0x80, r16  
        LDI r16, 0x11   ; TCCR1B = (1 << WGM13) | (1 << CS00);
        STS 0x81, r16  
        LDI r16, 0x0    ; TCNT1H = 0x0;
        STS 0x85, r16  
        LDI r16, 0x0    ; TCNT1L = 0x0;
        STS 0x84, r16  

        LDI r16, 0x8   ; OCR1AL = 0x8; // TCNT1 should read 0x0
        STS 0x88, r16  ; // TCNT1 should read 0x2 (going up)
        LDS r17, 0x84  ; // TCNT1 should read 0x4 (going down)
        LDS r18, 0x84  ; // TCNT1 should read 0x2 (going down)
        NOP            ; // TCNT1 should read 0x0 (going up)
        NOP            ; // TCNT1 should read 0x1 (going up)
        NOP            ; // TCNT1 should read 0x2 (going up)
        NOP            ; // TCNT1 should read 0x3 (going up)
        NOP            ; // TCNT1 should read 0x4 (going up)
        NOP            ; // TCNT1 should read 0x5 (going up)
        LDS r19, 0x84  ; // TCNT1 should read 0x6 (going up)
        NOP            ; // TCNT1 should read 0x8 (going up)
        LDS r20, 0x84  ; // TCNT1 should read 0x7 (going up)
");
			
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config);
			
			var runner = new TestProgramRunner (cpu);
			
			runner.RunInstructions (program.InstructionCount);
            Assert.Multiple(() =>
            {
                Assert.That(cpu.ReadData(R17), Is.EqualTo(4));
                Assert.That(cpu.ReadData(R18), Is.EqualTo(2));
                Assert.That(cpu.ReadData(R19), Is.EqualTo(6));
                Assert.That(cpu.ReadData(R20), Is.EqualTo(7));
            });
        }

		[Test (Description = "Should update OCR1A when setting TCNT to 0 (issue #111)")]
		public void Timer1PhaseCorrectPwm2 ()
		{
			var program = Utils.AsmProgram (@"
        CLR r1          ; r1 is our zero register
        LDI r16, 0x0    ; OCR1AH = 0x0;
        STS 0x89, r1    
        LDI r16, 0x8    ; OCR1AL = 0x8;
        STS 0x88, r16  
        ; Set waveform generation mode (WGM) to PWM Phase/Frequency Correct mode (9)
        LDI r16, 0x01   ; TCCR1A = (1 << WGM10);
        STS 0x80, r16  
        LDI r16, 0x11   ; TCCR1B = (1 << WGM13) | (1 << CS00);
        STS 0x81, r16  
        STS 0x85, r1    ; TCNT1H = 0x0;
        STS 0x84, r1    ; TCNT1L = 0x0;
        
        LDI r16, 0x5   ; OCR1AL = 0x5; // TCNT1 should read 0x0
        STS 0x88, r16  ; // TCNT1 should read 0x2 (going up)
        STS 0x84, r1   ; TCNT1L = 0x0;
        LDS r17, 0x84  ; // TCNT1 should read 0x1 (going up)
        LDS r18, 0x84  ; // TCNT1 should read 0x3 (going up)
        LDS r19, 0x84  ; // TCNT1 should read 0x5 (going down)
        LDS r20, 0x84  ; // TCNT1 should read 0x3 (going down)
");
			
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config);
			
			var runner = new TestProgramRunner (cpu);
			
			runner.RunInstructions (program.InstructionCount);
			Assert.Multiple(() =>
			{
				Assert.That(cpu.ReadData(R17), Is.EqualTo(1));
				Assert.That(cpu.ReadData(R18), Is.EqualTo(3));
				Assert.That(cpu.ReadData(R19), Is.EqualTo(5));
				Assert.That(cpu.ReadData(R20), Is.EqualTo(3));
			});
		}

		[Test (Description = "Should mask the unused bits of OCR1A when using fixed top values")]
		public void Timer1PhaseCorrectPwm3 ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config);
			
			cpu.WriteData(TCCR1A, WGM10 | WGM11); // WGM: FastPWM, top 0x3ff
			cpu.WriteData(TCCR1B, WGM12);
			cpu.WriteData(OCR1AH, 0xff);
			cpu.WriteData(OCR1A, 0xff);
			
			Assert.Multiple(() =>
			{
				Assert.That(cpu.ReadData(OCR1A), Is.EqualTo(0xff));
				Assert.That(cpu.ReadData(OCR1AH), Is.EqualTo(0x03));
			});
		}
	}

	[TestFixture]
	public class ExternalClock
	{
		[Test (Description = "Should count on the falling edge of T0 when CS=110")]
		public void FallingEdgeT0 ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
			var port = new AvrIoPort(cpu, AvrIoPort.PortDConfig);
			
			cpu.WriteData(TCCR0B, CS02 | CS01); // Count on falling edge
			cpu.Cycles = 1;
			cpu.Tick();
			
			port.SetPinValue (T0, true); // Rising edge
			cpu.Cycles = 2;
			cpu.Tick();
			Assert.That(cpu.ReadData(TCNT0), Is.Zero);
			
			port.SetPinValue (T0, false); // Falling edge
			cpu.Cycles = 3;
			cpu.Tick();
			Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(1));
		}

		[Test (Description = "Should count on the rising edge of T0 when CS=111")]
		public void RisingEdgeT0 ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer = new AvrTimer (cpu, AvrTimer.Timer0Config);
			var port = new AvrIoPort(cpu, AvrIoPort.PortDConfig);
			
			cpu.WriteData(TCCR0B, CS02 | CS01 | CS00); // Count on rising edge
			cpu.Cycles = 1;
			cpu.Tick();
			
			port.SetPinValue (T0, true); // Rising edge
			cpu.Cycles = 2;
			cpu.Tick();
			Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(1));
			
			port.SetPinValue (T0, false); // Falling edge
			cpu.Cycles = 3;
			cpu.Tick();
			Assert.That(cpu.ReadData(TCNT0), Is.EqualTo(1));
		}
	}

	// ── Timer1 Input Capture (ICR) ────────────────────────────────────────────
	[TestFixture]
	public class InputCapture
	{
		const int TIFR1  = 0x36;
		const int TIMSK1 = 0x6f;
		const int TCCR1A = 0x80;
		const int TCCR1B = 0x81;
		const int TCNT1  = 0x84;
		const int ICR1   = 0x86;
		const int ICR1H  = 0x87;
		const int CS10   = 1;
		const int ICF1   = 1 << 5;  // TIFR1 bit 5
		const int ICIE1  = 1 << 5;  // TIMSK1 bit 5

		[Test (Description = "TriggerCapture latches TCNT into ICR and sets ICF1 flag")]
		public void TriggerCapture_LatchesTcntAndSetsFlag ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config);

			cpu.WriteData (TCCR1A, 0);      // Normal mode
			cpu.WriteData (TCCR1B, CS10);   // prescaler /1
			cpu.Cycles = 1;
			cpu.Tick ();
			cpu.Cycles = 10;
			cpu.Tick ();
			cpu.ReadData (TCNT1); // refresh _tcnt

			var tcntBefore = cpu.Mmio.DataView.GetUint16 (TCNT1, true);

			timer.TriggerCapture ();

			// ICR must hold the value that was in TCNT at the moment of capture
			var icr = cpu.Mmio.DataView.GetUint16 (ICR1, true);
			Assert.That (icr, Is.EqualTo (tcntBefore), "ICR must equal TCNT at moment of capture");

			// ICF1 flag must be set
			Assert.That (cpu.Mmio.Data[TIFR1] & ICF1, Is.EqualTo (ICF1), "ICF1 flag must be set after capture");
		}

		[Test (Description = "TriggerCapture fires capture interrupt when ICIE1 is enabled")]
		public void TriggerCapture_FiresInterruptWhenEnabled ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config);

			cpu.WriteData (TCCR1A, 0);
			cpu.WriteData (TCCR1B, CS10);
			cpu.WriteData (TIMSK1, ICIE1);   // enable input capture interrupt
			cpu.Mmio.Data[95] = 0x80;        // SREG: I-------

			timer.TriggerCapture ();
			cpu.Tick ();

			// PC must jump to the capture ISR vector (0x14 = address of TIMER1_CAPT)
			Assert.That (cpu.Pc, Is.EqualTo (0x14), "PC must jump to TIMER1_CAPT ISR vector 0x14");
			Assert.That (cpu.Mmio.Data[TIFR1] & ICF1, Is.EqualTo (0), "ICF1 must be cleared when interrupt is acknowledged");
		}

		[Test (Description = "Clearing ICF1 by writing 1 to TIFR1 removes the pending interrupt")]
		public void ClearICF1_ByWritingTIFR1 ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config);

			cpu.WriteData (TCCR1B, CS10);
			cpu.WriteData (TIMSK1, ICIE1);

			timer.TriggerCapture ();
			Assert.That (cpu.Mmio.Data[TIFR1] & ICF1, Is.EqualTo (ICF1));

			// Writing 1 to a TIFR bit clears it (write-1-to-clear per AVR spec)
			cpu.WriteData (TIFR1, ICF1);
			Assert.That (cpu.Mmio.Data[TIFR1] & ICF1, Is.EqualTo (0), "ICF1 must clear when 1 is written to TIFR1");
		}

		[Test (Description = "TriggerCapture on a timer without capture configured is a no-op")]
		public void TriggerCapture_NoOp_OnTimer0 ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer = new AvrTimer (cpu, AvrTimer.Timer0Config); // Timer0 has no capture

			Assert.DoesNotThrow (() => timer.TriggerCapture (),
				"TriggerCapture must not throw on a timer without capture support");
		}
	}

	// ── Timer OCFC output compare C ──────────────────────────────────────────
	[TestFixture]
	public class OutputCompareC
	{
		const int TIFR1  = 0x36;
		const int TIMSK1 = 0x6f;
		const int TCCR1A = 0x80;
		const int TCCR1B = 0x81;
		const int TCNT1  = 0x84;
		const int OCR1C  = 0x8c;
		const int OCR1CH = 0x8d;
		const int OCF1C  = 1 << 3; // TIFR1 bit 3
		const int OCIE1C = 1 << 3; // TIMSK1 bit 3
		const int CS10   = 1;
		const int SREG   = 95;

		[Test (Description = "OCF1C flag is set when TCNT1 matches OCR1C")]
		public void OCF1C_SetOnMatch ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config.CreateNew (
				ocrc: OCR1C, ocfc: OCF1C));

			cpu.WriteData (TCCR1A, 0);
			cpu.WriteData (TCCR1B, CS10);
			cpu.WriteData (OCR1CH, 0);
			cpu.WriteData (OCR1C,  5);

			cpu.Cycles = 1; cpu.Tick ();
			cpu.Cycles = 6; cpu.Tick ();
			cpu.ReadData (TCNT1);

			Assert.That (cpu.Mmio.Data[TIFR1] & OCF1C, Is.EqualTo (OCF1C), "OCF1C must be set when TCNT reaches OCR1C");
		}

		[Test (Description = "OCF1C flag can be cleared by writing 1 to TIFR1")]
		public void OCF1C_ClearedByWritingTIFR1 ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config.CreateNew (
				ocrc: OCR1C, ocfc: OCF1C));

			cpu.WriteData (TCCR1B, CS10);
			cpu.WriteData (OCR1CH, 0);
			cpu.WriteData (OCR1C,  5);
			cpu.Cycles = 1; cpu.Tick ();
			cpu.Cycles = 6; cpu.Tick ();

			Assert.That (cpu.Mmio.Data[TIFR1] & OCF1C, Is.EqualTo (OCF1C));
			cpu.WriteData (TIFR1, OCF1C); // write-1-to-clear
			Assert.That (cpu.Mmio.Data[TIFR1] & OCF1C, Is.EqualTo (0), "OCF1C must clear via TIFR1 write");
		}

		[Test (Description = "OCIE1C in TIMSK1 enables OCF1C interrupt")]
		public void OCIE1C_EnablesInterrupt ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer = new AvrTimer (cpu, AvrTimer.Timer1Config.CreateNew (
				ocrc: OCR1C, ocfc: OCF1C, ociec: OCIE1C,
				comparatorCInterrupt: 0x1c)); // some interrupt vector

			cpu.WriteData (TCCR1B, CS10);
			cpu.WriteData (OCR1CH, 0);
			cpu.WriteData (OCR1C,  5);
			cpu.WriteData (TIMSK1, OCIE1C);
			cpu.Mmio.Data[SREG] = 0x80;

			cpu.Cycles = 1; cpu.Tick ();
			cpu.Cycles = 6; cpu.Tick ();
			cpu.Tick (); // fire interrupt

			Assert.That (cpu.Pc, Is.EqualTo (0x1c), "PC must jump to OCF1C ISR vector");
		}
	}

	// ── ATtiny85 Timer1 + MmioController hook chaining ───────────────────────
	[TestFixture]
	public class Tiny85Timer1
	{
		// ATtiny85 Timer1 register addresses (from ATtiny85Simulation.cs)
		const int TCCR1  = 0x30;   // single control register (CS bits)
		const int TCNT1  = 0x2F;
		const int TIFR   = 0x58;   // shared with Timer0
		const int TIMSK  = 0x59;   // shared with Timer0
		const int TOV1   = 0x10;   // TIFR bit 4
		const int TOIE1  = 0x10;   // TIMSK bit 4

		private static AvrTimerConfig MakeTiny85Timer1Config ()
		{
			return new AvrTimerConfig (
				bits: 8,
				dividers: [0, 1, 2, 4, 8, 16, 32, 64],
				captureInterrupt:     0,
				comparatorAInterrupt: 0x03,
				comparatorBInterrupt: 0x09,
				comparatorCInterrupt: 0,
				overflowInterrupt:    0x04,
				tccra: 0x00,
				tccrb: TCCR1,
				tccrc: 0,
				tcnt:  TCNT1,
				ocra:  0x2E,
				ocrb:  0x2B,
				ocrc:  0,
				icr:   0,
				timsk: TIMSK,
				tifr:  TIFR,
				comparatorPortA: 0, comparatorPinA: 0,
				comparatorPortB: 0, comparatorPinB: 0,
				comparatorPortC: 0, comparatorPinC: 0,
				externalClockPort: 0, externalClockPin: 0,
				tov:  TOV1,  ocfa: 0x40, ocfb: 0x20, ocfc: 0,
				toie: TOIE1, ociea: 0x40, ocieb: 0x20, ociec: 0);
		}

		[Test (Description = "ATtiny85 Timer1 counts up with prescaler /1 (CS10=1)")]
		public void Timer1_Counts ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer1 = new AvrTimer (cpu, MakeTiny85Timer1Config ());

			cpu.WriteData (TCCR1, 1); // CS10 = /1
			cpu.Cycles = 1; cpu.Tick ();
			cpu.Cycles = 5; cpu.Tick ();

			var tcnt = cpu.ReadData (TCNT1);
			Assert.That (tcnt, Is.GreaterThan (0), "ATtiny85 Timer1 must count when enabled");
		}

		[Test (Description = "ATtiny85 Timer0 and Timer1 can share TIFR/TIMSK without clobbering each other (hook chaining)")]
		public void SharedTifrTimsk_BothTimersWork ()
		{
			// This tests the MmioController write-hook chaining: both timers register
			// write hooks for the same TIFR/TIMSK registers.  Without chaining the
			// second registration overwrites the first and only one timer responds.
			const int TCCR0B = 0x53;
			const int TCNT0  = 0x52;
			const int TOV0   = 0x02;  // TIFR bit 1

			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);

			// ATtiny85 Timer0 config (uses same TIFR/TIMSK as Timer1)
			var timer0Config = AvrTimer.Timer0Config.CreateNew (
				tov:  TOV0, ocfa: 0x10, ocfb: 0x08,
				toie: TOV0, ociea: 0x10, ocieb: 0x08,
				tccra: 0x4A, tccrb: (byte)TCCR0B, tcnt: (byte)TCNT0,
				tifr: TIFR, timsk: TIMSK);

			var timer0 = new AvrTimer (cpu, timer0Config);
			var timer1 = new AvrTimer (cpu, MakeTiny85Timer1Config ());

			// Start both timers with prescaler /1
			cpu.WriteData (TCCR0B, 1);
			cpu.WriteData (TCCR1,  1);

			cpu.Cycles = 1; cpu.Tick ();
			cpu.Cycles = 10; cpu.Tick ();

			var tcnt0 = cpu.ReadData (TCNT0);
			var tcnt1 = cpu.ReadData (TCNT1);

			Assert.That (tcnt0, Is.GreaterThan (0), "Timer0 must count with shared TIFR/TIMSK");
			Assert.That (tcnt1, Is.GreaterThan (0), "Timer1 must count with shared TIFR/TIMSK");
		}

		[Test (Description = "ATtiny85 Timer1 overflow sets TOV1 in shared TIFR and fires interrupt")]
		public void Timer1_OverflowInterrupt ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);
			var timer1 = new AvrTimer (cpu, MakeTiny85Timer1Config ());

			// Start just before overflow
			cpu.WriteData (TCNT1, 0xFF);
			cpu.WriteData (TCCR1, 1); // /1
			cpu.Cycles = 1; cpu.Tick ();

			// TOV1 should be set after overflow
			cpu.Cycles = 2; cpu.Tick ();
			cpu.ReadData (TCNT1);

			Assert.That (cpu.Mmio.Data[TIFR] & TOV1, Is.EqualTo (TOV1),
				"TOV1 must be set in shared TIFR after Timer1 overflow");
		}

		[Test (Description = "MmioController chains write hooks: second registration does not overwrite first")]
		public void MmioController_HookChaining ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x1000]);

			var calls = new List<int>();
			cpu.Mmio.RegisterWrite (0x50, (v, o, a, m) => { calls.Add(1); return false; });
			cpu.Mmio.RegisterWrite (0x50, (v, o, a, m) => { calls.Add(2); return false; });

			cpu.WriteData (0x50, 0xFF);

			Assert.That (calls, Is.EquivalentTo (new[] { 1, 2 }),
				"Both registered write hooks must be called (hook chaining)");
		}
	}

	[TestFixture]
	public class Timer5
	{
		// ATmega2560 Timer5 register addresses (data memory / extended I/O)
		const ushort TCCR5A = 0x120;
		const ushort TCCR5B = 0x121;
		const ushort TCNT5L = 0x124;
		const ushort TCNT5H = 0x125;
		const ushort OCR5AL = 0x128;
		const ushort ICR5L  = 0x126;
		const ushort ICR5H  = 0x127;
		const int    TIFR5  = 0x3A;
		const int    TIMSK5 = 0x73;
		const int    SREG   = 95;

		// TIFR5 / TIMSK5 bit masks
		const int TOV5  = 1;     // bit 0 — timer overflow flag/enable
		const int TOIE5 = 1;     // bit 0
		const int OCFA5 = 2;     // bit 1
		const int ICF5  = 0x20;  // bit 5
		const int ICIE5 = 0x20;  // bit 5

		// ATmega2560 Timer5 overflow vector (0-indexed, 4-byte entries)
		// vector 50 × 4 = 200 = 0xC8
		const int Timer5OvfVector = 0xC8;

		// Build the Timer5 config (same as in ArduinoMegaSimulation)
		private static AvrTimerConfig MakeTimer5Config () =>
			new AvrTimerConfig (
				bits:                 16,
				dividers:             AvrTimer.Timer01Dividers,
				captureInterrupt:     0xB8,
				comparatorAInterrupt: 0xBC,
				comparatorBInterrupt: 0xC0,
				comparatorCInterrupt: 0xC4,
				overflowInterrupt:    Timer5OvfVector,
				tccra: TCCR5A, tccrb: TCCR5B, tccrc: 0x122,
				tcnt:  TCNT5L, icr:   ICR5L,
				ocra:  OCR5AL, ocrb:  0x12A, ocrc: 0x12C,
				timsk: TIMSK5, tifr:  TIFR5,
				tov: TOV5, ocfa: OCFA5, ocfb: 4, ocfc: 8,
				toie: TOIE5, ociea: 2, ocieb: 4, ociec: 8,
				icf:  ICF5,  icie:  ICIE5);

		[Test (Description = "ATmega2560 Timer5 counts up with /1 prescaler")]
		public void Timer5_Counts ()
		{
			// ATmega2560 flash size triggers 22-bit PC
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x20000]);
			var timer = new AvrTimer (cpu, MakeTimer5Config ());

			cpu.WriteData (TCCR5B, 1); // CS50 = /1
			cpu.Cycles = 1; cpu.Tick ();
			cpu.Cycles = 5; cpu.Tick ();

			// Read 16-bit TCNT5 (little-endian — low byte first)
			var low  = (int)cpu.Mmio.Data[TCNT5L];
			var high = (int)cpu.Mmio.Data[TCNT5H];
			cpu.ReadData (TCNT5L); // trigger ReadTcnt hook to refresh
			var tcnt = cpu.Mmio.DataView.GetUint16 (TCNT5L, true);

			Assert.That (tcnt, Is.GreaterThan (0), "Timer5 must count when enabled with /1 prescaler");
		}

		[Test (Description = "ATmega2560 Timer5 overflow sets TOV5 and jumps to overflow vector")]
		public void Timer5_OverflowInterrupt ()
		{
			var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x20000]);
			var timer = new AvrTimer (cpu, MakeTimer5Config ());

			// Set TCNT5 = 0xFFFF via proper 16-bit write sequence (high byte → temp register, low byte → WriteTcnt)
			cpu.WriteData (TCNT5H, 0xFF); // → _highByteTemp = 0xFF
			cpu.WriteData (TCNT5L, 0xFF); // → WriteTcnt: _tcntNext = 0xFFFF, _tcntUpdated = true

			cpu.WriteData (TCCR5B, 1);    // CS50 = /1; starts timer, schedules count at cycle 1

			// Enable global interrupts and TOIE5 (direct write to data memory is enough for SetInterruptFlag)
			cpu.Mmio.Data[SREG]   = 0x80;
			cpu.Mmio.Data[TIMSK5] = TOIE5;

			// Tick 1: Count() applies _tcntNext=0xFFFF (no overflow — tcntUpdated takes priority)
			cpu.Cycles = 1; cpu.Tick ();
			// Tick 2: Count() increments 0xFFFF → overflow → TOV5 set → interrupt dispatched → PC = 0xC8
			cpu.Cycles = 2; cpu.Tick ();
			cpu.ReadData (TCNT5L);

			Assert.Multiple (() =>
			{
				Assert.That (cpu.Mmio.Data[TIFR5] & TOV5, Is.EqualTo (0),
					"TOV5 should be cleared after interrupt dispatch");
				Assert.That (cpu.Pc, Is.EqualTo (Timer5OvfVector),
					"PC must jump to Timer5 overflow vector");
			});
		}

		[Test (Description = "ATmega2560 Timer5 input capture latches TCNT5 into ICR5 and sets ICF5")]
		public void Timer5_InputCapture ()
		{
			var cpu  = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x20000]);
			var timer = new AvrTimer (cpu, MakeTimer5Config ());

			cpu.WriteData (TCCR5B, 1); // /1 prescaler
			cpu.Cycles = 1; cpu.Tick ();
			cpu.Cycles = 10; cpu.Tick ();
			cpu.ReadData (TCNT5L); // refresh

			var tcntBefore = cpu.Mmio.DataView.GetUint16 (TCNT5L, true);

			timer.TriggerCapture ();

			var icr = cpu.Mmio.DataView.GetUint16 (ICR5L, true);

			Assert.Multiple (() =>
			{
				Assert.That (icr, Is.EqualTo (tcntBefore), "ICR5 must latch TCNT5 on TriggerCapture");
				Assert.That (cpu.Mmio.Data[TIFR5] & ICF5, Is.EqualTo (ICF5), "ICF5 must be set after capture");
			});
		}
	}
}
