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
    public readonly Queue<byte> Responses = new();

    public int Transfer(byte outgoing)
    {
        Mosi.Add(outgoing);
        return Responses.Count > 0 ? Responses.Dequeue() : 0xFF;
    }
}

/// <summary>
/// A single-address I²C slave. ACKs transactions addressed to <see cref="Address"/> (when
/// <see cref="Present"/>), logs the bytes the firmware writes, and replays a pre-loaded response
/// queue on reads (0xFF when drained). Covers the common "firmware talks to one sensor" test.
/// </summary>
internal sealed class TwiDeviceStub(AvrTwi twi) : ITwiEventHandler
{
    public byte Address;     // 7-bit slave address this device answers to
    public bool Present;     // whether a device is connected at Address

    public readonly List<byte> Writes = new();
    public readonly Queue<byte> Responses = new();

    private bool _selected;

    public void Start(bool repeated) => twi.CompleteStart();

    public void Stop()
    {
        _selected = false;
        twi.CompleteStop();
    }

    public void ConnectToSlave(byte address, bool write)
    {
        _selected = Present && address == Address;
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
        twi.CompleteRead(b);
    }
}
