using AVR8Sharp.Core.Cpu;

namespace AVR8Sharp.Core.Peripherals;

public class AvrTimer
{
    // Force Output Compare (FOC) bits
    const int FOCA = 1 << 7;
    const int FOCB = 1 << 6;
    const int FOCC = 1 << 5;

    const int TOP_OCRA = 1;
    const int TOP_ICR = 2;

    const int OC_TOGGLE = 1;

    public static readonly int[] Timer01Dividers = new[]
    {
        0,
        1,
        8,
        64,
        256,
        1024,
        0, // External clock - see ExternalClockMode
        0, // Ditto
    };

    public static readonly AvrTimerConfig DefaultTimerBits = new AvrTimerConfig(
        // TIFR bits
        tov: 1,
        ocfa: 2,
        ocfb: 4,
        ocfc: 0, // Unused

        // TIMSK bits
        toie: 1,
        ociea: 2,
        ocieb: 4,
        ociec: 0 // Unused
    );

    public static readonly AvrTimerConfig Timer0Config = new AvrTimerConfig(
        bits: 8,
        dividers: Timer01Dividers,
        captureInterrupt: 0, // Not Available
        comparatorAInterrupt: 0x1c,
        comparatorBInterrupt: 0x1e,
        comparatorCInterrupt: 0,
        overflowInterrupt: 0x20,
        tifr: 0x35,
        ocra: 0x47,
        ocrb: 0x48,
        ocrc: 0, // Not Available
        icr: 0, // Not Available
        tcnt: 0x46,
        tccra: 0x44,
        tccrb: 0x45,
        tccrc: 0, // Not Available
        timsk: 0x6e,
        comparatorPortA: AvrIoPort.PortDConfig.PORT,
        comparatorPinA: 6,
        comparatorPortB: AvrIoPort.PortDConfig.PORT,
        comparatorPinB: 5,
        comparatorPortC: 0, // Not Available
        comparatorPinC: 0,
        externalClockPort: AvrIoPort.PortDConfig.PORT,
        externalClockPin: 4,
        // Apply default bits
        tov: DefaultTimerBits.TOV,
        ocfa: DefaultTimerBits.OCFA,
        ocfb: DefaultTimerBits.OCFB,
        ocfc: DefaultTimerBits.OCFC,
        toie: DefaultTimerBits.TOIE,
        ociea: DefaultTimerBits.OCIEA,
        ocieb: DefaultTimerBits.OCIEB,
        ociec: DefaultTimerBits.OCIEC
    );

    public static readonly AvrTimerConfig Timer1Config = new AvrTimerConfig(
        bits: 16,
        dividers: Timer01Dividers,
        captureInterrupt: 0x14,
        comparatorAInterrupt: 0x16,
        comparatorBInterrupt: 0x18,
        comparatorCInterrupt: 0,
        overflowInterrupt: 0x1a,
        tifr: 0x36,
        ocra: 0x88,
        ocrb: 0x8a,
        ocrc: 0, // Not Available
        icr: 0x86,
        tcnt: 0x84,
        tccra: 0x80,
        tccrb: 0x81,
        tccrc: 0x82,
        timsk: 0x6f,
        comparatorPortA: AvrIoPort.PortBConfig.PORT,
        comparatorPinA: 1,
        comparatorPortB: AvrIoPort.PortBConfig.PORT,
        comparatorPinB: 2,
        comparatorPortC: 0, // Not Available
        comparatorPinC: 0,
        externalClockPort: AvrIoPort.PortDConfig.PORT,
        externalClockPin: 5,
        // Apply default bits
        tov: DefaultTimerBits.TOV,
        ocfa: DefaultTimerBits.OCFA,
        ocfb: DefaultTimerBits.OCFB,
        ocfc: DefaultTimerBits.OCFC,
        icf: 0x20,  // ICF1 = TIFR1 bit 5 (ATmega328P datasheet)
        toie: DefaultTimerBits.TOIE,
        ociea: DefaultTimerBits.OCIEA,
        ocieb: DefaultTimerBits.OCIEB,
        ociec: DefaultTimerBits.OCIEC,
        icie: 0x20  // ICIE1 = TIMSK1 bit 5 (ATmega328P datasheet)
    );

    public static readonly AvrTimerConfig Timer2Config = new AvrTimerConfig(
        bits: 8,
        dividers:
        [
            0,
            1,
            8,
            32,
            64,
            128,
            256,
            1024
        ],
        captureInterrupt: 0, // Not Available
        comparatorAInterrupt: 0x0e,
        comparatorBInterrupt: 0x10,
        comparatorCInterrupt: 0,
        overflowInterrupt: 0x12,
        tifr: 0x37,
        ocra: 0xb3,
        ocrb: 0xb4,
        ocrc: 0, // Not Available
        icr: 0, // Not Available
        tcnt: 0xb2,
        tccra: 0xb0,
        tccrb: 0xb1,
        tccrc: 0, // Not Available
        timsk: 0x70,
        comparatorPortA: AvrIoPort.PortBConfig.PORT,
        comparatorPinA: 3,
        comparatorPortB: AvrIoPort.PortDConfig.PORT,
        comparatorPinB: 3,
        comparatorPortC: 0, // Not Available
        comparatorPinC: 0,
        externalClockPort: 0, // Not Available
        externalClockPin: 0,
        // Apply default bits
        tov: DefaultTimerBits.TOV,
        ocfa: DefaultTimerBits.OCFA,
        ocfb: DefaultTimerBits.OCFB,
        ocfc: DefaultTimerBits.OCFC,
        toie: DefaultTimerBits.TOIE,
        ociea: DefaultTimerBits.OCIEA,
        ocieb: DefaultTimerBits.OCIEB,
        ociec: DefaultTimerBits.OCIEC
    );

