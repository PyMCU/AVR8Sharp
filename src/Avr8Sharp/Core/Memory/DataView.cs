using System.Buffers.Binary;

namespace Avr8Sharp.Core.Memory;

public class DataView
{
    private readonly byte[] _data;

    // Nota: Quité el 'ref'. En C#, los arreglos (byte[]) ya son tipos por referencia.
    // Al pasar el arreglo normal, DataView y la clase principal apuntan a los mismos datos en memoria.
    public DataView(byte[] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public sbyte GetInt8(int byteOffset) 
        => (sbyte)_data[byteOffset];

    public void SetInt8(int byteOffset, sbyte value) 
        => _data[byteOffset] = (byte)value;

    public short GetInt16(int byteOffset, bool littleEndian = false)
    {
        // Creamos un Span al vuelo apuntando desde el offset indicado
        ReadOnlySpan<byte> span = _data.AsSpan(byteOffset);
        return littleEndian 
            ? BinaryPrimitives.ReadInt16LittleEndian(span) 
            : BinaryPrimitives.ReadInt16BigEndian(span);
    }

    public void SetInt16(int byteOffset, short value, bool littleEndian = false)
    {
        Span<byte> span = _data.AsSpan(byteOffset);
        if (littleEndian)
            BinaryPrimitives.WriteInt16LittleEndian(span, value);
        else
            BinaryPrimitives.WriteInt16BigEndian(span, value);
    }

    public ushort GetUint16(int byteOffset, bool littleEndian = false)
    {
        ReadOnlySpan<byte> span = _data.AsSpan(byteOffset);
        return littleEndian 
            ? BinaryPrimitives.ReadUInt16LittleEndian(span) 
            : BinaryPrimitives.ReadUInt16BigEndian(span);
    }

    public void SetUint16(int byteOffset, ushort value, bool littleEndian = false)
    {
        Span<byte> span = _data.AsSpan(byteOffset);
        if (littleEndian)
            BinaryPrimitives.WriteUInt16LittleEndian(span, value);
        else
            BinaryPrimitives.WriteUInt16BigEndian(span, value);
    }
}