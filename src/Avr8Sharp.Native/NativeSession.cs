using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.TestKit;
using Avr8Sharp.TestKit.Probes;

namespace Avr8Sharp.Native;

/// <summary>
/// Managed state behind a single opaque handle handed to Python. Holds the simulation plus the
/// peripherals registered against it, indexed by the small integer ids exposed across the C ABI
/// (port 0..N, serial 0..N, timer 0..N). A <see cref="System.Runtime.InteropServices.GCHandle"/>
/// keeps this object alive while Python owns the handle.
/// </summary>
internal sealed class NativeSession(AvrTestSimulation sim)
{
    public readonly AvrTestSimulation Sim = sim;
    public readonly List<AvrIoPort> Ports = new();
    public readonly List<SerialProbe> Serials = new();
    public readonly List<AvrTimer> Timers = new();

    /// <summary>ADC peripheral (ATmega328P-family sessions only); null otherwise.</summary>
    public AvrAdc? Adc;

    /// <summary>SPI master-bus stub: captures MOSI, replays a canned response queue.</summary>
    public SpiDeviceStub? Spi;

    /// <summary>I²C single-address slave stub: ACKs its address, logs writes, replays reads.</summary>
    public TwiDeviceStub? Twi;

    /// <summary>Power-on snapshot of the Data array, captured by <c>a8s_snapshot</c>.</summary>
    public byte[]? Snapshot;

    /// <summary>Last error message, retrievable via <c>a8s_last_error</c>.</summary>
    public string LastError = "";
}