    public static readonly WgmConfig[] WgmModes8Bit =
    [
        new WgmConfig(mode: TimerMode.Normal, timerTopValue: 0xff, ocrUpdateMode: OcrUpdateMode.Immediate,
            tovUpdateMode: TovUpdateMode.Max, flags: 0),
        new WgmConfig(mode: TimerMode.PWMPhaseCorrect, timerTopValue: 0xff, ocrUpdateMode: OcrUpdateMode.Top,
            tovUpdateMode: TovUpdateMode.Bottom, flags: 0),
        new WgmConfig(mode: TimerMode.CTC, timerTopValue: TOP_OCRA, ocrUpdateMode: OcrUpdateMode.Immediate,
            tovUpdateMode: TovUpdateMode.Max, flags: 0),
        new WgmConfig(mode: TimerMode.FastPWM, timerTopValue: 0xff, ocrUpdateMode: OcrUpdateMode.Bottom,
            tovUpdateMode: TovUpdateMode.Max, flags: 0),
        new WgmConfig(mode: TimerMode.Reserved, timerTopValue: 0xff, ocrUpdateMode: OcrUpdateMode.Immediate,
            tovUpdateMode: TovUpdateMode.Max, flags: 0),
        new WgmConfig(mode: TimerMode.PWMPhaseCorrect, timerTopValue: TOP_OCRA, ocrUpdateMode: OcrUpdateMode.Top,
            tovUpdateMode: TovUpdateMode.Bottom, flags: OC_TOGGLE),
        new WgmConfig(mode: TimerMode.Reserved, timerTopValue: 0xff, ocrUpdateMode: OcrUpdateMode.Immediate,
            tovUpdateMode: TovUpdateMode.Max, flags: 0),
        new WgmConfig(mode: TimerMode.FastPWM, timerTopValue: TOP_OCRA, ocrUpdateMode: OcrUpdateMode.Bottom,
            tovUpdateMode: TovUpdateMode.Top, flags: OC_TOGGLE),
    ];

    public static readonly WgmConfig[] WgmModes16Bits =
    [
        new WgmConfig(mode: TimerMode.Normal, timerTopValue: 0xffff, ocrUpdateMode: OcrUpdateMode.Immediate,
            tovUpdateMode: TovUpdateMode.Max, flags: 0),
        new WgmConfig(mode: TimerMode.PWMPhaseCorrect, timerTopValue: 0x00ff, ocrUpdateMode: OcrUpdateMode.Top,
            tovUpdateMode: TovUpdateMode.Bottom, flags: 0),
        new WgmConfig(mode: TimerMode.PWMPhaseCorrect, timerTopValue: 0x01ff, ocrUpdateMode: OcrUpdateMode.Top,
            tovUpdateMode: TovUpdateMode.Bottom, flags: 0),
        new WgmConfig(mode: TimerMode.PWMPhaseCorrect, timerTopValue: 0x03ff, ocrUpdateMode: OcrUpdateMode.Top,
            tovUpdateMode: TovUpdateMode.Bottom, flags: 0),
        new WgmConfig(mode: TimerMode.CTC, timerTopValue: TOP_OCRA, ocrUpdateMode: OcrUpdateMode.Immediate,
            tovUpdateMode: TovUpdateMode.Max, flags: 0),
        new WgmConfig(mode: TimerMode.FastPWM, timerTopValue: 0x00ff, ocrUpdateMode: OcrUpdateMode.Bottom,
            tovUpdateMode: TovUpdateMode.Top, flags: 0),
        new WgmConfig(mode: TimerMode.FastPWM, timerTopValue: 0x01ff, ocrUpdateMode: OcrUpdateMode.Bottom,
            tovUpdateMode: TovUpdateMode.Top, flags: 0),
        new WgmConfig(mode: TimerMode.FastPWM, timerTopValue: 0x03ff, ocrUpdateMode: OcrUpdateMode.Bottom,
            tovUpdateMode: TovUpdateMode.Top, flags: 0),
        new WgmConfig(mode: TimerMode.PWMPhaseFrequencyCorrect, timerTopValue: TOP_ICR,
            ocrUpdateMode: OcrUpdateMode.Bottom, tovUpdateMode: TovUpdateMode.Bottom, flags: 0),
        new WgmConfig(mode: TimerMode.PWMPhaseFrequencyCorrect, timerTopValue: TOP_OCRA,
            ocrUpdateMode: OcrUpdateMode.Bottom, tovUpdateMode: TovUpdateMode.Bottom, flags: OC_TOGGLE),
        new WgmConfig(mode: TimerMode.PWMPhaseCorrect, timerTopValue: TOP_ICR, ocrUpdateMode: OcrUpdateMode.Top,
            tovUpdateMode: TovUpdateMode.Bottom, flags: 0),
        new WgmConfig(mode: TimerMode.PWMPhaseCorrect, timerTopValue: TOP_OCRA, ocrUpdateMode: OcrUpdateMode.Top,
            tovUpdateMode: TovUpdateMode.Bottom, flags: OC_TOGGLE),
        new WgmConfig(mode: TimerMode.CTC, timerTopValue: TOP_ICR, ocrUpdateMode: OcrUpdateMode.Immediate,
            tovUpdateMode: TovUpdateMode.Max, flags: 0),
        new WgmConfig(mode: TimerMode.Reserved, timerTopValue: 0xffff, ocrUpdateMode: OcrUpdateMode.Immediate,
            tovUpdateMode: TovUpdateMode.Max, flags: 0),
        new WgmConfig(mode: TimerMode.FastPWM, timerTopValue: TOP_ICR, ocrUpdateMode: OcrUpdateMode.Bottom,
            tovUpdateMode: TovUpdateMode.Top, flags: OC_TOGGLE),
        new WgmConfig(mode: TimerMode.FastPWM, timerTopValue: TOP_OCRA, ocrUpdateMode: OcrUpdateMode.Bottom,
            tovUpdateMode: TovUpdateMode.Top, flags: OC_TOGGLE),
    ];

