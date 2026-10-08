using AVR8Sharp.Core;

namespace AVR8Sharp.Core.Peripherals;

/// <summary>
/// Register map and wiring of the ATtiny25/45/85 Timer/Counter1. The defaults describe the
/// ATtiny85 (data-space addresses, that is I/O address + 0x20).
/// </summary>
public class AvrAttinyTimer1Config(
    ushort tccr1 = 0x50,
    ushort gtccr = 0x4c,
    ushort tcnt1 = 0x4f,
    ushort ocr1a = 0x4e,
    ushort ocr1b = 0x4b,
    ushort ocr1c = 0x4d,
    ushort tifr = 0x58,
    ushort timsk = 0x59,
    byte overflowInterrupt = 0x04,
    byte comparatorAInterrupt = 0x03,
    byte comparatorBInterrupt = 0x09,
    byte tov1 = 1 << 2,
    byte ocf1a = 1 << 6,
    byte ocf1b = 1 << 5,
    byte toie1 = 1 << 2,
    byte ocie1a = 1 << 6,
    byte ocie1b = 1 << 5,
    ushort comparatorPort = 0x38,
    byte comparatorPinA = 1,
    byte comparatorPinB = 4,
    int[]? dividers = null)
{
    public readonly ushort TCCR1 = tccr1;
    public readonly ushort GTCCR = gtccr;
    public readonly ushort TCNT1 = tcnt1;
    public readonly ushort OCR1A = ocr1a;
    public readonly ushort OCR1B = ocr1b;
    public readonly ushort OCR1C = ocr1c;
    public readonly ushort TIFR = tifr;
    public readonly ushort TIMSK = timsk;

    public readonly byte OverflowInterrupt = overflowInterrupt;
    public readonly byte ComparatorAInterrupt = comparatorAInterrupt;
    public readonly byte ComparatorBInterrupt = comparatorBInterrupt;

    // TIFR bits
    public readonly byte TOV1 = tov1;
    public readonly byte OCF1A = ocf1a;
    public readonly byte OCF1B = ocf1b;

    // TIMSK bits
    public readonly byte TOIE1 = toie1;
    public readonly byte OCIE1A = ocie1a;
    public readonly byte OCIE1B = ocie1b;

    /// <summary>PORT register of the port that carries OC1A / OC1B (PORTB on the ATtiny85).</summary>
    public readonly ushort ComparatorPort = comparatorPort;
    public readonly byte ComparatorPinA = comparatorPinA;
    public readonly byte ComparatorPinB = comparatorPinB;

    /// <summary>CS13:CS10 to prescaler: 0 stops the timer, n selects 2^(n-1).</summary>
    public readonly int[] Dividers = dividers ?? DefaultDividers;

    public static readonly int[] DefaultDividers =
    [
        0, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384,
    ];
}

/// <summary>
/// ATtiny25/45/85 Timer/Counter1 (8-bit, with OCR1C as programmable TOP).
/// <para>
/// This chip's TC1 is not the ATmega timer: a single TCCR1 (CTC1, PWM1A, COM1A, CS13:0),
/// a GTCCR holding PWM1B and COM1B, a third compare register OCR1C, and prescalers up to
/// /16384. Ported from avr8js <c>timer-attiny.ts</c> (commit 82f9fa5). Differences from the
/// upstream port:
/// <list type="bullet">
/// <item>PWM mode is single slope (count 0..OCR1C, reset after the OCR1C match), as in the
/// datasheet, instead of phase correct.</item>
/// <item>OCR1C resets to 0xFF.</item>
/// <item>A TCNT1 above TOP counts up to 0xFF before wrapping, like the hardware does.</item>
/// <item>TIFR writes only touch the TC1 flags, so the register can be shared with Timer0.</item>
/// </list>
/// The complementary outputs of COM1x = 01 in PWM mode (PB0 and PB3) are not driven.
/// </para>
/// </summary>
public class AvrAttinyTimer1
{
    // TCCR1 bits
    private const int CTC1 = 1 << 7;
    private const int PWM1A = 1 << 6;
    private const int CS_MASK = 0x0f;

