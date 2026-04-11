using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.TestKit.Probes;

namespace Avr8Sharp.TestKit.Boards;

/// <summary>
/// Pre-configured simulation for the <b>Arduino Uno</b> (ATmega328P).
/// <para>
/// All standard peripherals are created automatically:
/// three GPIO ports (B, C, D), three timers (0, 1, 2), and USART0.
/// Serial output is captured in <see cref="Serial"/>.
/// </para>
/// </summary>
/// <example>
/// <code>
/// var uno = new ArduinoUnoSimulation()
///     .WithHex(File.ReadAllText("sketch.hex"));
///
/// uno.RunMilliseconds(500);
///
/// uno.PortB.Should().HavePinHigh(5);   // digital pin 13
/// uno.Serial.Should().Contain("Hello");
/// </code>
/// </example>
public sealed class ArduinoUnoSimulation : AvrTestSimulation
{
    // ATmega328P: 32 KB flash, 2 KB SRAM, 16 MHz
    private const int Flash = 0x8000;
    private const int Sram  = 2048;
    private const uint Frequency = 16_000_000;

    // ── GPIO ports ────────────────────────────────────────────────────────────
    /// <summary>Port B — digital pins 8–13, SPI, crystal.</summary>
    public AvrIoPort PortB { get; }
    /// <summary>Port C — analog pins A0–A5, TWI (A4/A5).</summary>
    public AvrIoPort PortC { get; }
    /// <summary>Port D — digital pins 0–7, USART (0/1), external interrupts (2/3).</summary>
    public AvrIoPort PortD { get; }

    // ── Timers ────────────────────────────────────────────────────────────────
    /// <summary>Timer 0 — 8-bit, PWM on OC0A (PD6) and OC0B (PD5).</summary>
    public AvrTimer Timer0 { get; }
    /// <summary>Timer 1 — 16-bit, PWM on OC1A (PB1) and OC1B (PB2).</summary>
    public AvrTimer Timer1 { get; }
    /// <summary>Timer 2 — 8-bit async, PWM on OC2A (PB3) and OC2B (PD3).</summary>
    public AvrTimer Timer2 { get; }

    // ── USART ─────────────────────────────────────────────────────────────────
    /// <summary>Captures all bytes sent via USART0 (TX = PD1).</summary>
    public SerialProbe Serial { get; }

    // ── EEPROM ────────────────────────────────────────────────────────────────
    /// <summary>ATmega328P internal EEPROM — 1024 bytes, volatile (in-memory backend).</summary>
    public AvrEeprom Eeprom { get; }

    public ArduinoUnoSimulation() : base(Flash, Sram)
    {
        WithFrequency(Frequency);

        AddGpio(AvrIoPort.PortBConfig, out var portB); PortB = portB;
        AddGpio(AvrIoPort.PortCConfig, out var portC); PortC = portC;
        AddGpio(AvrIoPort.PortDConfig, out var portD); PortD = portD;

        AddTimer(AvrTimer.Timer0Config, out var t0); Timer0 = t0;
        AddTimer(AvrTimer.Timer1Config, out var t1); Timer1 = t1;
        AddTimer(AvrTimer.Timer2Config, out var t2); Timer2 = t2;

        AddUsart(AvrUsart.Usart0Config, out var serial); Serial = serial;

        AddEeprom(AvrEeprom.EepromConfig, out var eeprom); Eeprom = eeprom;
    }
}
