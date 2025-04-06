using AVR8Sharp.Peripherals;

namespace AVr8SharpTests;

[TestFixture]
public class Clock
{
	// Clock Registers
	const int CLKPC = 0x61;

	// Register bit names
	const int CLKPCE = 128;

	[Test (Description = "Should set the prescaler when double-writing CLKPC")]
	public void Prescaler ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[0x1000]);
		var clock = new AvrClock (cpu, 16_000_000, AvrClock.ClockConfig);
		
		cpu.WriteData (CLKPC, CLKPCE);
		cpu.WriteData (CLKPC, 3); // Divide by 8 (16MHz / 8 = 2MHz)
        Assert.Multiple(() =>
        {
            Assert.That(clock.Frequency, Is.EqualTo(2_000_000));
            Assert.That(cpu.ReadData(CLKPC), Is.EqualTo(3));
        });
    }
	
	[Test (Description = "Should not update the prescaler if CLKPCE was not set CLKPC")]
	public void NoPrescaler ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[0x1000]);
		var clock = new AvrClock (cpu, 16_000_000, AvrClock.ClockConfig);
		
		cpu.WriteData (CLKPC, 3); // Divide by 8 (16MHz / 8 = 2MHz)
		Assert.Multiple(() =>
		{
			Assert.That(clock.Frequency, Is.EqualTo(16_000_000)); // Default frequency
			Assert.That(cpu.ReadData(CLKPC), Is.EqualTo(0));
		});
	}
	
	[Test (Description = "Should not update the prescaler if more than 4 cycles passed since setting CLKPCE")]
	public void NoPrescalerAfter4Cycles ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[0x1000]);
		var clock = new AvrClock (cpu, 16_000_000, AvrClock.ClockConfig);
		
		cpu.WriteData (CLKPC, CLKPCE);
		cpu.Cycles += 6;
		cpu.WriteData (CLKPC, 3); // Divide by 8 (16MHz / 8 = 2MHz)
		Assert.Multiple(() =>
		{
			Assert.That(clock.Frequency, Is.EqualTo(16_000_000)); // Default frequency
			Assert.That(cpu.ReadData(CLKPC), Is.EqualTo(0));
		});
	}
	
	[Test (Description = "Should return the current prescaler value")]
	public void PrescalerValue ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[0x1000]);
		var clock = new AvrClock (cpu, 16_000_000, AvrClock.ClockConfig);
		
		cpu.WriteData (CLKPC, CLKPCE);
		cpu.WriteData (CLKPC, 5); // Divide by 32 (16MHz / 32 = 500kHz)
		cpu.Cycles = 16_000_000; // 1 second
		
		Assert.That(clock.Prescaler, Is.EqualTo(32));
	}
	
	[Test (Description = "Should return current number of milliseconds, derived from base freq + prescaler")]
	public void TimeMillis ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[0x1000]);
		var clock = new AvrClock (cpu, 16_000_000, AvrClock.ClockConfig);
		
		cpu.WriteData (CLKPC, CLKPCE);
		cpu.WriteData (CLKPC, 2); // Divide by 4 (16MHz / 4 = 4MHz)
		cpu.Cycles = 16_000_000; // 1 second
		
		Assert.That(clock.TimeMillis, Is.EqualTo(4000)); // 4 seconds
	}
	
	[Test (Description = "Should return current number of microseconds, derived from base freq + prescaler")]
	public void TimeMicros ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[0x1000]);
		var clock = new AvrClock (cpu, 16_000_000, AvrClock.ClockConfig);
		
		cpu.WriteData (CLKPC, CLKPCE);
		cpu.WriteData (CLKPC, 2); // Divide by 4 (16MHz / 4 = 4MHz)
		cpu.Cycles = 16_000_000; // 1 second
		
		Assert.That(clock.TimeMicros, Is.EqualTo(4_000_000)); // 4 seconds
	}
	
	[Test (Description = "Should return current number of nanoseconds, derived from base freq + prescaler")]
	public void TimeNanos ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[0x1000]);
		var clock = new AvrClock (cpu, 16_000_000, AvrClock.ClockConfig);
		
		cpu.WriteData (CLKPC, CLKPCE);
		cpu.WriteData (CLKPC, 2); // Divide by 4 (16MHz / 4 = 4MHz)
		cpu.Cycles = 16_000_000; // 1 second
		
		Assert.That(clock.TimeNanos, Is.EqualTo(4_000_000_000)); // 4 seconds
	}
	
	[Test (Description = "Should correctly calculate time when changing the prescale value at runtime")]
	public void TimeMillisAfterPrescaleChange ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[0x1000]);
		var clock = new AvrClock (cpu, 16_000_000, AvrClock.ClockConfig);
		
		cpu.Cycles = 16_000_000; // Run for 1 second at 16MHz
		cpu.WriteData (CLKPC, CLKPCE);
		cpu.WriteData (CLKPC, 2); // Divide by 4 (16MHz / 4 = 4MHz)
		cpu.Cycles += 2 * 4_000_000; // Run for 2 seconds at 4MHz
		
		Assert.That(clock.TimeMillis, Is.EqualTo(3000)); // 3 seconds (1s at 16MHz + 2s at 4MHz)
		
		cpu.WriteData (CLKPC, CLKPCE);
		cpu.WriteData (CLKPC, 1); // Divide by 2 (16MHz / 2 = 8MHz)
		cpu.Cycles += (int)(0.5 * 8_000_000); // Run for 0.5 seconds at 8MHz
		
		Assert.That(clock.TimeMillis, Is.EqualTo(3500)); // 3.5 seconds (1s at 16MHz + 2s at 4MHz + 0.5s at 8MHz)
	}
}
