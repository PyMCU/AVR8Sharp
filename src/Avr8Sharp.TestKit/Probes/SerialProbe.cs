using System.Text;
using AVR8Sharp.Core.Peripherals;

namespace Avr8Sharp.TestKit.Probes;

/// <summary>
/// Captures all bytes transmitted by a USART peripheral, making them available
/// for assertions via <see cref="Assertions.SerialProbeAssertions"/>.
/// </summary>
public class SerialProbe
{
    private readonly StringBuilder _buffer = new();
    private readonly AvrUsart _usart;

    internal SerialProbe(AvrUsart usart)
    {
        _usart = usart;
        usart.OnByteTransmit = b => _buffer.Append((char)b);
    }

    /// <summary>All characters received so far as a single string.</summary>
    public string Text => _buffer.ToString();

    /// <summary>
    /// The received text split on <c>'\n'</c>, with trailing <c>'\r'</c> stripped from each line.
    /// </summary>
    public IReadOnlyList<string> Lines
        => Text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

    /// <summary>Clears the captured output buffer.</summary>
    public void Clear() => _buffer.Clear();

    /// <summary>
    /// Injects a byte into the USART receiver, simulating an incoming character
    /// from the outside world (e.g. a host sending a command to the firmware).
    /// </summary>
    public void InjectByte(byte value) => _usart.WriteByte(value);
}
