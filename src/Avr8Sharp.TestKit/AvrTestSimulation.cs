using AVR8Sharp.Core;
using AVR8Sharp.Core.Cpu;
using AVR8Sharp.Core.Peripherals;
using AVR8Sharp.Core.Utils;
using Avr8Sharp.TestKit.Probes;
using AvrCpu = AVR8Sharp.Core.Cpu.Cpu;

namespace Avr8Sharp.TestKit;

/// <summary>
/// A fluent simulation harness for integration testing compiled AVR programs.
/// Supports loading Intel HEX files, raw assembly, or binary bytes and provides
/// execution control and FluentAssertions-based assertions.
/// <para>
/// For common boards (Arduino Uno, Mega, ATtiny85) use the pre-configured
/// subclasses: <see cref="Boards.ArduinoUnoSimulation"/>,
/// <see cref="Boards.ArduinoMegaSimulation"/>, <see cref="Boards.ATtiny85Simulation"/>.
/// </para>
/// </summary>
/// <example>
/// <code>
/// var sim = AvrTestSimulation.Create()
///     .WithFrequency(16_000_000)
///     .WithHex(File.ReadAllText("blink.hex"))
///     .AddGpio(AvrIoPort.PortBConfig, out var portB)
///     .AddUsart(AvrUsart.Usart0Config, out var serial)
///     .AddTimer(AvrTimer.Timer0Config);
///
/// sim.RunMilliseconds(500);
///
/// portB.Should().HavePinHigh(5);
/// serial.Should().Contain("Hello");
/// </code>
/// </example>
public class AvrTestSimulation
{
    private const ushort BreakOpcode = 0x9598;

    internal readonly AvrRunner Runner;

    public AvrCpu Cpu => Runner.Cpu;
    public byte[] Data => Runner.Cpu.Data;
    public AvrMemoryView Memory => new(Runner.Cpu.Data);

    protected AvrTestSimulation(int flashSize, int sramBytes)
    {
        Runner = new AvrRunner(new byte[flashSize], sramBytes);
    }

    /// <summary>Creates a new blank simulation with the given flash and SRAM sizes.</summary>
    public static AvrTestSimulation Create(int flashSize = AvrRunner.FLASH, int sramBytes = 8192)
        => new(flashSize, sramBytes);

    // ── Program loading ──────────────────────────────────────────────────────

    /// <summary>Sets the CPU clock frequency (default: 16 MHz).</summary>
    public AvrTestSimulation WithFrequency(uint hz)
    {
        Runner.SetSpeed(hz);
        return this;
    }

    /// <summary>Loads a program from an Intel HEX string (output of gcc, avra, avr-objcopy, PyMCU, etc.).</summary>
    public AvrTestSimulation WithHex(string hexContent)
    {
        Runner.LoadHex(hexContent);
        return this;
    }

    /// <summary>Assembles inline AVR assembly source and loads the result.</summary>
    public AvrTestSimulation WithAsm(string asmSource)
    {
        var assembler = new AvrAssembler();
        var bytes = assembler.Assemble(asmSource);
        if (assembler.Errors.Count > 0)
            throw new InvalidOperationException(
                "Assembly failed:\n  " + string.Join("\n  ", assembler.Errors));
        Runner.LoadProgram(bytes);
        return this;
    }

    /// <summary>Loads a raw program byte array (e.g. extracted from an ELF binary).</summary>
    public AvrTestSimulation WithProgram(byte[] bytes)
    {
        Runner.LoadProgram(bytes);
        return this;
    }

    // ── Peripheral setup ─────────────────────────────────────────────────────

    /// <summary>Adds a GPIO port and returns an <see cref="AvrIoPort"/> for assertions and pin driving.</summary>
    public AvrTestSimulation AddGpio(AvrPortConfig config, out AvrIoPort port)
    {
        port = new AvrIoPort(Runner.Cpu, config);
        return this;
    }

    /// <summary>Adds a USART and attaches a <see cref="SerialProbe"/> that captures all transmitted bytes.</summary>
    public AvrTestSimulation AddUsart(AvrUsartConfig config, out SerialProbe serial)
    {
        var usart = new AvrUsart(Runner.Cpu, config, Runner.Speed);
        serial = new SerialProbe(usart);
        return this;
    }

    /// <summary>Adds a USART without a capture probe (use when you only need RX injection).</summary>
    public AvrTestSimulation AddUsart(AvrUsartConfig config)
    {
        _ = new AvrUsart(Runner.Cpu, config, Runner.Speed);
        return this;
    }

