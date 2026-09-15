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

    // ── RunMicroseconds ──────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="AvrTestSimulation.RunMicroseconds"/> advances exactly
    /// <c>us / 1e6 * frequency</c> cycles, the fine-grained sibling of
    /// <see cref="AvrTestSimulation.RunMilliseconds"/> used to time an external stimulus
    /// (a pin pulse, a sensor's response delay) between writes.
    /// </summary>
    [Test]
    public void RunMicroseconds_AdvancesExactCycleCount()
    {
        var sim = AvrTestSimulation.Create().WithFrequency(16_000_000);

        sim.RunMicroseconds(10);

        Assert.That(sim.Cpu.Cycles, Is.EqualTo(160)); // 10 us * 16 cycles/us @ 16 MHz
    }

    /// <summary>
    /// Runnable example: drive a pin the firmware has configured as an input from the outside
    /// world, timing the pulse with <see cref="AvrTestSimulation.RunMicroseconds"/> — the pattern
    /// an external stimulus test (a button, an echo pin, a bit-banged sensor reply) is built
    /// from. Mirrors what a real firmware's <c>pulseio.PulseIn</c> would capture on the pin.
    /// <para>
    /// <see cref="AvrIoPort.GetPinState"/> reports what the chip itself is driving (its DDR/PORT
    /// configuration), not an externally injected level, so reading the pulse back — the way
    /// firmware reads its own PIN register — goes through <see cref="Cpu.ReadData"/> instead.
    /// </para>
    /// </summary>
    [Test]
    public void SetPinValue_DrivesAnInputPinForATimedPulse()
    {
        const int Pind = 0x29;
        const int Pd2Mask = 1 << 2;

        var sim = AvrTestSimulation.Create()
            .WithFrequency(16_000_000)
            .AddGpio(AvrIoPort.PortDConfig, out var portD);

        // PD2 stays at its reset default (input, no pull-up): driving it is uncontested.
        portD.SetPinValue(2, true);
        Assert.That(sim.Cpu.ReadData(Pind) & Pd2Mask, Is.Not.Zero, "the pulse starts high");

        sim.RunMicroseconds(500);
        portD.SetPinValue(2, false);

        Assert.That(sim.Cpu.ReadData(Pind) & Pd2Mask, Is.Zero, "the pulse ends low");
        Assert.That(sim.Cpu.Cycles, Is.EqualTo(8000)); // 500 us * 16 cycles/us @ 16 MHz
    }

    // ── Custom USART probe ────────────────────────────────────────────────────

    /// <summary>
    /// Placeholder: attach a <see cref="Avr8Sharp.TestKit.Probes.SerialProbe"/> manually
    /// to a custom simulation and assert serial output.
    /// <para>
    /// Replace <c>WithHex(Placeholders.Break)</c> with firmware that writes to USART0.
    /// </para>
    /// </summary>
    [Test]
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

        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Firmware", "serial_hello.hex");
        sim.WithHex(File.ReadAllText(path));    // TODO: sim.WithHex(File.ReadAllText("firmware/hello.hex"))

        sim.RunMilliseconds(100);

        serial.Should().Contain("Hello");
    }
}
