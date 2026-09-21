using System.Diagnostics;
using AVR8Sharp.Core;
using AVR8Sharp.Core.Decoders;
using AVR8Sharp.Core.Peripherals;
using AVR8Sharp.Core.Utils;

namespace Avr8Sharp.Benchmark;

// Throughput benchmark for the AVR core, running real firmware for a fixed number
// of simulated *cycles*.
//
// Reports two numbers:
//   * MIPS              — millions of *instructions* retired per wall second
//                         (this is the figure comparable to RP2040Sharp's bench).
//   * cycles/s          — simulated AVR cycles per wall second; at 16 MHz the
//                         realtime ratio is cycles/s / 16e6.
//
// The loop is the same one the runner uses (decoder.Decode + cpu.Tick), driven through
// a generic method so the struct decoder is monomorphized (no virtual dispatch), exactly
// like AvrRunner.ExecuteInternal. Class decoders (ProfilingDecoder) run through the
// interface, which is precisely the overhead being measured. Warmup runs through the
// *same* loop method as the timed section so tiered PGO has promoted it before the
// clock starts.
//
//   dotnet run -c Release --project src/Avr8Sharp.Benchmark [native|lut|switch|count|profile-empty|profile-count] [cycles] [hex]
public static class Program
{
    private const uint Speed = 16_000_000;

    public static void Main(string[] args)
    {
        var decoderName = args.Length > 0 ? args[0].ToLowerInvariant() : "native";
        var target = args.Length > 1 ? long.Parse(args[1]) : 16_000_000L;
        var warmup = Math.Max(target * 4, 8_000_000L);

        var hexPath = args.Length > 2
            ? args[2]
            : Path.Combine(AppContext.BaseDirectory, "blink.hex");
        var hex = File.ReadAllText(hexPath);
        var cpu = BuildCpu(hex);

        switch (decoderName)
        {
            case "lut":     Measure(cpu, new LutDecoder(),       decoderName, target, warmup); break;
            case "switch":  Measure(cpu, new SwitchDecoder(),    decoderName, target, warmup); break;
            case "count":
                Measure(cpu, new CountingDecoder(new ExecutionCounts(cpu.ProgramMemory.Length)),
                    decoderName, target, warmup);
                break;
            case "profile-empty":
                MeasureClass(cpu, new ProfilingDecoder((_, _) => { }), decoderName, target, warmup);
                break;
            case "profile-count":
            {
                var counts = new uint[cpu.ProgramMemory.Length];
                MeasureClass(cpu, new ProfilingDecoder((pc, _) => counts[(int)pc]++), decoderName, target, warmup);
                break;
            }
            default:        Measure(cpu, new NativeLutDecoder(), decoderName, target, warmup); break;
        }
    }

    private static void Measure<TDecoder>(Cpu cpu, TDecoder decoder, string name, long targetCycles, long warmupCycles)
        where TDecoder : struct, IInstructionDecoder
    {
        RunForCycles(cpu, ref decoder, warmupCycles);

        var sw = Stopwatch.StartNew();
        var instructions = RunForCycles(cpu, ref decoder, targetCycles);
        sw.Stop();

        Report(name, targetCycles, instructions, sw.Elapsed.TotalSeconds);
    }

    private static void MeasureClass(Cpu cpu, IInstructionDecoder decoder, string name, long targetCycles, long warmupCycles)
    {
        RunForCyclesClass(cpu, decoder, warmupCycles);

        var sw = Stopwatch.StartNew();
        var instructions = RunForCyclesClass(cpu, decoder, targetCycles);
        sw.Stop();

        Report(name, targetCycles, instructions, sw.Elapsed.TotalSeconds);
    }

    private static long RunForCycles<TDecoder>(Cpu cpu, ref TDecoder decoder, long cycles)
        where TDecoder : struct, IInstructionDecoder
    {
        var target = (long)cpu.Cycles + cycles;
        var instructions = 0L;
        while ((long)cpu.Cycles < target)
        {
            decoder.Decode(cpu);
            cpu.Tick();
            instructions++;
        }
        return instructions;
    }

    private static long RunForCyclesClass(Cpu cpu, IInstructionDecoder decoder, long cycles)
    {
        var target = (long)cpu.Cycles + cycles;
        var instructions = 0L;
        while ((long)cpu.Cycles < target)
        {
            decoder.Decode(cpu);
            cpu.Tick();
            instructions++;
        }
        return instructions;
    }

    private static void Report(string name, long targetCycles, long instructions, double seconds)
    {
        var mips = instructions / seconds / 1e6;
        var cyclesPerSec = targetCycles / seconds;
        var realtimeRatio = cyclesPerSec / Speed;
        var cpi = (double)targetCycles / instructions;

        Console.WriteLine($"decoder         : {name}");
        Console.WriteLine($"cycles          : {targetCycles:N0}");
        Console.WriteLine($"instructions    : {instructions:N0}");
        Console.WriteLine($"wall time       : {seconds:N3} s");
        Console.WriteLine($"throughput      : {mips:N1} MIPS");
        Console.WriteLine($"cycles/s        : {cyclesPerSec:N0}");
        Console.WriteLine($"avg CPI         : {cpi:N3}  (simulated cycles per instruction)");
        Console.WriteLine($"realtime ratio  : {realtimeRatio:N2}x at {Speed / 1e6:N0} MHz");
    }

    private static Cpu BuildCpu(string hex)
    {
        var runner = AvrBuilder.Create()
            .SetSpeed(Speed)
            .SetHex(hex)
            .AddGpioPort(AvrIoPort.PortBConfig, out _)
            .AddGpioPort(AvrIoPort.PortCConfig, out _)
            .AddGpioPort(AvrIoPort.PortDConfig, out _)
            .AddUsart(AvrUsart.Usart0Config, out _)
            .AddTimer(AvrTimer.Timer0Config, out _)
            .AddTimer(AvrTimer.Timer1Config, out _)
            .AddTimer(AvrTimer.Timer2Config, out _)
            .Build();
        return runner.Cpu;
    }
}
