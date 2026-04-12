using Avr8Sharp.TestKit;
using Avr8Sharp.TestKit.Boards;

namespace Avr8Sharp.TestKit.Samples;

/// <summary>
/// Sample tests for the <b>Arduino Mega 2560</b> (ATmega2560).
/// <para>
/// Tests marked <c>[Ignore]</c> use <see cref="Placeholders.Break"/> as a stand-in.
/// Replace <c>WithHex(Placeholders.Break)</c> with your compiled hex and remove
/// the <c>[Ignore]</c> attribute.
/// </para>
/// </summary>
[TestFixture]
public class ArduinoMegaSamples
{
    // ── GPIO ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Runnable example: drive alternating pins on Port B HIGH/LOW.
    /// ATmega2560 Port B shares the same I/O register addresses as ATmega328P.
    /// </summary>
    [Test]
    public void Gpio_PortB_ShouldDriveAlternatingPins()
    {
        // DDRB  = I/O 0x04 (data 0x24)
        // PORTB = I/O 0x05 (data 0x25)
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(@"
            ldi r16, 0xFF
            out 0x04, r16       ; DDRB = 0xFF → all pins output
            ldi r16, 0xAA       ; 0b10101010
            out 0x05, r16       ; PORTB = 0xAA → odd pins HIGH
            break
        ");

        mega.RunToBreak();

        // Odd bits HIGH (1, 3, 5, 7), even bits LOW (0, 2, 4, 6).
        mega.PortB.Should().HaveOutputValue(0xAA);
        mega.PortB.Should().HavePinHigh(1);
        mega.PortB.Should().HavePinHigh(3);
        mega.PortB.Should().HavePinLow(0);
        mega.PortB.Should().HavePinLow(2);
    }

    // ── Placeholder tests (need real firmware) ────────────────────────────────

    /// <summary>
    /// Placeholder: each of the four hardware serial ports should print its channel ID.
    /// <para>
    /// Compile the following sketch targeting the Arduino Mega:
    /// <code>
    /// void setup() {
    ///     Serial.begin(115200);  Serial.println("ch0");
    ///     Serial1.begin(115200); Serial1.println("ch1");
    ///     Serial2.begin(115200); Serial2.println("ch2");
    ///     Serial3.begin(115200); Serial3.println("ch3");
    /// }
    /// void loop() {}
    /// </code>
    /// </para>
    /// </summary>
    [Test]
    public void Serial_AllChannels_ShouldPrintChannelIds()
    {
        var mega = new ArduinoMegaSimulation();
        var hexPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Firmware", "multi_serial.hex");
        mega.WithHex(File.ReadAllText(hexPath));

        mega.RunMilliseconds(100);

        mega.Serial0.Should().ContainLine("ch0");
        mega.Serial1.Should().ContainLine("ch1");
        mega.Serial2.Should().ContainLine("ch2");
        mega.Serial3.Should().ContainLine("ch3");
    }

    /// <summary>
    /// Placeholder: Port A should be all HIGH, Port L should be all LOW.
    /// <para>
    /// Compile a sketch that sets both ports explicitly:
    /// <code>
    /// void setup() {
    ///     DDRA  = 0xFF; PORTA = 0xFF;   // port A all HIGH
    ///     DDRL  = 0xFF; PORTL = 0x00;   // port L all LOW
    /// }
    /// </code>
    /// </para>
    /// </summary>
    [Test]
    public void Gpio_MultiPort_ShouldControlPortsIndependently()
    {
        var mega = new ArduinoMegaSimulation();
        var hexPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Firmware", "multi_port.hex");
        mega.WithHex(File.ReadAllText(hexPath));

        mega.RunMilliseconds(10);

        mega.PortA.Should().HaveOutputValue(0xFF);   // all HIGH
        mega.PortL.Should().HaveOutputValue(0x00);   // all LOW
    }
}
