using AVR8Sharp.Core.Decoders;
using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

/// <summary>
/// Exact-count tests for <see cref="CountingDecoder"/>/<see cref="ExecutionCounts"/>:
/// a known loop whose trip count, branch taken/not-taken split, and per-PC cycle
/// totals are all predictable from the datasheet timing rules.
/// </summary>
[TestFixture]
public class CountingDecoderTests
{
    private static CountingDecoder RunToPc(byte[] program, uint stopPc)
    {
        var cpu = new AVR8Sharp.Core.Cpu(program);
        var decoder = new CountingDecoder(cpu);
        var guard = 0;
        while (cpu.Pc != stopPc)
        {
            decoder.Decode(cpu);
            cpu.Tick();
            if (++guard > 100_000) throw new Exception("test program did not converge");
        }
        return decoder;
    }

    [Test(Description = "ldi/dec/brne loop: body hits == trip count, closing branch taken N-1 and not-taken once")]
    public void Loop_ExactCounts()
    {
        var program = new AsmProgram(@"
                ldi  r16, 5      ; pc 0, 1 exec, 1 cycle
            loop:
                dec  r16         ; pc 1, 5 exec, 1 cycle each
                brne loop        ; pc 2, 5 exec; taken 4x (2 cy), falls through once (1 cy)
                break            ; pc 3, never executed
        ").Compile();

        var cpu = new AVR8Sharp.Core.Cpu(program.Program);
        var decoder = new CountingDecoder(cpu);
        while (cpu.Pc != 3)
        {
            decoder.Decode(cpu);
            cpu.Tick();
        }
        var c = decoder.Counts;

        Assert.Multiple(() =>
        {
            Assert.That(c.PcCount[0], Is.EqualTo(1u), "ldi");
            Assert.That(c.PcCount[1], Is.EqualTo(5u), "dec == trip count");
            Assert.That(c.PcCount[2], Is.EqualTo(5u), "brne == trip count");
            Assert.That(c.PcCount[3], Is.EqualTo(0u), "break never executed");

            Assert.That(c.BranchTaken[2], Is.EqualTo(4u), "brne taken N-1");
            Assert.That(c.BranchNotTaken[2], Is.EqualTo(1u), "brne not taken once");
            Assert.That(c.BranchTaken[0] + c.BranchNotTaken[0], Is.EqualTo(0u), "ldi is not a branch");
            Assert.That(c.BranchTaken[1] + c.BranchNotTaken[1], Is.EqualTo(0u), "dec is not a branch");

            Assert.That(c.PcCycles[0], Is.EqualTo(1UL));
            Assert.That(c.PcCycles[1], Is.EqualTo(5UL));
            Assert.That(c.PcCycles[2], Is.EqualTo(9UL), "4x2 taken + 1x1 not-taken");

            Assert.That(c.TotalInstructions, Is.EqualTo(11UL));
            Assert.That(c.TotalCycles, Is.EqualTo(cpu.Cycles));
        });
    }

    [Test(Description = "cpse skip coverage: equal operands skip the next instruction (taken), unequal fall through (not taken); a skipped 2-word instruction leaves both words at zero executions")]
    public void Cpse_SkipTaken_NotTaken_AndTwoWordSkip()
    {
        var program = new AsmProgram(@"
                ldi  r16, 0      ; pc 0
                ldi  r17, 0      ; pc 1
                cpse r16, r17    ; pc 2, equal: skips the jmp (taken, 3 cy over a 2-word)
                jmp  end         ; pc 3-4, never executed
                ldi  r18, 5      ; pc 5
                cpse r16, r18    ; pc 6, 0 != 5: falls through (not taken, 1 cy)
                nop              ; pc 7
            end:
                break            ; pc 8, never executed
        ").Compile();

        var c = RunToPc(program.Program, 8).Counts;

        Assert.Multiple(() =>
        {
            Assert.That(c.PcCount[2], Is.EqualTo(1u));
            Assert.That(c.BranchTaken[2], Is.EqualTo(1u), "cpse equal -> taken");
            Assert.That(c.BranchNotTaken[2], Is.EqualTo(0u));
            Assert.That(c.PcCycles[2], Is.EqualTo(3UL), "skip over a 2-word instruction costs 3 cycles");

            Assert.That(c.PcCount[3], Is.EqualTo(0u), "skipped jmp word 1");
            Assert.That(c.PcCount[4], Is.EqualTo(0u), "skipped jmp word 2");

            Assert.That(c.PcCount[6], Is.EqualTo(1u));
            Assert.That(c.BranchTaken[6], Is.EqualTo(0u));
            Assert.That(c.BranchNotTaken[6], Is.EqualTo(1u), "cpse unequal -> not taken");
            Assert.That(c.PcCycles[6], Is.EqualTo(1UL));

            Assert.That(c.TotalInstructions, Is.EqualTo(6UL));
        });
    }

    [Test(Description = "RJMP/RET-class instructions count as executed only: no branch entries")]
    public void Unconditional_ControlFlow_CountsAsExecutedOnly()
    {
        var program = new AsmProgram(@"
                rjmp fwd         ; pc 0, 1 exec, 2 cycles, no branch record
                nop              ; pc 1, never executed
            fwd:
                break            ; pc 2, never executed
        ").Compile();

        var c = RunToPc(program.Program, 2).Counts;

        Assert.Multiple(() =>
        {
            Assert.That(c.PcCount[0], Is.EqualTo(1u));
            Assert.That(c.PcCycles[0], Is.EqualTo(2UL));
            Assert.That(c.BranchTaken[0] + c.BranchNotTaken[0], Is.EqualTo(0u));
            Assert.That(c.PcCount[1], Is.EqualTo(0u));
        });
    }

    [Test(Description = "ToJson emits non-zero PCs only, with branch objects holding taken/notTaken")]
    public void ToJson_Dumps_NonZero_Counters()
    {
        var program = new AsmProgram(@"
                ldi  r16, 2
            loop:
                dec  r16
                brne loop
                break
        ").Compile();

        var json = RunToPc(program.Program, 3).Counts.ToJson();

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\"pc\":{\"0\":1,\"1\":2,\"2\":2}"));
            Assert.That(json, Does.Contain("\"2\":{\"taken\":1,\"notTaken\":1}"));
            Assert.That(json, Does.Contain("\"totalInstructions\":5"));
        });
    }
}
