using AVR8Sharp.Core;
using AVR8Sharp.Core.Decoders;
using AVR8Sharp.Core.Peripherals;
using AVR8Sharp.Core.Utils;
using Avr8Sharp.TestKit.Probes;
using AvrCpu = AVR8Sharp.Core.Cpu;

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

    private LutDecoder _decoder;

    public byte[] Data => Runner.Cpu.Mmio.Data;
    public AvrMemoryView Memory => new(Runner.Cpu.Mmio.Data);

    protected AvrTestSimulation(int flashSize, int sramBytes)
    {
        Runner = new AvrRunner(new byte[flashSize], sramBytes);
        _decoder = new LutDecoder();
    }

    /// <summary>Creates a new blank simulation with the given flash and SRAM sizes.</summary>
    public static AvrTestSimulation Create(int flashSize = 0x8000, int sramBytes = 8192)
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

    /// <summary>
    /// Resets the CPU to its power-on state: PC=0, SP=RAMEND, SREG=0, pending interrupts cleared.
    /// Does not clear the loaded program.
    /// Returns <c>this</c> for chaining.
    /// </summary>
    public AvrTestSimulation Reset()
    {
        Runner.Cpu.Reset();
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

    /// <summary>Adds an EEPROM peripheral with a volatile in-memory backend.</summary>
    public AvrTestSimulation AddEeprom(AvrEepromConfig config, out AvrEeprom eeprom, uint eepromSize = 1024)
    {
        var backend = new EepromMemoryBackend(eepromSize);
        eeprom = new AvrEeprom(Runner.Cpu, backend, config);
        return this;
    }

    /// <summary>Adds an EEPROM peripheral (no out handle).</summary>
    public AvrTestSimulation AddEeprom(AvrEepromConfig config, uint eepromSize = 1024)
    {
        var backend = new EepromMemoryBackend(eepromSize);
        _ = new AvrEeprom(Runner.Cpu, backend, config);
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
    /// Executes one decoded instruction and one CPU tick.
    /// Converts a raw <see cref="IndexOutOfRangeException"/> (PC out of flash) into an
    /// <see cref="InvalidOperationException"/> with diagnostic context.
    /// </summary>
    private void Step()
    {
        try
        {
            _decoder.Decode(Runner.Cpu);
            Runner.Cpu.Tick();
        }
        catch (IndexOutOfRangeException)
        {
            var pc = Runner.Cpu.Pc;
            if (pc < Runner.Cpu.ProgramMemory.Length) throw;
            var flash = Runner.Cpu.ProgramMemory.Length * 2;
            throw new InvalidOperationException(
                $"Simulation crashed: PC=0x{pc:X4} (byte addr 0x{pc * 2:X5}) is out of flash " +
                $"(flash={flash} bytes, 0x{flash:X}). " +
                $"Cycles={Runner.Cpu.Cycles}, SREG=0x{Runner.Cpu.Sreg:X2}, SP=0x{Runner.Cpu.Sp:X4}.");
        }
    }

    /// <summary>
    /// Runs the simulation for exactly <paramref name="cycles"/> CPU cycles.
    /// Returns <c>this</c> for chaining.
    /// </summary>
    public AvrTestSimulation RunCycles(long cycles)
    {
        var target = (long)Runner.Cpu.Cycles + cycles;
        while ((long)Runner.Cpu.Cycles < target)
            Step();
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
            Step();
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
            Step();
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
            var pc = Runner.Cpu.Pc;
            if (Runner.Cpu.ProgramMemory[(int)pc] == BreakOpcode)
                return this;
            Step();
        }
        throw new TimeoutException(
            $"RunToBreak: BREAK instruction not reached within {maxInstructions} instructions.");
    }

    /// <summary>
    /// Runs until the program counter reaches <paramref name="byteAddress"/>.
    /// <para>
    /// <paramref name="byteAddress"/> is the byte offset into flash, as shown in
    /// <c>avr-objdump</c> / disassembly output (= <c>PC × 2</c>).
    /// Use <see cref="RunToWordAddress"/> if you prefer the word-index convention
    /// used by timer and interrupt vector configurations.
    /// </para>
    /// Returns <c>this</c> for chaining.
    /// </summary>
    public AvrTestSimulation RunToAddress(int byteAddress, int maxInstructions = 100_000)
        => RunUntil(s => (int)(s.Cpu.Pc * 2) == byteAddress, maxInstructions);

    /// <summary>
    /// Runs until the program counter equals <paramref name="wordAddress"/>.
    /// <para>
    /// <paramref name="wordAddress"/> is the word index into program memory — the same
    /// convention used by timer configs, interrupt vector addresses, and <c>cpu.Pc</c>.
    /// Use <see cref="RunToAddress"/> if you prefer the byte-offset convention from disassembly.
    /// </para>
    /// Returns <c>this</c> for chaining.
    /// </summary>
    public AvrTestSimulation RunToWordAddress(uint wordAddress, int maxInstructions = 100_000)
        => RunUntil(s => s.Cpu.Pc == wordAddress, maxInstructions);

    // ── Cycle-based helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Runs instructions until <paramref name="predicate"/> returns <c>true</c>,
    /// evaluated before each instruction. Unlike <see cref="RunUntil(Func{AvrTestSimulation,bool},int)"/>
    /// the timeout is expressed in <paramref name="maxMs"/> of simulated time, making
    /// it easier to reason about firmware timing.
    /// </summary>
    public AvrTestSimulation RunUntilMs(
        Func<AvrTestSimulation, bool> predicate,
        double maxMs = 1000)
    {
        var deadline = (long)Runner.Cpu.Cycles + (long)(maxMs / 1000.0 * Runner.Speed);
        while ((long)Runner.Cpu.Cycles < deadline)
        {
            if (predicate(this)) return this;
            Step();
        }
        throw new TimeoutException(
            $"RunUntilMs: predicate was not satisfied within {maxMs} ms of simulated time " +
            $"({Runner.Cpu.Cycles} cycles elapsed).");
    }

    /// <summary>
    /// Runs until the captured <paramref name="serial"/> text satisfies <paramref name="predicate"/>,
    /// or until <paramref name="maxMs"/> of simulated time elapses.
    /// </summary>
    public AvrTestSimulation RunUntilSerial(
        Probes.SerialProbe serial,
        Func<string, bool> predicate,
        double maxMs = 2000)
        => RunUntilMs(_ => predicate(serial.Text), maxMs);

    /// <summary>
    /// Runs until <paramref name="serial"/> contains the given <paramref name="text"/> as a substring,
    /// or until <paramref name="maxMs"/> of simulated time elapses.
    /// </summary>
    public AvrTestSimulation RunUntilSerial(
        Probes.SerialProbe serial,
        string text,
        double maxMs = 2000)
        => RunUntilMs(_ => serial.Text.Contains(text), maxMs);

    /// <summary>
    /// Runs until <paramref name="serial"/> has received at least <paramref name="byteCount"/> bytes,
    /// or until <paramref name="maxMs"/> of simulated time elapses.
    /// </summary>
    public AvrTestSimulation RunUntilSerialBytes(
        Probes.SerialProbe serial,
        int byteCount,
        double maxMs = 2000)
        => RunUntilMs(_ => serial.ByteCount >= byteCount, maxMs);

    // ── Profiling (slow path) ─────────────────────────────────────────────────

    /// <summary>
    /// Slow-path step using a <see cref="ProfilingDecoder"/> that fires a callback before each instruction.
    /// The hot-path <see cref="Step()"/> is completely unaffected.
    /// </summary>
    private void StepProfiling(ProfilingDecoder decoder)
    {
        try
        {
            decoder.Decode(Runner.Cpu);
            Runner.Cpu.Tick();
        }
        catch (IndexOutOfRangeException)
        {
            var pc = Runner.Cpu.Pc;
            if (pc < Runner.Cpu.ProgramMemory.Length) throw;
            var flash = Runner.Cpu.ProgramMemory.Length * 2;
            throw new InvalidOperationException(
                $"Simulation crashed: PC=0x{pc:X4} (byte addr 0x{pc * 2:X5}) is out of flash " +
                $"(flash={flash} bytes, 0x{flash:X}). " +
                $"Cycles={Runner.Cpu.Cycles}, SREG=0x{Runner.Cpu.Sreg:X2}, SP=0x{Runner.Cpu.Sp:X4}.");
        }
    }

    /// <summary>Runs exactly <paramref name="cycles"/> CPU cycles through the profiling slow path.</summary>
    public AvrTestSimulation RunCyclesProfiled(long cycles, ProfilingDecoder decoder)
    {
        var target = (long)Runner.Cpu.Cycles + cycles;
        while ((long)Runner.Cpu.Cycles < target)
            StepProfiling(decoder);
        return this;
    }

    /// <summary>Runs <paramref name="ms"/> simulated milliseconds through the profiling slow path.</summary>
    public AvrTestSimulation RunMillisecondsProfiled(double ms, ProfilingDecoder decoder)
        => RunCyclesProfiled((long)(ms / 1000.0 * Runner.Speed), decoder);

    /// <summary>Runs exactly <paramref name="count"/> instructions through the profiling slow path.</summary>
    public AvrTestSimulation RunInstructionsProfiled(int count, ProfilingDecoder decoder)
    {
        for (var i = 0; i < count; i++)
            StepProfiling(decoder);
        return this;
    }

    /// <summary>
    /// Runs until <paramref name="predicate"/> returns <c>true</c> through the profiling slow path.
    /// Throws <see cref="TimeoutException"/> if <paramref name="maxInstructions"/> is reached.
    /// </summary>
    public AvrTestSimulation RunUntilProfiled(
        Func<AvrTestSimulation, bool> predicate,
        ProfilingDecoder decoder,
        int maxInstructions = 100_000)
    {
        for (var i = 0; i < maxInstructions; i++)
        {
            if (predicate(this)) return this;
            StepProfiling(decoder);
        }
        throw new TimeoutException(
            $"RunUntilProfiled: predicate was not satisfied within {maxInstructions} instructions.");
    }
}