    // GTCCR bits
    private const int PWM1B = 1 << 6;
    private const int FOC1B = 1 << 3;
    private const int FOC1A = 1 << 2;
    private const int PSR1 = 1 << 1;
    private const int PSR0 = 1 << 0;

    private readonly Cpu _cpu;
    private readonly AvrAttinyTimer1Config _config;
    private readonly Action _countAction;

    private ulong _lastCycle;
    private int _tcnt;
    private int _tcntNext;
    private bool _tcntUpdated;
    private byte _ocrA;
    private byte _ocrB;
    private byte _ocrC = 0xff;
    private int _divider;
    private bool _updateDivider;

    private readonly AvrInterruptConfig _ovf;
    private readonly AvrInterruptConfig _ocfa;
    private readonly AvrInterruptConfig _ocfb;

    public AvrAttinyTimer1(Cpu cpu, AvrAttinyTimer1Config? config = null)
    {
        _cpu = cpu;
        _config = config ?? new AvrAttinyTimer1Config();
        _countAction = () => Count(true);

        _ovf = new AvrInterruptConfig(
            address: _config.OverflowInterrupt,
            enableRegister: _config.TIMSK,
            enableMask: _config.TOIE1,
            flagRegister: _config.TIFR,
            flagMask: _config.TOV1);
        _ocfa = new AvrInterruptConfig(
            address: _config.ComparatorAInterrupt,
            enableRegister: _config.TIMSK,
            enableMask: _config.OCIE1A,
            flagRegister: _config.TIFR,
            flagMask: _config.OCF1A);
        _ocfb = new AvrInterruptConfig(
            address: _config.ComparatorBInterrupt,
            enableRegister: _config.TIMSK,
            enableMask: _config.OCIE1B,
            flagRegister: _config.TIFR,
            flagMask: _config.OCF1B);

        cpu.Mmio.Data[_config.OCR1C] = 0xff;
        cpu.OnPeripheralReset += Reset;

        cpu.Mmio.RegisterRead(_config.TCNT1, _ =>
        {
            Count(false);
            return _cpu.Mmio.Data[_config.TCNT1] = (byte)_tcnt;
        });

        cpu.Mmio.RegisterWrite(_config.TCNT1, (value, _, _, _) =>
        {
            _tcntNext = value;
            _tcntUpdated = true;
            _cpu.UpdateClockEvent(_countAction, 0);
            if (_divider != 0)
            {
                TimerUpdated(_tcntNext, _tcntNext, false);
            }

            return false;
        });

        cpu.Mmio.RegisterWrite(_config.OCR1A, (value, _, _, _) => { _ocrA = value; return false; });
        cpu.Mmio.RegisterWrite(_config.OCR1B, (value, _, _, _) => { _ocrB = value; return false; });
        cpu.Mmio.RegisterWrite(_config.OCR1C, (value, _, _, _) => { _ocrC = value; return false; });

        cpu.Mmio.RegisterWrite(_config.TCCR1, (value, _, _, _) =>
        {
            // Bring the counter up to date with the old mode before the new one applies.
            Count(false);
            _cpu.Mmio.Data[_config.TCCR1] = value;
            _updateDivider = true;
            _cpu.ClearClockEvent(_countAction);
            _cpu.AddClockEvent(_countAction, 0);
            UpdateCompConfig();
            return true;
        });

        cpu.Mmio.RegisterWrite(_config.GTCCR, (value, _, _, _) =>
        {
            Count(false);
            if ((value & PSR1) != 0)
            {
                _lastCycle = _cpu.Cycles;
            }

            // FOC1x and PSRx are strobes and always read as zero.
            _cpu.Mmio.Data[_config.GTCCR] = (byte)(value & ~(FOC1A | FOC1B | PSR1 | PSR0));
            UpdateCompConfig();

            if ((value & FOC1A) != 0) ForceCompare(true);
            if ((value & FOC1B) != 0) ForceCompare(false);
            return true;
        });

        // TIFR and TIMSK are shared with Timer0: only touch the TC1 bits.
        cpu.Mmio.RegisterWrite(_config.TIFR, (value, oldValue, _, _) =>
        {
            // A hook that ran before this one may have overwritten the whole register.
            var mine = _config.TOV1 | _config.OCF1A | _config.OCF1B;
            _cpu.Mmio.Data[_config.TIFR] = (byte)((_cpu.Mmio.Data[_config.TIFR] & ~mine) | (oldValue & mine));
            _cpu.ClearInterruptByFlag(_ovf, value);
            _cpu.ClearInterruptByFlag(_ocfa, value);
            _cpu.ClearInterruptByFlag(_ocfb, value);
            return true;
        });

        cpu.Mmio.RegisterWrite(_config.TIMSK, (value, _, _, _) =>
        {
            _cpu.UpdateInterruptEnable(_ovf, value);
            _cpu.UpdateInterruptEnable(_ocfa, value);
            _cpu.UpdateInterruptEnable(_ocfb, value);
            return false;
        });
    }

