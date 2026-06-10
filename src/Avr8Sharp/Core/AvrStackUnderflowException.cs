namespace AVR8Sharp.Core;

/// <summary>
/// Thrown when a RET / RETI pops a return address from above the top of SRAM --
/// i.e. the program executed a return with an empty (underflowed) call stack.
/// On real silicon this reads unmapped memory and jumps to a garbage address;
/// the emulator surfaces it explicitly instead of letting the out-of-bounds SRAM
/// read fall out as a bare IndexOutOfRangeException, so the cause (a function that
/// returned without a matching call, or execution falling into a stray RET) is
/// obvious from the diagnostic.
/// </summary>
public sealed class AvrStackUnderflowException : Exception
{
    /// <summary>Program counter (word address) of the offending RET/RETI.</summary>
    public uint Pc { get; }

    /// <summary>Stack pointer at the time of the return.</summary>
    public int StackPointer { get; }

    public AvrStackUnderflowException(uint pc, int stackPointer)
        : base($"Stack underflow on RET: SP=0x{stackPointer:X4} is at the top of SRAM, " +
               $"so there is no return address to pop (PC=0x{pc:X4} word). This usually means " +
               $"a function returned without a matching call, or execution reached a stray RET " +
               $"(e.g. fell through into a subroutine body).")
    {
        Pc = pc;
        StackPointer = stackPointer;
    }
}
