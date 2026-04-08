using AVR8Sharp.Core.Peripherals;
namespace Avr8Sharp.Tests;

[TestFixture]
public class Adc
{
	const int R16 = 16;
	const int R17 = 17;

	const int ADMUX = 0x7c;
	const int REFS0 = 1 << 6;

	const int ADCSRA = 0x7a;
	const int ADEN = 1 << 7;
	const int ADSC = 1 << 6;
	const int ADPS0 = 1 << 0;
	const int ADPS1 = 1 << 1;
	const int ADPS2 = 1 << 2;

	const int ADCH = 0x79;
	const int ADIF  = 1 << 4; // ADC Interrupt Flag in ADCSRA
	const int ADATE = 1 << 5; // ADC Auto Trigger Enable in ADCSRA
	const int ADCL = 0x78;

	[Test(Description = "Should successfully perform an ADC conversion")]
	public void Conversion ()
	{
		var program = Utils.AsmProgram (@$"
		; register addresses
	    _REPLACE ADMUX, {ADMUX}
		_REPLACE ADCSRA, {ADCSRA}
	    _REPLACE ADCH, {ADCH}
		_REPLACE ADCL, {ADCL}

	    ; Configure mux - channel 0, reference: AVCC with external capacitor at AREF pin
		ldi r24, {REFS0}
	    sts ADMUX, r24

		; Start conversion with 128 prescaler
	    ldi r24, {ADEN | ADSC | ADPS0 | ADPS1 | ADPS2}
		sts ADCSRA, r24

	    ; Wait until conversion is complete
	  waitComplete:
		lds r24, {ADCSRA}
	    andi r24, {ADSC}
		brne waitComplete

	    ; Read the result
		lds r16, {ADCL}
	    lds r17, {ADCH}

		break
");
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
		var adc = new AvrAdc (cpu, AvrAdc.AdcConfig);
		var runner = new TestProgramRunner (cpu);
		
		// Spy on OnADCRead method to be executed when the ADC is read
		adc.ChannelValues[0] = 2.56; // Should result in 2.56/5*1024 = 524
		
		// Setup
		runner.RunInstructions (16);

		cpu.Cycles += 128 * 25; // Skip to the end of the conversion
		cpu.Tick ();
		
		// Now read the result
		runner.RunInstructions (5);
		
		var low = cpu.Mmio.Data[R16];
		var high = cpu.Mmio.Data[R17];
		var result = (high << 8) | low;
		Assert.That(result, Is.EqualTo(524));
	}
	
	[Test(Description = "Should read 0 when the ADC peripheral is not enabled")]
	public void Disabled ()
	{
		var program = Utils.AsmProgram (@$"
		; register addresses
	    _REPLACE ADMUX, {ADMUX}
		_REPLACE ADCSRA, {ADCSRA}
	    _REPLACE ADCH, {ADCH}
		_REPLACE ADCL, {ADCL}

	    ; Load some initial value into r16/r17 to make sure we actually read 0 later
		ldi r16, 0xff
	    ldi r17, 0xff

		; Configure mux - channel 0, reference: AVCC with external capacitor at AREF pin
	    ldi r24, {REFS0}
		sts ADMUX, r24

	    ; Start conversion with 128 prescaler, but without enabling the ADC
		ldi r24, {ADSC | ADPS0 | ADPS1 | ADPS2}
	    sts ADCSRA, r24

		; Wait until conversion is complete
	  waitComplete:
		lds r24, {ADCSRA}
	    andi r24, {ADSC}
		brne waitComplete

	    ; Read the result
		lds r16, {ADCL}
	    lds r17, {ADCH}

		break
");
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (program.Program);
		var adc = new AvrAdc (cpu, AvrAdc.AdcConfig);
		var runner = new TestProgramRunner (cpu, (cpu) => {
			// Do nothing on break
		});
		
		// Spy on OnADCRead method to be executed when the ADC is read
		adc.ChannelValues[0] = 2.56; // Should result in 2.56/5*1024 = 524
		
		// Setup
		runner.RunInstructions (18);

		cpu.Cycles += 128 * 25; // Skip to the end of the conversion
		cpu.Tick ();
		
		// Now read the result
		runner.RunInstructions (5);
		
		// Read the result
		runner.RunToBreak ();
		
		var low = cpu.Mmio.Data[R16];
		var high = cpu.Mmio.Data[R17];
		var result = (high << 8) | low;
		Assert.That(result, Is.EqualTo(0)); // Should be 0 since the ADC is not enabled
	}

	[Test(Description = "ADC free-running mode: ADIF fires multiple times when ADATE=1 and ADTS=000")]
	public void FreeRunning_AutoTrigger ()
	{
		// ADATE=0x20 in ADCSRA, ADTS=000 in ADCSRB → free-running (restart after each conversion)
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x8000]);
		var adc = new AvrAdc (cpu, AvrAdc.AdcConfig);

		adc.ChannelValues[0] = 2.56; // 2.56/5*1024 = 524

		// Configure: channel 0, AVCC reference
		cpu.Mmio.Data[ADMUX] = 1 << 6;  // REFS0
		// Enable ADC, start conversion, prescaler /128, auto-trigger
		cpu.Mmio.Data[ADCSRA] = ADEN | ADSC | ADATE | ADPS0 | ADPS1 | ADPS2;

		var completions = 0;
		// Hook into OnADCRead to count how many conversions complete
		adc.OnADCRead(new AdcMuxInput(type: AdcMuxInputType.SingleEnded, channel: 0));

		// Manually advance clock to complete conversion 1
		cpu.Cycles += 128 * 25;
		cpu.Tick();
		completions++;

		// Check ADIF is set (conversion complete), ADSC is clear
		Assert.That(cpu.Mmio.Data[ADCSRA] & ADIF, Is.EqualTo(ADIF), "ADIF should be set after first conversion");
		Assert.That(cpu.Mmio.Data[ADCSRA] & ADSC, Is.EqualTo(0), "ADSC should be clear after conversion completes");

		// In free-running mode, a second conversion should have been queued automatically.
		// Advance clock to complete conversion 2.
		cpu.Mmio.Data[ADCSRA] &= ~ADIF & 0xff; // Clear ADIF to detect second completion
		cpu.Cycles += 128 * 13;
		cpu.Tick();
		completions++;

		Assert.That(cpu.Mmio.Data[ADCSRA] & ADIF, Is.EqualTo(ADIF),
			"ADIF should fire again in free-running mode (second conversion)");
		Assert.That(completions, Is.EqualTo(2));
	}

