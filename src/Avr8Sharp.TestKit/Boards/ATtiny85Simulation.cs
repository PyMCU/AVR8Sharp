using AVR8Sharp.Core.Peripherals;

namespace Avr8Sharp.TestKit.Boards;

/// <summary>
/// Pre-configured simulation for the <b>ATtiny85</b>.
/// <para>
/// All standard peripherals are created automatically:
/// Port B (the only GPIO port), Timer 0, and Timer 1.
/// The ATtiny85 has no hardware USART; use the USI peripheral via
/// <see cref="AvrTestSimulation.AddUsart"/> if needed.
/// </para>
/// <para>
/// Timer 1 note: the ATtiny85 TC1 is an 8-bit timer with a single TCCR1 register
/// (no TCCRA/TCCRB split). This simulation maps TCCR1 (0x30) as the clock-select
/// register (CS13:CS10). Prescalers /1–/64 (CS10-CS12) are supported; /128 and above
/// (CS13=1) are not because the existing timer engine only reads the low 3 CS bits.
/// CTC and PWM modes (CTC1, PWM1A bits in TCCR1) are not emulated — Normal mode only.
/// </para>
/// </summary>
/// <example>
/// <code>
/// var tiny = new ATtiny85Simulation()
///     .WithHex(File.ReadAllText("firmware.hex"));
///
/// tiny.RunMilliseconds(250);
///
/// tiny.PortB.Should().HavePinHigh(0);   // PB0
/// tiny.PortB.Should().HavePinLow(1);    // PB1
/// </code>
/// </example>
public sealed class ATtiny85Simulation : AvrTestSimulation
{
    // ATtiny85: 8 KB flash, 512 B SRAM, 8 MHz internal RC oscillator
    private const int Flash = 0x2000;
    private const int Sram  = 512;
    private const uint Frequency = 8_000_000;

    // ── ATtiny85 Port B config ────────────────────────────────────────────────
    // Registers: PIN 0x36, DDR 0x37, PORT 0x38
    // INT0 on PB2, PCINT0 on all pins
    private static readonly AvrExternalInterrupt Tiny85Int0 = new(
        eicr: 0x55, iscOffset: 0, eimsk: 0x5B, eifr: 0x5A, index: 6, interrupt: 1);

    private static readonly AvrPortConfig Tiny85PortBConfig = new(
        pin: 0x36, ddr: 0x37, port: 0x38,
        pinChange: new AvrPinChangeInterrupt(
            pcie: 5, pcicr: 0x5B, pcifr: 0x5A, pcmsk: 0x35,
            pinChangeInterrupt: 2, mask: 0x3F, offset: 0),
        externalInterrupts: [null, null, Tiny85Int0]);

    // ── ATtiny85 Timer 0 config ───────────────────────────────────────────────
    // 8-bit, registers at 0x48–0x53 area; interrupt vectors use 2-byte entries.
    // TIFR 0x58, TIMSK 0x59; OVF vector 5, COMPA 0xA, COMPB 0xB
    private static readonly AvrTimerConfig Tiny85Timer0Config = new(
        bits: 8,
        dividers: AvrTimer.Timer01Dividers,
        captureInterrupt:     0,
        comparatorAInterrupt: 0x0A,  // vector 5
        comparatorBInterrupt: 0x0B,  // but COMPB shares the pin set—B is absent; 0xB kept for compat
        comparatorCInterrupt: 0,
        overflowInterrupt:    0x05,  // vector 5 (OVF)
        tccra: 0x4A, tccrb: 0x53, tccrc: 0x00,
        tcnt:  0x52, ocra:  0x49,  ocrb: 0x48, ocrc: 0, icr: 0,
        timsk: 0x59, tifr:  0x58,
        comparatorPortA: Tiny85PortBConfig.PORT, comparatorPinA: 0,
        comparatorPortB: Tiny85PortBConfig.PORT, comparatorPinB: 1,
        comparatorPortC: 0, comparatorPinC: 0,
        externalClockPort: Tiny85PortBConfig.PORT, externalClockPin: 2,
        tov: 2, ocfa: 16, ocfb: 8, ocfc: 0,
        toie: 2, ociea: 16, ocieb: 8, ociec: 0);

