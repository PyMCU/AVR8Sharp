using AVR8Sharp.Core;

namespace AVR8Sharp.Core.Peripherals;

public class AvrWatchdog
{
    // Register Bits
    const int MCUSR_PORF = 0x1; // Power-on Reset Flag
    const int MCUSR_EXTRF = 0x2; // External Reset Flag
    const int MCUSR_WDRF = 0x8; // Watchdog System Reset Flag
    const int MCUSR_FLAGS_MASK = 0x0f; // PORF | EXTRF | BORF | WDRF; bits 7:4 are reserved

    const int WDTCSR_WDIF = 0x80; // Watchdog Interrupt Flag
    const int WDTCSR_WDIE = 0x40; // Watchdog Interrupt Enable
    const int WDTCSR_WDP3 = 0x20; // Watchdog Timer Prescaler
    const int WDTCSR_WDCE = 0x10; // Watchdog Change Enable
    const int WDTCSR_WDE = 0x08; // Watchdog System Reset Enable
    const int WDTCSR_WDP2 = 0x04; // Watchdog Timer Prescaler
    const int WDTCSR_WDP1 = 0x02; // Watchdog Timer Prescaler
    const int WDTCSR_WDP0 = 0x01; // Watchdog Timer Prescaler
    const int WDTCSR_WDP210 = WDTCSR_WDP2 | WDTCSR_WDP1 | WDTCSR_WDP0;

    const int WDTCSR_PROTECT_MASK = WDTCSR_WDE | WDTCSR_WDP3 | WDTCSR_WDP210;

    public static readonly AvrWatchdogConfig WatchdogConfig = new AvrWatchdogConfig
    {
        WatchdogInterrupt = 0x0c,

        MCUSR = 0x54,
        WDTCSR = 0x60
    };

    readonly long _clockFrequency = 128_000;

    private readonly Cpu _cpu;
    private readonly AvrWatchdogConfig _config;
    private readonly AvrClock _clock;

    private ulong _changeEnabledCycles = 0;
    private ulong _watchdogTimeout = 0;
    private bool _enabledValue = false;
    private bool _scheduled = false;
    private bool _watchdogReset = false;

    private readonly AvrInterruptConfig _watchdog;

    public bool Enabled
    {
        get { return _enabledValue; }
    }

    /// <summary>
    /// The base clock frequency is 128KHz. Thus, a prescaler of 2048 gives 16ms timeout.
    /// </summary>
    public double Prescaler
    {
        get
        {
            var wdtcsr = _cpu.Mmio.Data[_config.WDTCSR];
            var value = ((wdtcsr & WDTCSR_WDP3) >> 2) | (wdtcsr & WDTCSR_WDP210);
            return 2048 << value;
        }
    }

    public AvrWatchdog(Cpu cpu, AvrWatchdogConfig config, AvrClock clock)
    {
        _cpu = cpu;
        _clock = clock;
        _config = config;

        _watchdog = new AvrInterruptConfig(
            address: config.WatchdogInterrupt,
            flagRegister: config.WDTCSR,
            flagMask: WDTCSR_WDIF,
            enableRegister: config.WDTCSR,
            enableMask: WDTCSR_WDIE
        );

        _cpu.OnWatchdogReset = ResetWatchdog;

        // MCUSR is owned here because the watchdog is the peripheral that reports reset causes.
        // Constructing the peripheral models the power-on of the chip, so PORF starts set.
        PowerOnReset();

        // A flag is cleared by writing a logic zero to it; writing a one never sets it.
        _cpu.Mmio.RegisterWrite(config.MCUSR, (value, oldValue, _, _) =>
        {
            _cpu.Mmio.Data[config.MCUSR] = (byte)(oldValue & value & MCUSR_FLAGS_MASK);
            return true;
        });

        // Any CPU reset that is not the watchdog's own is seen by the chip as an external
        // reset (RESET pin), which sets EXTRF. MCUSR itself survives the reset, as on silicon.
        _cpu.OnPeripheralReset += () =>
        {
            if (!_watchdogReset) _cpu.Mmio.Data[config.MCUSR] |= MCUSR_EXTRF;
        };

        _cpu.Mmio.RegisterWrite(config.WDTCSR, (value, oldValue, _, _) =>
        {
            if ((value & WDTCSR_WDCE) != 0 && (value & WDTCSR_WDE) != 0)
            {
                _changeEnabledCycles = _cpu.Cycles + 4;
                value = (byte)(value & ~WDTCSR_PROTECT_MASK);
            }
            else
            {
                if (_cpu.Cycles >= _changeEnabledCycles)
                {
                    value = (byte)((value & ~WDTCSR_PROTECT_MASK) | (oldValue & WDTCSR_PROTECT_MASK));
                }

                _enabledValue = (value & WDTCSR_WDE) != 0 || (value & WDTCSR_WDIE) != 0;
                _cpu.Mmio.Data[config.WDTCSR] = value;
            }

            if (Enabled)
            {
                ResetWatchdog();

                _cpu.UpdateClockEvent(CheckWatchdog, (int)(_watchdogTimeout - _cpu.Cycles));
                _scheduled = true;
            }
            else if (_scheduled)
            {
                _cpu.ClearClockEvent(CheckWatchdog);
                _scheduled = false;
            }

            _cpu.ClearInterruptByFlag(_watchdog, value);
            return true;
        });
    }

    /// <summary>
    /// Puts MCUSR in its power-on state: only PORF set. Does not reset the CPU. The constructor
    /// already does this; call it again to rewind a restored machine to a fresh power-on.
    /// </summary>
    public void PowerOnReset()
    {
        _cpu.Mmio.Data[_config.MCUSR] = MCUSR_PORF;
    }

    private void ResetWatchdog()
    {
        var cycles = (int)Math.Floor((_clock.Frequency / _clockFrequency) * Prescaler);
        _watchdogTimeout = _cpu.Cycles + (ulong)cycles;
    }

    private void CheckWatchdog()
    {
        if (Enabled && _cpu.Cycles >= _watchdogTimeout)
        {
            // Watchdog timed out!
            var wdtcsr = _cpu.Mmio.Data[_config.WDTCSR];
            if ((wdtcsr & WDTCSR_WDIE) != 0)
            {
                _cpu.SetInterruptFlag(_watchdog);
            }

            if ((wdtcsr & WDTCSR_WDE) != 0)
            {
                if ((wdtcsr & WDTCSR_WDIE) != 0)
                {
                    _cpu.Mmio.Data[_config.WDTCSR] &= ~WDTCSR_WDIE & 0xff;
                }
                else
                {
                    _watchdogReset = true;
                    try { _cpu.Reset(); }
                    finally { _watchdogReset = false; }
                    _scheduled = false;
                    _cpu.Mmio.Data[_config.MCUSR] |= MCUSR_WDRF;
                    return;
                }
            }

            ResetWatchdog();
        }

        if (Enabled)
        {
            _scheduled = true;
            _cpu.AddClockEvent(CheckWatchdog, (int)(_watchdogTimeout - _cpu.Cycles));
        }
        else
        {
            _scheduled = false;
        }
    }
}

public class AvrWatchdogConfig
{
    public byte WatchdogInterrupt { get; set; }

    public byte MCUSR { get; set; }
    public byte WDTCSR { get; set; }
}