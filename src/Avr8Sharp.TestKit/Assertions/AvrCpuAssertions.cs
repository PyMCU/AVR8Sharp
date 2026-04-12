using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using AvrCpu = AVR8Sharp.Core.Cpu;

namespace Avr8Sharp.TestKit.Assertions;

/// <summary>
/// FluentAssertions assertion class for <see cref="AvrCpu"/> state inspection.
/// Obtain via <c>sim.Cpu.Should()</c>.
/// </summary>
public class AvrCpuAssertions : ReferenceTypeAssertions<AvrCpu, AvrCpuAssertions>
{
    public AvrCpuAssertions(AvrCpu cpu) : base(cpu) { }

    protected override string Identifier => "CPU";

    // ── Registers ────────────────────────────────────────────────────────────

    /// <summary>Asserts that general-purpose register R<paramref name="index"/> equals <paramref name="expected"/>.</summary>
    public AndConstraint<AvrCpuAssertions> HaveRegister(
        int index, byte expected, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Mmio.Data[index] == expected)
            .FailWith(
                "Expected register R{0} to be 0x{1:X2} ({1}){reason}, but found 0x{2:X2} ({2}).",
                index, expected, Subject.Mmio.Data[index]);

        return new AndConstraint<AvrCpuAssertions>(this);
    }

    // ── Program counter & stack pointer ──────────────────────────────────────

    /// <summary>Asserts that the program counter equals <paramref name="expected"/> (word address).</summary>
    public AndConstraint<AvrCpuAssertions> HavePC(
        uint expected, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Pc == expected)
            .FailWith(
                "Expected PC to be 0x{0:X4}{reason}, but found 0x{1:X4}.",
                expected, Subject.Pc);

        return new AndConstraint<AvrCpuAssertions>(this);
    }

    /// <summary>Asserts that the stack pointer equals <paramref name="expected"/>.</summary>
    public AndConstraint<AvrCpuAssertions> HaveSP(
        ushort expected, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Sp == expected)
            .FailWith(
                "Expected SP to be 0x{0:X4}{reason}, but found 0x{1:X4}.",
                expected, Subject.Sp);

        return new AndConstraint<AvrCpuAssertions>(this);
    }

    // ── Cycle counter ─────────────────────────────────────────────────────────

    /// <summary>Asserts that the cycle counter equals <paramref name="expected"/>.</summary>
    public AndConstraint<AvrCpuAssertions> HaveCycles(
        int expected, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Cycles == expected)
            .FailWith(
                "Expected cycle count to be {0}{reason}, but found {1}.",
                expected, Subject.Cycles);

        return new AndConstraint<AvrCpuAssertions>(this);
    }

    // ── SREG raw value ────────────────────────────────────────────────────────

    /// <summary>Asserts that the status register (SREG) equals <paramref name="expected"/>.</summary>
    public AndConstraint<AvrCpuAssertions> HaveSreg(
        byte expected, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Sreg == expected)
            .FailWith(
                "Expected SREG to be 0b{0} (0x{0:X2}){reason}, but found 0b{1} (0x{1:X2}).",
                expected, Subject.Sreg);

        return new AndConstraint<AvrCpuAssertions>(this);
    }

    // ── SREG individual flags ─────────────────────────────────────────────────

    private bool SregBit(int bit) => (Subject.Sreg & (1 << bit)) != 0;

    /// <summary>
    /// Asserts the SREG carry flag (C, bit 0).
    /// Pass <c>false</c> to assert it is clear.
    /// </summary>
    public AndConstraint<AvrCpuAssertions> HaveCarryFlag(
        bool expected = true, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(SregBit(0) == expected)
            .FailWith(expected
                ? "Expected SREG carry flag (C) to be set{reason}, but it was clear."
                : "Expected SREG carry flag (C) to be clear{reason}, but it was set.");

        return new AndConstraint<AvrCpuAssertions>(this);
    }

    /// <summary>
    /// Asserts the SREG zero flag (Z, bit 1).
    /// Pass <c>false</c> to assert it is clear.
    /// </summary>
    public AndConstraint<AvrCpuAssertions> HaveZeroFlag(
        bool expected = true, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(SregBit(1) == expected)
            .FailWith(expected
                ? "Expected SREG zero flag (Z) to be set{reason}, but it was clear."
                : "Expected SREG zero flag (Z) to be clear{reason}, but it was set.");

        return new AndConstraint<AvrCpuAssertions>(this);
    }

    /// <summary>
    /// Asserts the SREG negative flag (N, bit 2).
    /// Pass <c>false</c> to assert it is clear.
    /// </summary>
    public AndConstraint<AvrCpuAssertions> HaveNegativeFlag(
        bool expected = true, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(SregBit(2) == expected)
            .FailWith(expected
                ? "Expected SREG negative flag (N) to be set{reason}, but it was clear."
                : "Expected SREG negative flag (N) to be clear{reason}, but it was set.");

        return new AndConstraint<AvrCpuAssertions>(this);
    }

    /// <summary>
    /// Asserts the SREG overflow flag (V, bit 3).
    /// Pass <c>false</c> to assert it is clear.
    /// </summary>
    public AndConstraint<AvrCpuAssertions> HaveOverflowFlag(
        bool expected = true, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(SregBit(3) == expected)
            .FailWith(expected
                ? "Expected SREG overflow flag (V) to be set{reason}, but it was clear."
                : "Expected SREG overflow flag (V) to be clear{reason}, but it was set.");

        return new AndConstraint<AvrCpuAssertions>(this);
    }

    /// <summary>
    /// Asserts the SREG sign flag (S, bit 4). S = N ⊕ V.
    /// Pass <c>false</c> to assert it is clear.
    /// </summary>
    public AndConstraint<AvrCpuAssertions> HaveSignFlag(
        bool expected = true, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(SregBit(4) == expected)
            .FailWith(expected
                ? "Expected SREG sign flag (S) to be set{reason}, but it was clear."
                : "Expected SREG sign flag (S) to be clear{reason}, but it was set.");

        return new AndConstraint<AvrCpuAssertions>(this);
    }

    /// <summary>
    /// Asserts the SREG half-carry flag (H, bit 5).
    /// Pass <c>false</c> to assert it is clear.
    /// </summary>
    public AndConstraint<AvrCpuAssertions> HaveHalfCarryFlag(
        bool expected = true, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(SregBit(5) == expected)
            .FailWith(expected
                ? "Expected SREG half-carry flag (H) to be set{reason}, but it was clear."
                : "Expected SREG half-carry flag (H) to be clear{reason}, but it was set.");

        return new AndConstraint<AvrCpuAssertions>(this);
    }

    /// <summary>
    /// Asserts the SREG bit-copy flag (T, bit 6).
    /// Pass <c>false</c> to assert it is clear.
    /// </summary>
    public AndConstraint<AvrCpuAssertions> HaveTFlag(
        bool expected = true, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(SregBit(6) == expected)
            .FailWith(expected
                ? "Expected SREG T flag (bit 6) to be set{reason}, but it was clear."
                : "Expected SREG T flag (bit 6) to be clear{reason}, but it was set.");

        return new AndConstraint<AvrCpuAssertions>(this);
    }

    /// <summary>
    /// Asserts the SREG global interrupt enable flag (I, bit 7).
    /// Pass <c>false</c> to assert interrupts are disabled.
    /// </summary>
    public AndConstraint<AvrCpuAssertions> HaveInterruptsEnabled(
        bool expected = true, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.InterruptsEnabled == expected)
            .FailWith(expected
                ? "Expected global interrupts to be enabled{reason}, but they were disabled."
                : "Expected global interrupts to be disabled{reason}, but they were enabled.");

        return new AndConstraint<AvrCpuAssertions>(this);
    }
}
