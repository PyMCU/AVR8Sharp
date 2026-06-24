using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

[TestFixture]
public class Clock : AvrTestBase
{
	// Clock Registers
	const int CLKPC = 0x61;

	// Register bit names
	const int CLKPCE = 128;

	private AvrClock _clock;

	protected override void SetupPeripherals()
	{
		_clock = new AvrClock (Cpu, 16_000_000, AvrClock.ClockConfig);
	}

	[Test (Description = "Should set the prescaler when double-writing CLKPC")]
	public void Prescaler ()
	{
		Cpu.WriteData (CLKPC, CLKPCE);
		Cpu.WriteData (CLKPC, 3); // Divide by 8 (16MHz / 8 = 2MHz)
        Assert.Multiple(() =>
        {
            Assert.That(_clock.Frequency, Is.EqualTo(2_000_000));
            Assert.That(Cpu.ReadData(CLKPC), Is.EqualTo(3));
        });
    }
	
	[Test (Description = "Should not update the prescaler if CLKPCE was not set CLKPC")]
	public void NoPrescaler ()
	{
		Cpu.WriteData (CLKPC, 3); // Divide by 8 (16MHz / 8 = 2MHz)
		Assert.Multiple(() =>
		{
			Assert.That(_clock.Frequency, Is.EqualTo(16_000_000)); // Default frequency
			Assert.That(Cpu.ReadData(CLKPC), Is.EqualTo(0));
		});
	}
	
	[Test (Description = "Should not update the prescaler if more than 4 cycles passed since setting CLKPCE")]
	public void NoPrescalerAfter4Cycles ()
	{
		Cpu.WriteData (CLKPC, CLKPCE);
		Cpu.Cycles += 6;
		Cpu.WriteData (CLKPC, 3); // Divide by 8 (16MHz / 8 = 2MHz)
		Assert.Multiple(() =>
		{
			Assert.That(_clock.Frequency, Is.EqualTo(16_000_000)); // Default frequency
			Assert.That(Cpu.ReadData(CLKPC), Is.EqualTo(0));
		});
	}
	
	[Test (Description = "Reserved CLKPS values (9-15) follow the empirical 328P pattern 2^(index-8)")]
	public void ReservedPrescalerValues ()
	{
		// Index 9 is datasheet-reserved; on real 328P silicon it divides by 2. This locks in
		// the documented empirical behaviour for the undefined region.
		Cpu.WriteData (CLKPC, CLKPCE);
		Cpu.WriteData (CLKPC, 9);
		Assert.Multiple(() =>
		{
			Assert.That(_clock.Prescaler, Is.EqualTo(2));
			Assert.That(_clock.Frequency, Is.EqualTo(8_000_000)); // 16MHz / 2
		});

		// Index 15 divides by 128 (2^(15-8)).
		Cpu.WriteData (CLKPC, CLKPCE);
		Cpu.WriteData (CLKPC, 15);
		Assert.That(_clock.Prescaler, Is.EqualTo(128));
	}

	[Test (Description = "Should return the current prescaler value")]
	public void PrescalerValue ()
	{
		Cpu.WriteData (CLKPC, CLKPCE);
		Cpu.WriteData (CLKPC, 5); // Divide by 32 (16MHz / 32 = 500kHz)
		Cpu.Cycles = 16_000_000; // 1 second
		
		Assert.That(_clock.Prescaler, Is.EqualTo(32));
	}
	
	[Test (Description = "Should return current number of milliseconds, derived from base freq + prescaler")]
	public void TimeMillis ()
	{
		Cpu.WriteData (CLKPC, CLKPCE);
		Cpu.WriteData (CLKPC, 2); // Divide by 4 (16MHz / 4 = 4MHz)
		Cpu.Cycles = 16_000_000; // 1 second
		
		Assert.That(_clock.TimeMillis, Is.EqualTo(4000)); // 4 seconds
	}
	
	[Test (Description = "Should return current number of microseconds, derived from base freq + prescaler")]
	public void TimeMicros ()
	{
		Cpu.WriteData (CLKPC, CLKPCE);
		Cpu.WriteData (CLKPC, 2); // Divide by 4 (16MHz / 4 = 4MHz)
		Cpu.Cycles = 16_000_000; // 1 second
		
		Assert.That(_clock.TimeMicros, Is.EqualTo(4_000_000)); // 4 seconds
	}
	
	[Test (Description = "Should return current number of nanoseconds, derived from base freq + prescaler")]
	public void TimeNanos ()
	{
		Cpu.WriteData (CLKPC, CLKPCE);
		Cpu.WriteData (CLKPC, 2); // Divide by 4 (16MHz / 4 = 4MHz)
		Cpu.Cycles = 16_000_000; // 1 second
		
		Assert.That(_clock.TimeNanos, Is.EqualTo(4_000_000_000)); // 4 seconds
	}
	
	[Test (Description = "Should correctly calculate time when changing the prescale value at runtime")]
	public void TimeMillisAfterPrescaleChange ()
	{
		Cpu.Cycles = 16_000_000; // Run for 1 second at 16MHz
		Cpu.WriteData (CLKPC, CLKPCE);
		Cpu.WriteData (CLKPC, 2); // Divide by 4 (16MHz / 4 = 4MHz)
		Cpu.Cycles += 2 * 4_000_000; // Run for 2 seconds at 4MHz
		
		Assert.That(_clock.TimeMillis, Is.EqualTo(3000)); // 3 seconds (1s at 16MHz + 2s at 4MHz)
		
		Cpu.WriteData (CLKPC, CLKPCE);
		Cpu.WriteData (CLKPC, 1); // Divide by 2 (16MHz / 2 = 8MHz)
		Cpu.Cycles += (int)(0.5 * 8_000_000); // Run for 0.5 seconds at 8MHz
		
		Assert.That(_clock.TimeMillis, Is.EqualTo(3500)); // 3.5 seconds (1s at 16MHz + 2s at 4MHz + 0.5s at 8MHz)
	}
}
