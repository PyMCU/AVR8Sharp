using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
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
    private ReadOnlyCollection<string>? _linesCache;
    private string? _textCache;

    internal SerialProbe(AvrUsart usart)
    {
        _usart = usart;
        usart.OnByteTransmit = b =>
        {
            _rawBytes.Add(b);
            _linesCache = null;
            _textCache = null;
            Version++;
        };
    }

    /// <summary>
    /// Bumped on every received byte and on <see cref="Clear"/>. Lets run helpers skip
    /// re-evaluating a text predicate while the captured output is unchanged.
    /// </summary>
    internal int Version { get; private set; }

    /// <summary>
    /// All characters received so far as a single string (Latin-1 encoded).
    /// The string is cached until the next byte arrives, so polling it between
    /// instructions does not allocate.
    /// </summary>
    public string Text => _textCache ??= Encoding.Latin1.GetString(CollectionsMarshal.AsSpan(_rawBytes));

    /// <summary>All received bytes as a raw byte array (useful for binary-protocol tests).</summary>
    public byte[] Bytes => _rawBytes.ToArray();

    /// <summary>Number of bytes received so far.</summary>
    public int ByteCount => _rawBytes.Count;

    /// <summary>
    /// The received text split on <c>'\n'</c>, with trailing <c>'\r'</c> stripped from each line.
    /// The result is cached and invalidated whenever a new byte arrives.
    /// </summary>
    public IReadOnlyList<string> Lines
        => _linesCache ??= Array.AsReadOnly(Text.Split('\n').Select(l => l.TrimEnd('\r')).ToArray());

    /// <summary>Clears the captured output buffer.</summary>
    public void Clear()
    {
        _rawBytes.Clear();
        _linesCache = null;
        _textCache = null;
        Version++;
    }

    /// <summary>
    /// Injects a byte into the USART receiver, simulating an incoming character
    /// from the outside world (e.g. a host sending a command to the firmware).
    /// </summary>
    public void InjectByte(byte value) => _usart.WriteByte(value);
}
