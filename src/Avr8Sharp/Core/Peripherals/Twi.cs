using AVR8Sharp.Core;

namespace AVR8Sharp.Core.Peripherals;

public class AvrTwi
{
    // Register bits
    const int TWCR_TWINT = 0x80; // TWI Interrupt Flag
    const int TWCR_TWEA = 0x40; // TWI Enable Acknowledge Bit
    const int TWCR_TWSTA = 0x20; // TWI START Condition Bit
    const int TWCR_TWSTO = 0x10; // TWI STOP Condition Bit
    const int TWCR_TWWC = 0x08; // TWI Write Collision Flag
    const int TWCR_TWEN = 0x04; // TWI Enable Bit
    const int TWCR_TWIE = 0x01; // TWI Interrupt Enable
    const int TWSR_TWS_MASK = 0xf8; // TWI Status
    const int TWSR_TWPS1 = 0x02; // TWI Prescaler
    const int TWSR_TWPS0 = 0x01; // TWI Prescaler
    const int TWSR_TWPS_MASK = TWSR_TWPS1 | TWSR_TWPS0; // TWI Prescaler Mask
    const int TWAR_TWA_MASK = 0xfe; // TWI (Slave) Address Mask
    const int TWAR_TWGCE = 0x01; // TWI General Call Recognition Enable Bit

    const int STATUS_BUS_ERROR = 0x00;

    const int STATUS_TWI_IDLE = 0xf8;

    // Master states
    const int STATUS_START = 0x08;
    const int STATUS_REPEATED_START = 0x10;
    const int STATUS_SLAW_ACK = 0x18;
    const int STATUS_SLAW_NACK = 0x20;
    const int STATUS_DATA_SENT_ACK = 0x28;
    const int STATUS_DATA_SENT_NACK = 0x30;
    const int STATUS_DATA_LOST_ARBITRATION = 0x38;
    const int STATUS_SLAR_ACK = 0x40;
    const int STATUS_SLAR_NACK = 0x48;
    const int STATUS_DATA_RECEIVED_ACK = 0x50;

    const int STATUS_DATA_RECEIVED_NACK = 0x58;
    // Slave receive states
    const int STATUS_SLAVE_SLAW_ACK = 0x60;      // SLA+W received, ACK returned
    const int STATUS_SLAVE_GCALL_ACK = 0x70;     // General call received, ACK returned
    const int STATUS_SLAVE_DATA_RX_ACK = 0x80;   // Data byte received, ACK returned
    const int STATUS_SLAVE_DATA_RX_NACK = 0x88;  // Data byte received, NACK returned
    const int STATUS_SLAVE_STOP = 0xA0;          // STOP or repeated START received while addressed as slave (TW_SR_STOP)
    // Slave transmit states
    const int STATUS_SLAVE_SLAR_ACK = 0xA8;      // SLA+R received, ACK returned

    public static readonly AvrTwiConfig TwiConfig = new()
    {
        TwiInterrupt = 0x30,

        TWBR = 0xb8,
        TWSR = 0xb9,
        TWAR = 0xba,
        TWDR = 0xbb,
        TWCR = 0xbc,
        TWAMR = 0xbd
    };

    /// <summary>ATmega2560 TWI: same registers as the ATmega328P; TWI is vector 39 -> word 0x4E.</summary>
    public static readonly AvrTwiConfig Mega2560TwiConfig = new()
    {
        TwiInterrupt = 0x4e,

        TWBR = 0xb8,
        TWSR = 0xb9,
        TWAR = 0xba,
        TWDR = 0xbb,
        TWCR = 0xbc,
        TWAMR = 0xbd
    };

    private readonly Cpu _cpu;
    private readonly AvrTwiConfig _config;
    private readonly uint _freqHz;

    private readonly AvrInterruptConfig _twi;

    private bool _busy = false;

    // Wire timing of the event being processed. The event "starts" when it is dispatched
    // to the handler; TWINT is raised no earlier than _eventStartCycle + _eventWireCycles.
    private ulong _eventStartCycle;
    private ulong _eventWireCycles;
    // Incremented whenever a deferred completion is scheduled or invalidated (reset), so a
    // stale clock event can never touch the state of a later event.
    private int _sequence;
    private Action? _deferred;

