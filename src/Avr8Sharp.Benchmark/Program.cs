using System.Diagnostics;
using AVR8Sharp.Core;
using AVR8Sharp.Core.Decoders;
using AVR8Sharp.Core.Peripherals;
using AVR8Sharp.Core.Utils;

namespace Avr8Sharp.Benchmark;

// Throughput benchmark for the AVR core, running the real Arduino "blink" firmware.
//
// Reports two numbers:
//   * MIPS              — millions of *instructions* retired per wall second
//                         (this is the figure comparable to RP2040Sharp's bench).
//   * realtime ratio    — simulated AVR *cycles* per wall second / clock speed;
//                         1.0x == real time at 16 MHz, higher == faster than the chip.
//
// The loop is the same one the runner uses (decoder.Decode + cpu.Tick), driven through
// a generic method so the struct decoder is monomorphized (no virtual dispatch), exactly
// like AvrRunner.ExecuteInternal.
//
//   dotnet run -c Release --project src/Avr8Sharp.Benchmark [native|lut|switch] [instructions]
public static class Program
{
    private const uint Speed = 16_000_000;

    public static void Main(string[] args)
    {
        var decoderName = args.Length > 0 ? args[0].ToLowerInvariant() : "native";
        var target = args.Length > 1 ? long.Parse(args[1]) : 5_000_000_000L;
        var warmup = target / 20;

        var hexPath = Path.Combine(AppContext.BaseDirectory, "blink.hex");
        var hex = File.ReadAllText(hexPath);
        var cpu = BuildCpu(hex);

        switch (decoderName)
        {
            case "lut":    Measure(cpu, new LutDecoder(),       decoderName, target, warmup); break;
            case "switch": Measure(cpu, new SwitchDecoder(),    decoderName, target, warmup); break;
            default:       Measure(cpu, new NativeLutDecoder(), decoderName, target, warmup); break;
        }
    }

    private static void Measure<TDecoder>(Cpu cpu, TDecoder decoder, string name, long target, long warmup)
        where TDecoder : struct, IInstructionDecoder
    {
        // Warm up the JIT (tiered compilation + PGO) before timing.
        Run(cpu, ref decoder, warmup);

        var startCycles = cpu.Cycles;
        var sw = Stopwatch.StartNew();
        Run(cpu, ref decoder, target);
        sw.Stop();

        var simCycles = cpu.Cycles - startCycles;
        var seconds = sw.Elapsed.TotalSeconds;
        var mips = target / seconds / 1e6;
        var cyclesPerSec = simCycles / seconds;
        var realtimeRatio = cyclesPerSec / Speed;
        var cpi = (double)simCycles / target;

        Console.WriteLine($"decoder         : {name}");
        Console.WriteLine($"instructions    : {target:N0}");
        Console.WriteLine($"wall time       : {seconds:N3} s");
        Console.WriteLine($"throughput      : {mips:N1} MIPS");
        Console.WriteLine($"avg CPI         : {cpi:N3}  (simulated cycles per instruction)");
        Console.WriteLine($"realtime ratio  : {realtimeRatio:N2}x at {Speed / 1e6:N0} MHz");
    }

    private static void Run<TDecoder>(Cpu cpu, ref TDecoder decoder, long instructions)
        where TDecoder : struct, IInstructionDecoder
    {
        for (long i = 0; i < instructions; i++)
        {
            decoder.Decode(cpu);
            cpu.Tick();
        }
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
