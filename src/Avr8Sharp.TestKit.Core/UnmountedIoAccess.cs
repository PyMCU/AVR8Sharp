namespace Avr8Sharp.TestKit;

/// <summary>What <see cref="AvrTestSimulation"/> does when firmware touches the registers of a GPIO port that was never mounted.</summary>
public enum UnmountedAccessMode
{
    /// <summary>Default. The access reads as plain memory (0 until written), exactly like the core.</summary>
    Ignore = 0,

    /// <summary>The first access to each register is recorded in <see cref="AvrTestSimulation.Warnings"/>; execution continues.</summary>
    Warn = 1,

    /// <summary>The access throws <see cref="UnmountedIoAccessException"/>.</summary>
    Throw = 2,
}

/// <summary>Thrown in <see cref="UnmountedAccessMode.Throw"/> mode when firmware accesses a register of an unmounted peripheral.</summary>
public sealed class UnmountedIoAccessException(string message, ushort address, bool isWrite) : InvalidOperationException(message)
{
    /// <summary>Data-space address that was accessed.</summary>
    public ushort Address { get; } = address;

    /// <summary><c>true</c> for a write, <c>false</c> for a read.</summary>
    public bool IsWrite { get; } = isWrite;
}
