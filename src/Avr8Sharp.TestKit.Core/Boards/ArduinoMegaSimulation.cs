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
    // ATmega2560: 256 KB flash, 8 KB SRAM + 256 B extended I/O, 4 KB EEPROM, 16 MHz
    // Sram = 0x100 (extended I/O, 0x100-0x1FF) + 0x2000 (SRAM, 0x200-0x21FF) = 0x2100
    // Total _ram = Sram + RegisterSpace (0x100) = 0x2200, covering 0x0000–0x21FF.
    private const int Flash = 0x40000;
    private const int Sram = 0x2100;
    private const uint Frequency = 16_000_000;
    private const uint EepromSize = 4096;

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

    // ── EEPROM ────────────────────────────────────────────────────────────────
    /// <summary>ATmega2560 internal EEPROM — 4096 bytes, volatile (in-memory backend).</summary>
    public AvrEeprom Eeprom { get; }

    // ── SPI / TWI / ADC / watchdog ────────────────────────────────────────────
    /// <summary>SPI peripheral (MOSI = PB2, MISO = PB3, SCK = PB1). Transfers are answered by <see cref="SpiBus"/>.</summary>
    public AvrSpi Spi { get; }
    /// <summary>Scripted SPI slave: records MOSI bytes, answers with queued responses or 0xFF.</summary>
    public SpiDeviceStub SpiBus { get; }
    /// <summary>TWI (I²C) peripheral (SCL = PD0, SDA = PD1). Transactions are answered by <see cref="TwiBus"/>.</summary>
    public AvrTwi Twi { get; }
    /// <summary>Scripted I²C slave; NACKs every address until one is added to <see cref="TwiDeviceStub.Addresses"/>.</summary>
    public TwiDeviceStub TwiBus { get; }
    /// <summary>
    /// 10-bit ADC with 16 single-ended channels (A0-A15; channels 8-15 via MUX5), the 1.1 V bandgap
    /// and GND. Differential inputs are not modelled. Channels read 0 V until set via <see cref="AvrAdc.ChannelValues"/>.
    /// </summary>
    public AvrAdc Adc { get; }
    /// <summary>Watchdog timer. It also owns MCUSR, which reads PORF (0x01) after construction.</summary>
    public AvrWatchdog Watchdog { get; }

    public ArduinoMegaSimulation() : base(Flash, Sram)
    {
        WithFrequency(Frequency);

        // ATmega2560 SRAM starts at 0x200 (extended I/O occupies 0x100-0x1FF); a
        // PUSH/CALL below it has overflowed the stack into the I/O/register space.
        Cpu.StackLowLimit = 0x200;
        Cpu.RamStart = 0x200;

        AddGpio(AvrIoPort.Mega2560PortAConfig, out var pA); PortA = pA;
        AddGpio(AvrIoPort.Mega2560PortBConfig, out var pB); PortB = pB;
        AddGpio(AvrIoPort.Mega2560PortCConfig, out var pC); PortC = pC;
        AddGpio(AvrIoPort.Mega2560PortDConfig, out var pD); PortD = pD;
        AddGpio(AvrIoPort.Mega2560PortEConfig, out var pE); PortE = pE;
        AddGpio(AvrIoPort.Mega2560PortFConfig, out var pF); PortF = pF;
        AddGpio(AvrIoPort.Mega2560PortGConfig, out var pG); PortG = pG;
        AddGpio(AvrIoPort.Mega2560PortHConfig, out var pH); PortH = pH;
        AddGpio(AvrIoPort.Mega2560PortJConfig, out var pJ); PortJ = pJ;
        AddGpio(AvrIoPort.Mega2560PortKConfig, out var pK); PortK = pK;
        AddGpio(AvrIoPort.Mega2560PortLConfig, out var pL); PortL = pL;

        AddTimer(AvrTimer.Mega2560Timer0Config, out var t0); Timer0 = t0;
        AddTimer(AvrTimer.Mega2560Timer1Config, out var t1); Timer1 = t1;
        AddTimer(AvrTimer.Mega2560Timer2Config, out var t2); Timer2 = t2;
        AddTimer(AvrTimer.Mega2560Timer3Config, out var t3); Timer3 = t3;
        AddTimer(AvrTimer.Mega2560Timer4Config, out var t4); Timer4 = t4;
        AddTimer(AvrTimer.Mega2560Timer5Config, out var t5); Timer5 = t5;

        AddUsart(AvrUsart.Mega2560Usart0Config, out var s0); Serial0 = s0;
        AddUsart(AvrUsart.Mega2560Usart1Config, out var s1); Serial1 = s1;
        AddUsart(AvrUsart.Mega2560Usart2Config, out var s2); Serial2 = s2;
        AddUsart(AvrUsart.Mega2560Usart3Config, out var s3); Serial3 = s3;

        AddEeprom(AvrEeprom.Mega2560EepromConfig, out var eeprom, EepromSize); Eeprom = eeprom;

        AddSpi(AvrSpi.Mega2560SpiConfig, out var spi); Spi = spi;
        SpiBus = new SpiDeviceStub();
        spi.OnTransfer = SpiBus.Transfer;

        AddTwi(AvrTwi.Mega2560TwiConfig, out var twi); Twi = twi;
        TwiBus = new TwiDeviceStub(twi);
        twi.EventHandler = TwiBus;

        AddAdc(AvrAdc.Mega2560AdcConfig, out var adc); Adc = adc;

        AddWatchdog(AvrWatchdog.Mega2560WatchdogConfig, out var watchdog); Watchdog = watchdog;
    }
}
