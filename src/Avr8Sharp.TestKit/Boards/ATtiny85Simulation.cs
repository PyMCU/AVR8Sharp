using AVR8Sharp.Core.Peripherals;

namespace Avr8Sharp.TestKit.Boards;

/// <summary>
/// Pre-configured simulation for the <b>ATtiny85</b>.
/// <para>
/// All standard peripherals are created automatically:
/// Port B (the only GPIO port) and Timer 0.
/// The ATtiny85 has no hardware USART; use the USI peripheral via
/// <see cref="AvrTestSimulation.AddUsart"/> if needed.
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

    // ── GPIO ──────────────────────────────────────────────────────────────────
    /// <summary>
    /// Port B — the only I/O port on the ATtiny85.
    /// Pins: PB0–PB5 (PB3=RESET by default).
    /// </summary>
    public AvrIoPort PortB { get; }

    // ── Timers ────────────────────────────────────────────────────────────────
    /// <summary>Timer 0 — 8-bit, OC0A on PB0, OC0B on PB1.</summary>
    public AvrTimer Timer0 { get; }

    public ATtiny85Simulation() : base(Flash, Sram)
    {
        WithFrequency(Frequency);

        AddGpio(Tiny85PortBConfig, out var portB); PortB = portB;
        AddTimer(Tiny85Timer0Config, out var t0);  Timer0 = t0;
    }
}
