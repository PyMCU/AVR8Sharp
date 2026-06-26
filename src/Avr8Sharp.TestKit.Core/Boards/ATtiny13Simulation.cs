using AVR8Sharp.Core.Peripherals;

namespace Avr8Sharp.TestKit.Boards;

/// <summary>
/// Pre-configured simulation for the <b>ATtiny13/13A</b> (avr25 core).
/// <para>
/// One GPIO port: Port B (PB0–PB5, PB5 is RESET by default). 1 KB flash, 64 B SRAM, 9.6 MHz
/// internal RC by default (modelled at the configured frequency).
/// </para>
/// <para>GPIO-focused preset: Timer0/ADC are not wired (use the generic helpers if needed).</para>
/// </summary>
public sealed class ATtiny13Simulation : AvrTestSimulation
{
    private const int Flash = 0x0400;
    private const int Sram = 64;
    private const uint Frequency = 9_600_000;

    // Port B — PIN 0x36, DDR 0x37, PORT 0x38
    private static readonly AvrPortConfig PortBConfig = new(pin: 0x36, ddr: 0x37, port: 0x38);

    /// <summary>Port B — the only GPIO port (PB0–PB5).</summary>
    public AvrIoPort PortB { get; }

    public ATtiny13Simulation() : base(Flash, Sram)
    {
        WithFrequency(Frequency);
        Cpu.StackLowLimit = 0x60;

        AddGpio(PortBConfig, out var pb); PortB = pb;
    }
}
