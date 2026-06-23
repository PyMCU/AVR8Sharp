using AVR8Sharp.Core;

namespace AVR8Sharp.Core.Peripherals;

public class AvrClock
{
    public const int CLKPCE = 128;

    public static readonly AvrClockConfig ClockConfig = new AvrClockConfig(0x61);

    // CLKPS3:0 → system clock division factor. Indices 0..8 are the datasheet-defined
    // factors 1,2,4,…,256 (ATmega328P Table 13-12). Indices 9..15 are marked "Reserved" by
    // the datasheet — their behaviour is undefined and firmware must not select them. The
    // values below were measured on real ATmega328P silicon, where the divider follows the
    // pattern 2^(index-8) (i.e. 2,4,…,128) in that region; they are kept because empirical
    // hardware behaviour is the closest thing to a source of truth for an undefined region.
    public static readonly int[] Prescalers =
    [
        1, 2, 4, 8, 16, 32, 64, 128, 256,
        // Reserved region (empirical ATmega328P measurements — see note above):
        2, 4, 8, 16, 32, 64, 128,
    ];

    ulong _clockEnabledCycles = 0;
    int _prescalerValue = 1;
    int _cyclesDelta = 0;
    readonly uint _baseFreqHz = 0;
    readonly Cpu _cpu;

    public uint Frequency
    {
        get { return (uint)(_baseFreqHz / (_prescalerValue != 0 ? _prescalerValue : 1)); }
    }

    public int Prescaler
    {
        get { return _prescalerValue; }
    }

    public uint TimeNanos
    {
        get { return (uint)(((double)_cpu.Cycles + _cyclesDelta) / Frequency * 1e9); }
    }

    public uint TimeMicros
    {
        get { return (uint)(((double)_cpu.Cycles + _cyclesDelta) / Frequency * 1e6); }
    }

    public uint TimeMillis
    {
        get { return (uint)(((double)_cpu.Cycles + _cyclesDelta) / Frequency * 1e3); }
    }

    public AvrClock(Cpu cpu, uint baseFreqHz, AvrClockConfig clockConfig)
    {
        _baseFreqHz = baseFreqHz;
        _cpu = cpu;
        cpu.Mmio.RegisterWrite(clockConfig.CLKPR, (clkpr, _, _, _) =>
        {
            if ((_clockEnabledCycles == 0 || _clockEnabledCycles < cpu.Cycles) && clkpr == CLKPCE)
            {
                _clockEnabledCycles = cpu.Cycles + 4;
            }
            else if (_clockEnabledCycles != 0 && _clockEnabledCycles >= cpu.Cycles)
            {
                _clockEnabledCycles = 0;
                var index = clkpr & 0xf;
                var oldPrescaler = _prescalerValue;
                _prescalerValue = Prescalers[index];
                cpu.Mmio.Data[clockConfig.CLKPR] = (byte)index;
                if (oldPrescaler != _prescalerValue)
                {
                    _cyclesDelta = (int)(((double)cpu.Cycles + _cyclesDelta) * (oldPrescaler / (double)_prescalerValue) -
                                         (double)cpu.Cycles);
                }
            }

            return true;
        });
    }
}

public class AvrClockConfig(byte clkpr)
{
    public readonly byte CLKPR = clkpr;
}