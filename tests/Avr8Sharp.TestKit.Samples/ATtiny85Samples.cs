using Avr8Sharp.TestKit;
using Avr8Sharp.TestKit.Boards;

namespace Avr8Sharp.TestKit.Samples;

/// <summary>
/// Sample tests for the <b>ATtiny85</b>.
/// <para>
/// ATtiny85 register map (I/O addresses for the <c>out</c> instruction):
/// <list type="table">
///   <item><term>0x16</term><description>PINB  (data 0x36)</description></item>
///   <item><term>0x17</term><description>DDRB  (data 0x37)</description></item>
///   <item><term>0x18</term><description>PORTB (data 0x38)</description></item>
/// </list>
/// </para>
/// <para>
/// Tests marked <c>[Ignore]</c> use <see cref="Placeholders.Break"/> as a stand-in.
/// Replace <c>WithHex(Placeholders.Break)</c> with your compiled hex and remove
/// the <c>[Ignore]</c> attribute.
/// </para>
/// </summary>
[TestFixture]
public class ATtiny85Samples
{
    // ── GPIO ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Runnable example: set PB0 as output and drive it HIGH.
    /// </summary>
    [Test]
    public void Gpio_PB0_ShouldBeHigh()
    {
        var tiny = new ATtiny85Simulation();
        tiny.WithAsm(@"
            ldi r16, 0x01       ; bit 0 = PB0
            out 0x17, r16       ; DDRB  = 0x01 → PB0 as output
            out 0x18, r16       ; PORTB = 0x01 → PB0 HIGH
            break
        ");

        tiny.RunToBreak();

        tiny.PortB.Should().HavePinHigh(0);  // PB0
    }

    /// <summary>
    /// Runnable example: PB0–PB3 as outputs, all driven LOW before a chaser loop starts.
    /// Also verifies PB4 and PB5 remain input-floating (not configured as outputs).
    /// </summary>
    [Test]
    public void Gpio_LedPins_ShouldStartLow_BeforeChaseBegins()
    {
        var tiny = new ATtiny85Simulation();
        tiny.WithAsm(@"
            ldi r16, 0x0F       ; bits 0-3 = PB0-PB3
            out 0x17, r16       ; DDRB  = 0x0F → PB0-PB3 as outputs
            ldi r16, 0x00
            out 0x18, r16       ; PORTB = 0x00 → all LOW
            break
        ");

        tiny.RunToBreak();

        tiny.PortB.Should().HavePinInput(4);   // PB4 not configured → floating input
        tiny.PortB.Should().HavePinInput(5);   // PB5 not configured → floating input
        tiny.PortB.Should().HavePinLow(0);     // PB0 output, currently LOW
        tiny.PortB.Should().HavePinLow(3);     // PB3 output, currently LOW
    }

    // ── Placeholder tests (need real firmware) ────────────────────────────────

    /// <summary>
    /// Placeholder: 4-LED chaser — each LED lights in sequence (PB0 → PB1 → PB2 → PB3).
    /// <para>
    /// Compile the following with avr-gcc or the Arduino IDE targeting ATtiny85 at 8 MHz:
    /// <code>
    /// byte leds[] = { PB0, PB1, PB2, PB3 };
    /// int  i = 0;
    /// void setup() {
    ///     for (byte j = 0; j &lt; 4; j++) pinMode(leds[j], OUTPUT);
    /// }
    /// void loop() {
    ///     digitalWrite(leds[i], HIGH);
    ///     delay(250);
    ///     digitalWrite(leds[i], LOW);
    ///     i = (i + 1) % 4;
    /// }
    /// </code>
    /// </para>
    /// </summary>
    [Test, Ignore("Replace placeholder hex with a compiled led-chaser sketch")]
    public void Gpio_LedChaser_ShouldLightEachLedInTurn()
    {
        var tiny = new ATtiny85Simulation();
        tiny.WithHex(Placeholders.Break);   // TODO: tiny.WithHex(File.ReadAllText("firmware/led_chaser.hex"))

        // After 125 ms PB0 should be HIGH (first LED on, halfway through first 250 ms step).
        tiny.RunMilliseconds(125);
        tiny.PortB.Should().HavePinHigh(0);

        // After 375 ms total: PB0 LOW, PB1 HIGH.
        tiny.RunMilliseconds(250);
        tiny.PortB.Should().HavePinLow(0);
        tiny.PortB.Should().HavePinHigh(1);
    }
}
