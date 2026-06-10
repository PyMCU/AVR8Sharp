namespace AVR8Sharp.Core;

/// <summary>
/// Thrown when a PUSH / CALL / RCALL / ICALL (or interrupt dispatch) writes below
/// <see cref="Cpu.StackLowLimit"/> -- i.e. the stack grew down past the start of
/// SRAM into the I/O and register space. On real silicon this silently corrupts
/// registers and peripheral state and the program fails in baffling ways; the
/// emulator surfaces it explicitly so the cause (unbounded recursion or an
/// oversized call chain / local frame) is obvious. Disabled when StackLowLimit is
/// 0 (the default for a bare core).
/// </summary>
public sealed class AvrStackOverflowException : Exception
{
    /// <summary>Program counter (word address) of the offending push.</summary>
    public uint Pc { get; }

    /// <summary>The data address the push tried to write (below the stack limit).</summary>
    public int Address { get; }

    /// <summary>The configured lowest legal stack address (RAMSTART).</summary>
    public int Limit { get; }

    public AvrStackOverflowException(uint pc, int address, int limit)
        : base($"Stack overflow: a push to 0x{address:X4} is below the stack limit " +
               $"0x{limit:X4} (RAMSTART), so the stack has grown into the I/O/register " +
               $"space (PC=0x{pc:X4} word). This usually means unbounded recursion or a " +
               $"call chain / local frame too large for the available SRAM.")
    {
        Pc = pc;
        Address = address;
        Limit = limit;
    }
}
