#nullable enable
using System.Runtime.CompilerServices;
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
        var prev = _writeHooks[address];
        if (prev == null)
        {
            _writeHooks[address] = hook;
        }
        else
        {
            // Chain: both handlers run regardless of order.
            // The write is vetoed (default write skipped) if either handler returns true.
            // This supports multiple peripherals sharing the same register (e.g. ATtiny85 TIFR/TIMSK).
            var captured = prev;
            _writeHooks[address] = (value, oldValue, addr, mask) =>
            {
                var r1 = captured(value, oldValue, addr, mask);
                var r2 = hook(value, oldValue, addr, mask);
                return r1 || r2;
            };
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadData(ushort address)
    {
        var hook = _readHooks[address];

        return hook == null ? Data[address] : hook(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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