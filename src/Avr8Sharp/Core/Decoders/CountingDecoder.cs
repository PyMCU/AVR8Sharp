using System.Runtime.CompilerServices;

namespace AVR8Sharp.Core.Decoders;

/// <summary>
/// <see cref="NativeLutDecoder"/> wrapped with per-instruction counters: executions and cycles
/// per PC, plus taken/not-taken for conditional control flow — the whole BRBS/BRBC family,
/// SBRC/SBRS, SBIC/SBIS and CPSE. RJMP/JMP/IJMP/CALL/RET and friends are counted as executed
/// only. "Taken" is decided by comparing the PC after the instruction with the fall-through
/// address, so a branch to its own fall-through (offset 0) reads as not-taken.
/// <para>
/// A struct like the other decoders, so it monomorphizes into <c>AvrRunner.ExecuteInternal</c>
/// with no virtual dispatch; the counting itself is a handful of array writes. Unlike
/// <see cref="ProfilingDecoder"/> there is no delegate call per instruction.
/// </para>
/// </summary>
public struct CountingDecoder : IInstructionDecoder
{
    // opcode -> 1 for the counted conditional instructions, 0 otherwise.
    private static readonly byte[] s_branchMap = BuildBranchMap();

    private NativeLutDecoder _inner;
    private readonly ExecutionCounts _counts;

    /// <summary>Creates a decoder writing into <paramref name="counts"/>, which must be sized
    /// to at least <c>cpu.ProgramMemory.Length</c> words.</summary>
    public CountingDecoder(ExecutionCounts counts)
    {
        _inner = new NativeLutDecoder();
        _counts = counts ?? throw new ArgumentNullException(nameof(counts));
    }

    /// <summary>Creates a decoder with a fresh <see cref="ExecutionCounts"/> sized to the CPU's flash.</summary>
    public CountingDecoder(Cpu cpu) : this(new ExecutionCounts(cpu.ProgramMemory.Length)) { }

    /// <summary>The counters this decoder writes into.</summary>
    public ExecutionCounts Counts => _counts;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Decode(Cpu cpu)
    {
        var pc = (int)cpu.Pc;
        var opcode = cpu.ProgramMemory[pc];
        var cyclesBefore = cpu.Cycles;

        _inner.Decode(cpu);

        var counts = _counts;
        counts.PcCount[pc]++;
        counts.PcCycles[pc] += cpu.Cycles - cyclesBefore;

        if (s_branchMap[opcode] != 0)
        {
            // Every counted conditional is a single word, so fall-through is pc + 1
            // (wrapped like the decoder does at the end of flash).
            var fallThrough = (uint)pc + 1;
            var len = (uint)cpu.ProgramMemory.Length;
            if (fallThrough >= len) fallThrough -= len;
            if (cpu.Pc == fallThrough)
                counts.BranchNotTaken[pc]++;
            else
                counts.BranchTaken[pc]++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsConditional(ushort opcode)
        => (opcode & 0xF800) == 0xF000    // BRBS / BRBC (all BREQ..BRGE aliases)
           || (opcode & 0xFC00) == 0x1000 // CPSE
           || (opcode & 0xFC08) == 0xFC00 // SBRC / SBRS
           || (opcode & 0xFD00) == 0x9900; // SBIC / SBIS

    private static byte[] BuildBranchMap()
    {
        var map = new byte[0x10000];
        for (var i = 0; i < map.Length; i++)
            map[i] = IsConditional((ushort)i) ? (byte)1 : (byte)0;
        return map;
    }
}