    private readonly Cpu.Cpu _cpu;
    private readonly AvrTimerConfig _config;

    private readonly int _max;
    private readonly bool _hasCaptureInterrupt;
    private int _lastCycle = 0;
    private ushort _ocrA = 0;
    private ushort _nextOcrA = 0;
    private ushort _ocrB = 0;
    private ushort _nextOcrB = 0;
    private readonly bool _hasOcrC;
    private ushort _ocrC = 0;
    private ushort _nextOcrC = 0;
    private OcrUpdateMode _ocrUpdateMode = OcrUpdateMode.Immediate;
    private TovUpdateMode _tovUpdateMode = TovUpdateMode.Max;
    private ushort _icr = 0; // Only for 16-bit timers
    private TimerMode _timerMode;
    private int _topValue;
    private ushort _tcnt = 0;
    private ushort _tcntNext = 0;
    private byte _compA = 0;
    private byte _compB = 0;
    private byte _compC = 0;
    private bool _tcntUpdated = false;
    private bool _updateDivider = false;
    private bool _countingUp = true;
    private int _divider = 0;
    private int _cachedTop;
    private AvrIoPort? _externalClockPort;
    private bool _externalClockRisingEdge = false;
    private readonly Action _countAction;

    // This is the temporary register used to access 16-bit registers (section 16.3 of the datasheet)
    private byte _highByteTemp = 0;

    // Interrupts
    private readonly AvrInterruptConfig _ovf;
    private readonly AvrInterruptConfig _ocfa;
    private readonly AvrInterruptConfig _ocfb;
    private readonly AvrInterruptConfig _ocfc;
    private readonly AvrInterruptConfig? _capt; // Input Capture — only for 16-bit timers

    public byte TCCRA
    {
        get { return _cpu.Mmio.Data[_config.TCCRA]; }
    }

    public byte TCCRB
    {
        get { return _cpu.Mmio.Data[_config.TCCRB]; }
    }

    public byte TIMSK
    {
        get { return _cpu.Mmio.Data[_config.TIMSK]; }
    }

    public int CS
    {
        get { return TCCRB & 0x7; }
    }

    public int WGM
    {
        get
        {
            var mask = _config.Bits == 16 ? 0x18 : 0x8;
            return ((TCCRB & mask) >> 1) | (TCCRA & 0x3);
        }
    }

    public int TOP
    {
        get
        {
            switch (_topValue)
            {
                case TOP_OCRA:
                    return _ocrA;
                case TOP_ICR:
                    return _icr;
                default:
                    return _topValue;
            }
        }
    }

    public int OcrMask
    {
        get
        {
            switch (_topValue)
            {
                case TOP_OCRA:
                case TOP_ICR:
                    return 0xffff;
                default:
                    return _topValue;
            }
        }
    }

    public int DebugTCNT
    {
        get { return _tcnt; }
    }

    public AvrTimer(Cpu.Cpu cpu, AvrTimerConfig config)
    {
        _cpu = cpu;
        _config = config;

        _max = config.Bits == 16 ? 0xffff : 0xff;
        _hasOcrC = config.OCRC != 0;
        _hasCaptureInterrupt = config.CaptureInterrupt != 0 && config.ICF != 0;

        _countAction = () => Count(true, false);

        _ovf = new AvrInterruptConfig(
            address: config.OverflowInterrupt,
            enableRegister: config.TIMSK,
            enableMask: config.TOIE,
            flagRegister: config.TIFR,
            flagMask: config.TOV
        );

        _ocfa = new AvrInterruptConfig(
            address: config.ComparatorAInterrupt,
            enableRegister: config.TIMSK,
            enableMask: config.OCIEA,
            flagRegister: config.TIFR,
            flagMask: config.OCFA
        );

        _ocfb = new AvrInterruptConfig(
            address: config.ComparatorBInterrupt,
            enableRegister: config.TIMSK,
            enableMask: config.OCIEB,
            flagRegister: config.TIFR,
            flagMask: config.OCFB
        );

        _ocfc = new AvrInterruptConfig(
            address: config.ComparatorCInterrupt,
            enableRegister: config.TIMSK,
            enableMask: config.OCIEC,
            flagRegister: config.TIFR,
            flagMask: config.OCFC
        );

        if (_hasCaptureInterrupt)
        {
            _capt = new AvrInterruptConfig(
                address: config.CaptureInterrupt,
                enableRegister: config.TIMSK,
                enableMask: config.ICIE,
                flagRegister: config.TIFR,
                flagMask: config.ICF
            );
        }

        UpdateWgmConfig();

        cpu.Mmio.RegisterRead(config.TCNT, ReadTcnt);
        cpu.Mmio.RegisterWrite(config.TCNT, WriteTcnt);

        cpu.Mmio.RegisterWrite(config.OCRA, WriteOcra);
        cpu.Mmio.RegisterWrite(config.OCRB, WriteOcrb);
        if (_hasOcrC)
        {
            cpu.Mmio.RegisterWrite(config.OCRC, WriteOcrc);
        }

        if (_config.Bits == 16)
        {
            cpu.Mmio.RegisterWrite(config.ICR, WriteIcr);

            Func<byte, byte, ushort, byte, bool> updateTempRegister = (value, _, _, _) =>
            {
                _highByteTemp = value;
                return false;
            };
            Func<byte, byte, ushort, byte, bool> updateOCRHighRegister = (value, old, addr, _) =>
            {
                _highByteTemp = (byte)(value & (OcrMask >> 8));
                _cpu.Mmio.Data[addr] = _highByteTemp;
                return true;
            };

            cpu.Mmio.RegisterWrite((ushort)(config.TCNT + 1), updateTempRegister);
            cpu.Mmio.RegisterWrite((ushort)(config.OCRA + 1), updateOCRHighRegister);
            cpu.Mmio.RegisterWrite((ushort)(config.OCRB + 1), updateOCRHighRegister);
            if (_hasOcrC)
            {
                cpu.Mmio.RegisterWrite((ushort)(config.OCRC + 1), updateOCRHighRegister);
            }

            cpu.Mmio.RegisterWrite((ushort)(config.ICR + 1), updateOCRHighRegister);
        }

        cpu.Mmio.RegisterWrite(config.TCCRA, (value, _, _, _) =>
        {
            _cpu.Mmio.Data[config.TCCRA] = value;
            UpdateWgmConfig();
            return true;
        });

        cpu.Mmio.RegisterWrite(config.TCCRB, (value, _, _, _) =>
        {
            if (_config.TCCRC == 0)
            {
                CheckForceCompare(value);
                value &= ~(FOCA | FOCB) & 0xff;
            }

            _cpu.Mmio.Data[_config.TCCRB] = value;
            _updateDivider = true;
            _cpu.ClearClockEvent(_countAction);
            _cpu.AddClockEvent(_countAction, 0);
            UpdateWgmConfig();
            return true;
        });

        if (_config.TCCRC != 0)
        {
            cpu.Mmio.RegisterWrite(config.TCCRC, (value, _, _, _) =>
            {
                CheckForceCompare(value);
                return false;
            });
        }

        cpu.Mmio.RegisterWrite(config.TIFR, (value, _, _, _) =>
        {
            _cpu.Mmio.Data[config.TIFR] = value;
            _cpu.ClearInterruptByFlag(_ovf, value);
            _cpu.ClearInterruptByFlag(_ocfa, value);
            _cpu.ClearInterruptByFlag(_ocfb, value);
            if (_hasOcrC) _cpu.ClearInterruptByFlag(_ocfc, value);
            if (_hasCaptureInterrupt) _cpu.ClearInterruptByFlag(_capt!, value);
            return true;
        });

        cpu.Mmio.RegisterWrite(config.TIMSK, (value, _, _, _) =>
        {
            _cpu.UpdateInterruptEnable(_ovf, value);
            _cpu.UpdateInterruptEnable(_ocfa, value);
            _cpu.UpdateInterruptEnable(_ocfb, value);
            if (_hasOcrC) _cpu.UpdateInterruptEnable(_ocfc, value);
            if (_hasCaptureInterrupt) _cpu.UpdateInterruptEnable(_capt!, value);
            return false;
        });
    }

