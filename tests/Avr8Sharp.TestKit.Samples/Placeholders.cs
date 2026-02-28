namespace Avr8Sharp.TestKit.Samples;

/// <summary>
/// Minimal Intel HEX strings used as placeholders in sample tests marked <c>[Ignore]</c>.
/// <para>
/// When you have a compiled firmware file, replace the placeholder with either:
/// <list type="bullet">
///   <item><c>.WithHex(File.ReadAllText("firmware/sketch.hex"))</c></item>
///   <item><c>.WithHex(EmbeddedHex.Load("sketch.hex"))</c></item>
/// </list>
/// Then remove the <c>[Ignore]</c> attribute so the test runs in CI.
/// </para>
/// </summary>
public static class Placeholders
{
    /// <summary>
    /// A valid Intel HEX record containing only a <c>BREAK</c> (0x9598) instruction at address 0.
    /// <para>
    /// Simulations loaded with this hex will stop immediately on <c>RunToBreak()</c>
    /// without executing any program logic.
    /// </para>
    /// </summary>
    public const string Break =
        ":020000009895D1\n" +
        ":00000001FF";
}