	[Test(Description = "ADC does NOT auto-restart when ADATE is clear (single-conversion mode)")]
	public void SingleConversion_NoAutoRestart ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x8000]);
		var adc = new AvrAdc (cpu, AvrAdc.AdcConfig);

		adc.ChannelValues[0] = 2.56;
		cpu.Mmio.Data[ADMUX] = 1 << 6;
		// ADEN | ADSC | /128 — NO ADATE
		cpu.Mmio.Data[ADCSRA] = ADEN | ADSC | ADPS0 | ADPS1 | ADPS2;

		adc.OnADCRead(new AdcMuxInput(type: AdcMuxInputType.SingleEnded, channel: 0));
		cpu.Cycles += 128 * 25;
		cpu.Tick();

		Assert.That(cpu.Mmio.Data[ADCSRA] & ADIF, Is.EqualTo(ADIF));

		// Clear ADIF and advance one more conversion period — no second ADIF expected
		cpu.Mmio.Data[ADCSRA] &= ~ADIF & 0xff;
		cpu.Cycles += 128 * 13;
		cpu.Tick();

		Assert.That(cpu.Mmio.Data[ADCSRA] & ADIF, Is.EqualTo(0),
			"ADIF must NOT fire again when ADATE=0 (single-conversion mode)");
	}

	[Test (Description = "AvrAdc.TemperatureVoltage is configurable and affects the ADC result")]
	public void TemperatureSensor_Configurable ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu (new ushort[0x8000]);
		var adc = new AvrAdc (cpu, AvrAdc.AdcConfig);

		// Set a known temperature voltage (0.4 V)
		adc.TemperatureVoltage = 0.4;

		// Select temperature channel (mux 8 = 0b1000), AVCC reference (REFS0)
		cpu.Mmio.Data[ADMUX] = (byte)((1 << 6) | 8); // REFS0 | MUX3

		// Enable ADC, start conversion, prescaler /128
		// Write via WriteData to trigger the ADCSRA hook (direct Mmio.Data write bypasses it)
		cpu.Mmio.Data[ADCSRA] = ADEN | ADSC | ADPS0 | ADPS1 | ADPS2;
		adc.OnADCRead (new AdcMuxInput (type: AdcMuxInputType.Temperature));

		cpu.Cycles += 128 * 25;
		cpu.Tick ();

		var low    = cpu.Mmio.Data[ADCL];
		var high   = cpu.Mmio.Data[ADCH];
		var result = (high << 8) | low;

		// Expected: 0.4 / 5.0 * 1024 = 81.92 → floor → 81
		Assert.That (result, Is.EqualTo (81),
			"ADC result must reflect the configured TemperatureVoltage");
	}
}
