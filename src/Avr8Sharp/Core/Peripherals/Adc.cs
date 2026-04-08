using AVR8Sharp.Core.Cpu;
using ADCMuxConfiguration = System.Collections.Generic.Dictionary<int, AVR8Sharp.Core.Peripherals.AdcMuxInput>;

namespace AVR8Sharp.Core.Peripherals;

public class AvrAdc
{
    public static ADCMuxConfiguration Atmega328Channels { get; } = new ADCMuxConfiguration
    {
        { 0, new AdcMuxInput(type: AdcMuxInputType.SingleEnded, channel: 0) },
        { 1, new AdcMuxInput(type: AdcMuxInputType.SingleEnded, channel: 1) },
        { 2, new AdcMuxInput(type: AdcMuxInputType.SingleEnded, channel: 2) },
        { 3, new AdcMuxInput(type: AdcMuxInputType.SingleEnded, channel: 3) },
        { 4, new AdcMuxInput(type: AdcMuxInputType.SingleEnded, channel: 4) },
        { 5, new AdcMuxInput(type: AdcMuxInputType.SingleEnded, channel: 5) },
        { 6, new AdcMuxInput(type: AdcMuxInputType.SingleEnded, channel: 6) },
        { 7, new AdcMuxInput(type: AdcMuxInputType.SingleEnded, channel: 7) },
        { 8, new AdcMuxInput(type: AdcMuxInputType.Temperature) },
        { 14, new AdcMuxInput(type: AdcMuxInputType.Constant, voltage: 1.1) },
        { 15, new AdcMuxInput(type: AdcMuxInputType.Constant, voltage: 0) },
    };

    public static readonly AdcMuxInput FallbackMuxInput = new AdcMuxInput(type: AdcMuxInputType.Constant, voltage: 0);

    public static readonly AvrAdcConfig AdcConfig = new AvrAdcConfig(
        admux: 0x7c,
        adcsra: 0x7a,
        adcsrb: 0x7b,
        adcl: 0x78,
        adch: 0x79,
        didr0: 0x7e,
        adcInterrupt: 0x2a,
        numChannels: 8,
        muxInputMask: 0xf,
        muxChannels: Atmega328Channels,
        adcReferences:
        [
            AdcReference.AVCC,
            AdcReference.AREF,
            AdcReference.Internal1V1,
            AdcReference.Internal2V56
        ]
    );

    public const int ADPS_MASK = 0x7;
    public const int ADIE = 0x8;
    public const int ADIF = 0x10;
    public const int ADATE = 0x20; // ADC Auto Trigger Enable (in ADCSRA)
    public const int ADSC = 0x40;
    public const int ADEN = 0x80;
    public const int ADTS_MASK = 0x7; // Auto Trigger Source bits in ADCSRB (000 = free-running)

    public const int MUX_MASK = 0x1f;
    public const int ADLAR = 0x20;
    public const int MUX5 = 0x8;
    public const int REFS2 = 0x8;
    public const int REFS_MASK = 0x3;
    public const int REFS_SHIFT = 6;

    readonly Cpu.Cpu _cpu;
    bool _converting = false;
    int _conversionCycles = 25;
    readonly AvrAdcConfig _config;
    readonly AvrInterruptConfig _adc;
    readonly double avcc = 5.0;
    readonly double aref = 5.0;
    
    private int _cachedSampleCycles;
    private double _cachedReferenceVoltage;
    private readonly AdcMuxInput[] _muxArray = new AdcMuxInput[32];
    private int _pendingAdcResult;
    private readonly Action _completeAdcAction;

    public int SampleCycles
    {
        get { return _conversionCycles * Prescaler; }
    }

    public int Prescaler
    {
        get
        {
            var adcsra = _cpu.Mmio.Data[_config.ADCSRA];
            var adps = adcsra & ADPS_MASK;
            switch (adps)
            {
                case 0:
                case 1:
                    return 2;
                case 2:
                    return 4;
                case 3:
                    return 8;
                case 4:
                    return 16;
                case 5:
                    return 32;
                case 6:
                    return 64;
                default:
                    return 128;
            }
        }
    }

