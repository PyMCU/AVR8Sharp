using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace Avr8Sharp.TestKit.Assertions;

/// <summary>
/// FluentAssertions assertion class for <see cref="AvrMemoryView"/> (SRAM / register space).
/// Obtain via <c>sim.Memory.Should()</c>.
/// </summary>
public class AvrMemoryAssertions : ReferenceTypeAssertions<AvrMemoryView, AvrMemoryAssertions>
{
    public AvrMemoryAssertions(AvrMemoryView memory) : base(memory) { }

    protected override string Identifier => "memory";

    // ── Byte assertions ───────────────────────────────────────────────────────

    /// <summary>Asserts that the byte at <paramref name="address"/> equals <paramref name="expected"/>.</summary>
    public AndConstraint<AvrMemoryAssertions> HaveByteAt(
        int address, byte expected, string because = "", params object[] becauseArgs)
    {
        var actual = Subject[address];
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(actual == expected)
            .FailWith(
                "Expected memory[0x{0:X4}] to be 0x{1:X2} ({1}){reason}, but found 0x{2:X2} ({2}).",
                address, expected, actual);

        return new AndConstraint<AvrMemoryAssertions>(this);
    }

    // ── Word assertions ───────────────────────────────────────────────────────

    /// <summary>
    /// Asserts that the 16-bit little-endian word at <paramref name="address"/> equals <paramref name="expected"/>.
    /// </summary>
    public AndConstraint<AvrMemoryAssertions> HaveWordAt(
        int address, ushort expected, string because = "", params object[] becauseArgs)
    {
        var actual = Subject.ReadUInt16LE(address);
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(actual == expected)
            .FailWith(
                "Expected memory word (LE) at 0x{0:X4} to be 0x{1:X4} ({1}){reason}, but found 0x{2:X4} ({2}).",
                address, expected, actual);

        return new AndConstraint<AvrMemoryAssertions>(this);
    }

    /// <summary>
    /// Asserts that the 16-bit big-endian word at <paramref name="address"/> equals <paramref name="expected"/>.
    /// </summary>
    public AndConstraint<AvrMemoryAssertions> HaveWordBEAt(
        int address, ushort expected, string because = "", params object[] becauseArgs)
    {
        var actual = Subject.ReadUInt16BE(address);
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(actual == expected)
            .FailWith(
                "Expected memory word (BE) at 0x{0:X4} to be 0x{1:X4} ({1}){reason}, but found 0x{2:X4} ({2}).",
                address, expected, actual);

        return new AndConstraint<AvrMemoryAssertions>(this);
    }

    // ── Block assertions ──────────────────────────────────────────────────────

    /// <summary>
    /// Asserts that the bytes at <paramref name="address"/> match <paramref name="expected"/> exactly.
    /// </summary>
    public AndConstraint<AvrMemoryAssertions> HaveBytesAt(
        int address, byte[] expected, string because = "", params object[] becauseArgs)
    {
        var memory = Subject.Data;

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(address >= 0 && address + expected.Length <= memory.Length)
            .FailWith(
                "Expected to read {0} bytes starting at address 0x{1:X4}{reason}, but the memory only has {2} bytes (Valid range: 0x0000 - 0x{3:X4}).",
                expected.Length,
                address,
                memory.Length,
                memory.Length - 1);

        var actual = memory.AsSpan(address, expected.Length);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(actual.SequenceEqual(expected))
            .FailWith(
                "Expected {0} bytes at 0x{1:X4} to equal [{2}]{reason}, but found [{3}].",
                expected.Length,
                address,
                string.Join(", ", expected.Select(b => $"0x{b:X2}")),
                string.Join(", ", actual.ToArray().Select(b => $"0x{b:X2}")));

        return new AndConstraint<AvrMemoryAssertions>(this);
    }
}
