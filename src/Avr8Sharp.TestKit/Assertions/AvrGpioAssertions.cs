using AVR8Sharp.Core.Peripherals;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace Avr8Sharp.TestKit.Assertions;

/// <summary>
/// FluentAssertions assertion class for <see cref="AvrIoPort"/> state inspection.
/// Obtain via <c>portB.Should()</c>.
/// </summary>
public class AvrGpioAssertions : ReferenceTypeAssertions<AvrIoPort, AvrGpioAssertions>
{
    public AvrGpioAssertions(AvrIoPort port) : base(port) { }

    protected override string Identifier => "GPIO port";

    // ── Single-pin assertions ─────────────────────────────────────────────────

    /// <summary>Asserts that pin <paramref name="pin"/> is driven <c>High</c> (output).</summary>
    public AndConstraint<AvrGpioAssertions> HavePinHigh(
        int pin, string because = "", params object[] becauseArgs)
    {
        var state = Subject.GetPinState((byte)pin);
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(state == PinState.High)
            .FailWith(
                "Expected pin {0} to be High{reason}, but its state was {1}.",
                pin, state);

        return new AndConstraint<AvrGpioAssertions>(this);
    }

    /// <summary>Asserts that pin <paramref name="pin"/> is driven <c>Low</c> (output).</summary>
    public AndConstraint<AvrGpioAssertions> HavePinLow(
        int pin, string because = "", params object[] becauseArgs)
    {
        var state = Subject.GetPinState((byte)pin);
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(state == PinState.Low)
            .FailWith(
                "Expected pin {0} to be Low{reason}, but its state was {1}.",
                pin, state);

        return new AndConstraint<AvrGpioAssertions>(this);
    }

    /// <summary>Asserts that pin <paramref name="pin"/> is configured as input (floating).</summary>
    public AndConstraint<AvrGpioAssertions> HavePinInput(
        int pin, string because = "", params object[] becauseArgs)
    {
        var state = Subject.GetPinState((byte)pin);
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(state == PinState.Input)
            .FailWith(
                "Expected pin {0} to be Input{reason}, but its state was {1}.",
                pin, state);

        return new AndConstraint<AvrGpioAssertions>(this);
    }

    /// <summary>Asserts that pin <paramref name="pin"/> is configured as input with pull-up enabled.</summary>
    public AndConstraint<AvrGpioAssertions> HavePinInputPullup(
        int pin, string because = "", params object[] becauseArgs)
    {
        var state = Subject.GetPinState((byte)pin);
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(state == PinState.InputPullup)
            .FailWith(
                "Expected pin {0} to be InputPullup{reason}, but its state was {1}.",
                pin, state);

        return new AndConstraint<AvrGpioAssertions>(this);
    }

    /// <summary>Asserts that pin <paramref name="pin"/> has the given <paramref name="expected"/> state.</summary>
    public AndConstraint<AvrGpioAssertions> HavePinState(
        int pin, PinState expected, string because = "", params object[] becauseArgs)
    {
        var state = Subject.GetPinState((byte)pin);
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(state == expected)
            .FailWith(
                "Expected pin {0} to have state {1}{reason}, but found {2}.",
                pin, expected, state);

        return new AndConstraint<AvrGpioAssertions>(this);
    }

    // ── Multi-pin assertions ──────────────────────────────────────────────────

    /// <summary>
    /// Asserts that every pin in <paramref name="pins"/> is driven <c>High</c>.
    /// All failures are reported in a single assertion scope.
    /// </summary>
    public AndConstraint<AvrGpioAssertions> HavePinsHigh(
        IEnumerable<int> pins, string because = "", params object[] becauseArgs)
    {
        using var scope = new AssertionScope();

        foreach (var pin in pins)
        {
            var state = Subject.GetPinState((byte)pin);

            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(state == PinState.High)
                .FailWith("Expected pin {0} to be High{reason}, but its state was {1}.", pin, state); // 2. Agregamos {reason}
        }

        return new AndConstraint<AvrGpioAssertions>(this);
    }

    /// <summary>
    /// Asserts that every pin in <paramref name="pins"/> is driven <c>Low</c>.
    /// All failures are reported in a single assertion scope.
    /// </summary>
    public AndConstraint<AvrGpioAssertions> HavePinsLow(
        IEnumerable<int> pins, string because = "", params object[] becauseArgs)
    {
        using var scope = new AssertionScope();

        foreach (var pin in pins)
        {
            var state = Subject.GetPinState((byte)pin);

            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(state == PinState.Low)
                .FailWith("Expected pin {0} to be Low{reason}, but its state was {1}.", pin, state);
        }

        return new AndConstraint<AvrGpioAssertions>(this);
    }

    // ── Port-wide assertions ──────────────────────────────────────────────────

    /// <summary>
    /// Asserts the bitmask of output-pins that are currently driven <c>High</c>.
    /// Only output pins (DDR bit = 1) are considered; input pins are treated as 0.
    /// </summary>
    public AndConstraint<AvrGpioAssertions> HaveOutputValue(
        byte expected, string because = "", params object[] becauseArgs)
    {
        byte actual = 0;
        for (var i = 0; i < 8; i++)
        {
            if (Subject.GetPinState((byte)i) == PinState.High)
                actual |= (byte)(1 << i);
        }

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(actual == expected)
            .FailWith(
                "Expected port output value to be 0x{0:X2} (0b{0:B8}){reason}, but found 0x{1:X2} (0b{1:B8}).",
                expected, actual);

        return new AndConstraint<AvrGpioAssertions>(this);
    }
}