    public AdcReference ReferenceVoltageType
    {
        get
        {
            var admux = _cpu.Mmio.Data[_config.ADMUX];
            var refs = (admux >> REFS_SHIFT) & REFS_MASK;
            if (_config.AdcReferences.Length > 4 && (admux & REFS2) != 0)
            {
                refs |= 0x4;
            }

            return _config.AdcReferences[refs] ?? ReferenceVoltageType;
        }
    }

    public double ReferenceVoltage
    {
        get
        {
            switch (ReferenceVoltageType)
            {
                case AdcReference.AVCC:
                    return avcc;
                case AdcReference.AREF:
                    return aref;
                case AdcReference.Internal1V1:
                    return 1.1;
                case AdcReference.Internal2V56:
                    return 2.56;
                default:
                    return avcc;
            }
        }
    }

    public double[] ChannelValues { get; }

    /// <summary>
    /// Voltage returned by the internal temperature sensor channel (mux input 8 on ATmega328P).
    /// Defaults to ~0.378 V which corresponds to approximately 25 °C.
    /// Set this to simulate a different ambient temperature.
    /// </summary>
    public double TemperatureVoltage { get; set; } = 0.378125;

    public AvrAdc(Cpu.Cpu cpu, AvrAdcConfig config)
    {
        _cpu = cpu;
        _config = config;
        _adc = new AvrInterruptConfig(
            address: _config.AdcInterrupt,
            flagRegister: _config.ADCSRA,
            flagMask: ADIF,
            enableRegister: _config.ADCSRA,
            enableMask: ADIE
        );
        ChannelValues = new double[config.NumChannels];
        
        for (var i = 0; i < 32; i++)
        {
            _muxArray[i] = config.MuxChannels.GetValueOrDefault(i, FallbackMuxInput);
        }

        _completeAdcAction = () => CompleteAdcRead(_pendingAdcResult);

        _cpu.Mmio.RegisterWrite(config.ADMUX, (value, _, _, _) => {
            _cpu.Mmio.Data[config.ADMUX] = value;
            UpdateCaches();
            return true;
        });
        
        _cpu.Mmio.RegisterWrite(config.ADCSRA, (value, oldValue, _, _) =>
        {
            if ((value & ADEN) != 0 && (oldValue & ADEN) == 0)
            {
                _conversionCycles = 25;
            }

            cpu.Mmio.Data[config.ADCSRA] = value;
            UpdateCaches();
            cpu.UpdateInterruptEnable(_adc, value);

            if (!_converting && (value & ADSC) != 0)
            {
                if ((value & ADEN) == 0)
                {
                    // Special case: reading while the ADC is not enabled should return 0
                    cpu.AddClockEvent(() => CompleteAdcRead(0), SampleCycles);
                    return true;
                }

                var channel = cpu.Mmio.Data[config.ADMUX] & MUX_MASK;
                if ((cpu.Mmio.Data[config.ADCSRB] & MUX5) != 0)
                {
                    channel |= 0x20;
                }

                var muxInput = _muxArray[channel & 0x1F];
                _converting = true;
                OnADCRead(muxInput);
                return true;
            }

            return false;
        });

        UpdateCaches();
    }

    public void OnADCRead(AdcMuxInput input)
    {
        var voltage = input.Type switch
        {
            AdcMuxInputType.Constant => input.Voltage,
            AdcMuxInputType.SingleEnded => ChannelValues[input.Channel],
            AdcMuxInputType.Differential => input.Gain * (ChannelValues[input.PositiveChannel] - ChannelValues[input.NegativeChannel]),
            AdcMuxInputType.Temperature => TemperatureVoltage,
            _ => 0.0
        };

        var rawValue = voltage / _cachedReferenceVoltage * 1024;
        _pendingAdcResult = Math.Clamp((int)Math.Floor(rawValue), 0, 1023);

        _cpu.AddClockEvent(_completeAdcAction, _cachedSampleCycles);
    }

