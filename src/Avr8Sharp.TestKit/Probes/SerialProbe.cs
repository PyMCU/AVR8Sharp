using System.Text;
using AVR8Sharp.Core.Peripherals;

namespace Avr8Sharp.TestKit.Probes;

/// <summary>
/// Captures all bytes transmitted by a USART peripheral, making them available
/// for assertions via <see cref="Assertions.SerialProbeAssertions"/>.
/// </summary>
public class SerialProbe
{
    private readonly List<byte> _rawBytes = new();
    private readonly AvrUsart _usart;
    private List<string>? _linesCache;

    internal SerialProbe(AvrUsart usart)
    {
        _usart = usart;
        usart.OnByteTransmit = b =>
        {
            _rawBytes.Add(b);
            _linesCache = null;
        };
    }

    /// <summary>All characters received so far as a single string (Latin-1 encoded).</summary>
    public string Text => Encoding.Latin1.GetString(_rawBytes.ToArray());

    /// <summary>All received bytes as a raw byte array (useful for binary-protocol tests).</summary>
    public byte[] Bytes => _rawBytes.ToArray();

    /// <summary>Number of bytes received so far.</summary>
    public int ByteCount => _rawBytes.Count;

    /// <summary>
    /// The received text split on <c>'\n'</c>, with trailing <c>'\r'</c> stripped from each line.
    /// The result is cached and invalidated whenever a new byte arrives.
    /// </summary>
    public IReadOnlyList<string> Lines
        => _linesCache ??= Text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

    /// <summary>Clears the captured output buffer.</summary>
    public void Clear()
    {
        _rawBytes.Clear();
        _linesCache = null;
    }

    /// <summary>
    /// Injects a byte into the USART receiver, simulating an incoming character
    /// from the outside world (e.g. a host sending a command to the firmware).
    /// </summary>
    public void InjectByte(byte value) => _usart.WriteByte(value);
}
