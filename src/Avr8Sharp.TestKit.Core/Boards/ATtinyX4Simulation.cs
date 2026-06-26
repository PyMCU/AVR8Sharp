using AVR8Sharp.Core.Peripherals;

namespace Avr8Sharp.TestKit.Boards;

/// <summary>
/// Pre-configured simulation for the <b>ATtiny24/44/84</b> family (avr25 core).
/// <para>
/// Two GPIO ports are wired: Port A (PA0–PA7) and Port B (PB0–PB3). The variants differ only
/// in flash/SRAM size — use <see cref="ATtiny84"/>, <see cref="ATtiny44"/>, <see cref="ATtiny24"/>.
/// </para>
/// <para>
/// GPIO-focused preset: timers, ADC and USI are not wired (add them via the generic
/// <see cref="AvrTestSimulation"/> helpers if a test needs them). 8 MHz internal RC.
/// </para>
/// </summary>
public sealed class ATtinyX4Simulation : AvrTestSimulation
{
    private const uint Frequency = 8_000_000;

    // Port A — PIN 0x39, DDR 0x3A, PORT 0x3B
    private static readonly AvrPortConfig PortAConfig = new(pin: 0x39, ddr: 0x3A, port: 0x3B);
    // Port B — PIN 0x36, DDR 0x37, PORT 0x38 (PB3 is RESET by default)
    private static readonly AvrPortConfig PortBConfig = new(pin: 0x36, ddr: 0x37, port: 0x38);

    /// <summary>Port A — PA0–PA7.</summary>
    public AvrIoPort PortA { get; }
    /// <summary>Port B — PB0–PB3.</summary>
    public AvrIoPort PortB { get; }

    public ATtinyX4Simulation(int flash, int sram) : base(flash, sram)
    {
        WithFrequency(Frequency);
        Cpu.StackLowLimit = 0x60;   // SRAM starts at 0x60 on ATtiny parts

        AddGpio(PortAConfig, out var pa); PortA = pa;
        AddGpio(PortBConfig, out var pb); PortB = pb;
    }

    /// <summary>ATtiny84 — 8 KB flash, 512 B SRAM.</summary>
    public static ATtinyX4Simulation ATtiny84() => new(0x2000, 512);
    /// <summary>ATtiny44 — 4 KB flash, 256 B SRAM.</summary>
    public static ATtinyX4Simulation ATtiny44() => new(0x1000, 256);
    /// <summary>ATtiny24 — 2 KB flash, 128 B SRAM.</summary>
    public static ATtinyX4Simulation ATtiny24() => new(0x0800, 128);
}