    private byte ReadTcnt(ushort addr)
    {
        Count(false);
        if (_config.Bits == 16)
        {
            _cpu.Mmio.Data[addr + 1] = (byte)(_tcnt >> 8);
        }

        return _cpu.Mmio.Data[addr] = (byte)(_tcnt & 0xff);
    }

    private bool WriteTcnt(byte value, byte _, ushort __, byte ___)
    {
        _tcntNext = (ushort)((_highByteTemp << 8) | value);
        _countingUp = true;
        _tcntUpdated = true;
        _cpu.UpdateClockEvent(_countAction, 0);
        if (_divider != 0)
        {
            TimerUpdated(_tcntNext, _tcntNext);
        }

        return false;
    }
    
    private bool WriteOcra(byte value, byte _, ushort __, byte ___)
    {
        _nextOcrA = (ushort)((_highByteTemp << 8) | value);
        if (_ocrUpdateMode == OcrUpdateMode.Immediate)
        {
            _ocrA = _nextOcrA;
            UpdateCachedTop();
        }

        return false;
    }
    
    private bool WriteOcrb(byte value, byte _, ushort __, byte ___)
    {
        _nextOcrB = (ushort)((_highByteTemp << 8) | value);
        if (_ocrUpdateMode == OcrUpdateMode.Immediate)
        {
            _ocrB = _nextOcrB;
        }

        return false;
    }
    
    private bool WriteOcrc(byte value, byte _, ushort __, byte ___)
    {
        _nextOcrC = (ushort)((_highByteTemp << 8) | value);
        if (_ocrUpdateMode == OcrUpdateMode.Immediate)
        {
            _ocrC = _nextOcrC;
        }

        return false;
    }
    
    private bool WriteIcr(byte value, byte _, ushort __, byte ___)
    {
        _icr = (ushort)((_highByteTemp << 8) | value);
        UpdateCachedTop();
        return false;
    }

    public void Reset()
    {
        _divider = 0;
        _lastCycle = 0;
        _ocrA = 0;
        _nextOcrA = 0;
        _ocrB = 0;
        _nextOcrB = 0;
        _ocrC = 0;
        _nextOcrC = 0;
        _icr = 0;
        _tcnt = 0;
        _tcntNext = 0;
        _tcntUpdated = false;
        _countingUp = false;
        _updateDivider = true;
    }

    /// <summary>
    /// Trigger an Input Capture event (equivalent to an edge on the ICPn pin).
    /// Captures the current TCNT value into ICR and sets the ICF flag.
    /// Only has effect on 16-bit timers that have a capture interrupt configured.
    /// </summary>
    public void TriggerCapture()
    {
        if (!_hasCaptureInterrupt || _capt == null) return;

        // Capture current TCNT value into ICR (per AVR spec §16.6.3)
        Count(false);
        _icr = _tcnt;
        UpdateCachedTop();

        // Update the 16-bit ICR register in memory so firmware can read it
        _cpu.Mmio.Data[_config.ICR] = (byte)(_icr & 0xff);
        _cpu.Mmio.Data[_config.ICR + 1] = (byte)(_icr >> 8);

        _cpu.SetInterruptFlag(_capt);
    }

