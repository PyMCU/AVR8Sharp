using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

[TestFixture]
public class Watchdog : AvrTestBase
{
	const int R20 = 20;

	const int MCUSR = 0x54;
	const int WDRF = 1 << 3;

	const int WDTCSR = 0x60;
	const int WDP0 = 1 << 0;
	const int WDP1 = 1 << 1;
	const int WDP2 = 1 << 2;
	const int WDE = 1 << 3;
	const int WDCE = 1 << 4;
	const int WDP3 = 1 << 5;
	const int WDIE = 1 << 6;

	const int INT_WDT = 0xc;

	private AvrWatchdog _watchdog;

	protected override void SetupPeripherals()
	{
		_watchdog = new AvrWatchdog(Cpu, AvrWatchdog.WatchdogConfig, Clock);
	}
	
	[Test(Description = "Should correctly calculate the prescaler from WDTCSR")]
	public void SetPrescaler()
	{
		Cpu.WriteData (WDTCSR, WDCE | WDE);
		Cpu.WriteData (WDTCSR, 0);
		
		Assert.That(_watchdog.Prescaler, Is.EqualTo(2048));
		
		Cpu.WriteData (WDTCSR, WDP2 | WDP1 | WDP0);
		
		Assert.That(_watchdog.Prescaler, Is.EqualTo(256 * 1024));
		
		Cpu.WriteData (WDTCSR, WDP3 | WDP0);
		
		Assert.That(_watchdog.Prescaler, Is.EqualTo(1024 * 1024));
	}
	
	[Test(Description = "Should not change the prescaler unless WDCE is set")]
	public void SetPrescalerWithoutWDCE()
	{
		Cpu.WriteData (WDTCSR, 0);
		Assert.That(_watchdog.Prescaler, Is.EqualTo(2048));
		
		Cpu.WriteData (WDTCSR, WDP2 | WDP1 | WDP0);
		Assert.That(_watchdog.Prescaler, Is.EqualTo(2048));
		
		Cpu.WriteData (WDTCSR, WDCE | WDE);
		Cpu.Cycles += 5; // WDCE should expire after 4 cycles
		Cpu.WriteData (WDTCSR, WDP2 | WDP1 | WDP0);
		Assert.That(_watchdog.Prescaler, Is.EqualTo(2048));
	}
	
