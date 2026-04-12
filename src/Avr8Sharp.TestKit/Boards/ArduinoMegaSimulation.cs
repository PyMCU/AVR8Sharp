using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.TestKit.Probes;

namespace Avr8Sharp.TestKit.Boards;

/// <summary>
/// Pre-configured simulation for the <b>Arduino Mega 2560</b> (ATmega2560).
/// <para>
/// Includes all 11 GPIO ports (A–L), six timers (0–5), and four USART
/// channels (Serial0–3). Each serial channel is captured in a
/// <see cref="SerialProbe"/> accessible via <see cref="Serial0"/>–<see cref="Serial3"/>.
/// </para>
/// </summary>
/// <example>
/// <code>
/// var mega = new ArduinoMegaSimulation()
///     .WithHex(File.ReadAllText("sketch.hex"));
///
/// mega.RunMilliseconds(1000);
///
/// mega.PortB.Should().HavePinHigh(7);   // digital pin 13
/// mega.Serial0.Should().Contain("Ready");
/// </code>
/// </example>
public sealed class ArduinoMegaSimulation : AvrTestSimulation
{
    // ATmega2560: 256 KB flash, 8 KB SRAM, 16 MHz
    private const int Flash = 0x40000;
    private const int Sram = 0x2100;
    private const uint Frequency = 16_000_000;

    // ── ATmega2560 Timer interrupt vectors (JMP/4-byte vectors) ──────────────
    // ATmega2560 uses 4-byte JMP entries; vector N sits at byte address (N-1)×4.
    // Interrupt addresses are WORD indices (byte_address / 2) because DoAvrInterrupt
    // sets cpu.Pc = address directly and Pc is a word index into ProgramMemory[].

    private static readonly AvrTimerConfig Mega2560Timer0Config =
        AvrTimer.Timer0Config.CreateNew(
            comparatorAInterrupt: 0x2A,   // vector 22 — word addr (byte 0x54)
            comparatorBInterrupt: 0x2C,   // vector 23 — word addr (byte 0x58)
            overflowInterrupt:    0x2E);  // vector 24 — word addr (byte 0x5C)

    private static readonly AvrTimerConfig Mega2560Timer1Config =
        AvrTimer.Timer1Config.CreateNew(
            captureInterrupt:     0x20,   // vector 17 — word addr (byte 0x40)
            comparatorAInterrupt: 0x22,   // vector 18 — word addr (byte 0x44)
            comparatorBInterrupt: 0x24,   // vector 19 — word addr (byte 0x48)
            comparatorCInterrupt: 0x26,   // vector 20 — word addr (byte 0x4C)
            overflowInterrupt:    0x28);  // vector 21 — word addr (byte 0x50)

    private static readonly AvrTimerConfig Mega2560Timer2Config =
        AvrTimer.Timer2Config.CreateNew(
            comparatorAInterrupt: 0x1A,   // vector 14 — word addr (byte 0x34)
            comparatorBInterrupt: 0x1C,   // vector 15 — word addr (byte 0x38)
            overflowInterrupt:    0x1E);  // vector 16 — word addr (byte 0x3C)

    // Timer 3 — 16-bit, registers 0x90–0x9C, TIMSK3 0x71, TIFR3 0x38
    private static readonly AvrTimerConfig Mega2560Timer3Config = new AvrTimerConfig(
        bits:                 16,
        dividers:             AvrTimer.Timer01Dividers,
        captureInterrupt:     0x3E,   // vector 32 — word addr (byte 0x7C)
        comparatorAInterrupt: 0x40,   // vector 33 — word addr (byte 0x80)
        comparatorBInterrupt: 0x42,   // vector 34 — word addr (byte 0x84)
        comparatorCInterrupt: 0x44,   // vector 35 — word addr (byte 0x88)
        overflowInterrupt:    0x46,   // vector 36 — word addr (byte 0x8C)
        tccra: 0x90, tccrb: 0x91, tccrc: 0x92,
        tcnt:  0x94, ocra:  0x98, ocrb: 0x9A, ocrc: 0x9C, icr: 0x96,
        timsk: 0x71, tifr:  0x38,
        comparatorPortA: AvrIoPort.PortEConfig.PORT, comparatorPinA: 3,
        comparatorPortB: AvrIoPort.PortEConfig.PORT, comparatorPinB: 4,
        comparatorPortC: AvrIoPort.PortEConfig.PORT, comparatorPinC: 5,
        externalClockPort: AvrIoPort.PortEConfig.PORT, externalClockPin: 6,
        tov: 1, ocfa: 2, ocfb: 4, ocfc: 8,
        toie: 1, ociea: 2, ocieb: 4, ociec: 8);

