using AVR8Sharp.Peripherals;
namespace AVr8SharpTests;

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
		var cpu = new AVR8Sharp.Cpu.Cpu (program.Program);
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
		
		var low = cpu.Data[R16];
		var high = cpu.Data[R17];
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
		var cpu = new AVR8Sharp.Cpu.Cpu (program.Program);
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
		
		var low = cpu.Data[R16];
		var high = cpu.Data[R17];
		var result = (high << 8) | low;
		Assert.That(result, Is.EqualTo(0)); // Should be 0 since the ADC is not enabled
	}

		
}
