using System.Runtime.InteropServices;
using System.Text;
using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.TestKit;
using Avr8Sharp.TestKit.Boards;

namespace Avr8Sharp.Native;

/// <summary>
/// Flat C ABI exported by the Native AOT shared library and consumed from Python via ctypes.
/// <para>
/// Every entry point is coarse-grained: a single call crosses the managed boundary once and runs
/// many thousands of CPU cycles entirely on the native side. The per-instruction loop is never
/// exposed, so FFI overhead is amortised to ~0 and the emulator keeps its full speed.
/// </para>
/// <para>
/// Sessions are opaque handles (a pinned <see cref="GCHandle"/> to a <see cref="NativeSession"/>).
/// Functions that can fail return an int status (0 = ok, negative = error); the message is then
/// retrievable via <see cref="LastError"/>.
/// </para>
/// </summary>
public static unsafe class Exports
{
    private const int Ok = 0;
    private const int ErrBadHandle = -1;
    private const int ErrException = -2;
    private const int ErrBadIndex = -3;

    private const ushort BreakOpcode = 0x9598;

    // ── Handle helpers ────────────────────────────────────────────────────────

    private static NativeSession? Get(IntPtr h)
        => h == IntPtr.Zero ? null : GCHandle.FromIntPtr(h).Target as NativeSession;

    private static IntPtr Wrap(NativeSession s)
        => GCHandle.ToIntPtr(GCHandle.Alloc(s));

    /// <summary>Runs <paramref name="body"/> guarded against bad handles and exceptions.</summary>
    private static int Run(IntPtr h, Action<NativeSession> body)
    {
        if (Get(h) is not { } s) return ErrBadHandle;
        try { body(s); return Ok; }
        catch (Exception e) { s.LastError = e.Message; return ErrException; }
    }

    private static string Utf8(byte* p, int len) => Encoding.UTF8.GetString(p, len);

