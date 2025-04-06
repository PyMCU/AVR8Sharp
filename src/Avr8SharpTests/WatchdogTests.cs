namespace AVr8SharpTests;

[TestFixture]
public class Watchdog
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
	
	[Test(Description = "Should correctly calculate the prescaler from WDTCSR")]
	public void SetPrescaler()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
		var clock = new AVR8Sharp.Peripherals.AvrClock(cpu, 16_000_000, AVR8Sharp.Peripherals.AvrClock.ClockConfig);
		var watchdog = new AVR8Sharp.Peripherals.AvrWatchdog(cpu, AVR8Sharp.Peripherals.AvrWatchdog.WatchdogConfig, clock);
		
		cpu.WriteData (WDTCSR, WDCE | WDE);
		cpu.WriteData (WDTCSR, 0);
		
		Assert.That(watchdog.Prescaler, Is.EqualTo(2048));
		
		cpu.WriteData (WDTCSR, WDP2 | WDP1 | WDP0);
		
		Assert.That(watchdog.Prescaler, Is.EqualTo(256 * 1024));
		
		cpu.WriteData (WDTCSR, WDP3 | WDP0);
		
		Assert.That(watchdog.Prescaler, Is.EqualTo(1024 * 1024));
	}
	
	[Test(Description = "Should not change the prescaler unless WDCE is set")]
	public void SetPrescalerWithoutWDCE()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
		var clock = new AVR8Sharp.Peripherals.AvrClock(cpu, 16_000_000, AVR8Sharp.Peripherals.AvrClock.ClockConfig);
		var watchdog = new AVR8Sharp.Peripherals.AvrWatchdog(cpu, AVR8Sharp.Peripherals.AvrWatchdog.WatchdogConfig, clock);
		
		cpu.WriteData (WDTCSR, 0);
		Assert.That(watchdog.Prescaler, Is.EqualTo(2048));
		
		cpu.WriteData (WDTCSR, WDP2 | WDP1 | WDP0);
		Assert.That(watchdog.Prescaler, Is.EqualTo(2048));
		
		cpu.WriteData (WDTCSR, WDCE | WDE);
		cpu.Cycles += 5; // WDCE should expire after 4 cycles
		cpu.WriteData (WDTCSR, WDP2 | WDP1 | WDP0);
		Assert.That(watchdog.Prescaler, Is.EqualTo(2048));
	}
	
	[Test(Description = "Should reset the CPU when the timer expires")]
	public void ResetOnTimeout()
	{
		var program = Utils.AsmProgram (@$"
    ; register addresses
    _REPLACE WDTCSR, {WDTCSR}

    ; Setup watchdog
    ldi r16, {WDE | WDCE}
    sts WDTCSR, r16
    ldi r16, {WDE}
    sts WDTCSR, r16
    
    nop

    break
");
		
		var cpu = new AVR8Sharp.Cpu.Cpu(program.Program);
		var clock = new AVR8Sharp.Peripherals.AvrClock(cpu, 16_000_000, AVR8Sharp.Peripherals.AvrClock.ClockConfig);
		var watchdog = new AVR8Sharp.Peripherals.AvrWatchdog(cpu, AVR8Sharp.Peripherals.AvrWatchdog.WatchdogConfig, clock);
		var runner = new TestProgramRunner(cpu);
		
		// Setup: enable watchdog timer
		runner.RunInstructions(4);
		Assert.That(watchdog.Enabled, Is.True);
		
		// Now we skip 8ms. Watchdog shouldn't fire, yet
		cpu.Cycles += 16000 * 8;
		runner.RunInstructions(1);
		
		// Now we skip an extra 8ms. Watchdog should fire and reset!
		cpu.Cycles += 16000 * 8;
		cpu.Tick();
        Assert.Multiple(() =>
        {
            Assert.That(cpu.PC, Is.EqualTo(0));
            Assert.That(cpu.ReadData(MCUSR), Is.EqualTo(WDRF));
        });
    }

	[Test (Description = "Should extend the watchdog timeout when executing a WDR instruction")]
	public void ExtendTimeout ()
	{
		var program = Utils.AsmProgram (@$"
    ; register addresses
    _REPLACE WDTCSR, {WDTCSR}

    ; Setup watchdog
    ldi r16, {WDE | WDCE}
    sts WDTCSR, r16
    ldi r16, {WDE}
    sts WDTCSR, r16
    
    wdr
    nop

    break");
		
		var cpu = new AVR8Sharp.Cpu.Cpu(program.Program);
		var clock = new AVR8Sharp.Peripherals.AvrClock(cpu, 16_000_000, AVR8Sharp.Peripherals.AvrClock.ClockConfig);
		var watchdog = new AVR8Sharp.Peripherals.AvrWatchdog(cpu, AVR8Sharp.Peripherals.AvrWatchdog.WatchdogConfig, clock);
		var runner = new TestProgramRunner(cpu);
		
		// Setup: enable watchdog timer
		runner.RunInstructions(4);
		Assert.That(watchdog.Enabled, Is.True);
		
		// Now we skip 8ms. Watchdog shouldn't fire, yet
		cpu.Cycles += 16000 * 8;
		runner.RunInstructions(1);
		Assert.That(cpu.PC, Is.Not.EqualTo(0));
		
		// Now we skip an extra 8ms. We extended the timeout with WDR, so watchdog won't fire yet
		cpu.Cycles += 16000 * 8;
		runner.RunInstructions(1);
		Assert.That(cpu.PC, Is.Not.EqualTo(0));
		
		// Finally, another 8ms bring us to 16ms since last WDR, and watchdog should fire
		cpu.Cycles += 16000 * 8;
		cpu.Tick();
		Assert.That(cpu.PC, Is.EqualTo(0));
	}
	
	[Test (Description = "Should fire an interrupt when the watchdog expires and WDIE is set")]
	public void InterruptOnTimeout ()
	{
		var program = Utils.AsmProgram (@$"
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
");
		
		var cpu = new AVR8Sharp.Cpu.Cpu(program.Program);
		var clock = new AVR8Sharp.Peripherals.AvrClock(cpu, 16_000_000, AVR8Sharp.Peripherals.AvrClock.ClockConfig);
		var watchdog = new AVR8Sharp.Peripherals.AvrWatchdog(cpu, AVR8Sharp.Peripherals.AvrWatchdog.WatchdogConfig, clock);
		var runner = new TestProgramRunner(cpu);
		
		runner.RunInstructions (4);
		Assert.That (watchdog.Enabled, Is.True);
		
		// Now we skip 8ms. Watchdog shouldn't fire, yet
		cpu.Cycles += 16000 * 8;
		runner.RunInstructions (1);
		
		// Now we skip an extra 8ms. Watchdog should fire and jump to the interrupt handler
		cpu.Cycles += 16000 * 8;
		runner.RunInstructions (1);
		
		Assert.That (cpu.PC, Is.EqualTo(INT_WDT));
		// The watchdog timer should also clean the WDIE bit, so next timeout will reset the MCU.
		Assert.That ((cpu.ReadData (WDTCSR) & WDIE), Is.EqualTo(0));
	}

	[Test (Description = "Should not reset the CPU if the watchdog has been disabled")]
	public void NoResetIfDisabled ()
	{
		var program = Utils.AsmProgram (@$"
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
");
		
		var cpu = new AVR8Sharp.Cpu.Cpu(program.Program);
		var clock = new AVR8Sharp.Peripherals.AvrClock(cpu, 16_000_000, AVR8Sharp.Peripherals.AvrClock.ClockConfig);
		var watchdog = new AVR8Sharp.Peripherals.AvrWatchdog(cpu, AVR8Sharp.Peripherals.AvrWatchdog.WatchdogConfig, clock);
		var runner = new TestProgramRunner(cpu);
		
		// Setup: enable watchdog timer
		runner.RunInstructions(4);
		Assert.That(watchdog.Enabled, Is.True);
		
		// Now we skip 8ms. Watchdog shouldn't fire, yet. We disable it.
		cpu.Cycles += 16000 * 8;
		runner.RunInstructions(4);
		
		// Now we skip an extra 20ms. Watchdog shouldn't reset!
		cpu.Cycles += 16000 * 20;
		runner.RunInstructions(1);
		Assert.That(cpu.PC, Is.Not.EqualTo(0));
		Assert.That(cpu.ReadData(R20), Is.EqualTo(55)); // assert that `ldi r20, 55` ran
	}
}