    private void UpdateWgmConfig()
    {
        var wgmModes = _config.Bits == 16 ? WgmModes16Bits : WgmModes8Bit;
        if (wgmModes.Length <= WGM)
        {
            return;
        }

        var wgmConfig = wgmModes[WGM];
        _timerMode = wgmConfig.Mode;
        _topValue = wgmConfig.TimerTopValue;
        UpdateCachedTop();
        _ocrUpdateMode = wgmConfig.OCRUpdateMode;
        _tovUpdateMode = wgmConfig.TOVUpdateMode;
        var flags = wgmConfig.Flags;

        var pwmMode = _timerMode == TimerMode.FastPWM ||
                      _timerMode == TimerMode.PWMPhaseCorrect ||
                      _timerMode == TimerMode.PWMPhaseFrequencyCorrect;

        var prevCompA = _compA;
        _compA = (byte)((TCCRA >> 6) & 0x3);
        if (_compA == 1 && pwmMode && (flags & OC_TOGGLE) == 0)
        {
            _compA = 0;
        }

        if (prevCompA != _compA)
        {
            UpdateCompA(_compA != 0 ? PinOverrideMode.Enable : PinOverrideMode.None);
        }

        var prevCompB = _compB;
        _compB = (byte)((TCCRA >> 4) & 0x3);
        if (_compB == 1 && pwmMode)
        {
            _compB = 0; // Reserved, according to the datasheet
        }

        if (prevCompB != _compB)
        {
            UpdateCompB(_compB != 0 ? PinOverrideMode.Enable : PinOverrideMode.None);
        }

        if (!_hasOcrC) return;
        var prevCompC = _compC;
        _compC = (byte)((TCCRA >> 2) & 0x3);
        if (_compC == 1 && pwmMode)
        {
            _compC = 0; // Reserved, according to the datasheet
        }

        if (prevCompC != _compC)
        {
            UpdateCompC(_compC != 0 ? PinOverrideMode.Enable : PinOverrideMode.None);
        }
        
        UpdateCachedTop();
    }

    // original count function
    public void Count(bool reschedule, bool external = false)
    {
        var delta = _cpu.Cycles - _lastCycle;

        if (_divider != 0 && delta >= _divider || external)
        {
            var counterDelta = external ? 1 : delta / _divider;
            _lastCycle += counterDelta * _divider;
            var val = _tcnt;
            var phasePwm = _timerMode == TimerMode.PWMPhaseCorrect || _timerMode == TimerMode.PWMPhaseFrequencyCorrect;
            int newVal;
            if (phasePwm) 
            {
                newVal = PhasePwmCount(val, (byte)counterDelta);
            } 
            else 
            {
                newVal = val + counterDelta;
                while (newVal > _cachedTop) 
                {
                    newVal -= (_cachedTop + 1);
                }
            }
            var overflow = val + counterDelta > _cachedTop;
            // A CPU write overrides (has priority over) all counter clear or count operations.
            if (!_tcntUpdated)
            {
                _tcnt = (ushort)newVal;
                if (!phasePwm)
                {
                    TimerUpdated(newVal, val);
                }
            }

            if (!phasePwm)
            {
                if (_timerMode == TimerMode.FastPWM && overflow)
                {
                    if (_compA != 0)
                    {
                        UpdateCompPin(_compA, 'A', true);
                    }

                    if (_compB != 0)
                    {
                        UpdateCompPin(_compB, 'B', true);
                    }
                }

                if (_ocrUpdateMode == OcrUpdateMode.Bottom && overflow)
                {
                    // OCRUpdateMode.Top only occurs in Phase Correct modes, handled by phasePwmCount()
                    _ocrA = _nextOcrA;
                    _ocrB = _nextOcrB;
                    _ocrC = _nextOcrC;
                    UpdateCachedTop();
                }

                // OCRUpdateMode.Bottom only occurs in Phase Correct modes, handled by phasePwmCount().
                // Thus we only handle TOVUpdateMode.Top or TOVUpdateMode.Max here.
                if (overflow && (_tovUpdateMode == TovUpdateMode.Top || _cachedTop == _max))
                {
                    _cpu.SetInterruptFlag(_ovf);
                }
            }
        }

        if (_tcntUpdated)
        {
            _tcnt = _tcntNext;
            _tcntUpdated = false;
            if (_tcnt == 0 && _ocrUpdateMode == OcrUpdateMode.Bottom ||
                _tcnt == _cachedTop && _ocrUpdateMode == OcrUpdateMode.Top)
            {
                _ocrA = _nextOcrA;
                _ocrB = _nextOcrB;
                _ocrC = _nextOcrC;
                UpdateCachedTop();
            }
        }

        if (_updateDivider)
        {
            var newDivider = _config.Dividers?[CS] ?? 0;
            _lastCycle = newDivider != 0 ? _cpu.Cycles : 0;
            _updateDivider = false;
            _divider = newDivider;
            if (_config.ExternalClockPort != 0 && _externalClockPort == null)
            {
                _externalClockPort = _cpu.GpioByPort.GetValueOrDefault(_config.ExternalClockPort);
            }

            if (_externalClockPort != null)
            {
                _externalClockPort.ExternalClockListeners[_config.ExternalClockPin] = null;
            }

            if (newDivider != 0)
            {
                _cpu.AddClockEvent(_countAction, _lastCycle + newDivider - _cpu.Cycles);
            }
            else if (_externalClockPort != null &&
                     (CS == (int)ExternalClockMode.FallingEdge || CS == (int)ExternalClockMode.RisingEdge))
            {
                _externalClockPort.ExternalClockListeners[_config.ExternalClockPin] = ExternalClockCallback;
                _externalClockRisingEdge = CS == (int)ExternalClockMode.RisingEdge;
            }

            return;
        }

        if (reschedule && _divider != 0)
        {
            _cpu.AddClockEvent(_countAction, _lastCycle + _divider - _cpu.Cycles);
        }
    }
    