    private byte Tccr1 => _cpu.Mmio.Data[_config.TCCR1];
    private byte Gtccr => _cpu.Mmio.Data[_config.GTCCR];
    private int CS => Tccr1 & CS_MASK;
    private bool CtcMode => (Tccr1 & CTC1) != 0;
    private bool PwmA => (Tccr1 & PWM1A) != 0;
    private bool PwmB => (Gtccr & PWM1B) != 0;
    private int ComA => (Tccr1 >> 4) & 0x3;
    private int ComB => (Gtccr >> 4) & 0x3;

    /// <summary>TOP is OCR1C in CTC and PWM modes, 0xFF otherwise.</summary>
    private int Top => CtcMode || PwmA || PwmB ? _ocrC : 0xff;

    public int DebugTCNT => _tcnt;

    public void Reset()
    {
        _divider = 0;
        _lastCycle = 0;
        _tcnt = 0;
        _tcntNext = 0;
        _tcntUpdated = false;
        _updateDivider = true;
        _ocrA = 0;
        _ocrB = 0;
        _ocrC = 0xff;
        _cpu.Mmio.Data[_config.TCCR1] = 0;
        _cpu.Mmio.Data[_config.GTCCR] = 0;
        _cpu.Mmio.Data[_config.TCNT1] = 0;
        _cpu.Mmio.Data[_config.OCR1A] = 0;
        _cpu.Mmio.Data[_config.OCR1B] = 0;
        _cpu.Mmio.Data[_config.OCR1C] = 0xff;
    }

    public void Count(bool reschedule)
    {
        var delta = _cpu.Cycles - _lastCycle;

        if (_divider != 0 && delta >= (ulong)_divider)
        {
            var counterDelta = delta / (ulong)_divider;
            _lastCycle += counterDelta * (ulong)_divider;
            var prev = _tcnt;
            var top = Top;
            var overflow = false;
            int newVal;

            if (prev > top && counterDelta < (ulong)(0x100 - prev))
            {
                // Past TOP (TCNT1 written above OCR1C): the counter only wraps at 0xFF.
                newVal = prev + (int)counterDelta;
            }
            else
            {
                var start = (ulong)prev;
                var steps = counterDelta;
                if (prev > top)
                {
                    steps -= (ulong)(0x100 - prev);
                    start = 0;
                    overflow = true;
                }

                var total = start + steps;
                if (total > (ulong)top) overflow = true;
                newVal = (int)(total % (ulong)(top + 1));
            }

            if (!_tcntUpdated)
            {
                _tcnt = newVal;
                TimerUpdated(newVal, prev, overflow);
            }

            if (overflow)
            {
                _cpu.SetInterruptFlag(_ovf);
            }
        }

        if (_tcntUpdated)
        {
            _tcnt = _tcntNext;
            _tcntUpdated = false;
        }

        if (_updateDivider)
        {
            var newDivider = _config.Dividers.Length > CS ? _config.Dividers[CS] : 0;
            _lastCycle = newDivider != 0 ? _cpu.Cycles : 0;
            _updateDivider = false;
            _divider = newDivider;
            if (newDivider != 0)
            {
                _cpu.AddClockEvent(_countAction, (int)(_lastCycle + (ulong)newDivider - _cpu.Cycles));
            }

            return;
        }

        if (reschedule && _divider != 0)
        {
            _cpu.AddClockEvent(_countAction, (int)(_lastCycle + (ulong)_divider - _cpu.Cycles));
        }
    }

