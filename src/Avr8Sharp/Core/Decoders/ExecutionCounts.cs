using System.Text;

namespace AVR8Sharp.Core.Decoders;

/// <summary>
/// Full-run execution counters produced by <see cref="CountingDecoder"/>: per-PC execution
/// counts, per-PC cycle totals, and taken/not-taken counts for the conditional-control-flow
/// instructions (the BRBS/BRBC family, SBRC/SBRS, SBIC/SBIS and CPSE).
/// <para>
/// All arrays are indexed by <b>word</b> address, the same convention as <see cref="Cpu.Pc"/>
/// (byte address = index × 2). <see cref="BranchTaken"/>/<see cref="BranchNotTaken"/> are zero
/// for PCs that never executed a conditional instruction.
/// </para>
/// </summary>
public sealed class ExecutionCounts
{
    /// <summary>Times each program word was executed (decoded and retired).</summary>
    public readonly uint[] PcCount;

    /// <summary>Total cycles charged to each program word, including multi-cycle costs.
    /// Interrupt entry (dispatched in <see cref="Cpu.Tick"/>) is not attributed to any PC,
    /// so <see cref="TotalCycles"/> can trail <see cref="Cpu.Cycles"/> by a few cycles per
    /// interrupt taken.</summary>
    public readonly ulong[] PcCycles;

    /// <summary>Per-PC count of conditional instructions whose branch/skip fired.</summary>
    public readonly uint[] BranchTaken;

    /// <summary>Per-PC count of conditional instructions that fell through.</summary>
    public readonly uint[] BranchNotTaken;

    public ExecutionCounts(int programWords)
    {
        PcCount = new uint[programWords];
        PcCycles = new ulong[programWords];
        BranchTaken = new uint[programWords];
        BranchNotTaken = new uint[programWords];
    }

    /// <summary>Number of program words the arrays cover (flash size in words).</summary>
    public int ProgramWords => PcCount.Length;

    /// <summary>Sum of all per-PC execution counts.</summary>
    public ulong TotalInstructions
    {
        get
        {
            ulong total = 0;
            foreach (var n in PcCount) total += n;
            return total;
        }
    }

    /// <summary>Sum of all per-PC cycle counts.</summary>
    public ulong TotalCycles
    {
        get
        {
            ulong total = 0;
            foreach (var n in PcCycles) total += n;
            return total;
        }
    }

    /// <summary>Zeroes every counter, keeping the allocated arrays.</summary>
    public void Reset()
    {
        Array.Clear(PcCount);
        Array.Clear(PcCycles);
        Array.Clear(BranchTaken);
        Array.Clear(BranchNotTaken);
    }

    /// <summary>
    /// Dumps the non-zero counters as JSON: flat <c>"pc"</c>/<c>"cycles"</c> objects keyed by
    /// word address, and a <c>"branches"</c> object of <c>{"taken":…,"notTaken":…}</c> pairs.
    /// </summary>
    public string ToJson()
    {
        var sb = new StringBuilder(4096);
        sb.Append("{\"programWords\":").Append(ProgramWords)
          .Append(",\"totalInstructions\":").Append(TotalInstructions)
          .Append(",\"totalCycles\":").Append(TotalCycles)
          .Append(",\"pc\":{");
        var first = true;
        for (var i = 0; i < PcCount.Length; i++)
        {
            if (PcCount[i] == 0) continue;
            if (!first) sb.Append(',');
            sb.Append('\"').Append(i).Append("\":").Append(PcCount[i]);
            first = false;
        }
        sb.Append("},\"cycles\":{");
        first = true;
        for (var i = 0; i < PcCycles.Length; i++)
        {
            if (PcCycles[i] == 0) continue;
            if (!first) sb.Append(',');
            sb.Append('\"').Append(i).Append("\":").Append(PcCycles[i]);
            first = false;
        }
        sb.Append("},\"branches\":{");
        first = true;
        for (var i = 0; i < BranchTaken.Length; i++)
        {
            if (BranchTaken[i] == 0 && BranchNotTaken[i] == 0) continue;
            if (!first) sb.Append(',');
            sb.Append('\"').Append(i).Append("\":{\"taken\":").Append(BranchTaken[i])
              .Append(",\"notTaken\":").Append(BranchNotTaken[i]).Append('}');
            first = false;
        }
        sb.Append("}}");
        return sb.ToString();
    }
}
