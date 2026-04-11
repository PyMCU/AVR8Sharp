using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.TestKit;

namespace Avr8Sharp.TestKit.Samples;

/// <summary>
/// Sample tests demonstrating <see cref="AvrTestSimulation"/> used directly —
/// without a pre-configured board class.
/// <para>
/// Use this pattern when targeting a custom AVR design that doesn't map to
/// Arduino Uno, Mega, or ATtiny85 out of the box.
/// </para>
/// </summary>
[TestFixture]
public class LowLevelSamples
{
    // ── Manual peripheral setup ───────────────────────────────────────────────

    /// <summary>
    /// Runnable example: create a bare simulation, attach only Port B, run a GPIO test.
    /// This is the manual equivalent of what <c>ArduinoUnoSimulation</c> does internally.
    /// </summary>
    [Test]
    public void Manual_PortB_LowerNibbleShouldBeHigh()
    {
        // Build a simulation sized for ATmega328P flash/SRAM.
        var sim = AvrTestSimulation.Create(flashSize: 0x8000, sramBytes: 2048)
            .WithFrequency(16_000_000)
            .AddGpio(AvrIoPort.PortBConfig, out var portB)
            .AddTimer(AvrTimer.Timer0Config)
            .AddTimer(AvrTimer.Timer1Config);

        sim.WithAsm(@"
            ldi r16, 0xFF
            out 0x04, r16       ; DDRB  = 0xFF → all pins output
            ldi r16, 0x0F
            out 0x05, r16       ; PORTB = 0x0F → lower nibble HIGH
            break
        ");

        sim.RunToBreak();

        portB.Should().HaveOutputValue(0x0F);  // pins 0-3 HIGH, 4-7 LOW
    }

    // ── RunUntil ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Runnable example: use <c>RunUntil</c> to stop execution mid-program
    /// as soon as a register reaches a target value.
    /// </summary>
    [Test]
    public void RunUntil_StopsWhenRegisterReachesTarget()
    {
        // Program counts R16 from 0 to 5, then breaks.
        // RunUntil stops early when R16 == 3.
        var sim = AvrTestSimulation.Create();
        sim.WithAsm(@"
            ldi r16, 0
            inc r16             ; R16 = 1
            inc r16             ; R16 = 2
            inc r16             ; R16 = 3
            inc r16             ; R16 = 4  (never reached)
            inc r16             ; R16 = 5  (never reached)
            break
        ");

        // RunUntil evaluates the predicate BEFORE each instruction.
        // Execution stops the moment R16 first equals 3.
        sim.RunUntil(s => s.Cpu.Mmio.Data[16] == 3);

        sim.Cpu.Should().HaveRegister(16, 3);
    }

    // ── RunInstructions ───────────────────────────────────────────────────────

    /// <summary>
    /// Runnable example: advance the simulation by an exact instruction count
    /// rather than by simulated time or a predicate.
    /// </summary>
    [Test]
    public void RunInstructions_ShouldExecuteExactCount()
    {
        // ldi = 1 cycle/instruction, inc = 1 cycle/instruction.
        // After 3 instructions (ldi + 2×inc), R16 = 2; the 4th inc is not reached.
        var sim = AvrTestSimulation.Create();
        sim.WithAsm(@"
            ldi r16, 0          ; instruction 1
            inc r16             ; instruction 2 → R16 = 1
            inc r16             ; instruction 3 → R16 = 2
            inc r16             ; instruction 4 → not reached
            break
        ");

        sim.RunInstructions(3);

        sim.Cpu.Should().HaveRegister(16, 2);
    }

    // ── Custom USART probe ────────────────────────────────────────────────────

    /// <summary>
    /// Placeholder: attach a <see cref="Avr8Sharp.TestKit.Probes.SerialProbe"/> manually
    /// to a custom simulation and assert serial output.
    /// <para>
    /// Replace <c>WithHex(Placeholders.Break)</c> with firmware that writes to USART0.
    /// </para>
    /// </summary>
    [Test, Ignore("Replace placeholder hex with firmware that uses USART0")]
    public void Manual_SerialProbe_ShouldCaptureUsartOutput()
    {
        var sim = AvrTestSimulation.Create(flashSize: 0x8000, sramBytes: 2048)
            .WithFrequency(16_000_000)
            .AddGpio(AvrIoPort.PortBConfig, out _)
            .AddGpio(AvrIoPort.PortCConfig, out _)
            .AddGpio(AvrIoPort.PortDConfig, out _)
            .AddTimer(AvrTimer.Timer0Config)
            .AddTimer(AvrTimer.Timer1Config)
            .AddTimer(AvrTimer.Timer2Config)
            .AddUsart(AvrUsart.Usart0Config, out var serial);

        sim.WithHex(Placeholders.Break);    // TODO: sim.WithHex(File.ReadAllText("firmware/hello.hex"))

        sim.RunMilliseconds(100);

        serial.Should().Contain("Hello");
        serial.Should().NotContain("Error");
    }
}
