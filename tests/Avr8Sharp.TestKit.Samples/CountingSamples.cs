using Avr8Sharp.TestKit;

namespace Avr8Sharp.TestKit.Samples;

/// <summary>
/// Sample tests for the counted-run API: <see cref="AvrTestSimulation.RunCyclesCounted"/>,
/// <see cref="AvrTestSimulation.RunUntilCounted"/> and <see cref="AvrTestSimulation.EnableCounting"/>.
/// They drive a loop with a known trip count and assert the exact per-PC counters.
/// </summary>
[TestFixture]
public class CountingSamples
{
    // pc0 ldi, pc1..2 loop body (dec + brne), pc3 break. Trip count 5.
    private const string Loop5 = @"
        ldi  r16, 5
    loop:
        dec  r16
        brne loop
        break
    ";

    [Test]
    public void RunCyclesCounted_ReturnsExactCounters()
    {
        var sim = AvrTestSimulation.Create().WithAsm(Loop5);

        // 1 (ldi) + 5 (dec) + 4*2+1 (brne taken/not-taken) = 15 cycles lands on the break.
        var counts = sim.RunCyclesCounted(15);

        Assert.Multiple(() =>
        {
            Assert.That(sim.Cpu.Pc, Is.EqualTo(3u));
            Assert.That(counts.PcCount[1], Is.EqualTo(5u), "loop body hits == trip count");
            Assert.That(counts.BranchTaken[2], Is.EqualTo(4u));
            Assert.That(counts.BranchNotTaken[2], Is.EqualTo(1u));
            Assert.That(counts.PcCycles[2], Is.EqualTo(9UL));
            Assert.That(counts.TotalInstructions, Is.EqualTo(11UL));
        });
    }

    [Test]
    public void RunUntilCounted_CountsUntilPredicate()
    {
        var sim = AvrTestSimulation.Create().WithAsm(Loop5);

        var counts = sim.RunUntilCounted(s => s.Cpu.Pc == 3);

        Assert.That(counts.PcCount[0], Is.EqualTo(1u));
        Assert.That(counts.BranchTaken[2], Is.EqualTo(4u));
    }

    [Test]
    public void EnableCounting_AccumulatesAcrossRuns_AndDisableRestoresPlainPath()
    {
        var sim = AvrTestSimulation.Create().WithAsm(Loop5);

        var counts = sim.EnableCounting();
        sim.RunInstructions(6);          // ldi + dec/brne x2.5, lands mid-loop
        sim.RunInstructions(5);          // rest of the trip count

        Assert.Multiple(() =>
        {
            Assert.That(counts.PcCount[1], Is.EqualTo(5u));
            Assert.That(counts.BranchTaken[2] + counts.BranchNotTaken[2], Is.EqualTo(5u));
            Assert.That(sim.Counts, Is.SameAs(counts));
        });

        sim.DisableCounting();
        sim.RunInstructions(1);
        Assert.Multiple(() =>
        {
            Assert.That(sim.Counts, Is.SameAs(counts), "counters stay readable after disable");
            Assert.That(counts.PcCount[2], Is.EqualTo(5u), "disabled run did not count");
        });
    }
}