    public void CompleteAdcRead(int result)
    {
        _converting = false;
        _conversionCycles = 13;
        UpdateCaches();
        var admux = _config.ADMUX;
        var adcl = _config.ADCL;
        var adch = _config.ADCH;
        var adcsra = _config.ADCSRA;
        if ((_cpu.Mmio.Data[admux] & ADLAR) != 0)
        {
            _cpu.Mmio.Data[adcl] = (byte)((result << 6) & 0xff);
            _cpu.Mmio.Data[adch] = (byte)(result >> 2);
        }
        else
        {
            _cpu.Mmio.Data[adcl] = (byte)(result & 0xff);
            _cpu.Mmio.Data[adch] = (byte)((result >> 8) & 0x3);
        }

        _cpu.Mmio.Data[adcsra] &= ~ADSC & 0xff;
        _cpu.SetInterruptFlag(_adc);

        // Auto-trigger: free-running mode (ADATE=1, ADTS=000 in ADCSRB)
        if ((_cpu.Mmio.Data[adcsra] & ADEN) != 0 &&
            (_cpu.Mmio.Data[adcsra] & ADATE) != 0 &&
            (_cpu.Mmio.Data[_config.ADCSRB] & ADTS_MASK) == 0)
        {
            _converting = true;
            var channel = _cpu.Mmio.Data[_config.ADMUX] & MUX_MASK;
            if ((_cpu.Mmio.Data[_config.ADCSRB] & MUX5) != 0)
            {
                channel |= 0x20;
            }
            OnADCRead(_muxArray[channel & 0x1F]);
        }
    }

    private void UpdateCaches()
    {
        var admux = _cpu.Mmio.Data[_config.ADMUX];
        var refs = (admux >> REFS_SHIFT) & REFS_MASK;
        if (_config.AdcReferences.Length > 4 && (admux & REFS2) != 0) refs |= 0x4;

        var refType = _config.AdcReferences[refs] ?? AdcReference.AVCC;
        _cachedReferenceVoltage = refType switch
        {
            AdcReference.AREF => aref,
            AdcReference.Internal1V1 => 1.1,
            AdcReference.Internal2V56 => 2.56,
            _ => avcc
        };

        var adcsra = _cpu.Mmio.Data[_config.ADCSRA];
        var adps = adcsra & ADPS_MASK;
        var prescaler = adps switch
        {
            2 => 4, 3 => 8, 4 => 16, 5 => 32, 6 => 64, 7 => 128,
            _ => 2
        };
        _cachedSampleCycles = _conversionCycles * prescaler;
    }
}

public enum AdcReference
{
    AVCC = 0,
    AREF = 1,
    Internal1V1 = 2,
    Internal2V56 = 3,
    Reserved = 4,
}

public enum AdcMuxInputType
{
    SingleEnded = 0,
    Differential = 1,
    Constant = 2,
    Temperature = 3,
}

public class AdcMuxInput(
    AdcMuxInputType type,
    int channel = 0,
    double voltage = 0,
    int positiveChannel = 0,
    int negativeChannel = 0,
    int gain = 1)
{
    public readonly AdcMuxInputType Type = type;
    public readonly int Channel = channel;
    public readonly double Voltage = voltage;
    public readonly int PositiveChannel = positiveChannel;
    public readonly int NegativeChannel = negativeChannel;
    public readonly int Gain = gain;
}

public class AvrAdcConfig(
    byte admux,
    byte adcsra,
    byte adcsrb,
    byte adcl,
    byte adch,
    byte didr0,
    byte adcInterrupt,
    byte numChannels,
    byte muxInputMask,
    ADCMuxConfiguration muxChannels,
    AdcReference?[] adcReferences)
{
    public readonly byte ADMUX = admux;
    public readonly byte ADCSRA = adcsra;
    public readonly byte ADCSRB = adcsrb;
    public readonly byte ADCL = adcl;
    public readonly byte ADCH = adch;
    public readonly byte DIDR0 = didr0;
    public readonly byte AdcInterrupt = adcInterrupt;
    public readonly byte NumChannels = numChannels;
    public readonly byte MuxInputMask = muxInputMask;
    public readonly ADCMuxConfiguration MuxChannels = muxChannels;
    public readonly AdcReference?[] AdcReferences = adcReferences;
}