    // Bus time of each event, in SCL periods. START and STOP are one period each (the
    // setup/hold times of the datasheet SCL timing are about half a period apiece). An
    // address or data byte is 8 bits plus the ACK/NACK bit, so 9 SCL periods.
    const int START_SCL_PERIODS = 1;
    const int STOP_SCL_PERIODS = 1;
    const int BYTE_SCL_PERIODS = 9;

    public ITwiEventHandler EventHandler { get; set; }

    /// <summary>
    /// When true (default), each bus event (START, address, data byte, STOP) takes its wire
    /// time at <see cref="SclFrequency"/> before TWINT is raised, as on silicon. The time is
    /// counted from the moment the event is dispatched to <see cref="EventHandler"/>, so a
    /// handler that completes late (for example from an external circuit solver) is not
    /// delayed any further once the wire time has already elapsed. Set to false to complete
    /// every event as soon as the handler reports it.
    /// </summary>
    public bool EmulateBusTiming { get; set; } = true;

    /// <summary>
    /// CPU cycles of one SCL period: CPU clock / SCL frequency = 16 + 2 * TWBR * prescaler
    /// (ATmega328P datasheet, "Bit Rate Generator Unit").
    /// </summary>
    public int SclPeriodCycles => 16 + 2 * _cpu.Mmio.Data[_config.TWBR] * Prescaler;

    public int Prescaler
    {
        get
        {
            switch (_cpu.Mmio.Data[_config.TWSR] & TWSR_TWPS_MASK)
            {
                case 0:
                    return 1;
                case 1:
                    return 4;
                case 2:
                    return 16;
                case 3:
                    return 64;
                default:
                    return 0;
            }
        }
    }

    public long SclFrequency => _freqHz / (16 + 2 * _cpu.Mmio.Data[_config.TWBR] * Prescaler);

    public int Status => _cpu.Mmio.Data[_config.TWSR] & TWSR_TWS_MASK;

    /// <summary>
    /// The 7-bit slave address currently written in TWAR by the firmware (i.e. the
    /// address passed to <c>Wire.begin(address)</c>). Returns 0 when the device is
    /// not configured as a slave.
    /// </summary>
    public byte SlaveAddress => (byte)((_cpu.Mmio.Data[_config.TWAR] & TWAR_TWA_MASK) >> 1);

    /// <summary>
    /// Raised whenever the firmware writes to TWAR. The argument is the new 7-bit
    /// slave address (0 means slave mode disabled).
    /// </summary>
    public event Action<byte>? SlaveAddressChanged;

    public AvrTwi(Cpu cpu, AvrTwiConfig config, uint freqHz)
    {
        _cpu = cpu;
        _config = config;
        _freqHz = freqHz;

        EventHandler = new NoopTwiEventHandler(this);

        _twi = new AvrInterruptConfig(
            address: _config.TwiInterrupt,
            flagRegister: _config.TWCR,
            flagMask: TWCR_TWINT,
            enableRegister: _config.TWCR,
            enableMask: TWCR_TWIE
        );

        UpdateStatus(STATUS_TWI_IDLE);

        cpu.OnPeripheralReset += () =>
        {
            // Cpu.Reset already dropped the pending clock events
            _sequence++;
            _deferred = null;
            _busy = false;
            _eventWireCycles = 0;
            // TWSR resets to idle (TWS = 0xF8) and TWDR to 0xFF (datasheet register summary).
            // TWAR is left at 0, which this model reads as "not a slave".
            _cpu.Mmio.Data[_config.TWSR] = STATUS_TWI_IDLE;
            _cpu.Mmio.Data[_config.TWDR] = 0xff;
            SlaveAddressChanged?.Invoke(0);
        };

        cpu.Mmio.RegisterWrite(_config.TWAR, (value, _, _, _) =>
        {
            _cpu.Mmio.Data[_config.TWAR] = value;
            SlaveAddressChanged?.Invoke((byte)((value & TWAR_TWA_MASK) >> 1));
            return true;
        });

        cpu.Mmio.RegisterWrite(_config.TWCR, (value, _, _, _) =>
        {
            // TWINT is cleared by writing a one; writing zero must not drop a pending flag.
            _cpu.Mmio.Data[_config.TWCR] = (byte)((value & ~TWCR_TWINT) | (_cpu.Mmio.Data[_config.TWCR] & TWCR_TWINT));
            var clearInt = (value & TWCR_TWINT) != 0;
            _cpu.ClearInterruptByFlag(_twi, value);
            _cpu.UpdateInterruptEnable(_twi, value);
            if (clearInt && (value & TWCR_TWEN) != 0 && !_busy)
            {
                var twdrValue = _cpu.Mmio.Data[_config.TWDR];
                _cpu.AddClockEvent(() =>
                {
                    if ((value & TWCR_TWSTA) != 0)
                    {
                        BeginEvent(START_SCL_PERIODS);
                        EventHandler.Start(Status != STATUS_TWI_IDLE);
                    }
                    else if ((value & TWCR_TWSTO) != 0)
                    {
                        BeginEvent(STOP_SCL_PERIODS);
                        EventHandler.Stop();
                    }
                    else if (Status == STATUS_START || Status == STATUS_REPEATED_START)
                    {
                        BeginEvent(BYTE_SCL_PERIODS);
                        EventHandler.ConnectToSlave((byte)(twdrValue >> 1), (twdrValue & 0x1) == 0);
                    }
                    else if (Status == STATUS_SLAW_ACK || Status == STATUS_DATA_SENT_ACK)
                    {
                        BeginEvent(BYTE_SCL_PERIODS);
                        EventHandler.WriteByte(twdrValue);
                    }
                    else if (Status == STATUS_SLAR_ACK || Status == STATUS_DATA_RECEIVED_ACK)
                    {
                        BeginEvent(BYTE_SCL_PERIODS);
                        var ack = (value & TWCR_TWEA) != 0;
                        EventHandler.ReadByte(ack);
                    }
                }, 0);
                return true;
            }

            return true;
        });
    }