    private void UpdateCachedTop()
    {
        switch (_topValue)
        {
            case TOP_OCRA:
                _cachedTop = _ocrA;
                break;
            case TOP_ICR:
                _cachedTop = _icr;
                break;
            default:
                _cachedTop = _topValue;
                break;
        }
    }

    private void ExternalClockCallback(bool value)
    {
        if (value == _externalClockRisingEdge)
        {
            Count(false, true);
        }
    }

    private int PhasePwmCount(ushort value, byte delta)
    {
        if (value == 0 && TOP == 0)
        {
            delta = 0;
            if (_ocrUpdateMode == OcrUpdateMode.Top)
            {
                _ocrA = _nextOcrA;
                _ocrB = _nextOcrB;
                _ocrC = _nextOcrC;
                UpdateCachedTop();
            }
        }
        
        if (delta == 1)
        {
            if (_countingUp)
            {
                value++;
                if (value == _cachedTop && !_tcntUpdated)
                {
                    _countingUp = false;
                    if (_ocrUpdateMode == OcrUpdateMode.Top)
                    {
                        _ocrA = _nextOcrA;
                        _ocrB = _nextOcrB;
                        _ocrC = _nextOcrC;
                        UpdateCachedTop();
                    }
                }
            }
            else
            {
                value--;
                if (value == 0 && !_tcntUpdated)
                {
                    _countingUp = true;
                    _cpu.SetInterruptFlag(_ovf);
                    if (_ocrUpdateMode == OcrUpdateMode.Bottom)
                    {
                        _ocrA = _nextOcrA;
                        _ocrB = _nextOcrB;
                        _ocrC = _nextOcrC;
                        UpdateCachedTop();
                    }
                }
            }

            if (!_tcntUpdated)
            {
                if (value == _ocrA) { _cpu.SetInterruptFlag(_ocfa); if (_compA != 0) UpdateCompPin(_compA, 'A'); }
                if (value == _ocrB) { _cpu.SetInterruptFlag(_ocfb); if (_compB != 0) UpdateCompPin(_compB, 'B'); }
                if (_hasOcrC && value == _ocrC) { _cpu.SetInterruptFlag(_ocfc); if (_compC != 0) UpdateCompPin(_compC, 'C'); }
            }

            return value & _max;
        }

        while (delta > 0)
        {
            if (_countingUp)
            {
                value++;
                if (value == TOP && !_tcntUpdated)
                {
                    _countingUp = false;
                    if (_ocrUpdateMode == OcrUpdateMode.Top)
                    {
                        _ocrA = _nextOcrA;
                        _ocrB = _nextOcrB;
                        _ocrC = _nextOcrC;
                        UpdateCachedTop();
                    }
                }
            }
            else
            {
                value--;
                if (value == 0 && !_tcntUpdated)
                {
                    _countingUp = true;
                    _cpu.SetInterruptFlag(_ovf);
                    if (_ocrUpdateMode == OcrUpdateMode.Bottom)
                    {
                        _ocrA = _nextOcrA;
                        _ocrB = _nextOcrB;
                        _ocrC = _nextOcrC;
                        UpdateCachedTop();
                    }
                }
            }

            if (!_tcntUpdated)
            {
                if (value == _ocrA)
                {
                    _cpu.SetInterruptFlag(_ocfa);
                    if (_compA != 0)
                    {
                        UpdateCompPin(_compA, 'A');
                    }
                }

                if (value == _ocrB)
                {
                    _cpu.SetInterruptFlag(_ocfb);
                    if (_compB != 0)
                    {
                        UpdateCompPin(_compB, 'B');
                    }
                }

                if (_hasOcrC && value == _ocrC)
                {
                    _cpu.SetInterruptFlag(_ocfc);
                    if (_compC != 0)
                    {
                        UpdateCompPin(_compC, 'C');
                    }
                }
            }

            delta--;
        }

        return value & _max;
    }

    private void TimerUpdated(int value, int prevNumber)
    {
        var overflow = prevNumber > value;
        if ((prevNumber < _ocrA || overflow) && value >= _ocrA || prevNumber < _ocrA && overflow)
        {
            _cpu.SetInterruptFlag(_ocfa);
            if (_compA != 0)
            {
                UpdateCompPin(_compA, 'A');
            }
        }

        if ((prevNumber < _ocrB || overflow) && value >= _ocrB || prevNumber < _ocrB && overflow)
        {
            _cpu.SetInterruptFlag(_ocfb);
            if (_compB != 0)
            {
                UpdateCompPin(_compB, 'B');
            }
        }

        if (_hasOcrC && ((prevNumber < _ocrC || overflow) && value >= _ocrC || prevNumber < _ocrC && overflow))
        {
            _cpu.SetInterruptFlag(_ocfc);
            if (_compC != 0)
            {
                UpdateCompPin(_compC, 'C');
            }
        }
    }

    private void CheckForceCompare(int value)
    {
        if (_timerMode == TimerMode.FastPWM || _timerMode == TimerMode.PWMPhaseCorrect ||
            _timerMode == TimerMode.PWMPhaseFrequencyCorrect)
        {
            // The FOCnA/FOCnB/FOCnC bits are only active when the WGMn3:0 bits specifies a non-PWM mode
            return;
        }

        if ((value & FOCA) != 0)
        {
            UpdateCompPin(_compA, 'A');
        }

        if ((value & FOCB) != 0)
        {
            UpdateCompPin(_compB, 'B');
        }

        if (_config.ComparatorPortC != 0 && (value & FOCC) != 0)
        {
            UpdateCompPin(_compC, 'C');
        }
    }

