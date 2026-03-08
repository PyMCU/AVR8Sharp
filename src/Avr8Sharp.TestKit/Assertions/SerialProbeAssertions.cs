using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Avr8Sharp.TestKit.Probes;

namespace Avr8Sharp.TestKit.Assertions;

/// <summary>
/// FluentAssertions assertion class for <see cref="SerialProbe"/> (USART TX output).
/// Obtain via <c>serial.Should()</c>.
/// </summary>
public class SerialProbeAssertions : ReferenceTypeAssertions<SerialProbe, SerialProbeAssertions>
{
    public SerialProbeAssertions(SerialProbe probe) : base(probe) { }

    protected override string Identifier => "serial output";

    // ── Content assertions ────────────────────────────────────────────────────

    /// <summary>Asserts that the serial output contains <paramref name="expected"/> as a substring.</summary>
    public AndConstraint<SerialProbeAssertions> Contain(
        string expected, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Text.Contains(expected))
            .FailWith(
                "Expected serial output to contain {0}{reason}, but the captured output was:\n{1}",
                expected, Subject.Text);

        return new AndConstraint<SerialProbeAssertions>(this);
    }

    /// <summary>Asserts that the serial output does <em>not</em> contain <paramref name="unexpected"/>.</summary>
    public AndConstraint<SerialProbeAssertions> NotContain(
        string unexpected, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(!Subject.Text.Contains(unexpected))
            .FailWith(
                "Expected serial output not to contain {0}{reason}, but it did.",
                unexpected);

        return new AndConstraint<SerialProbeAssertions>(this);
    }

    /// <summary>Asserts that the serial output starts with <paramref name="expected"/>.</summary>
    public AndConstraint<SerialProbeAssertions> StartWith(
        string expected, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Text.StartsWith(expected))
            .FailWith(
                "Expected serial output to start with {0}{reason}, but the captured output was:\n{1}",
                expected, Subject.Text);

        return new AndConstraint<SerialProbeAssertions>(this);
    }

    /// <summary>Asserts that the serial output ends with <paramref name="expected"/> (trailing whitespace ignored).</summary>
    public AndConstraint<SerialProbeAssertions> EndWith(
        string expected, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Text.TrimEnd().EndsWith(expected))
            .FailWith(
                "Expected serial output to end with {0}{reason}, but the captured output was:\n{1}",
                expected, Subject.Text);

        return new AndConstraint<SerialProbeAssertions>(this);
    }

    /// <summary>Asserts that the serial output exactly equals <paramref name="expected"/>.</summary>
    public AndConstraint<SerialProbeAssertions> Be(
        string expected, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Text == expected)
            .FailWith(
                "Expected serial output to be exactly {0}{reason}, but found:\n{1}",
                expected, Subject.Text);

        return new AndConstraint<SerialProbeAssertions>(this);
    }

    /// <summary>Asserts that nothing has been transmitted yet.</summary>
    public AndConstraint<SerialProbeAssertions> BeEmpty(
        string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(string.IsNullOrEmpty(Subject.Text))
            .FailWith(
                "Expected serial output to be empty{reason}, but found:\n{0}",
                Subject.Text);

        return new AndConstraint<SerialProbeAssertions>(this);
    }

    // ── Line-level assertions ─────────────────────────────────────────────────

    /// <summary>
    /// Asserts that the output, when split by <c>'\n'</c>, has exactly <paramref name="expected"/> lines.
    /// </summary>
    public AndConstraint<SerialProbeAssertions> HaveLineCount(
        int expected, string because = "", params object[] becauseArgs)
    {
        var lines = Subject.Lines;
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(lines.Count == expected)
            .FailWith(
                "Expected serial output to have {0} line(s){reason}, but found {1}. Lines:\n{2}",
                expected, lines.Count, string.Join("\n", lines));

        return new AndConstraint<SerialProbeAssertions>(this);
    }

    /// <summary>Asserts that at least one line of output exactly equals <paramref name="line"/>.</summary>
    public AndConstraint<SerialProbeAssertions> ContainLine(
        string line, string because = "", params object[] becauseArgs)
    {
        var lines = Subject.Lines;
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(lines.Any(l => l == line))
            .FailWith(
                "Expected serial output to contain line {0}{reason}, but it did not. Lines were:\n{1}",
                line, string.Join("\n", lines));

        return new AndConstraint<SerialProbeAssertions>(this);
    }

    // ── Binary / byte-level assertions ───────────────────────────────────────

    /// <summary>
    /// Asserts that the raw byte stream contains <paramref name="value"/> at least once.
    /// </summary>
    public AndConstraint<SerialProbeAssertions> ContainByte(
        byte value, string because = "", params object[] becauseArgs)
    {
        var bytes = Subject.Bytes;
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(bytes.Contains(value))
            .FailWith(
                "Expected serial output to contain byte 0x{0:X2}{reason}, but it did not. Bytes: [{1}]",
                value, string.Join(", ", bytes.Select(b => $"0x{b:X2}")));

        return new AndConstraint<SerialProbeAssertions>(this);
    }

    /// <summary>
    /// Asserts that the raw byte stream is exactly equal to <paramref name="expected"/>.
    /// </summary>
    public AndConstraint<SerialProbeAssertions> HaveBytes(
        IEnumerable<byte> expected, string because = "", params object[] becauseArgs)
    {
        var actual = Subject.Bytes;
        var exp = expected.ToArray();
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(actual.SequenceEqual(exp))
            .FailWith(
                "Expected serial bytes to be [{0}]{reason}, but found [{1}].",
                string.Join(", ", exp.Select(b => $"0x{b:X2}")),
                string.Join(", ", actual.Select(b => $"0x{b:X2}")));

        return new AndConstraint<SerialProbeAssertions>(this);
    }

    /// <summary>
    /// Asserts that bytes starting at <paramref name="index"/> match <paramref name="expected"/>.
    /// </summary>
    public AndConstraint<SerialProbeAssertions> HaveBytesAt(
        int index, IEnumerable<byte> expected, string because = "", params object[] becauseArgs)
    {
        var actual = Subject.Bytes;
        var exp = expected.ToArray();
        var slice = actual.Skip(index).Take(exp.Length).ToArray();
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(slice.SequenceEqual(exp))
            .FailWith(
                "Expected serial bytes at index {0} to be [{1}]{reason}, but found [{2}].",
                index,
                string.Join(", ", exp.Select(b => $"0x{b:X2}")),
                string.Join(", ", slice.Select(b => $"0x{b:X2}")));

        return new AndConstraint<SerialProbeAssertions>(this);
    }
}