    // Timer 4 — 16-bit, registers 0xA0–0xAC, TIMSK4 0x72, TIFR4 0x39
    private static readonly AvrTimerConfig Mega2560Timer4Config = new AvrTimerConfig(
        bits:                 16,
        dividers:             AvrTimer.Timer01Dividers,
        captureInterrupt:     0x52,   // vector 42 — word addr (byte 0xA4)
        comparatorAInterrupt: 0x54,   // vector 43 — word addr (byte 0xA8)
        comparatorBInterrupt: 0x56,   // vector 44 — word addr (byte 0xAC)
        comparatorCInterrupt: 0x58,   // vector 45 — word addr (byte 0xB0)
        overflowInterrupt:    0x5A,   // vector 46 — word addr (byte 0xB4)
        tccra: 0xA0, tccrb: 0xA1, tccrc: 0xA2,
        tcnt:  0xA4, ocra:  0xA8, ocrb: 0xAA, ocrc: 0xAC, icr: 0xA6,
        timsk: 0x72, tifr:  0x39,
        comparatorPortA: AvrIoPort.PortHConfig.PORT, comparatorPinA: 3,
        comparatorPortB: AvrIoPort.PortHConfig.PORT, comparatorPinB: 4,
        comparatorPortC: AvrIoPort.PortHConfig.PORT, comparatorPinC: 5,
        externalClockPort: AvrIoPort.PortHConfig.PORT, externalClockPin: 3,
        tov: 1, ocfa: 2, ocfb: 4, ocfc: 8,
        toie: 1, ociea: 2, ocieb: 4, ociec: 8);

    // Timer 5 — 16-bit, registers 0x120–0x12D, TIMSK5 0x73, TIFR5 0x3A
    private static readonly AvrTimerConfig Mega2560Timer5Config = new AvrTimerConfig(
        bits:                 16,
        dividers:             AvrTimer.Timer01Dividers,
        captureInterrupt:     0x5C,   // vector 47 — word addr (byte 0xB8)
        comparatorAInterrupt: 0x5E,   // vector 48 — word addr (byte 0xBC)
        comparatorBInterrupt: 0x60,   // vector 49 — word addr (byte 0xC0)
        comparatorCInterrupt: 0x62,   // vector 50 — word addr (byte 0xC4)
        overflowInterrupt:    0x64,   // vector 51 — word addr (byte 0xC8)
        tccra: 0x120, tccrb: 0x121, tccrc: 0x122,
        tcnt:  0x124, icr:   0x126,
        ocra:  0x128, ocrb:  0x12A, ocrc: 0x12C,
        timsk: 0x73,  tifr:  0x3A,
        comparatorPortA: AvrIoPort.PortLConfig.PORT, comparatorPinA: 3,
        comparatorPortB: AvrIoPort.PortLConfig.PORT, comparatorPinB: 4,
        comparatorPortC: AvrIoPort.PortLConfig.PORT, comparatorPinC: 5,
        externalClockPort: AvrIoPort.PortLConfig.PORT, externalClockPin: 2,
        tov: 1, ocfa: 2, ocfb: 4, ocfc: 8,
        toie: 1, ociea: 2, ocieb: 4, ociec: 8,
        icf: 0x20, icie: 0x20);

    // USART0 — same I/O registers as ATmega328P (0xC0–0xC6) but different interrupt vectors
    private static readonly AvrUsartConfig Mega2560Usart0Config = new AvrUsartConfig
    {
        RxCompleteInterrupt        = 0x32,  // Vector 26 — byte addr 0x0064
        DataRegisterEmptyInterrupt = 0x34,  // Vector 27
        TxCompleteInterrupt        = 0x36,  // Vector 28
        UCSRA = 0xC0, UCSRB = 0xC1, UCSRC = 0xC2,
        UBRRL = 0xC4, UBRRH = 0xC5, UDR   = 0xC6,
    };

    // USART1 — registers 0xC8–0xCE
    private static readonly AvrUsartConfig Mega2560Usart1Config = new AvrUsartConfig
    {
        RxCompleteInterrupt        = 0x48,  // Vector 37
        DataRegisterEmptyInterrupt = 0x4A,  // Vector 38
        TxCompleteInterrupt        = 0x4C,  // Vector 39
        UCSRA = 0xC8, UCSRB = 0xC9, UCSRC = 0xCA,
        UBRRL = 0xCC, UBRRH = 0xCD, UDR   = 0xCE,
    };

    // USART2 — registers 0xD0–0xD6
    private static readonly AvrUsartConfig Mega2560Usart2Config = new AvrUsartConfig
    {
        RxCompleteInterrupt        = 0x66,  // Vector 51 — word addr (byte 0xCC)
        DataRegisterEmptyInterrupt = 0x68,  // Vector 52 — word addr (byte 0xD0)
        TxCompleteInterrupt        = 0x6A,  // Vector 53 — word addr (byte 0xD4)
        UCSRA = 0xD0, UCSRB = 0xD1, UCSRC = 0xD2,
        UBRRL = 0xD4, UBRRH = 0xD5, UDR   = 0xD6,
    };

    // USART3 — registers 0x130–0x136
    private static readonly AvrUsartConfig Mega2560Usart3Config = new AvrUsartConfig
    {
        RxCompleteInterrupt        = 0x6C,  // Vector 55
        DataRegisterEmptyInterrupt = 0x6E,  // Vector 56
        TxCompleteInterrupt        = 0x70,  // Vector 57
        UCSRA = 0x130, UCSRB = 0x131, UCSRC = 0x132,
        UBRRL = 0x134, UBRRH = 0x135, UDR   = 0x136,
    };