    private void BeginEvent(int sclPeriods)
    {
        _busy = true;
        _eventStartCycle = _cpu.Cycles;
        _eventWireCycles = EmulateBusTiming ? (ulong)sclPeriods * (ulong)SclPeriodCycles : 0;
    }

    /// <summary>
    /// Runs <paramref name="apply"/> now if the wire time of the current event has elapsed
    /// (or the bus is not busy with a dispatched event), otherwise at the end of the wire time.
    /// </summary>
    private void Finish(Action apply)
    {
        var remaining = _busy ? (long)(_eventStartCycle + _eventWireCycles) - (long)_cpu.Cycles : 0;
        if (remaining <= 0)
        {
            _sequence++;
            _deferred = null;
            _busy = false;
            apply();
            return;
        }

        var sequence = ++_sequence;
        _deferred = apply;
        _cpu.AddClockEvent(() =>
        {
            if (sequence != _sequence || _deferred == null) return;
            var action = _deferred;
            _deferred = null;
            _busy = false;
            action();
        }, (int)remaining);
    }

    public void CompleteStart()
    {
        Finish(() => UpdateStatus(Status == STATUS_TWI_IDLE ? STATUS_START : STATUS_REPEATED_START));
    }

    public void CompleteStop()
    {
        Finish(() =>
        {
            _cpu.Mmio.Data[_config.TWCR] &= ~TWCR_TWSTO & 0xff;
            UpdateStatus(STATUS_TWI_IDLE);
        });
    }

    /// <summary>
    /// Preloads TWDR with the address byte so that <see cref="CompleteConnect"/> can
    /// determine whether the transaction is Master Transmitter (R/W = 0) or Master
    /// Receiver (R/W = 1). In a real firmware flow the firmware writes this before
    /// triggering TWCR; call this from direct bridge code paths that bypass firmware.
    /// </summary>
    public void SetTwdr(byte value) => _cpu.Mmio.Data[_config.TWDR] = value;

    public void CompleteConnect(bool ack)
    {
        Finish(() =>
        {
            if ((_cpu.Mmio.Data[_config.TWDR] & 0x1) != 0)
            {
                UpdateStatus(ack ? STATUS_SLAR_ACK : STATUS_SLAR_NACK);
            }
            else
            {
                UpdateStatus(ack ? STATUS_SLAW_ACK : STATUS_SLAW_NACK);
            }
        });
    }

    public void CompleteWrite(bool ack)
    {
        Finish(() => UpdateStatus(ack ? STATUS_DATA_SENT_ACK : STATUS_DATA_SENT_NACK));
    }