    /// <summary>Copies <paramref name="src"/> into the caller buffer, returning the full length.</summary>
    private static int CopyOut(byte[] src, byte* outBuf, int cap)
    {
        var n = Math.Min(src.Length, cap);
        if (n > 0) Marshal.Copy(src, 0, (IntPtr)outBuf, n);
        return src.Length;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    [UnmanagedCallersOnly(EntryPoint = "a8s_create")]
    public static IntPtr Create(int flash, int sram)
        => Wrap(new NativeSession(AvrTestSimulation.Create(flash, sram)));

    [UnmanagedCallersOnly(EntryPoint = "a8s_create_uno")]
    public static IntPtr CreateUno()
    {
        var uno = new ArduinoUnoSimulation();
        var s = new NativeSession(uno);
        s.Ports.AddRange([uno.PortB, uno.PortC, uno.PortD]);
        s.Timers.AddRange([uno.Timer0, uno.Timer1, uno.Timer2]);
        s.Serials.Add(uno.Serial);
        WireAtmega328Analog(uno, s);
        return Wrap(s);
    }

    [UnmanagedCallersOnly(EntryPoint = "a8s_create_mega")]
    public static IntPtr CreateMega()
    {
        var m = new ArduinoMegaSimulation();
        var s = new NativeSession(m);
        s.Ports.AddRange([m.PortA, m.PortB, m.PortC, m.PortD, m.PortE, m.PortF,
                          m.PortG, m.PortH, m.PortJ, m.PortK, m.PortL]);
        s.Timers.AddRange([m.Timer0, m.Timer1, m.Timer2, m.Timer3, m.Timer4, m.Timer5]);
        s.Serials.AddRange([m.Serial0, m.Serial1, m.Serial2, m.Serial3]);
        return Wrap(s);
    }

    [UnmanagedCallersOnly(EntryPoint = "a8s_create_attiny85")]
    public static IntPtr CreateAttiny85()
    {
        var t = new ATtiny85Simulation();
        var s = new NativeSession(t);
        s.Ports.Add(t.PortB);
        s.Timers.AddRange([t.Timer0, t.Timer1]);
        return Wrap(s);
    }

    /// <summary>ATtiny24/44/84 (avr25): port indices 0=A, 1=B. flash/sram pick the variant.</summary>
    [UnmanagedCallersOnly(EntryPoint = "a8s_create_attinyx4")]
    public static IntPtr CreateAttinyX4(int flash, int sram)
    {
        var t = new ATtinyX4Simulation(flash, sram);
        var s = new NativeSession(t);
        s.Ports.AddRange([t.PortA, t.PortB]);
        return Wrap(s);
    }

    [UnmanagedCallersOnly(EntryPoint = "a8s_create_attiny13")]
    public static IntPtr CreateAttiny13()
    {
        var t = new ATtiny13Simulation();
        var s = new NativeSession(t);
        s.Ports.Add(t.PortB);
        return Wrap(s);
    }

    [UnmanagedCallersOnly(EntryPoint = "a8s_destroy")]
    public static void Destroy(IntPtr h)
    {
        if (h == IntPtr.Zero) return;
        var gch = GCHandle.FromIntPtr(h);
        if (gch.IsAllocated) gch.Free();
    }

    // ── Program loading ───────────────────────────────────────────────────────

    [UnmanagedCallersOnly(EntryPoint = "a8s_with_frequency")]
    public static int WithFrequency(IntPtr h, uint hz)
        => Run(h, s => s.Sim.WithFrequency(hz));

    [UnmanagedCallersOnly(EntryPoint = "a8s_with_hex")]
    public static int WithHex(IntPtr h, byte* hex, int len)
        => Run(h, s => s.Sim.WithHex(Utf8(hex, len)));

    [UnmanagedCallersOnly(EntryPoint = "a8s_with_asm")]
    public static int WithAsm(IntPtr h, byte* asm, int len)
        => Run(h, s => s.Sim.WithAsm(Utf8(asm, len)));

    [UnmanagedCallersOnly(EntryPoint = "a8s_with_program")]
    public static int WithProgram(IntPtr h, byte* bytes, int len)
        => Run(h, s =>
        {
            var buf = new byte[len];
            Marshal.Copy((IntPtr)bytes, buf, 0, len);
            s.Sim.WithProgram(buf);
        });

    // ── Generic peripheral wiring (custom sessions, ATmega328P layout) ─────────

    /// <summary>Adds GPIO port 0=B, 1=C, 2=D. Returns the new port index, or a negative error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "a8s_add_gpio")]
    public static int AddGpio(IntPtr h, int which)
    {
        if (Get(h) is not { } s) return ErrBadHandle;
        try
        {
            var cfg = which switch
            {
                0 => AvrIoPort.PortBConfig,
                1 => AvrIoPort.PortCConfig,
                2 => AvrIoPort.PortDConfig,
                _ => (AvrPortConfig?)null,
            };
            if (cfg is null) return ErrBadIndex;
            s.Sim.AddGpio(cfg, out var port);
            s.Ports.Add(port);
            return s.Ports.Count - 1;
        }
        catch (Exception e) { s.LastError = e.Message; return ErrException; }
    }

    /// <summary>Adds USART0 with a capture probe. Returns the new serial index.</summary>
    [UnmanagedCallersOnly(EntryPoint = "a8s_add_usart0")]
    public static int AddUsart0(IntPtr h)
    {
        if (Get(h) is not { } s) return ErrBadHandle;
        try
        {
            s.Sim.AddUsart(AvrUsart.Usart0Config, out var serial);
            s.Serials.Add(serial);
            return s.Serials.Count - 1;
        }
        catch (Exception e) { s.LastError = e.Message; return ErrException; }
    }

    /// <summary>Adds timer 0/1/2. Returns the new timer index.</summary>
    [UnmanagedCallersOnly(EntryPoint = "a8s_add_timer")]
    public static int AddTimer(IntPtr h, int which)
    {
        if (Get(h) is not { } s) return ErrBadHandle;
        try
        {
            var cfg = which switch
            {
                0 => AvrTimer.Timer0Config,
                1 => AvrTimer.Timer1Config,
                2 => AvrTimer.Timer2Config,
                _ => (AvrTimerConfig?)null,
            };
            if (cfg is null) return ErrBadIndex;
            s.Sim.AddTimer(cfg, out var timer);
            s.Timers.Add(timer);
            return s.Timers.Count - 1;
        }
        catch (Exception e) { s.LastError = e.Message; return ErrException; }
    }

    // ── Execution ─────────────────────────────────────────────────────────────

    [UnmanagedCallersOnly(EntryPoint = "a8s_run_cycles")]
    public static int RunCycles(IntPtr h, long cycles)
        => Run(h, s => s.Sim.RunCycles(cycles));

    [UnmanagedCallersOnly(EntryPoint = "a8s_run_ms")]
    public static int RunMs(IntPtr h, double ms)
        => Run(h, s => s.Sim.RunMilliseconds(ms));

    [UnmanagedCallersOnly(EntryPoint = "a8s_run_instructions")]
    public static int RunInstructions(IntPtr h, int count)
        => Run(h, s => s.Sim.RunInstructions(count));

    [UnmanagedCallersOnly(EntryPoint = "a8s_run_to_break")]
    public static int RunToBreak(IntPtr h, int maxInstructions)
        => Run(h, s => s.Sim.RunToBreak(maxInstructions));

    [UnmanagedCallersOnly(EntryPoint = "a8s_run_until_serial")]
    public static int RunUntilSerial(IntPtr h, int serialIdx, byte* text, int len, double maxMs)
        => Run(h, s =>
        {
            if (serialIdx < 0 || serialIdx >= s.Serials.Count)
                throw new ArgumentOutOfRangeException(nameof(serialIdx));
            s.Sim.RunUntilSerial(s.Serials[serialIdx], Utf8(text, len), maxMs);
        });

    [UnmanagedCallersOnly(EntryPoint = "a8s_run_until_serial_bytes")]
    public static int RunUntilSerialBytes(IntPtr h, int serialIdx, int byteCount, double maxMs)
        => Run(h, s =>
        {
            if (serialIdx < 0 || serialIdx >= s.Serials.Count)
                throw new ArgumentOutOfRangeException(nameof(serialIdx));
            s.Sim.RunUntilSerialBytes(s.Serials[serialIdx], byteCount, maxMs);
        });

    // ── Serial observation ────────────────────────────────────────────────────

    /// <summary>Copies the raw captured bytes of serial <paramref name="idx"/> into the caller
    /// buffer and returns the full byte count (which may exceed <paramref name="cap"/>).</summary>
    [UnmanagedCallersOnly(EntryPoint = "a8s_serial_read")]
    public static int SerialRead(IntPtr h, int idx, byte* outBuf, int cap)
    {
        if (Get(h) is not { } s) return ErrBadHandle;
        if (idx < 0 || idx >= s.Serials.Count) return ErrBadIndex;
        return CopyOut(s.Serials[idx].Bytes, outBuf, cap);
    }

    [UnmanagedCallersOnly(EntryPoint = "a8s_serial_byte_count")]
    public static int SerialByteCount(IntPtr h, int idx)
    {
        if (Get(h) is not { } s) return ErrBadHandle;
        if (idx < 0 || idx >= s.Serials.Count) return ErrBadIndex;
        return s.Serials[idx].ByteCount;
    }

    [UnmanagedCallersOnly(EntryPoint = "a8s_serial_clear")]
    public static int SerialClear(IntPtr h, int idx)
        => Run(h, s =>
        {
            if (idx < 0 || idx >= s.Serials.Count)
                throw new ArgumentOutOfRangeException(nameof(idx));
            s.Serials[idx].Clear();
        });

    [UnmanagedCallersOnly(EntryPoint = "a8s_serial_inject")]
    public static int SerialInject(IntPtr h, int idx, byte value)
        => Run(h, s =>
        {
            if (idx < 0 || idx >= s.Serials.Count)
                throw new ArgumentOutOfRangeException(nameof(idx));
            s.Serials[idx].InjectByte(value);
        });

    // ── GPIO observation ──────────────────────────────────────────────────────

    /// <summary>Returns the <see cref="PinState"/> of <paramref name="pin"/> on port
    /// <paramref name="portIdx"/> (0=Low,1=High,2=Input,3=InputPullup), or negative on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "a8s_gpio_pin")]
    public static int GpioPin(IntPtr h, int portIdx, int pin)
    {
        if (Get(h) is not { } s) return ErrBadHandle;
        if (portIdx < 0 || portIdx >= s.Ports.Count) return ErrBadIndex;
        return (int)s.Ports[portIdx].GetPinState((byte)pin);
    }

    // ── CPU / memory observation ──────────────────────────────────────────────

    [UnmanagedCallersOnly(EntryPoint = "a8s_cpu_pc")]
    public static uint CpuPc(IntPtr h) => Get(h) is { } s ? s.Sim.Cpu.Pc : 0u;

    [UnmanagedCallersOnly(EntryPoint = "a8s_cpu_sp")]
    public static uint CpuSp(IntPtr h) => Get(h) is { } s ? s.Sim.Cpu.Sp : 0u;

    [UnmanagedCallersOnly(EntryPoint = "a8s_cpu_cycles")]
    public static ulong CpuCycles(IntPtr h) => Get(h) is { } s ? s.Sim.Cpu.Cycles : 0;

    [UnmanagedCallersOnly(EntryPoint = "a8s_cpu_sreg")]
    public static int CpuSreg(IntPtr h) => Get(h) is { } s ? s.Sim.Cpu.Sreg : ErrBadHandle;

    /// <summary>Reads data-space byte at <paramref name="addr"/> (registers, I/O, SRAM), or -1.</summary>
    [UnmanagedCallersOnly(EntryPoint = "a8s_read_data")]
    public static int ReadData(IntPtr h, int addr)
    {
        if (Get(h) is not { } s) return ErrBadHandle;
        return s.Sim.Cpu.ReadData((ushort)addr);
    }

    [UnmanagedCallersOnly(EntryPoint = "a8s_write_data")]
    public static int WriteData(IntPtr h, int addr, byte value)
        => Run(h, s => s.Sim.Cpu.WriteData((ushort)addr, value));

    // ── Reset / snapshot (mirrors PyMCU's SimSession fast-reset) ───────────────

    [UnmanagedCallersOnly(EntryPoint = "a8s_reset")]
    public static int Reset(IntPtr h) => Run(h, s => s.Sim.Reset());

    /// <summary>Captures the current Data array as the power-on snapshot for <c>a8s_restore</c>.
    /// Call this right after creating a board, before loading firmware, to capture peripheral
    /// power-on defaults (e.g. UCSRA bits) exactly like PyMCU's SimSession.</summary>
    [UnmanagedCallersOnly(EntryPoint = "a8s_snapshot")]
    public static int Snapshot(IntPtr h)
        => Run(h, s => s.Snapshot = (byte[])s.Sim.Data.Clone());

    /// <summary>Restores the snapshot, resets all timers/CPU, zeroes the cycle counter, and clears
    /// all serial probes — the full PyMCU SimSession per-test reset, done natively.</summary>
    [UnmanagedCallersOnly(EntryPoint = "a8s_restore")]
    public static int Restore(IntPtr h)
        => Run(h, s =>
        {
            if (s.Snapshot is null)
                throw new InvalidOperationException("a8s_restore called before a8s_snapshot.");
            Array.Copy(s.Snapshot, s.Sim.Data, s.Snapshot.Length);
            foreach (var t in s.Timers) t.Reset();
            s.Sim.Cpu.Reset();
            s.Sim.Cpu.Cycles = 0;
            foreach (var probe in s.Serials) probe.Clear();
            if (s.Spi is not null) { s.Spi.Mosi.Clear(); s.Spi.Responses.Clear(); }
            if (s.Twi is not null) { s.Twi.Writes.Clear(); s.Twi.Responses.Clear(); }
        });

    // ── Errors ────────────────────────────────────────────────────────────────

    /// <summary>Copies the last error message (UTF-8) into the caller buffer; returns full length.</summary>
    [UnmanagedCallersOnly(EntryPoint = "a8s_last_error")]
    public static int LastError(IntPtr h, byte* outBuf, int cap)
    {
        if (Get(h) is not { } s) return ErrBadHandle;
        return CopyOut(Encoding.UTF8.GetBytes(s.LastError), outBuf, cap);
    }

    // ── Analog/bus peripherals (ATmega328P-family sessions) ───────────────────

    /// <summary>Wires ADC, an SPI bus stub, and a single-address I²C slave stub onto an
    /// ATmega328P-style simulation, using the 328P peripheral configs.</summary>
    private static void WireAtmega328Analog(AvrTestSimulation sim, NativeSession s)
    {
        sim.AddAdc(AvrAdc.AdcConfig, out var adc);
        s.Adc = adc;

        sim.AddSpi(AvrSpi.SpiConfig, out var spi);
        var spiStub = new SpiDeviceStub();
        spi.OnTransfer = spiStub.Transfer;
        s.Spi = spiStub;

        sim.AddTwi(AvrTwi.TwiConfig, out var twi);
        var twiStub = new TwiDeviceStub(twi);
        twi.EventHandler = twiStub;
        s.Twi = twiStub;
    }

    /// <summary>Sets the analog voltage (volts) presented on ADC <paramref name="channel"/>,
    /// so a firmware ADC read on that channel returns the corresponding value.</summary>
    [UnmanagedCallersOnly(EntryPoint = "a8s_adc_set_channel")]
    public static int AdcSetChannel(IntPtr h, int channel, double volts)
        => Run(h, s =>
        {
            if (s.Adc is null) throw new InvalidOperationException("Session has no ADC peripheral.");
            if (channel < 0 || channel >= s.Adc.ChannelValues.Length)
                throw new ArgumentOutOfRangeException(nameof(channel));
            s.Adc.ChannelValues[channel] = volts;
        });

    /// <summary>Queues a byte for the SPI slave stub to return (MISO) on the next transfer.</summary>
    [UnmanagedCallersOnly(EntryPoint = "a8s_spi_queue_response")]
    public static int SpiQueueResponse(IntPtr h, byte value)
        => Run(h, s =>
        {
            if (s.Spi is null) throw new InvalidOperationException("Session has no SPI peripheral.");
            s.Spi.Responses.Enqueue(value);
        });

    /// <summary>Copies the bytes the firmware has clocked out over SPI (MOSI) into the caller
    /// buffer; returns the full count.</summary>
    [UnmanagedCallersOnly(EntryPoint = "a8s_spi_read_mosi")]
    public static int SpiReadMosi(IntPtr h, byte* outBuf, int cap)
    {
        if (Get(h) is not { } s) return ErrBadHandle;
        if (s.Spi is null) return ErrBadIndex;
        return CopyOut(s.Spi.Mosi.ToArray(), outBuf, cap);
    }

    /// <summary>Configures the I²C slave stub: ACK transactions to <paramref name="address"/>
    /// (7-bit) when <paramref name="present"/> is non-zero.</summary>
    [UnmanagedCallersOnly(EntryPoint = "a8s_twi_set_slave")]
    public static int TwiSetSlave(IntPtr h, int address, int present)
        => Run(h, s =>
        {
            if (s.Twi is null) throw new InvalidOperationException("Session has no TWI peripheral.");
            s.Twi.Address = (byte)address;
            s.Twi.Present = present != 0;
        });

    /// <summary>Queues a byte for the I²C slave stub to return on the next firmware read.</summary>
    [UnmanagedCallersOnly(EntryPoint = "a8s_twi_queue_response")]
    public static int TwiQueueResponse(IntPtr h, byte value)
        => Run(h, s =>
        {
            if (s.Twi is null) throw new InvalidOperationException("Session has no TWI peripheral.");
            s.Twi.Responses.Enqueue(value);
        });

    /// <summary>Copies the bytes the firmware has written to the I²C slave into the caller
    /// buffer; returns the full count.</summary>
    [UnmanagedCallersOnly(EntryPoint = "a8s_twi_read_writes")]
    public static int TwiReadWrites(IntPtr h, byte* outBuf, int cap)
    {
        if (Get(h) is not { } s) return ErrBadHandle;
        if (s.Twi is null) return ErrBadIndex;
        return CopyOut(s.Twi.Writes.ToArray(), outBuf, cap);
    }

    // ── AOT smoke test ────────────────────────────────────────────────────────

    /// <summary>Self-contained sanity check: assembles and runs a tiny program, returns
    /// <c>(cycles &lt;&lt; 8) | r16</c>; a correct result has low byte 0x2A (42).</summary>
    [UnmanagedCallersOnly(EntryPoint = "a8s_selftest")]
    public static long SelfTest()
    {
        var sim = AvrTestSimulation.Create();
        sim.WithAsm("ldi r16, 0x2a\nnop\nnop\nbreak\n").RunToBreak();
        return ((long)sim.Cpu.Cycles << 8) | sim.Cpu.ReadData(16);
    }
}