    private void UpdateCompPin(byte compValue, char pinName, bool bottom = false)
    {
        var newValue = PinOverrideMode.None;
        var invertingMode = compValue == 3;
        var isSet = _countingUp == invertingMode;
        switch (_timerMode)
        {
            case TimerMode.Normal:
            case TimerMode.CTC:
                newValue = CompToOverride(compValue);
                break;
            case TimerMode.FastPWM:
                if (compValue == 1)
                    newValue = bottom ? PinOverrideMode.None : PinOverrideMode.Toggle;
                else
                    newValue = invertingMode ^ bottom ? PinOverrideMode.Set : PinOverrideMode.Clear;
                break;
            case TimerMode.PWMPhaseCorrect:
            case TimerMode.PWMPhaseFrequencyCorrect:
                if (compValue == 1)
                    newValue = PinOverrideMode.Toggle;
                else
                    newValue = isSet ? PinOverrideMode.Set : PinOverrideMode.Clear;
                break;
        }

        if (newValue != PinOverrideMode.None)
        {
            switch (pinName)
            {
                case 'A':
                    UpdateCompA(newValue);
                    break;
                case 'B':
                    UpdateCompB(newValue);
                    break;
                case 'C':
                    UpdateCompC(newValue);
                    break;
            }
        }
    }

    private void UpdateCompA(PinOverrideMode mode)
    {
        _cpu.GpioByPort.TryGetValue(_config.ComparatorPortA, out var port);
        port?.TimerOverridePin(_config.ComparatorPinA, mode);
    }

    private void UpdateCompB(PinOverrideMode mode)
    {
        _cpu.GpioByPort.TryGetValue(_config.ComparatorPortB, out var port);
        port?.TimerOverridePin(_config.ComparatorPinB, mode);
    }

    private void UpdateCompC(PinOverrideMode mode)
    {
        _cpu.GpioByPort.TryGetValue(_config.ComparatorPortC, out var port);
        port?.TimerOverridePin(_config.ComparatorPinC, mode);
    }

    private static PinOverrideMode CompToOverride(byte comp)
    {
        switch (comp)
        {
            case 1:
                return PinOverrideMode.Toggle;
            case 2:
                return PinOverrideMode.Clear;
            case 3:
                return PinOverrideMode.Set;
            default:
                return PinOverrideMode.Enable;
        }
    }
}

public class AvrTimerConfig
{
    public byte Bits { get; set; }
    public int[]? Dividers { get; set; }

    // Interrupt Vectors
    public readonly byte CaptureInterrupt;
    public readonly byte ComparatorAInterrupt;
    public readonly byte ComparatorBInterrupt;
    public readonly byte ComparatorCInterrupt; // Optional: 0 if not used
    public readonly byte OverflowInterrupt;

    // Register Addresses
    public readonly ushort TIFR;
    public readonly ushort OCRA;
    public readonly ushort OCRB;
    public readonly ushort OCRC; // Optional: 0 if not used
    public readonly ushort ICR;
    public readonly ushort TCNT;
    public readonly ushort TCCRA;
    public readonly ushort TCCRB;
    public readonly ushort TCCRC;
    public readonly ushort TIMSK;

    // TIFR bits
    public readonly byte TOV;
    public readonly byte OCFA;
    public readonly byte OCFB;
    public readonly byte OCFC; // Optional: Only if CompareCInterrupt is != 0
    public readonly byte ICF;  // Input Capture Flag — optional, 16-bit timers only

    // TIMSK bits
    public readonly byte TOIE;
    public readonly byte OCIEA;
    public readonly byte OCIEB;
    public readonly byte OCIEC; // Optional: Only if CompareCInterrupt is != 0
    public readonly byte ICIE;  // Input Capture Interrupt Enable — optional, 16-bit timers only

    // Output Compare Inputs
    public readonly ushort ComparatorPortA;
    public readonly byte ComparatorPinA;
    public readonly ushort ComparatorPortB;
    public readonly byte ComparatorPinB;
    public readonly ushort ComparatorPortC; // Optional: 0 if not used
    public readonly byte ComparatorPinC;

    // External clock pin
    public readonly ushort ExternalClockPort;
    public readonly byte ExternalClockPin;

    public AvrTimerConfig(
        byte bits = 0,
        int[]? dividers = null,
        byte captureInterrupt = 0,
        byte comparatorAInterrupt = 0,
        byte comparatorBInterrupt = 0,
        byte comparatorCInterrupt = 0,
        byte overflowInterrupt = 0,
        ushort tifr = 0,
        ushort ocra = 0,
        ushort ocrb = 0,
        ushort ocrc = 0,
        ushort icr = 0,
        ushort tcnt = 0,
        ushort tccra = 0,
        ushort tccrb = 0,
        ushort tccrc = 0,
        ushort timsk = 0,
        byte tov = 0,
        byte ocfa = 0,
        byte ocfb = 0,
        byte ocfc = 0,
        byte icf = 0,
        byte toie = 0,
        byte ociea = 0,
        byte ocieb = 0,
        byte ociec = 0,
        byte icie = 0,
        ushort comparatorPortA = 0,
        byte comparatorPinA = 0,
        ushort comparatorPortB = 0,
        byte comparatorPinB = 0,
        ushort comparatorPortC = 0,
        byte comparatorPinC = 0,
        ushort externalClockPort = 0,
        byte externalClockPin = 0
    )
    {
        Bits = bits;
        Dividers = dividers;
        CaptureInterrupt = captureInterrupt;
        ComparatorAInterrupt = comparatorAInterrupt;
        ComparatorBInterrupt = comparatorBInterrupt;
        ComparatorCInterrupt = comparatorCInterrupt;
        OverflowInterrupt = overflowInterrupt;
        TIFR = tifr;
        OCRA = ocra;
        OCRB = ocrb;
        OCRC = ocrc;
        ICR = icr;
        TCNT = tcnt;
        TCCRA = tccra;
        TCCRB = tccrb;
        TCCRC = tccrc;
        TIMSK = timsk;
        TOV = tov;
        OCFA = ocfa;
        OCFB = ocfb;
        OCFC = ocfc;
        ICF = icf;
        TOIE = toie;
        OCIEA = ociea;
        OCIEB = ocieb;
        OCIEC = ociec;
        ICIE = icie;
        ComparatorPortA = comparatorPortA;
        ComparatorPinA = comparatorPinA;
        ComparatorPortB = comparatorPortB;
        ComparatorPinB = comparatorPinB;
        ComparatorPortC = comparatorPortC;
        ComparatorPinC = comparatorPinC;
        ExternalClockPort = externalClockPort;
        ExternalClockPin = externalClockPin;
    }