    public void CompleteRead(byte data)
    {
        Finish(() =>
        {
            var ack = (_cpu.Mmio.Data[_config.TWCR] & TWCR_TWEA) != 0;
            _cpu.Mmio.Data[_config.TWDR] = data;
            UpdateStatus(ack ? STATUS_DATA_RECEIVED_ACK : STATUS_DATA_RECEIVED_NACK);
        });
    }

    /// <summary>
    /// Simulate an external I2C master addressing this device.
    /// Returns true when the address matches TWAR (respecting TWAMR mask and general-call flag).
    /// On match, TWSR is set to 0x60 (write) or 0xA8 (read) and TWINT is raised.
    /// </summary>
    public bool SimulateIncomingAddress (byte address, bool isWrite)
    {
        var ownAddress = (_cpu.Mmio.Data[_config.TWAR] & TWAR_TWA_MASK) >> 1;
        var generalCallEnabled = (_cpu.Mmio.Data[_config.TWAR] & TWAR_TWGCE) != 0;
        var mask = (_cpu.Mmio.Data[_config.TWAMR] & TWAR_TWA_MASK) >> 1;

        var isGeneralCall = address == 0 && generalCallEnabled;
        var addressMatch = ((address ^ ownAddress) & ~mask) == 0;

        if (!isGeneralCall && !addressMatch)
            return false;

        UpdateStatus (isGeneralCall ? STATUS_SLAVE_GCALL_ACK
                    : isWrite      ? STATUS_SLAVE_SLAW_ACK
                                   : STATUS_SLAVE_SLAR_ACK);
        return true;
    }

    /// <summary>
    /// Simulate a data byte delivered by the external master in slave-receive mode.
    /// Stores the byte in TWDR, sets TWSR to 0x80 (ACK) or 0x88 (NACK) per TWEA, and raises TWINT.
    /// </summary>
    public void SimulateIncomingData (byte data)
    {
        _cpu.Mmio.Data[_config.TWDR] = data;
        var ack = (_cpu.Mmio.Data[_config.TWCR] & TWCR_TWEA) != 0;
        UpdateStatus (ack ? STATUS_SLAVE_DATA_RX_ACK : STATUS_SLAVE_DATA_RX_NACK);
    }

    /// <summary>
    /// Simulate a STOP (or repeated START) condition from the external master while
    /// this device is addressed as slave. Sets TWSR to 0xA0 (TW_SR_STOP) and raises
    /// TWINT — this is the status on which the Arduino Wire library's ISR fires the
    /// user's onReceive callback, so slave-receiver firmware cannot complete a
    /// transaction without it.
    /// </summary>
    public void SimulateIncomingStop () => UpdateStatus (STATUS_SLAVE_STOP);

    /// <summary>
    /// Read the byte firmware placed in TWDR for slave-transmit mode.
    /// </summary>
    public byte ReadSlaveTransmitByte () => _cpu.Mmio.Data[_config.TWDR];

    private void UpdateStatus(int value)
    {
        _cpu.Mmio.Data[_config.TWSR] = (byte)((_cpu.Mmio.Data[_config.TWSR] & ~TWSR_TWS_MASK) | value);
        _cpu.SetInterruptFlag(_twi);
    }
}

public class NoopTwiEventHandler(AvrTwi twi) : ITwiEventHandler
{
    public void Start(bool repeated)
    {
        twi.CompleteStart();
    }

    public void Stop()
    {
        twi.CompleteStop();
    }

    public void ConnectToSlave(byte address, bool write)
    {
        // No device connected — always NACK
        twi.CompleteConnect(false);
    }

    public void WriteByte(byte data)
    {
        twi.CompleteWrite(false);
    }

    public void ReadByte(bool ack)
    {
        twi.CompleteRead(0xff);
    }
}

public struct AvrTwiConfig
{
    public byte TwiInterrupt { get; init; }

    public byte TWBR { get; init; }
    public byte TWCR { get; init; }
    public byte TWSR { get; init; }
    public byte TWDR { get; init; }
    public byte TWAR { get; init; }
    public byte TWAMR { get; init; }
}

public interface ITwiEventHandler
{
    void Start(bool repeated);
    void Stop();
    void ConnectToSlave(byte address, bool write);
    void WriteByte(byte data);
    void ReadByte(bool ack);
}