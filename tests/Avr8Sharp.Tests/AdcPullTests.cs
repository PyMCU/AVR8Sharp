using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

[TestFixture]
public class AdcPull : AvrTestBase
{
	const int ADMUX = 0x7c;
	const int ADCSRA = 0x7a;
	const int ADCSRB = 0x7b;
	const int ADCH = 0x79;
	const int ADCL = 0x78;
	const int REFS0 = 1 << 6;
	const int ADEN = 1 << 7;
	const int ADSC = 1 << 6;
	const int PRESCALER_128 = 7;

	AvrAdc _adc;

	protected override void SetupPeripherals () => _adc = new AvrAdc (Cpu, AvrAdc.AdcConfig);

	void Convert (int admux, int adcsrb = 0)
	{
		Cpu.WriteData (ADMUX, (byte)admux);
		Cpu.WriteData (ADCSRB, (byte)adcsrb);
		Cpu.WriteData (ADCSRA, ADEN | ADSC | PRESCALER_128);
		Cpu.Cycles += 128 * 26;
		Cpu.Tick ();
	}

	int Result => Cpu.Mmio.Data[ADCL] | (Cpu.Mmio.Data[ADCH] << 8);

	[Test]
	public void CalledOncePerConversionWithTheChannel ()
	{
		var calls = new List<int> ();
		_adc.ReadChannelVolts = ch => { calls.Add (ch); return 2.56; };
		_adc.ChannelValues[3] = 1.0; // ignored while the hook is set

		Convert (REFS0 | 3);
		Assert.That (calls, Is.EqualTo (new[] { 3 }));
		Assert.That (Result, Is.EqualTo (524));

		Convert (REFS0 | 5);
		Assert.That (calls, Is.EqualTo (new[] { 3, 5 }));
	}

	[Test]
	public void NotCalledWithoutConversions ()
	{
		var calls = 0;
		_adc.ReadChannelVolts = _ => { calls++; return 1; };
		Cpu.WriteData (ADMUX, REFS0 | 2);
		Cpu.WriteData (ADCSRA, ADEN | PRESCALER_128); // enabled, no start
		Cpu.Cycles += 100000;
		Cpu.Tick ();
		Assert.That (calls, Is.EqualTo (0));
	}

	[Test]
	public void ConstantAndTemperatureInputsDoNotPull ()
	{
		var calls = 0;
		_adc.ReadChannelVolts = _ => { calls++; return 1; };
		Convert (REFS0 | 8); // temperature
		Convert (REFS0 | 14); // 1.1 V bandgap
		Assert.That (calls, Is.EqualTo (0));
	}

	[Test]
	public void FallsBackToChannelValuesWhenNull ()
	{
		_adc.ChannelValues[1] = 2.56;
		Convert (REFS0 | 1);
		Assert.That (Result, Is.EqualTo (524));

		_adc.ReadChannelVolts = _ => 5.0;
		Convert (REFS0 | 1);
		Assert.That (Result, Is.EqualTo (1023));

		_adc.ReadChannelVolts = null;
		Convert (REFS0 | 1);
		Assert.That (Result, Is.EqualTo (524));
	}
}

[TestFixture]
public class AdcPullMega : AvrTestBase
{
	const int ADMUX = 0x7c;
	const int ADCSRA = 0x7a;
	const int ADCSRB = 0x7b;
	const int ADCH = 0x79;
	const int ADCL = 0x78;
	const int MUX5 = 0x8;

	AvrAdc _adc;

	protected override void SetupPeripherals () => _adc = new AvrAdc (Cpu, AvrAdc.Atmega2560AdcConfig);

	[TestCase (0, 0, 0)]
	[TestCase (7, 0, 7)]
	[TestCase (0, MUX5, 8)]
	[TestCase (7, MUX5, 15)]
	public void MuxChannelsReachTheHook (int mux, int adcsrb, int expected)
	{
		var calls = new List<int> ();
		_adc.ReadChannelVolts = ch => { calls.Add (ch); return 2.56; };
		Cpu.WriteData (ADMUX, (byte)(0x40 | mux));
		Cpu.WriteData (ADCSRB, (byte)adcsrb);
		Cpu.WriteData (ADCSRA, 0x80 | 0x40 | 7);
		Cpu.Cycles += 128 * 26;
		Cpu.Tick ();
		Assert.That (calls, Is.EqualTo (new[] { expected }));
		Assert.That (Cpu.Mmio.Data[ADCL] | (Cpu.Mmio.Data[ADCH] << 8), Is.EqualTo (524));
	}
}
