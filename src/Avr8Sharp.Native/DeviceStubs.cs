using AVR8Sharp.Core.Peripherals;

namespace Avr8Sharp.Native;

/// <summary>
/// A canned SPI slave on the bus. Captures every byte the firmware clocks out (MOSI) and replays
/// a pre-loaded response queue back (MISO) — the standard way to test SPI sensor/display drivers
/// without modelling a real device. Returns 0xFF once the queue is drained (idle MISO line).
/// </summary>
internal sealed class SpiDeviceStub
{
    public readonly List<byte> Mosi = new();
    // Settable so a session can point both bus stubs at ONE queue -- the bench
    // wiring "one scripted responder answers whichever bus reads".
    public Queue<byte> Responses = new();

    public int Transfer(byte outgoing)
    {
        Mosi.Add(outgoing);
        return Responses.Count > 0 ? Responses.Dequeue() : 0xFF;
    }
}

/// <summary>
/// An I²C slave. ACKs transactions addressed to any address in <see cref="Addresses"/>,
/// logs the bytes the firmware writes, and replays a pre-loaded response queue on reads
/// (0xFF when drained). The response queue is shared across addresses, the way a scripted
/// bench with one responder per pin would be wired.
/// </summary>
internal sealed class TwiDeviceStub(AvrTwi twi) : ITwiEventHandler
{
    /// <summary>7-bit slave addresses this device answers to.</summary>
    public readonly HashSet<byte> Addresses = new();

    public readonly List<byte> Writes = new();
    public readonly List<byte> Reads = new();
    public readonly List<byte> Addrs = new();
    // Settable so a session can share this queue with the SPI stub -- one
    // scripted responder feeding both buses, matching the oracle's single
    // take() script.
    public Queue<byte> Responses = new();

    private bool _selected;

    public void Start(bool repeated) { Addrs.Add(0xFE); twi.CompleteStart(); }

    public void Stop()
    {
        _selected = false;
        Addrs.Add(0xFF);
        twi.CompleteStop();
    }

    public void ConnectToSlave(byte address, bool write)
    {
        _selected = Addresses.Contains(address);
        Addrs.Add((byte)(address | (write ? 0 : 0x80)));
        twi.CompleteConnect(_selected);
    }

    public void WriteByte(byte data)
    {
        if (_selected) Writes.Add(data);
        twi.CompleteWrite(_selected);
    }

    public void ReadByte(bool ack)
    {
        var b = Responses.Count > 0 ? Responses.Dequeue() : (byte)0xFF;
        Reads.Add(b);
        twi.CompleteRead(b);
    }
}
