#nullable enable
namespace Avr8Sharp.Core.Memory;

public class MmioController
{
    public byte[] Data { get; }
    
    public DataView DataView { get; }

    private readonly Func<ushort, byte>?[] _readHooks = new Func<ushort, byte>?[0x10000];
    private readonly Func<byte, byte, ushort, byte, bool>?[] _writeHooks = new Func<byte, byte, ushort, byte, bool>?[0x10000];

    public MmioController(int memorySize)
    {
        Data = new byte[memorySize];
        DataView = new DataView(Data);
    }

    public void RegisterRead(ushort address, Func<ushort, byte> hook)
    {
        _readHooks[address] = hook;
    }

    public void RegisterWrite(ushort address, Func<byte, byte, ushort, byte, bool> hook)
    {
        _writeHooks[address] = hook;
    }

    public byte ReadData(ushort address)
    {
        var hook = _readHooks[address];
        
        return hook == null ? Data[address] : hook(address);
    }

    public void WriteData(ushort address, byte value, byte mask = 0xff)
    {
        var hook = _writeHooks[address];
        
        if (hook != null && hook(value, Data[address], address, mask))
        {
            return;
        }

        Data[address] = value;
    }
}