    private void TimerUpdated(int value, int prev, bool wrapped)
    {
        var top = Top;
        UpdateChannel(true, value, prev, wrapped, top);
        UpdateChannel(false, value, prev, wrapped, top);
    }

    private void UpdateChannel(bool channelA, int value, int prev, bool wrapped, int top)
    {
        var ocr = channelA ? _ocrA : _ocrB;
        var com = channelA ? ComA : ComB;
        var pwm = channelA ? PwmA : PwmB;
        var reached = ocr <= top && CompareReached(value, prev, ocr);

        if (reached)
        {
            _cpu.SetInterruptFlag(channelA ? _ocfa : _ocfb);
        }

        if (com == 0) return;

        if (!pwm)
        {
            if (reached) DriveCompareMatch(channelA, com);
            return;
        }

        // Single slope PWM: the output takes its "bottom" level when the counter restarts and
        // flips to the "match" level at OCR1x. COM1x1:0 = 11 is the inverting mode.
        var inverting = com == 3;
        if (wrapped)
        {
            var matchedAfterWrap = ocr <= top && value >= ocr;
            SetPin(channelA, matchedAfterWrap != inverting ? PinOverrideMode.Clear : PinOverrideMode.Set);
        }
        else if (reached)
        {
            SetPin(channelA, inverting ? PinOverrideMode.Set : PinOverrideMode.Clear);
        }
    }

    private static bool CompareReached(int value, int prev, int ocr)
    {
        var overflow = prev > value;
        return (prev < ocr || overflow) && value >= ocr || prev < ocr && overflow;
    }

    private void ForceCompare(bool channelA)
    {
        var com = channelA ? ComA : ComB;
        var pwm = channelA ? PwmA : PwmB;
        if (!pwm && com != 0)
        {
            DriveCompareMatch(channelA, com);
        }
    }

    private void DriveCompareMatch(bool channelA, int com)
    {
        var mode = com switch
        {
            1 => PinOverrideMode.Toggle,
            2 => PinOverrideMode.Clear,
            3 => PinOverrideMode.Set,
            _ => PinOverrideMode.None,
        };
        if (mode != PinOverrideMode.None) SetPin(channelA, mode);
    }

    private void SetPin(bool channelA, PinOverrideMode mode)
    {
        if (_cpu.GpioByPort.TryGetValue(_config.ComparatorPort, out var port))
        {
            port.TimerOverridePin(channelA ? _config.ComparatorPinA : _config.ComparatorPinB, mode);
        }
    }

    private void UpdateCompConfig()
    {
        if (!_cpu.GpioByPort.TryGetValue(_config.ComparatorPort, out var port)) return;
        port.TimerOverridePin(_config.ComparatorPinA, ComA != 0 ? PinOverrideMode.Enable : PinOverrideMode.None);
        port.TimerOverridePin(_config.ComparatorPinB, ComB != 0 ? PinOverrideMode.Enable : PinOverrideMode.None);
        ApplyPwmLevel(true);
        ApplyPwmLevel(false);
    }

    /// <summary>Puts a freshly enabled PWM output at the level the waveform has at the current count.</summary>
    private void ApplyPwmLevel(bool channelA)
    {
        var com = channelA ? ComA : ComB;
        if (com == 0 || !(channelA ? PwmA : PwmB)) return;
        var ocr = channelA ? _ocrA : _ocrB;
        var matched = ocr <= Top && _tcnt >= ocr;
        SetPin(channelA, matched != (com == 3) ? PinOverrideMode.Clear : PinOverrideMode.Set);
    }
}
