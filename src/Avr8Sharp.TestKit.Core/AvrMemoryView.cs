namespace Avr8Sharp.TestKit;

/// <summary>
/// A read-only view over the AVR data memory (registers + SRAM), used as the
/// subject for <see cref="Assertions.AvrMemoryAssertions"/>.
/// </summary>
public class AvrMemoryView
{
    internal readonly byte[] Data;

    internal AvrMemoryView(byte[] data) => Data = data;

    /// <summary>Returns the byte stored at <paramref name="address"/>.</summary>
    public byte this[int address] => Data[address];

    /// <summary>Reads a 16-bit little-endian word from <paramref name="address"/>.</summary>
    public ushort ReadUInt16LE(int address)
        => (ushort)(Data[address] | (Data[address + 1] << 8));

    /// <summary>Reads a 16-bit big-endian word from <paramref name="address"/>.</summary>
    public ushort ReadUInt16BE(int address)
        => (ushort)((Data[address] << 8) | Data[address + 1]);
}
