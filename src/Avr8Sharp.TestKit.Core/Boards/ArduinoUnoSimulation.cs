using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.TestKit.Probes;

namespace Avr8Sharp.TestKit.Boards;

/// <summary>
/// Pre-configured simulation for the <b>Arduino Uno</b> (ATmega328P).
/// <para>
/// All standard peripherals are created automatically:
/// three GPIO ports (B, C, D), three timers (0, 1, 2), USART0, EEPROM, SPI, TWI, ADC and the watchdog.
/// Serial output is captured in <see cref="Serial"/>. SPI and TWI are wired to scripted slaves
/// (<see cref="SpiBus"/>, <see cref="TwiBus"/>) so transfers always complete.
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

    // ── SPI / TWI / ADC / watchdog ────────────────────────────────────────────
    /// <summary>SPI peripheral (SPCR/SPSR/SPDR at 0x4C-0x4E). Transfers are answered by <see cref="SpiBus"/>.</summary>
    public AvrSpi Spi { get; }
    /// <summary>
    /// Scripted SPI slave: records every MOSI byte and answers with <see cref="SpiDeviceStub.Responses"/>,
    /// or 0xFF (idle MISO) once the queue is empty, so a transfer always completes.
    /// </summary>
    public SpiDeviceStub SpiBus { get; }
    /// <summary>TWI (I²C) peripheral (TWBR..TWAMR at 0xB8-0xBD). Transactions are answered by <see cref="TwiBus"/>.</summary>
    public AvrTwi Twi { get; }
    /// <summary>
    /// Scripted I²C slave. With no address in <see cref="TwiDeviceStub.Addresses"/> every SLA+W/R is
    /// NACKed, as on a bus with pull-ups and no device; add addresses to make it ACK.
    /// </summary>
    public TwiDeviceStub TwiBus { get; }
    /// <summary>10-bit ADC. Every channel reads 0 V until set through <see cref="AvrAdc.ChannelValues"/>.</summary>
    public AvrAdc Adc { get; }
    /// <summary>Watchdog timer. It also owns MCUSR, which reads PORF (0x01) after construction.</summary>
    public AvrWatchdog Watchdog { get; }

    public ArduinoUnoSimulation() : base(Flash, Sram)
    {
        WithFrequency(Frequency);

        // ATmega328P SRAM starts at 0x100; a PUSH/CALL below it has overflowed the
        // stack into the I/O/register space. Surface it instead of silently corrupting
        // peripheral state.
        Cpu.StackLowLimit = 0x100;

        AddGpio(AvrIoPort.PortBConfig, out var portB); PortB = portB;
        AddGpio(AvrIoPort.PortCConfig, out var portC); PortC = portC;
        AddGpio(AvrIoPort.PortDConfig, out var portD); PortD = portD;

        AddTimer(AvrTimer.Timer0Config, out var t0); Timer0 = t0;
        AddTimer(AvrTimer.Timer1Config, out var t1); Timer1 = t1;
        AddTimer(AvrTimer.Timer2Config, out var t2); Timer2 = t2;

        AddUsart(AvrUsart.Usart0Config, out var serial); Serial = serial;

        AddEeprom(AvrEeprom.EepromConfig, out var eeprom); Eeprom = eeprom;

        AddSpi(AvrSpi.SpiConfig, out var spi); Spi = spi;
        SpiBus = new SpiDeviceStub();
        spi.OnTransfer = SpiBus.Transfer;

        AddTwi(AvrTwi.TwiConfig, out var twi); Twi = twi;
        TwiBus = new TwiDeviceStub(twi);
        twi.EventHandler = TwiBus;

        AddAdc(AvrAdc.AdcConfig, out var adc); Adc = adc;

        AddWatchdog(AvrWatchdog.WatchdogConfig, out var watchdog); Watchdog = watchdog;
    }
}
