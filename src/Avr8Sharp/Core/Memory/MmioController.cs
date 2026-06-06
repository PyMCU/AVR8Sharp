#nullable enable
using System.Runtime.CompilerServices;
using Avr8Sharp.Core.Memory;

namespace AVR8Sharp.Core.Memory;

public class MmioController
{
    public byte[] Data { get; }

    public DataView DataView { get; }

    private readonly Func<ushort, byte>?[] _readHooks = new Func<ushort, byte>?[0x10000];

    // Each entry holds up to 4 write handlers inline — no closure allocation when chaining.
    private readonly WriteHookChain[] _writeHookChains = new WriteHookChain[0x10000];

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
        ref var chain = ref _writeHookChains[address];
        switch (chain.Count)
        {
            case 0: chain.H0 = hook; break;
            case 1: chain.H1 = hook; break;
            case 2: chain.H2 = hook; break;
            case 3: chain.H3 = hook; break;
            default: throw new InvalidOperationException($"More than 4 write hooks registered for MMIO address 0x{address:X4}.");
        }
        chain.Count++;
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
        ref var chain = ref _writeHookChains[address];
        var oldValue = Data[address];

        if (chain.Count > 0)
        {
            // Run all registered handlers; the write is vetoed if any returns true.
            var vetoed = chain.H0!(value, oldValue, address, mask);
            if (chain.Count > 1)
            {
                vetoed |= chain.H1!(value, oldValue, address, mask);
                if (chain.Count > 2)
                {
                    vetoed |= chain.H2!(value, oldValue, address, mask);
                    if (chain.Count > 3)
                        vetoed |= chain.H3!(value, oldValue, address, mask);
                }
            }
            if (vetoed) return;
        }

        Data[address] = (byte)((oldValue & ~mask) | (value & mask));
    }
}

/// <summary>
/// Holds up to 4 write hooks for a single MMIO address without heap-allocating closures.
/// </summary>
internal struct WriteHookChain
{
    public Func<byte, byte, ushort, byte, bool>? H0, H1, H2, H3;
    public int Count;
}