    // ── GPIO ports ────────────────────────────────────────────────────────────
    /// <summary>Port A — digital pins 22–29.</summary>
    public AvrIoPort PortA { get; }
    /// <summary>Port B — digital pins 10–13, 50–53 (SPI).</summary>
    public AvrIoPort PortB { get; }
    /// <summary>Port C — digital pins 30–37.</summary>
    public AvrIoPort PortC { get; }
    /// <summary>Port D — digital pins 18–21 (USART1/3), external interrupts.</summary>
    public AvrIoPort PortD { get; }
    /// <summary>Port E — digital pins 0–3 (USART0, PWM).</summary>
    public AvrIoPort PortE { get; }
    /// <summary>Port F — analog pins A0–A7.</summary>
    public AvrIoPort PortF { get; }
    /// <summary>Port G — digital pins 39–41.</summary>
    public AvrIoPort PortG { get; }
    /// <summary>Port H — digital pins 6–9, 16–17 (USART2, PWM).</summary>
    public AvrIoPort PortH { get; }
    /// <summary>Port J — digital pins 14–15 (USART3).</summary>
    public AvrIoPort PortJ { get; }
    /// <summary>Port K — analog pins A8–A15.</summary>
    public AvrIoPort PortK { get; }
    /// <summary>Port L — digital pins 42–49 (PWM).</summary>
    public AvrIoPort PortL { get; }

    // ── Timers ────────────────────────────────────────────────────────────────
    /// <summary>Timer 0 — 8-bit, configured with ATmega2560 interrupt vectors.</summary>
    public AvrTimer Timer0 { get; }
    /// <summary>Timer 1 — 16-bit (with OC1C), configured with ATmega2560 interrupt vectors.</summary>
    public AvrTimer Timer1 { get; }
    /// <summary>Timer 2 — 8-bit async, configured with ATmega2560 interrupt vectors.</summary>
    public AvrTimer Timer2 { get; }
    /// <summary>Timer 3 — 16-bit, OC3A/B/C on Port E pins 3/4/5.</summary>
    public AvrTimer Timer3 { get; }
    /// <summary>Timer 4 — 16-bit, OC4A/B/C on Port H pins 3/4/5.</summary>
    public AvrTimer Timer4 { get; }
    /// <summary>Timer 5 — 16-bit, OC5A/B/C on Port L pins 3/4/5.</summary>
    public AvrTimer Timer5 { get; }

    // ── USART ─────────────────────────────────────────────────────────────────
    /// <summary>Captures USART0 output (TX = PE1, "Serial" in Arduino IDE).</summary>
    public SerialProbe Serial0 { get; }
    /// <summary>Captures USART1 output (TX = PD3, "Serial1" in Arduino IDE).</summary>
    public SerialProbe Serial1 { get; }
    /// <summary>Captures USART2 output (TX = PH1, "Serial2" in Arduino IDE).</summary>
    public SerialProbe Serial2 { get; }
    /// <summary>Captures USART3 output (TX = PJ1, "Serial3" in Arduino IDE).</summary>
    public SerialProbe Serial3 { get; }

    public ArduinoMegaSimulation() : base(Flash, Sram)
    {
        WithFrequency(Frequency);

        AddGpio(AvrIoPort.PortAConfig, out var pA); PortA = pA;
        AddGpio(AvrIoPort.PortBConfig, out var pB); PortB = pB;
        AddGpio(AvrIoPort.PortCConfig, out var pC); PortC = pC;
        AddGpio(AvrIoPort.PortDConfig, out var pD); PortD = pD;
        AddGpio(AvrIoPort.PortEConfig, out var pE); PortE = pE;
        AddGpio(AvrIoPort.PortFConfig, out var pF); PortF = pF;
        AddGpio(AvrIoPort.PortGConfig, out var pG); PortG = pG;
        AddGpio(AvrIoPort.PortHConfig, out var pH); PortH = pH;
        AddGpio(AvrIoPort.PortJConfig, out var pJ); PortJ = pJ;
        AddGpio(AvrIoPort.PortKConfig, out var pK); PortK = pK;
        AddGpio(AvrIoPort.PortLConfig, out var pL); PortL = pL;

        AddTimer(Mega2560Timer0Config, out var t0); Timer0 = t0;
        AddTimer(Mega2560Timer1Config, out var t1); Timer1 = t1;
        AddTimer(Mega2560Timer2Config, out var t2); Timer2 = t2;
        AddTimer(Mega2560Timer3Config, out var t3); Timer3 = t3;
        AddTimer(Mega2560Timer4Config, out var t4); Timer4 = t4;
        AddTimer(Mega2560Timer5Config, out var t5); Timer5 = t5;

        AddUsart(Mega2560Usart0Config,     out var s0); Serial0 = s0;
        AddUsart(Mega2560Usart1Config,     out var s1); Serial1 = s1;
        AddUsart(Mega2560Usart2Config,     out var s2); Serial2 = s2;
        AddUsart(Mega2560Usart3Config,     out var s3); Serial3 = s3;
    }
}
