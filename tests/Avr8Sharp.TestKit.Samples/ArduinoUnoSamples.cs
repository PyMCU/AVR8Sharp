using Avr8Sharp.TestKit;
using Avr8Sharp.TestKit.Boards;

namespace Avr8Sharp.TestKit.Samples;

/// <summary>
/// Sample tests for the <b>Arduino Uno</b> (ATmega328P).
/// <para>
/// Tests marked <c>[Ignore]</c> use <see cref="Placeholders.Break"/> as a stand-in.
/// Replace <c>WithHex(Placeholders.Break)</c> with <c>WithHex(File.ReadAllText("firmware.hex"))</c>
/// (or your preferred loading method) and remove the <c>[Ignore]</c> attribute.
/// </para>
/// </summary>
[TestFixture]
public class ArduinoUnoSamples
{
    // ── GPIO ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Runnable example: configure PB5 (digital pin 13) as output and drive it HIGH.
    /// Equivalent to: <c>pinMode(13, OUTPUT); digitalWrite(13, HIGH);</c>
    /// </summary>
    [Test]
    public void Gpio_Pin13_ShouldBeHighAfterSetup()
    {
        var uno = new ArduinoUnoSimulation();
        uno.WithAsm(@"
            ldi r16, 0x20       ; 0x20 = bit 5 = PB5 (Arduino digital pin 13)
            out 0x04, r16       ; DDRB  = 0x20 → PB5 as output
            out 0x05, r16       ; PORTB = 0x20 → PB5 HIGH
            break
        ");

        uno.RunToBreak();

        uno.PortB.Should().HavePinHigh(5);   // PB5 = digital pin 13 (built-in LED)
    }

    /// <summary>
    /// Runnable example: drive the lower nibble of Port D HIGH, upper nibble LOW.
    /// Demonstrates bitmask assertion with <c>HaveOutputValue</c>.
    /// </summary>
    [Test]
    public void Gpio_PortD_ShouldHaveLowerNibbleHigh()
    {
        var uno = new ArduinoUnoSimulation();
        uno.WithAsm(@"
            ldi r16, 0xFF
            out 0x0A, r16       ; DDRD  = 0xFF → all pins output
            ldi r16, 0x0F
            out 0x0B, r16       ; PORTD = 0x0F → lower nibble HIGH
            break
        ");

        uno.RunToBreak();

        // HaveOutputValue checks the bitmask of all HIGH output pins on the port.
        uno.PortD.Should().HaveOutputValue(0x0F);

        uno.PortD.Should().HavePinHigh(0);  // PD0 HIGH
        uno.PortD.Should().HavePinHigh(3);  // PD3 HIGH
        uno.PortD.Should().HavePinLow(4);   // PD4 LOW
        uno.PortD.Should().HavePinLow(7);   // PD7 LOW
    }

    // ── CPU / registers ───────────────────────────────────────────────────────

    /// <summary>
    /// Runnable example: simple counter — count to 5 in R16 and assert the result.
    /// </summary>
    [Test]
    public void Cpu_RegisterShouldHoldCounterValue()
    {
        var uno = new ArduinoUnoSimulation();
        uno.WithAsm(@"
            ldi r16, 0
            inc r16             ; R16 = 1
            inc r16             ; R16 = 2
            inc r16             ; R16 = 3
            inc r16             ; R16 = 4
            inc r16             ; R16 = 5
            break
        ");

        uno.RunToBreak();

        uno.Cpu.Should().HaveRegister(16, 5);
    }

    // ── SRAM ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Runnable example: write two sentinel bytes to SRAM and verify them individually
    /// and as a little-endian word.
    /// </summary>
    [Test]
    public void Memory_ShouldReflectSramWrite()
    {
        var uno = new ArduinoUnoSimulation();
        uno.WithAsm(@"
            ldi r16, 0xAB
            sts 0x0100, r16     ; SRAM[0x0100] = 0xAB
            ldi r17, 0xCD
            sts 0x0101, r17     ; SRAM[0x0101] = 0xCD
            break
        ");

        uno.RunToBreak();

        uno.Memory.Should().HaveByteAt(0x0100, 0xAB);
        uno.Memory.Should().HaveByteAt(0x0101, 0xCD);
        uno.Memory.Should().HaveWordAt(0x0100, 0xCDAB);  // little-endian word
    }

    // ── Placeholder tests (need real firmware) ────────────────────────────────

    /// <summary>
    /// Placeholder: LED should blink at 1 Hz (HIGH at 500 ms, LOW at 1000 ms).
    /// <para>
    /// Compile the following Arduino sketch and pass it via
    /// <c>WithHex(File.ReadAllText("firmware/blink.hex"))</c>:
    /// <code>
    /// void setup() { pinMode(13, OUTPUT); }
    /// void loop()  { digitalWrite(13, HIGH); delay(500);
    ///                digitalWrite(13, LOW);  delay(500); }
    /// </code>
    /// </para>
    /// </summary>
    [Test, Ignore("Replace placeholder hex with a compiled blink sketch")]
    public void Gpio_Blink_ShouldToggleLedEvery500ms()
    {
        var uno = new ArduinoUnoSimulation();
        uno.WithHex(Placeholders.Break);    // TODO: uno.WithHex(File.ReadAllText("firmware/blink.hex"))

        uno.RunMilliseconds(500);
        uno.PortB.Should().HavePinHigh(5);  // digital pin 13 HIGH at 500 ms

        uno.RunMilliseconds(500);
        uno.PortB.Should().HavePinLow(5);   // digital pin 13 LOW at 1000 ms
    }

    /// <summary>
    /// Placeholder: <c>Serial.println("Hello from Uno!")</c> should appear on the serial probe.
    /// <para>
    /// Compile the following sketch:
    /// <code>
    /// void setup() { Serial.begin(115200); Serial.println("Hello from Uno!"); }
    /// void loop()  {}
    /// </code>
    /// </para>
    /// </summary>
    [Test, Ignore("Replace placeholder hex with a compiled serial sketch")]
    public void Serial_ShouldPrintStartupMessage()
    {
        var uno = new ArduinoUnoSimulation();
        uno.WithHex(Placeholders.Break);    // TODO: uno.WithHex(File.ReadAllText("firmware/serial_hello.hex"))

        uno.RunMilliseconds(100);

        uno.Serial.Should().Contain("Hello from Uno!");
        uno.Serial.Should().ContainLine("Hello from Uno!");
    }
}
