using AVR8Sharp.Core.Cpu;

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
    // Slave states
    // TODO: Implement slave states

    public static readonly AvrTwiConfig TwiConfig = new AvrTwiConfig
    {
        TwiInterrupt = 0x30,

        TWBR = 0xb8,
        TWSR = 0xb9,
        TWAR = 0xba,
        TWDR = 0xbb,
        TWCR = 0xbc,
        TWAMR = 0xbd
    };

    private readonly Cpu.Cpu _cpu;
    private readonly AvrTwiConfig _config;
    private readonly uint _freqHz;

    private readonly AvrInterruptConfig _twi;

    private bool _busy = false;

    public ITwiEventHandler EventHandler { get; set; }

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

    public long SclFrequency
    {
        get { return _freqHz / (16 + 2 * _cpu.Mmio.Data[_config.TWBR] * Prescaler); }
    }

    public int Status
    {
        get { return _cpu.Mmio.Data[_config.TWSR] & TWSR_TWS_MASK; }
    }

    public AvrTwi(Cpu.Cpu cpu, AvrTwiConfig config, uint freqHz)
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

        this.UpdateStatus(STATUS_TWI_IDLE);

        cpu.Mmio.RegisterWrite(_config.TWCR, (value, _, _, _) =>
        {
            _cpu.Mmio.Data[_config.TWCR] = value;
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
                        _busy = true;
                        EventHandler.Start(Status != STATUS_TWI_IDLE);
                    }
                    else if ((value & TWCR_TWSTO) != 0)
                    {
                        _busy = true;
                        EventHandler.Stop();
                    }
                    else if (Status == STATUS_START || Status == STATUS_REPEATED_START)
                    {
                        _busy = true;
                        EventHandler.ConnectToSlave((byte)(twdrValue >> 1), (twdrValue & 0x1) == 0);
                    }
                    else if (Status == STATUS_SLAW_ACK || Status == STATUS_DATA_SENT_ACK)
                    {
                        _busy = true;
                        EventHandler.WriteByte(twdrValue);
                    }
                    else if (Status == STATUS_SLAR_ACK || Status == STATUS_DATA_RECEIVED_ACK)
                    {
                        _busy = true;
                        var ack = (value & TWCR_TWEA) != 0;
                        EventHandler.ReadByte(ack);
                    }
                }, 0);
                return true;
            }

            return false;
        });
    }

    public void CompleteStart()
    {
        _busy = false;
        this.UpdateStatus(Status == STATUS_TWI_IDLE ? STATUS_START : STATUS_REPEATED_START);
    }

    public void CompleteStop()
    {
        _busy = false;
        _cpu.Mmio.Data[_config.TWCR] &= ~TWCR_TWSTO & 0xff;
        this.UpdateStatus(STATUS_TWI_IDLE);
    }

    public void CompleteConnect(byte address, bool read)
    {
        _busy = false;
        if ((_cpu.Mmio.Data[_config.TWDR] & 0x1) != 0)
        {
            this.UpdateStatus(read ? STATUS_SLAR_ACK : STATUS_SLAR_NACK);
        }
        else
        {
            this.UpdateStatus(read ? STATUS_SLAW_ACK : STATUS_SLAW_NACK);
        }
    }

    public void CompleteWrite(byte data)
    {
        _busy = false;
        this.UpdateStatus((data & 0x1) != 0 ? STATUS_DATA_SENT_ACK : STATUS_DATA_SENT_NACK);
    }

    public void CompleteRead(byte data)
    {
        _busy = false;
        var ack = (_cpu.Mmio.Data[_config.TWCR] & TWCR_TWEA) != 0;
        _cpu.Mmio.Data[_config.TWDR] = data;
        this.UpdateStatus(ack ? STATUS_DATA_RECEIVED_ACK : STATUS_DATA_RECEIVED_NACK);
    }

    private void UpdateStatus(int value)
    {
        _cpu.Mmio.Data[_config.TWSR] = (byte)((_cpu.Mmio.Data[_config.TWSR] & ~TWSR_TWS_MASK) | value);
        _cpu.SetInterruptFlag(_twi);
    }
}

public class NoopTwiEventHandler : ITwiEventHandler
{
    private readonly AvrTwi _twi;

    public NoopTwiEventHandler(AvrTwi twi)
    {
        _twi = twi;
    }

    public void Start(bool repeated)
    {
        _twi.CompleteStart();
    }

    public void Stop()
    {
        _twi.CompleteStop();
    }

    public void ConnectToSlave(byte address, bool read)
    {
        // No device connected — always NACK
        _twi.CompleteConnect(address, false);
    }

    public void WriteByte(byte data)
    {
        _twi.CompleteWrite(data);
    }

    public void ReadByte(bool ack)
    {
        _twi.CompleteRead(0xff);
    }
}

public class AvrTwiConfig
{
    public byte TwiInterrupt { get; set; }

    public byte TWBR { get; set; }
    public byte TWCR { get; set; }
    public byte TWSR { get; set; }
    public byte TWDR { get; set; }
    public byte TWAR { get; set; }
    public byte TWAMR { get; set; }
}

public interface ITwiEventHandler
{
    void Start(bool repeated);
    void Stop();
    void ConnectToSlave(byte address, bool read);
    void WriteByte(byte data);
    void ReadByte(bool ack);
}