    /// <summary>Adds a timer peripheral.</summary>
    public AvrTestSimulation AddTimer(AvrTimerConfig config)
    {
        _ = new AvrTimer(Runner.Cpu, config);
        return this;
    }

    /// <summary>Adds a timer peripheral and returns the instance for further configuration.</summary>
    public AvrTestSimulation AddTimer(AvrTimerConfig config, out AvrTimer timer)
    {
        timer = new AvrTimer(Runner.Cpu, config);
        return this;
    }

    /// <summary>Adds an SPI peripheral.</summary>
    public AvrTestSimulation AddSpi(AvrSpiConfig config, out AvrSpi spi)
    {
        spi = new AvrSpi(Runner.Cpu, config, Runner.Speed);
        return this;
    }

    /// <summary>Adds a TWI (I²C) peripheral.</summary>
    public AvrTestSimulation AddTwi(AvrTwiConfig config, out AvrTwi twi)
    {
        twi = new AvrTwi(Runner.Cpu, config, Runner.Speed);
        return this;
    }

    /// <summary>Adds an ADC peripheral.</summary>
    public AvrTestSimulation AddAdc(AvrAdcConfig config, out AvrAdc adc)
    {
        adc = new AvrAdc(Runner.Cpu, config);
        return this;
    }

    // ── Execution ────────────────────────────────────────────────────────────

    /// <summary>
    /// Runs the simulation for exactly <paramref name="cycles"/> CPU cycles.
    /// Returns <c>this</c> for chaining.
    /// </summary>
    public AvrTestSimulation RunCycles(long cycles)
    {
        var target = (long)Runner.Cpu.Cycles + cycles;
        while ((long)Runner.Cpu.Cycles < target)
        {
            Instruction.AvrInstruction(Runner.Cpu);
            Runner.Cpu.Tick();
        }
        return this;
    }

    /// <summary>
    /// Runs the simulation for <paramref name="ms"/> simulated milliseconds at the configured frequency.
    /// Returns <c>this</c> for chaining.
    /// </summary>
    public AvrTestSimulation RunMilliseconds(double ms)
        => RunCycles((long)(ms / 1000.0 * Runner.Speed));

    /// <summary>
    /// Executes exactly <paramref name="count"/> instructions.
    /// Returns <c>this</c> for chaining.
    /// </summary>
    public AvrTestSimulation RunInstructions(int count)
    {
        for (var i = 0; i < count; i++)
        {
            Instruction.AvrInstruction(Runner.Cpu);
            Runner.Cpu.Tick();
        }
        return this;
    }

    /// <summary>
    /// Runs instructions until <paramref name="predicate"/> returns <c>true</c>,
    /// evaluated before each instruction.
    /// Throws <see cref="TimeoutException"/> if <paramref name="maxInstructions"/> is reached.
    /// Returns <c>this</c> for chaining.
    /// </summary>
    public AvrTestSimulation RunUntil(
        Func<AvrTestSimulation, bool> predicate,
        int maxInstructions = 100_000)
    {
        for (var i = 0; i < maxInstructions; i++)
        {
            if (predicate(this))
                return this;
            Instruction.AvrInstruction(Runner.Cpu);
            Runner.Cpu.Tick();
        }
        throw new TimeoutException(
            $"RunUntil: predicate was not satisfied within {maxInstructions} instructions.");
    }

    /// <summary>
    /// Runs until a <c>BREAK</c> (0x9598) instruction is reached without executing it.
    /// Throws <see cref="TimeoutException"/> if <paramref name="maxInstructions"/> is reached.
    /// Returns <c>this</c> for chaining.
    /// </summary>
    public AvrTestSimulation RunToBreak(int maxInstructions = 100_000)
    {
        for (var i = 0; i < maxInstructions; i++)
        {
            if (Runner.Cpu.ProgramMemory[Runner.Cpu.PC] == BreakOpcode)
                return this;
            Instruction.AvrInstruction(Runner.Cpu);
            Runner.Cpu.Tick();
        }
        throw new TimeoutException(
            $"RunToBreak: BREAK instruction not reached within {maxInstructions} instructions.");
    }

    /// <summary>
    /// Runs until the program counter reaches <paramref name="byteAddress"/> (byte address = PC × 2).
    /// Returns <c>this</c> for chaining.
    /// </summary>
    public AvrTestSimulation RunToAddress(int byteAddress, int maxInstructions = 100_000)
        => RunUntil(s => (int)(s.Cpu.PC * 2) == byteAddress, maxInstructions);
}
