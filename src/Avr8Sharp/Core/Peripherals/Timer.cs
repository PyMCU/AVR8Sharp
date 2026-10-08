using AVR8Sharp.Core;

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

    // ── ATmega2560 ──────────────────────────────────────────────────────────
    // Naming: Mega2560Timer<N>Config. Interrupt "address" values are word indices = avr-libc vector
    // number x 2. Output compare / T / ICP pins follow the ATmega2560 datasheet pin table.

    /// <summary>ATmega2560 Timer0: 8-bit, OC0A = PB7, OC0B = PG5, T0 = PD7. Vectors COMPA 0x2A, COMPB 0x2C, OVF 0x2E.</summary>
    public static readonly AvrTimerConfig Mega2560Timer0Config = Timer0Config.CreateNew(
        comparatorAInterrupt: 0x2a,
        comparatorBInterrupt: 0x2c,
        overflowInterrupt: 0x2e,
        comparatorPortA: AvrIoPort.Mega2560PortBConfig.PORT,
        comparatorPinA: 7,
        comparatorPortB: AvrIoPort.Mega2560PortGConfig.PORT,
        comparatorPinB: 5,
        externalClockPort: AvrIoPort.Mega2560PortDConfig.PORT,
        externalClockPin: 7);

    /// <summary>ATmega2560 Timer1: 16-bit with channel C (OCR1C 0x8C), OC1A = PB5, OC1B = PB6, OC1C = PB7, T1 = PD6, ICP1 = PD4. Vectors CAPT 0x20, COMPA 0x22, COMPB 0x24, COMPC 0x26, OVF 0x28.</summary>
    public static readonly AvrTimerConfig Mega2560Timer1Config = Timer1Config.CreateNew(
        captureInterrupt: 0x20,
        comparatorAInterrupt: 0x22,
        comparatorBInterrupt: 0x24,
        comparatorCInterrupt: 0x26,
        overflowInterrupt: 0x28,
        ocrc: 0x8c,
        ocfc: 0x08,
        ociec: 0x08,
        comparatorPortA: AvrIoPort.Mega2560PortBConfig.PORT,
        comparatorPinA: 5,
        comparatorPortB: AvrIoPort.Mega2560PortBConfig.PORT,
        comparatorPinB: 6,
        comparatorPortC: AvrIoPort.Mega2560PortBConfig.PORT,
        comparatorPinC: 7,
        externalClockPort: AvrIoPort.Mega2560PortDConfig.PORT,
        externalClockPin: 6,
        icpPort: AvrIoPort.Mega2560PortDConfig.PORT,
        icpPin: 4);

    /// <summary>ATmega2560 Timer2: 8-bit async, OC2A = PB4, OC2B = PH6. Vectors COMPA 0x1A, COMPB 0x1C, OVF 0x1E.</summary>
    public static readonly AvrTimerConfig Mega2560Timer2Config = Timer2Config.CreateNew(
        comparatorAInterrupt: 0x1a,
        comparatorBInterrupt: 0x1c,
        overflowInterrupt: 0x1e,
        comparatorPortA: AvrIoPort.Mega2560PortBConfig.PORT,
        comparatorPinA: 4,
        comparatorPortB: AvrIoPort.Mega2560PortHConfig.PORT,
        comparatorPinB: 6);

    /// <summary>ATmega2560 Timer3: 16-bit, OC3A/B/C = PE3/PE4/PE5, T3 = PE6, ICP3 = PE7. Vectors CAPT 0x3E, COMPA 0x40, COMPB 0x42, COMPC 0x44, OVF 0x46.</summary>
    public static readonly AvrTimerConfig Mega2560Timer3Config = new AvrTimerConfig(
        bits: 16,
        dividers: Timer01Dividers,
        captureInterrupt: 0x3e,
        comparatorAInterrupt: 0x40,
        comparatorBInterrupt: 0x42,
        comparatorCInterrupt: 0x44,
        overflowInterrupt: 0x46,
        tifr: 0x38,
        ocra: 0x98,
        ocrb: 0x9a,
        ocrc: 0x9c,
        icr: 0x96,
        tcnt: 0x94,
        tccra: 0x90,
        tccrb: 0x91,
        tccrc: 0x92,
        timsk: 0x71,
        comparatorPortA: AvrIoPort.Mega2560PortEConfig.PORT,
        comparatorPinA: 3,
        comparatorPortB: AvrIoPort.Mega2560PortEConfig.PORT,
        comparatorPinB: 4,
        comparatorPortC: AvrIoPort.Mega2560PortEConfig.PORT,
        comparatorPinC: 5,
        externalClockPort: AvrIoPort.Mega2560PortEConfig.PORT,
        externalClockPin: 6,
        icpPort: AvrIoPort.Mega2560PortEConfig.PORT,
        icpPin: 7,
        tov: DefaultTimerBits.TOV,
        ocfa: DefaultTimerBits.OCFA,
        ocfb: DefaultTimerBits.OCFB,
        ocfc: 0x08,
        icf: 0x20,
        toie: DefaultTimerBits.TOIE,
        ociea: DefaultTimerBits.OCIEA,
        ocieb: DefaultTimerBits.OCIEB,
        ociec: 0x08,
        icie: 0x20
    );

    /// <summary>ATmega2560 Timer4: 16-bit, OC4A/B/C = PH3/PH4/PH5, T4 = PH7, ICP4 = PL0. Vectors CAPT 0x52, COMPA 0x54, COMPB 0x56, COMPC 0x58, OVF 0x5A.</summary>
    public static readonly AvrTimerConfig Mega2560Timer4Config = new AvrTimerConfig(
        bits: 16,
        dividers: Timer01Dividers,
        captureInterrupt: 0x52,
        comparatorAInterrupt: 0x54,
        comparatorBInterrupt: 0x56,
        comparatorCInterrupt: 0x58,
        overflowInterrupt: 0x5a,
        tifr: 0x39,
        ocra: 0xa8,
        ocrb: 0xaa,
        ocrc: 0xac,
        icr: 0xa6,
        tcnt: 0xa4,
        tccra: 0xa0,
        tccrb: 0xa1,
        tccrc: 0xa2,
        timsk: 0x72,
        comparatorPortA: AvrIoPort.Mega2560PortHConfig.PORT,
        comparatorPinA: 3,
        comparatorPortB: AvrIoPort.Mega2560PortHConfig.PORT,
        comparatorPinB: 4,
        comparatorPortC: AvrIoPort.Mega2560PortHConfig.PORT,
        comparatorPinC: 5,
        externalClockPort: AvrIoPort.Mega2560PortHConfig.PORT,
        externalClockPin: 7,
        icpPort: AvrIoPort.Mega2560PortLConfig.PORT,
        icpPin: 0,
        tov: DefaultTimerBits.TOV,
        ocfa: DefaultTimerBits.OCFA,
        ocfb: DefaultTimerBits.OCFB,
        ocfc: 0x08,
        icf: 0x20,
        toie: DefaultTimerBits.TOIE,
        ociea: DefaultTimerBits.OCIEA,
        ocieb: DefaultTimerBits.OCIEB,
        ociec: 0x08,
        icie: 0x20
    );

    /// <summary>ATmega2560 Timer5: 16-bit, OC5A/B/C = PL3/PL4/PL5, T5 = PL2, ICP5 = PL1. Vectors CAPT 0x5C, COMPA 0x5E, COMPB 0x60, COMPC 0x62, OVF 0x64.</summary>
    public static readonly AvrTimerConfig Mega2560Timer5Config = new AvrTimerConfig(
        bits: 16,
        dividers: Timer01Dividers,
        captureInterrupt: 0x5c,
        comparatorAInterrupt: 0x5e,
        comparatorBInterrupt: 0x60,
        comparatorCInterrupt: 0x62,
        overflowInterrupt: 0x64,
        tifr: 0x3a,
        ocra: 0x128,
        ocrb: 0x12a,
        ocrc: 0x12c,
        icr: 0x126,
        tcnt: 0x124,
        tccra: 0x120,
        tccrb: 0x121,
        tccrc: 0x122,
        timsk: 0x73,
        comparatorPortA: AvrIoPort.Mega2560PortLConfig.PORT,
        comparatorPinA: 3,
        comparatorPortB: AvrIoPort.Mega2560PortLConfig.PORT,
        comparatorPinB: 4,
        comparatorPortC: AvrIoPort.Mega2560PortLConfig.PORT,
        comparatorPinC: 5,
        externalClockPort: AvrIoPort.Mega2560PortLConfig.PORT,
        externalClockPin: 2,
        icpPort: AvrIoPort.Mega2560PortLConfig.PORT,
        icpPin: 1,
        tov: DefaultTimerBits.TOV,
        ocfa: DefaultTimerBits.OCFA,
        ocfb: DefaultTimerBits.OCFB,
        ocfc: 0x08,
        icf: 0x20,
        toie: DefaultTimerBits.TOIE,
        ociea: DefaultTimerBits.OCIEA,
        ocieb: DefaultTimerBits.OCIEB,
        ociec: 0x08,
        icie: 0x20
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

    private readonly Cpu _cpu;
    private readonly AvrTimerConfig _config;

    private readonly int _max;
    private readonly bool _hasCaptureInterrupt;
    private ulong _lastCycle = 0;
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

    /// <summary>The register layout this timer was built with.</summary>
    public AvrTimerConfig Config => _config;

    /// <summary>
    /// Describes the PWM waveform on compare channel <paramref name="channel"/> (0 = A, 1 = B, 2 = C)
    /// from the current registers: fast PWM and phase correct / phase and frequency correct, with
    /// COMnx = 2 (non-inverting) or 3 (inverting). Returns false when the channel does not exist.
    /// <paramref name="enabled"/> is false when the channel is not producing PWM (other mode,
    /// output disconnected, toggle mode, or clock stopped). Fast PWM high time is OCR + 1 counts,
    /// phase correct is OCR counts out of TOP, as the waveform generator does.
    /// </summary>
    public bool TryGetPwm(int channel, uint clockHz, out bool enabled, out double frequencyHz, out double duty)
    {
        enabled = false;
        frequencyHz = 0;
        duty = 0;
        int ocr;
        byte comp;
        switch (channel) {
            case 0: ocr = _ocrA; comp = _compA; break;
            case 1: ocr = _ocrB; comp = _compB; break;
            case 2 when _hasOcrC: ocr = _ocrC; comp = _compC; break;
            default: return false;
        }
        var divider = _config.Dividers != null && CS < _config.Dividers.Length ? _config.Dividers[CS] : 0;
        var top = TOP;
        // With TOP = OCRnA, channel A only toggles; it has no duty of its own
        if (comp < 2 || divider <= 0 || top <= 0 || (channel == 0 && _topValue == TOP_OCRA)) return true;
        double period, high;
        switch (_timerMode) {
            case TimerMode.FastPWM:
                period = top + 1;
                high = Math.Min(ocr, top) + 1;
                break;
            case TimerMode.PWMPhaseCorrect:
            case TimerMode.PWMPhaseFrequencyCorrect:
                period = 2.0 * top;
                high = 2.0 * Math.Min(ocr, top);
                break;
            default:
                return true;
        }
        enabled = true;
        frequencyHz = clockHz / (divider * period);
        duty = comp == 3 ? 1.0 - high / period : high / period;
        return true;
    }

    public int DebugTCNT
    {
        get { return _tcnt; }
    }

    public AvrTimer(Cpu cpu, AvrTimerConfig config)
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
            TryAttachIcp();
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
            // TIFR flags are cleared by writing 1; writing 0 leaves them untouched (datasheet:
            // "cleared by writing a logic one"), so the written value is never stored.
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

        cpu.OnPeripheralReset += ResetFromCpu;
        TryAttachIcp();
    }

    /// <summary>
    /// CPU reset: the timer stops (no clock source until TCCRnB is written again), counters
    /// and compare buffers clear, and the waveform mode returns to Normal. Pin overrides are
    /// released by the GPIO reset, so only the local bookkeeping is cleared here.
    /// </summary>
    private void ResetFromCpu()
    {
        Reset();
        _countingUp = true;
        _highByteTemp = 0;
        _compA = 0;
        _compB = 0;
        _compC = 0;
        if (_externalClockPort != null)
        {
            _externalClockPort.ExternalClockListeners[_config.ExternalClockPin] = null;
        }

        UpdateWgmConfig();
    }

    private bool _icpAttached;

    /// <summary>
    /// Hooks the ICPn pin (when the config models it) so an edge selected by ICESn (TCCRnB bit 6)
    /// triggers an input capture. Retried on TCCRB writes in case the port was added after the timer.
    /// </summary>
    private void TryAttachIcp()
    {
        if (_icpAttached || !_hasCaptureInterrupt || _config.IcpPort == 0) return;
        var port = _cpu.GpioByPort.GetValueOrDefault(_config.IcpPort);
        if (port == null) return;
        port.ExternalClockListeners[_config.IcpPin] = level =>
        {
            var risingEdgeSelected = (_cpu.Mmio.Data[_config.TCCRB] & 0x40) != 0;
            if (level == risingEdgeSelected) TriggerCapture();
        };
        _icpAttached = true;
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

        if (_divider != 0 && delta >= (ulong)_divider || external)
        {
            var counterDelta = external ? 1UL : delta / (ulong)_divider;
            _lastCycle += counterDelta * (ulong)_divider;
            var val = _tcnt;
            var phasePwm = _timerMode == TimerMode.PWMPhaseCorrect || _timerMode == TimerMode.PWMPhaseFrequencyCorrect;
            int newVal;
            if (phasePwm) 
            {
                newVal = PhasePwmCount(val, (byte)counterDelta);
            }
            else
            {
                newVal = val + (int)counterDelta;
                while (newVal > _cachedTop)
                {
                    newVal -= (_cachedTop + 1);
                }
            }
            var overflow = val + (int)counterDelta > _cachedTop;
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

                    if (_hasOcrC && _compC != 0)
                    {
                        UpdateCompPin(_compC, 'C', true);
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
                _cpu.AddClockEvent(_countAction, (int)(_lastCycle + (ulong)newDivider - _cpu.Cycles));
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
            _cpu.AddClockEvent(_countAction, (int)(_lastCycle + (ulong)_divider - _cpu.Cycles));
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
        // The interrupt flag is raised on the match itself, the pin follows one timer
        // clock later in fast PWM. See PinCompareValue.
        if (CompareReached(value, prevNumber, _ocrA))
        {
            _cpu.SetInterruptFlag(_ocfa);
        }

        if (_compA != 0 && CompareReached(value, prevNumber, PinCompareValue(_ocrA, _compA)))
        {
            UpdateCompPin(_compA, 'A');
        }

        if (CompareReached(value, prevNumber, _ocrB))
        {
            _cpu.SetInterruptFlag(_ocfb);
        }

        if (_compB != 0 && CompareReached(value, prevNumber, PinCompareValue(_ocrB, _compB)))
        {
            UpdateCompPin(_compB, 'B');
        }

        if (!_hasOcrC) return;

        if (CompareReached(value, prevNumber, _ocrC))
        {
            _cpu.SetInterruptFlag(_ocfc);
        }

        if (_compC != 0 && CompareReached(value, prevNumber, PinCompareValue(_ocrC, _compC)))
        {
            UpdateCompPin(_compC, 'C');
        }
    }

    /// <summary>
    /// Whether the counter passed <paramref name="compare"/> on its way from
    /// <paramref name="prevNumber"/> to <paramref name="value"/>, wrap included.
    /// A negative <paramref name="compare"/> is never reached.
    /// </summary>
    private static bool CompareReached(int value, int prevNumber, int compare)
    {
        if (compare < 0) return false;
        var overflow = prevNumber > value;
        return (prevNumber < compare || overflow) && value >= compare || prevNumber < compare && overflow;
    }

    /// <summary>
    /// The counter value at which a compare match reaches the output pin.
    /// <para>
    /// In fast PWM the waveform generator updates OCnx on the timer clock that follows
    /// the match, so the pulse is OCRnx + 1 counts wide. The ATmega328P datasheet spells
    /// out both ends of that: OCRnx at BOTTOM gives a narrow spike once per period, and
    /// OCRnx at MAX a constant level. Past TOP there is no match left to reach the pin,
    /// which this reports as -1, and the level written at BOTTOM stands for the whole
    /// period.
    /// </para>
    /// <para>
    /// Toggle mode (COMnx = 1) and the non-PWM modes keep the match value: flipping the
    /// pin a clock later only shifts the phase, and in fast PWM mode 7 OCRnA is TOP, so
    /// clamping would leave nothing to toggle on.
    /// </para>
    /// </summary>
    private int PinCompareValue(int ocr, byte compValue)
    {
        if (_timerMode != TimerMode.FastPWM || compValue == 1) return ocr;
        return ocr + 1 > _cachedTop ? -1 : ocr + 1;
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

    // Input capture pin (ICPn) and its port. Optional: 0 if the pin is not modelled.
    public readonly ushort IcpPort;
    public readonly byte IcpPin;

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
        byte externalClockPin = 0,
        ushort icpPort = 0,
        byte icpPin = 0
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
        IcpPort = icpPort;
        IcpPin = icpPin;
    }

    /// <summary>
    /// Creates a copy of this config replacing only the values that are passed. A parameter left
    /// <c>null</c> keeps the source value, so an explicit <c>0</c> (for example pin 0 or bit 0)
    /// is honoured.
    /// </summary>
    public AvrTimerConfig CreateNew(byte? bits = null,
        int[]? dividers = null,
        byte? captureInterrupt = null,
        byte? comparatorAInterrupt = null,
        byte? comparatorBInterrupt = null,
        byte? comparatorCInterrupt = null,
        byte? overflowInterrupt = null,
        ushort? tifr = null,
        ushort? ocra = null,
        ushort? ocrb = null,
        ushort? ocrc = null,
        ushort? icr = null,
        ushort? tcnt = null,
        ushort? tccra = null,
        ushort? tccrb = null,
        ushort? tccrc = null,
        ushort? timsk = null,
        byte? tov = null,
        byte? ocfa = null,
        byte? ocfb = null,
        byte? ocfc = null,
        byte? icf = null,
        byte? toie = null,
        byte? ociea = null,
        byte? ocieb = null,
        byte? ociec = null,
        byte? icie = null,
        ushort? comparatorPortA = null,
        byte? comparatorPinA = null,
        ushort? comparatorPortB = null,
        byte? comparatorPinB = null,
        ushort? comparatorPortC = null,
        byte? comparatorPinC = null,
        ushort? externalClockPort = null,
        byte? externalClockPin = null,
        ushort? icpPort = null,
        byte? icpPin = null)
    {
        return new AvrTimerConfig(
            bits: bits ?? Bits,
            dividers: dividers ?? Dividers,
            captureInterrupt: captureInterrupt ?? CaptureInterrupt,
            comparatorAInterrupt: comparatorAInterrupt ?? ComparatorAInterrupt,
            comparatorBInterrupt: comparatorBInterrupt ?? ComparatorBInterrupt,
            comparatorCInterrupt: comparatorCInterrupt ?? ComparatorCInterrupt,
            overflowInterrupt: overflowInterrupt ?? OverflowInterrupt,
            tifr: tifr ?? TIFR,
            ocra: ocra ?? OCRA,
            ocrb: ocrb ?? OCRB,
            ocrc: ocrc ?? OCRC,
            icr: icr ?? ICR,
            tcnt: tcnt ?? TCNT,
            tccra: tccra ?? TCCRA,
            tccrb: tccrb ?? TCCRB,
            tccrc: tccrc ?? TCCRC,
            timsk: timsk ?? TIMSK,
            tov: tov ?? TOV,
            ocfa: ocfa ?? OCFA,
            ocfb: ocfb ?? OCFB,
            ocfc: ocfc ?? OCFC,
            icf: icf ?? ICF,
            toie: toie ?? TOIE,
            ociea: ociea ?? OCIEA,
            ocieb: ocieb ?? OCIEB,
            ociec: ociec ?? OCIEC,
            icie: icie ?? ICIE,
            comparatorPortA: comparatorPortA ?? ComparatorPortA,
            comparatorPinA: comparatorPinA ?? ComparatorPinA,
            comparatorPortB: comparatorPortB ?? ComparatorPortB,
            comparatorPinB: comparatorPinB ?? ComparatorPinB,
            comparatorPortC: comparatorPortC ?? ComparatorPortC,
            comparatorPinC: comparatorPinC ?? ComparatorPinC,
            externalClockPort: externalClockPort ?? ExternalClockPort,
            externalClockPin: externalClockPin ?? ExternalClockPin,
            icpPort: icpPort ?? IcpPort,
            icpPin: icpPin ?? IcpPin
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