	[Test(Description = "Should reset the CPU when the timer expires")]
	public void ResetOnTimeout()
	{
		var program = new AsmProgram (@$"
    ; register addresses
    _REPLACE WDTCSR, {WDTCSR}

    ; Setup watchdog
    ldi r16, {WDE | WDCE}
    sts WDTCSR, r16
    ldi r16, {WDE}
    sts WDTCSR, r16
    
    nop

    break
").Compile();
		
		Cpu.LoadProgram(program.Program);
		var runner = new TestProgramRunner(Cpu);
		
		// Setup: enable watchdog timer
		runner.RunInstructions(4);
		Assert.That(_watchdog.Enabled, Is.True);
		
		// Now we skip 8ms. Watchdog shouldn't fire, yet
		Cpu.Cycles += 16000 * 8;
		runner.RunInstructions(1);
		
		// Now we skip an extra 8ms. Watchdog should fire and reset!
		Cpu.Cycles += 16000 * 8;
		Cpu.Tick();
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Pc, Is.EqualTo(0));
            Assert.That(Cpu.ReadData(MCUSR), Is.EqualTo(WDRF));
        });
    }

	[Test (Description = "Should extend the watchdog timeout when executing a WDR instruction")]
	public void ExtendTimeout ()
	{
		var program = new AsmProgram (@$"
    ; register addresses
    _REPLACE WDTCSR, {WDTCSR}

    ; Setup watchdog
    ldi r16, {WDE | WDCE}
    sts WDTCSR, r16
    ldi r16, {WDE}
    sts WDTCSR, r16
    
    wdr
    nop

    break").Compile();
		
		Cpu.LoadProgram(program.Program);
		var runner = new TestProgramRunner(Cpu);
		
		// Setup: enable watchdog timer
		runner.RunInstructions(4);
		Assert.That(_watchdog.Enabled, Is.True);
		
		// Now we skip 8ms. Watchdog shouldn't fire, yet
		Cpu.Cycles += 16000 * 8;
		runner.RunInstructions(1);
		Assert.That(Cpu.Pc, Is.Not.EqualTo(0));
		
		// Now we skip an extra 8ms. We extended the timeout with WDR, so watchdog won't fire yet
		Cpu.Cycles += 16000 * 8;
		runner.RunInstructions(1);
		Assert.That(Cpu.Pc, Is.Not.EqualTo(0));
		
		// Finally, another 8ms bring us to 16ms since last WDR, and watchdog should fire
		Cpu.Cycles += 16000 * 8;
		Cpu.Tick();
		Assert.That(Cpu.Pc, Is.EqualTo(0));
	}
	
	[Test (Description = "Should fire an interrupt when the watchdog expires and WDIE is set")]
	public void InterruptOnTimeout ()
	{
		var program = new AsmProgram (@$"
    ; register addresses
    _REPLACE WDTCSR, {WDTCSR}

    ; Setup watchdog
    ldi r16, {WDE | WDCE}
    sts WDTCSR, r16
    ldi r16, {WDE | WDIE}
    sts WDTCSR, r16
    
    nop
    sei

    break
").Compile();
		
		Cpu.LoadProgram(program.Program);
		var runner = new TestProgramRunner(Cpu);
		
		runner.RunInstructions (4);
		Assert.That (_watchdog.Enabled, Is.True);
		
		// Now we skip 8ms. Watchdog shouldn't fire, yet
		Cpu.Cycles += 16000 * 8;
		runner.RunInstructions (1);
		
		// Now we skip an extra 8ms. Watchdog should fire and jump to the interrupt handler
		Cpu.Cycles += 16000 * 8;
		runner.RunInstructions (1);
		
		Assert.That (Cpu.Pc, Is.EqualTo(INT_WDT));
		// The watchdog timer should also clean the WDIE bit, so next timeout will reset the MCU.
		Assert.That ((Cpu.ReadData (WDTCSR) & WDIE), Is.EqualTo(0));
	}

	[Test (Description = "Should not reset the CPU if the watchdog has been disabled")]
	public void NoResetIfDisabled ()
	{
		var program = new AsmProgram (@$"
    ; register addresses
    _REPLACE WDTCSR, {WDTCSR}

    ; Setup watchdog
    ldi r16, {WDE | WDCE}
    sts WDTCSR, r16
    ldi r16, {WDE}
    sts WDTCSR, r16
    
    ; disable watchdog
    ldi r16, {WDE | WDCE}
    sts WDTCSR, r16
    ldi r16, 0
    sts WDTCSR, r16

    ldi r20, 55

    break
").Compile();
		
		Cpu.LoadProgram(program.Program);
		var runner = new TestProgramRunner(Cpu);
		
		// Setup: enable watchdog timer
		runner.RunInstructions(4);
		Assert.That(_watchdog.Enabled, Is.True);
		
		// Now we skip 8ms. Watchdog shouldn't fire, yet. We disable it.
		Cpu.Cycles += 16000 * 8;
		runner.RunInstructions(4);
		
		// Now we skip an extra 20ms. Watchdog shouldn't reset!
		Cpu.Cycles += 16000 * 20;
		runner.RunInstructions(1);
		Assert.That(Cpu.Pc, Is.Not.EqualTo(0));
		Assert.That(Cpu.ReadData(R20), Is.EqualTo(55)); // assert that `ldi r20, 55` ran
	}

	[Test(Description = "Should not schedule duplicate CheckWatchdog events on consecutive WDTCSR writes")]
    public void NoDuplicateEventsOnConsecutiveWrites()
    {
       var program = new AsmProgram (@$"
    _REPLACE WDTCSR, {WDTCSR}

    ldi r16, {WDE | WDCE}
    sts WDTCSR, r16
    ldi r16, {WDE}
    sts WDTCSR, r16
    
    ldi r16, {WDE | WDCE}
    sts WDTCSR, r16
    ldi r16, {WDE}
    sts WDTCSR, r16
    
    ldi r16, {WDE | WDCE}
    sts WDTCSR, r16
    ldi r16, {WDE}
    sts WDTCSR, r16

    nop
    break
").Compile();

       Cpu.LoadProgram(program.Program);
       var runner = new TestProgramRunner(Cpu);

       runner.RunInstructions(12);
       Assert.That(_watchdog.Enabled, Is.True);

       Cpu.Cycles += 16000 * 8;
       runner.RunInstructions(1);
       Assert.That(Cpu.Pc, Is.Not.EqualTo(0), "CPU should not reset early from duplicate old events");

       Cpu.Cycles += 16000 * 8;
       Cpu.Tick();

       Assert.Multiple(() =>
       {
           Assert.That(Cpu.Pc, Is.EqualTo(0), "CPU should reset on timeout");
           Assert.That(Cpu.ReadData(MCUSR), Is.EqualTo(WDRF), "WDRF flag must be set on Watchdog reset");
       });
    }

	[Test(Description = "Should reschedule and fire earlier if the prescaler is shortened while already running")]
	public void RescheduleOnPrescalerShortened()
	{
		// Configure the watchdog to run at 128ms (WDP1 | WDP0)
		// 128ms * 16,000 cycles/ms = 2,048,000 cycles
		Cpu.Pc = 0x10;
		Cpu.WriteData(WDTCSR, WDCE | WDE);
		Cpu.WriteData(WDTCSR, WDE | WDP1 | WDP0);

		Assert.Multiple(() => {
			Assert.That(_watchdog.Enabled, Is.True);
			Assert.That(_watchdog.Prescaler, Is.EqualTo(16384), "Prescaler should be 128ms equivalent");
		});

		// Advance 32ms (512,000 cycles).
		// The Watchdog should not trigger yet (still ~1.5 million cycles left)
		Cpu.Cycles += 16000 * 32;
		Cpu.Tick();
		Assert.That(Cpu.Pc, Is.Not.EqualTo(0), "CPU should NOT reset at 32ms");

		// Lower the prescaler to minimum (16ms)
		// 16ms * 16,000 cycles/ms = 256,000 cycles
		Cpu.WriteData(WDTCSR, WDCE | WDE);
		Cpu.WriteData(WDTCSR, WDE); // WDP = 0 (16ms)

		// Advance 20ms.
		// With the previous bug: The event would still be scheduled for the original 128ms (fails the test).
		// With the fix: The event is rescheduled to trigger at 16ms from the last write.
		Cpu.Cycles += 16000 * 20;
		Cpu.Tick();

		Assert.Multiple(() =>
		{
			Assert.That(Cpu.Pc, Is.EqualTo(0), "CPU should have reset because 20ms > new 16ms timeout");
			Assert.That(Cpu.ReadData(MCUSR), Is.EqualTo(WDRF), "WDRF flag must be set");
		});
	}
}