    public AvrTimerConfig CreateNew(byte bits = 0,
        int[]? dividers = null,
        byte captureInterrupt = 0,
        byte comparatorAInterrupt = 0,
        byte comparatorBInterrupt = 0,
        byte comparatorCInterrupt = 0,
        byte overflowInterrupt = 0,
        ushort tifr = 0,
        ushort ocra = 0,
        ushort ocrb = 0,
        ushort ocrc = 0,
        ushort icr = 0,
        ushort tcnt = 0,
        ushort tccra = 0,
        ushort tccrb = 0,
        ushort tccrc = 0,
        ushort timsk = 0,
        byte tov = 0,
        byte ocfa = 0,
        byte ocfb = 0,
        byte ocfc = 0,
        byte icf = 0,
        byte toie = 0,
        byte ociea = 0,
        byte ocieb = 0,
        byte ociec = 0,
        byte icie = 0,
        ushort comparatorPortA = 0,
        byte comparatorPinA = 0,
        ushort comparatorPortB = 0,
        byte comparatorPinB = 0,
        ushort comparatorPortC = 0,
        byte comparatorPinC = 0,
        ushort externalClockPort = 0,
        byte externalClockPin = 0)
    {
        // The create new function should be used to create a new instance of the AvrTimerConfig class reusing the same values and replacing only the ones that are different
        return new AvrTimerConfig(
            bits: bits == 0 ? Bits : bits,
            dividers: dividers ?? Dividers,
            captureInterrupt: captureInterrupt == 0 ? CaptureInterrupt : captureInterrupt,
            comparatorAInterrupt: comparatorAInterrupt == 0 ? ComparatorAInterrupt : comparatorAInterrupt,
            comparatorBInterrupt: comparatorBInterrupt == 0 ? ComparatorBInterrupt : comparatorBInterrupt,
            comparatorCInterrupt: comparatorCInterrupt == 0 ? ComparatorCInterrupt : comparatorCInterrupt,
            overflowInterrupt: overflowInterrupt == 0 ? OverflowInterrupt : overflowInterrupt,
            tifr: tifr == 0 ? TIFR : tifr,
            ocra: ocra == 0 ? OCRA : ocra,
            ocrb: ocrb == 0 ? OCRB : ocrb,
            ocrc: ocrc == 0 ? OCRC : ocrc,
            icr: icr == 0 ? ICR : icr,
            tcnt: tcnt == 0 ? TCNT : tcnt,
            tccra: tccra == 0 ? TCCRA : tccra,
            tccrb: tccrb == 0 ? TCCRB : tccrb,
            tccrc: tccrc == 0 ? TCCRC : tccrc,
            timsk: timsk == 0 ? TIMSK : timsk,
            tov: tov == 0 ? TOV : tov,
            ocfa: ocfa == 0 ? OCFA : ocfa,
            ocfb: ocfb == 0 ? OCFB : ocfb,
            ocfc: ocfc == 0 ? OCFC : ocfc,
            icf: icf == 0 ? ICF : icf,
            toie: toie == 0 ? TOIE : toie,
            ociea: ociea == 0 ? OCIEA : ociea,
            ocieb: ocieb == 0 ? OCIEB : ocieb,
            ociec: ociec == 0 ? OCIEC : ociec,
            icie: icie == 0 ? ICIE : icie,
            comparatorPortA: comparatorPortA == 0 ? ComparatorPortA : comparatorPortA,
            comparatorPinA: comparatorPinA == 0 ? ComparatorPinA : comparatorPinA,
            comparatorPortB: comparatorPortB == 0 ? ComparatorPortB : comparatorPortB,
            comparatorPinB: comparatorPinB == 0 ? ComparatorPinB : comparatorPinB,
            comparatorPortC: comparatorPortC == 0 ? ComparatorPortC : comparatorPortC,
            comparatorPinC: comparatorPinC == 0 ? ComparatorPinC : comparatorPinC,
            externalClockPort: externalClockPort == 0 ? ExternalClockPort : externalClockPort,
            externalClockPin: externalClockPin == 0 ? ExternalClockPin : externalClockPin
        );
    }
}

public class WgmConfig(
    TimerMode mode,
    int timerTopValue,
    OcrUpdateMode ocrUpdateMode,
    TovUpdateMode tovUpdateMode,
    int flags)
{
    public readonly TimerMode Mode = mode;
    public readonly int TimerTopValue = timerTopValue;
    public readonly OcrUpdateMode OCRUpdateMode = ocrUpdateMode;
    public readonly TovUpdateMode TOVUpdateMode = tovUpdateMode;
    public readonly int Flags = flags;
}

public enum ExternalClockMode
{
    FallingEdge = 6,
    RisingEdge = 7,
}

public enum TimerMode
{
    Normal,
    PWMPhaseCorrect,
    CTC,
    FastPWM,
    PWMPhaseFrequencyCorrect,
    Reserved,
}

public enum TovUpdateMode
{
    Max,
    Top,
    Bottom,
}

public enum OcrUpdateMode
{
    Immediate,
    Top,
    Bottom,
}