    // ── ATtiny85 Timer 1 config ───────────────────────────────────────────────
    // 8-bit TC1. Single control register TCCR1 at 0x30.
    // TCCRA is mapped to address 0x00 (R0 — always 0 in well-behaved code → WGM = Normal mode).
    // TCCRB is mapped to TCCR1 (0x30) so CS10–CS12 select the prescaler.
    // Prescalers for CS10:CS12 on ATtiny85 TC1:
    //   1→/1, 2→/2, 3→/4, 4→/8, 5→/16, 6→/32, 7→/64
    // TIFR (0x58) and TIMSK (0x59) are shared with Timer0; MmioController hook-chaining
    // allows both timers to handle their own bits in the same register.
    // ATtiny85 datasheet interrupt vectors (word addresses):
    //   TIMER1_OVF  = 0x004, TIMER1_COMPA = 0x003, TIMER1_COMPB = 0x009
    private static readonly AvrTimerConfig Tiny85Timer1Config = new(
        bits: 8,
        dividers: [0, 1, 2, 4, 8, 16, 32, 64], // CS10:CS12 → /1../64
        captureInterrupt:     0,
        comparatorAInterrupt: 0x03,  // TIMER1_COMPA vector
        comparatorBInterrupt: 0x09,  // TIMER1_COMPB vector
        comparatorCInterrupt: 0,
        overflowInterrupt:    0x04,  // TIMER1_OVF vector
        tccra: 0x00,  // R0 — always 0 → forces Normal WGM mode
        tccrb: 0x30,  // TCCR1 — contains CS13:CS10
        tccrc: 0x00,
        tcnt:  0x2F,  // TCNT1
        ocra:  0x2E,  // OCR1A
        ocrb:  0x2B,  // OCR1B
        ocrc:  0,
        icr:   0,
        timsk: 0x59,  // shared TIMSK (Timer0 uses bits 0-2, Timer1 uses bits 4-6)
        tifr:  0x58,  // shared TIFR  (Timer0 uses bits 0-2, Timer1 uses bits 4-6)
        comparatorPortA: Tiny85PortBConfig.PORT, comparatorPinA: 1, // OC1A on PB1
        comparatorPortB: Tiny85PortBConfig.PORT, comparatorPinB: 4, // OC1B on PB4
        comparatorPortC: 0, comparatorPinC: 0,
        externalClockPort: 0, externalClockPin: 0,
        tov:  0x10,  // TOV1  = TIFR bit 4
        ocfa: 0x40,  // OCF1A = TIFR bit 6
        ocfb: 0x20,  // OCF1B = TIFR bit 5
        ocfc: 0,
        toie:  0x10, // TOIE1  = TIMSK bit 4
        ociea: 0x40, // OCIE1A = TIMSK bit 6
        ocieb: 0x20, // OCIE1B = TIMSK bit 5
        ociec: 0);

    // ── EEPROM ────────────────────────────────────────────────────────────────
    // ATtiny85: EECR=0x3C, EEDR=0x3D, EEARL=0x3E; no EEARH (9-bit address, 512 bytes)
    // EE_RDY: vector 7, word address 0x06
    private static readonly AvrEepromConfig Tiny85EepromConfig = new AvrEepromConfig(
        eepromReadyInterrupt: 0x06,
        eecr: 0x3C, eedr: 0x3D, eearl: 0x3E, eearh: 0x00,
        eraseCycles: 28800, writeCycles: 28800);

    // ── GPIO ──────────────────────────────────────────────────────────────────
    /// <summary>
    /// Port B — the only I/O port on the ATtiny85.
    /// Pins: PB0–PB5 (PB3=RESET by default).
    /// </summary>
    public AvrIoPort PortB { get; }

    // ── Timers ────────────────────────────────────────────────────────────────
    /// <summary>Timer 0 — 8-bit, OC0A on PB0, OC0B on PB1.</summary>
    public AvrTimer Timer0 { get; }

    /// <summary>
    /// Timer 1 — 8-bit TC1 (ATtiny85-specific). Normal mode only; prescalers /1–/64.
    /// OC1A on PB1, OC1B on PB4.
    /// </summary>
    public AvrTimer Timer1 { get; }

    /// <summary>ATtiny85 internal EEPROM — 512 bytes, volatile (in-memory backend).</summary>
    public AvrEeprom Eeprom { get; }

    public ATtiny85Simulation() : base(Flash, Sram)
    {
        WithFrequency(Frequency);

        AddGpio(Tiny85PortBConfig, out var portB); PortB = portB;
        AddTimer(Tiny85Timer0Config, out var t0);  Timer0 = t0;
        AddTimer(Tiny85Timer1Config, out var t1);  Timer1 = t1;
        AddEeprom(Tiny85EepromConfig, out var eeprom, 512); Eeprom = eeprom;
    }
}
