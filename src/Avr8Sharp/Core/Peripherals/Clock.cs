namespace AVR8Sharp.Core.Peripherals;

public class AvrClock
{
    public const int CLKPCE = 128;

    public static readonly AvrClockConfig ClockConfig = new AvrClockConfig(0x61);

    public static readonly int[] Prescalers =
    [
        1, 2, 4, 8, 16, 32, 64, 128, 256,

        // The following values are "reserved" according to the datasheet, so we measured
        // with a scope to figure them out (on ATmega328p)
        2, 4, 8, 16, 32, 64, 128,
    ];

    int _clockEnabledCycles = 0;
    int _prescalerValue = 1;
    int _cyclesDelta = 0;
    readonly uint _baseFreqHz = 0;
    readonly Cpu.Cpu _cpu;

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
        get { return (uint)((_cpu.Cycles + _cyclesDelta) / (double)Frequency * 1e9); }
    }

    public uint TimeMicros
    {
        get { return (uint)((_cpu.Cycles + _cyclesDelta) / (double)Frequency * 1e6); }
    }

    public uint TimeMillis
    {
        get { return (uint)((_cpu.Cycles + _cyclesDelta) / (double)Frequency * 1e3); }
    }

    public AvrClock(Cpu.Cpu cpu, uint baseFreqHz, AvrClockConfig clockConfig)
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
                    _cyclesDelta = (int)((cpu.Cycles + _cyclesDelta) * (oldPrescaler / (double)_prescalerValue) -
                                         cpu.Cycles);
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