using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

[TestFixture]
public class Adc : AvrTestBase
{
	const int ADMUX = 0x7c;
	const int REFS0 = 1 << 6;
	const int REFS1 = 1 << 7;

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

	private AvrAdc _adc;

	protected override void SetupPeripherals()
	{
		_adc = new AvrAdc (Cpu, AvrAdc.AdcConfig);
	}

	[Test(Description = "Should successfully perform an ADC conversion")]
	public void Conversion ()
	{
		var program = new AsmProgram (@$"
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
").Compile();
		Cpu.LoadProgram(program.Program);
		var runner = new TestProgramRunner (Cpu);
		
		// Spy on OnADCRead method to be executed when the ADC is read
		_adc.ChannelValues[0] = 2.56; // Should result in 2.56/5*1024 = 524
		
		// Setup
		runner.RunInstructions (16);

		Cpu.Cycles += 128 * 25; // Skip to the end of the conversion
		Cpu.Tick ();
		
		// Now read the result
		runner.RunInstructions (5);
		
		var low = Cpu.Mmio.Data[R16];
		var high = Cpu.Mmio.Data[R17];
		var result = (high << 8) | low;
		Assert.That(result, Is.EqualTo(524));
	}
	
	[Test(Description = "Should read 0 when the ADC peripheral is not enabled")]
	public void Disabled ()
	{
		var program = new AsmProgram (@$"
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
").Compile();
		Cpu.LoadProgram(program.Program);
		var runner = new TestProgramRunner (Cpu, (cpu) => {
			// Do nothing on break
		});
		
		// Spy on OnADCRead method to be executed when the ADC is read
		_adc.ChannelValues[0] = 2.56; // Should result in 2.56/5*1024 = 524
		
		// Setup
		runner.RunInstructions (18);

		Cpu.Cycles += 128 * 25; // Skip to the end of the conversion
		Cpu.Tick ();
		
		// Now read the result
		runner.RunInstructions (5);
		
		// Read the result
		runner.RunToBreak ();
		
		var low = Cpu.Mmio.Data[R16];
		var high = Cpu.Mmio.Data[R17];
		var result = (high << 8) | low;
		Assert.That(result, Is.EqualTo(0)); // Should be 0 since the ADC is not enabled
	}

	[Test(Description = "ADC free-running mode: ADIF fires multiple times when ADATE=1 and ADTS=000")]
	public void FreeRunning_AutoTrigger ()
	{
		// ADATE=0x20 in ADCSRA, ADTS=000 in ADCSRB → free-running (restart after each conversion)

		_adc.ChannelValues[0] = 2.56; // 2.56/5*1024 = 524

		// Configure: channel 0, AVCC reference
		Cpu.Mmio.Data[ADMUX] = 1 << 6;  // REFS0
		// Enable ADC, start conversion, prescaler /128, auto-trigger
		Cpu.Mmio.Data[ADCSRA] = ADEN | ADSC | ADATE | ADPS0 | ADPS1 | ADPS2;

		var completions = 0;
		// Hook into OnADCRead to count how many conversions complete
		_adc.OnADCRead(new AdcMuxInput(type: AdcMuxInputType.SingleEnded, channel: 0));

		// Manually advance clock to complete conversion 1
		Cpu.Cycles += 128 * 25;
		Cpu.Tick();
		completions++;

        Assert.Multiple(() =>
        {
            // ADIF set on completion. In free-running mode the next conversion starts
            // immediately, so ADSC reads 1 again (a conversion is in progress), per the
            // datasheet ("ADSC reads as one as long as a conversion is in progress").
            Assert.That(Cpu.Mmio.Data[ADCSRA] & ADIF, Is.EqualTo(ADIF), "ADIF should be set after first conversion");
            Assert.That(Cpu.Mmio.Data[ADCSRA] & ADSC, Is.EqualTo(ADSC), "ADSC stays set: free-running immediately starts the next conversion");
        });

        // In free-running mode, a second conversion should have been queued automatically.
        // Advance clock to complete conversion 2.
        Cpu.Mmio.Data[ADCSRA] &= ~ADIF & 0xff; // Clear ADIF to detect second completion
		Cpu.Cycles += 128 * 13;
		Cpu.Tick();
		completions++;

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Mmio.Data[ADCSRA] & ADIF, Is.EqualTo(ADIF),
                    "ADIF should fire again in free-running mode (second conversion)");
            Assert.That(completions, Is.EqualTo(2));
        });
    }

	[Test(Description = "ADC does NOT auto-restart when ADATE is clear (single-conversion mode)")]
	public void SingleConversion_NoAutoRestart ()
	{
		_adc.ChannelValues[0] = 2.56;
		Cpu.Mmio.Data[ADMUX] = 1 << 6;
		// ADEN | ADSC | /128 — NO ADATE
		Cpu.Mmio.Data[ADCSRA] = ADEN | ADSC | ADPS0 | ADPS1 | ADPS2;

		_adc.OnADCRead(new AdcMuxInput(type: AdcMuxInputType.SingleEnded, channel: 0));
		Cpu.Cycles += 128 * 25;
		Cpu.Tick();

		Assert.That(Cpu.Mmio.Data[ADCSRA] & ADIF, Is.EqualTo(ADIF));

		// Clear ADIF and advance one more conversion period — no second ADIF expected
		Cpu.Mmio.Data[ADCSRA] &= ~ADIF & 0xff;
		Cpu.Cycles += 128 * 13;
		Cpu.Tick();

		Assert.That(Cpu.Mmio.Data[ADCSRA] & ADIF, Is.EqualTo(0),
			"ADIF must NOT fire again when ADATE=0 (single-conversion mode)");
	}

	[Test(Description = "ADC auto-trigger: Trigger() starts a conversion only for the ADTS-selected source when ADATE=1")]
	public void AutoTrigger_StartsOnMatchingSource ()
	{
		const int ADCSRB = 0x7b;
		_adc.ChannelValues[0] = 2.56;
		Cpu.Mmio.Data[ADMUX] = REFS0;
		Cpu.Mmio.Data[ADCSRB] = 0x03;                            // ADTS = 011 → Timer0 Compare Match A
		Cpu.WriteData (ADCSRA, ADEN | ADATE | ADPS0 | ADPS1 | ADPS2); // enabled, auto-trigger, NO ADSC

		// Nothing converts until a trigger arrives.
		Cpu.Cycles += 128 * 25;
		Cpu.Tick();
		Assert.That (Cpu.Mmio.Data[ADCSRA] & ADIF, Is.EqualTo(0), "no conversion before any trigger");

		// A source that does not match ADTS is ignored.
		_adc.Trigger (AdcTriggerSource.Timer1Overflow);
		Assert.That (Cpu.Mmio.Data[ADCSRA] & ADSC, Is.EqualTo(0), "non-matching source must not start a conversion");

		// The matching source starts a conversion (ADSC goes high).
		_adc.Trigger (AdcTriggerSource.Timer0CompareMatchA);
		Assert.That (Cpu.Mmio.Data[ADCSRA] & ADSC, Is.EqualTo(ADSC), "matching source must start a conversion");

		Cpu.Cycles += 128 * 25;
		Cpu.Tick();
		Assert.That (Cpu.Mmio.Data[ADCSRA] & ADIF, Is.EqualTo(ADIF), "ADIF set after the triggered conversion completes");
	}

	[Test (Description = "AvrAdc.TemperatureVoltage is configurable and affects the ADC result")]
	public void TemperatureSensor_Configurable ()
	{
		// Set a known temperature voltage (0.4 V)
		_adc.TemperatureVoltage = 0.4;

		// Select temperature channel (mux 8 = 0b1000), AVCC reference (REFS0)
		Cpu.Mmio.Data[ADMUX] = (byte)((1 << 6) | 8); // REFS0 | MUX3

		// Enable ADC, start conversion, prescaler /128
		// Write via WriteData to trigger the ADCSRA hook (direct Mmio.Data write bypasses it)
		Cpu.Mmio.Data[ADCSRA] = ADEN | ADSC | ADPS0 | ADPS1 | ADPS2;
		_adc.OnADCRead (new AdcMuxInput (type: AdcMuxInputType.Temperature));

		Cpu.Cycles += 128 * 25;
		Cpu.Tick ();

		var low    = Cpu.Mmio.Data[ADCL];
		var high   = Cpu.Mmio.Data[ADCH];
		var result = (high << 8) | low;

		// Expected: 0.4 / 5.0 * 1024 = 81.92 → floor → 81
		Assert.That (result, Is.EqualTo (81),
			"ADC result must reflect the configured TemperatureVoltage");
	}

	[Test (Description = "ATmega328P REFS1:0 selects AREF, AVCC, reserved, internal 1.1 V (datasheet table 28-3)")]
	public void ReferenceSelection_FollowsDatasheetOrder ()
	{
		Cpu.Mmio.Data[ADMUX] = 0x00;
		Assert.That (_adc.ReferenceVoltageType, Is.EqualTo (AdcReference.AREF), "REFS = 00");
		Cpu.Mmio.Data[ADMUX] = REFS0;
		Assert.That (_adc.ReferenceVoltageType, Is.EqualTo (AdcReference.AVCC), "REFS = 01");
		Cpu.Mmio.Data[ADMUX] = 0xc0;
		Assert.That (_adc.ReferenceVoltageType, Is.EqualTo (AdcReference.Internal1V1), "REFS = 11 (analogReference(INTERNAL))");
		Assert.That (_adc.ReferenceVoltage, Is.EqualTo (1.1));
	}


	[Test(Description = "The AVCC reference follows the supply the host reports, so a 3.3 V board converts full-scale at 3.3 V")]
	public void Conversion_Scales_With_Avcc ()
	{
		var program = new AsmProgram (@$"
	    _REPLACE ADMUX, {ADMUX}
		_REPLACE ADCSRA, {ADCSRA}
	    _REPLACE ADCH, {ADCH}
		_REPLACE ADCL, {ADCL}
		ldi r24, {REFS0}
	    sts ADMUX, r24
	    ldi r24, {ADEN | ADSC | ADPS0 | ADPS1 | ADPS2}
		sts ADCSRA, r24
	  waitComplete:
		lds r24, {ADCSRA}
	    andi r24, {ADSC}
		brne waitComplete
		lds r16, {ADCL}
	    lds r17, {ADCH}
		break
").Compile();
		Cpu.LoadProgram(program.Program);
		var runner = new TestProgramRunner (Cpu);

		_adc.Avcc = 3.3;
		_adc.ChannelValues[0] = 1.65; // half of AVCC: 512

		runner.RunInstructions (16);
		Cpu.Cycles += 128 * 25;
		Cpu.Tick ();
		runner.RunInstructions (5);

		var result = (Cpu.Mmio.Data[R17] << 8) | Cpu.Mmio.Data[R16];
		Assert.That(result, Is.EqualTo(512));
	}

	[Test(Description = "REFS1:0 = 11 is the internal 1.1 V bandgap on the ATmega328P (the Arduino core's INTERNAL)")]
	public void Conversion_Against_The_Internal_Bandgap ()
	{
		var program = new AsmProgram (@$"
	    _REPLACE ADMUX, {ADMUX}
		_REPLACE ADCSRA, {ADCSRA}
	    _REPLACE ADCH, {ADCH}
		_REPLACE ADCL, {ADCL}
		ldi r24, {REFS1 | REFS0}
	    sts ADMUX, r24
	    ldi r24, {ADEN | ADSC | ADPS0 | ADPS1 | ADPS2}
		sts ADCSRA, r24
	  waitComplete:
		lds r24, {ADCSRA}
	    andi r24, {ADSC}
		brne waitComplete
		lds r16, {ADCL}
	    lds r17, {ADCH}
		break
").Compile();
		Cpu.LoadProgram(program.Program);
		var runner = new TestProgramRunner (Cpu);

		_adc.ChannelValues[0] = 0.55; // half of 1.1 V: 512

		runner.RunInstructions (16);
		Cpu.Cycles += 128 * 25;
		Cpu.Tick ();
		runner.RunInstructions (5);

		var result = (Cpu.Mmio.Data[R17] << 8) | Cpu.Mmio.Data[R16];
		Assert.That(result, Is.EqualTo(512));
	}

	[Test(Description = "ADIF is cleared by writing a one and a write of zero keeps it (datasheet 24.9.2)")]
	public void Adif_Is_Write_One_To_Clear ()
	{
		Cpu.Mmio.Data[ADCSRA] = ADEN | ADIF;
		Cpu.WriteData(ADCSRA, ADEN | ADPS0);
		Assert.That(Cpu.ReadData(ADCSRA) & ADIF, Is.EqualTo(ADIF), "writing 0 to ADIF must not clear it");

		Cpu.WriteData(ADCSRA, ADEN | ADIF);
		Assert.That(Cpu.ReadData(ADCSRA) & ADIF, Is.Zero, "writing 1 to ADIF clears it");
	}

	[Test(Description = "A write cannot set ADIF, and ADSC cannot be cleared while a conversion runs")]
	public void Adif_Cannot_Be_Set_And_Adsc_Sticks_While_Converting ()
	{
		Cpu.WriteData(ADCSRA, ADEN | ADIF);
		Assert.That(Cpu.ReadData(ADCSRA) & ADIF, Is.Zero, "ADIF is not a stored bit");

		Cpu.WriteData(ADCSRA, ADEN | ADSC | ADPS0 | ADPS1 | ADPS2);
		Cpu.WriteData(ADCSRA, ADEN | ADPS0 | ADPS1 | ADPS2);
		Assert.That(Cpu.ReadData(ADCSRA) & ADSC, Is.EqualTo(ADSC), "ADSC stays set while converting");
	}
}
