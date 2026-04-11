using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.TestKit.Assertions;
using Avr8Sharp.TestKit.Probes;
using AvrCpu = AVR8Sharp.Core.Cpu;

namespace Avr8Sharp.TestKit;

/// <summary>
/// Entry-point extension methods that add <c>.Should()</c> to all AVR TestKit types,
/// returning the appropriate FluentAssertions assertion class.
/// </summary>
public static class AssertionExtensions
{
    /// <summary>Returns CPU assertions for <paramref name="cpu"/>.</summary>
    public static AvrCpuAssertions Should(this AvrCpu cpu)
        => new(cpu);

    /// <summary>Returns serial-output assertions for <paramref name="probe"/>.</summary>
    public static SerialProbeAssertions Should(this SerialProbe probe)
        => new(probe);

    /// <summary>Returns GPIO pin assertions for <paramref name="port"/>.</summary>
    public static AvrGpioAssertions Should(this AvrIoPort port)
        => new(port);

    /// <summary>Returns data-memory assertions for <paramref name="memory"/>.</summary>
    public static AvrMemoryAssertions Should(this AvrMemoryView memory)
        => new(memory);
}
