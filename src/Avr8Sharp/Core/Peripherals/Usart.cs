using System.Text;
using AVR8Sharp.Core;

namespace AVR8Sharp.Core.Peripherals;

public class AvrUsart
{
    const int UCSRA_RXC = 0x80; // USART Receive Complete
    const int UCSRA_TXC = 0x40; // USART Transmit Complete
    const int UCSRA_UDRE = 0x20; // USART Data Register Empty
    const int UCSRA_FE = 0x10; // Frame Error
    const int UCSRA_DOR = 0x08; // Data OverRun
    const int UCSRA_UPE = 0x04; // USART Parity Error
    const int UCSRA_U2X = 0x02; // Double the USART Transmission Speed
    const int UCSRA_MPCM = 0x01; // Multi-processor Communication Mode
    const int UCSRA_CFG_MASK = UCSRA_U2X;
    const int UCSRB_RXCIE = 0x80; // RX Complete Interrupt Enable
    const int UCSRB_TXCIE = 0x40; // TX Complete Interrupt Enable
    const int UCSRB_UDRIE = 0x20; // USART Data Register Empty Interrupt Enable
    const int UCSRB_RXEN = 0x10; // Receiver Enable
    const int UCSRB_TXEN = 0x08; // Transmitter Enable
    const int UCSRB_UCSZ2 = 0x04; // Character Size
    const int UCSRB_RXB8 = 0x02; // Receive Data Bit 8
    const int UCSRB_TXB8 = 0x01; // Transmit Data Bit 8
    const int UCSRB_CFG_MASK = UCSRB_UCSZ2 | UCSRB_RXEN | UCSRB_TXEN;
    const int UCSRC_UMSEL1 = 0x80; // USART Mode Select 1
    const int UCSRC_UMSEL0 = 0x40; // USART Mode Select 0
    const int UCSRC_UMSEL_MASK = UCSRC_UMSEL1 | UCSRC_UMSEL0;
    const int UCSRC_UPM1 = 0x20; // Parity Mode 1
    const int UCSRC_UPM0 = 0x10; // Parity Mode 0
    const int UCSRC_USBS = 0x08; // Stop Bit Select
    const int UCSRC_UCSZ1 = 0x04; // Character Size
    const int UCSRC_UCSZ0 = 0x02; // Character Size
    const int UCSRC_UCPOL = 0x01; // Clock Polarity


    public static readonly AvrUsartConfig Usart0Config = new AvrUsartConfig
    {
        RxCompleteInterrupt = 0x24,
        DataRegisterEmptyInterrupt = 0x26,
        TxCompleteInterrupt = 0x28,
        UCSRA = 0xc0,
        UCSRB = 0xc1,
        UCSRC = 0xc2,
        UBRRL = 0xc4,
        UBRRH = 0xc5,
        UDR = 0xc6,
    };

    public static Dictionary<int, int> RxMasks { get; } = new Dictionary<int, int>
    {
        { 5, 0x1f },
        { 6, 0x3f },
        { 7, 0x7f },
        { 8, 0xff },
        { 9, 0x1ff }, // 9-bit mode: preserve bit 8 (RXB8)
    };

    private readonly Cpu _cpu;
    private readonly AvrUsartConfig _config;
    private readonly uint _freqHz;

    private bool _rxBusyValue = false;
    private ushort _rxBuffer = 0;
    private readonly StringBuilder _lineBuffer = new StringBuilder();

    private readonly AvrInterruptConfig _rxc;
    private readonly AvrInterruptConfig _udre;
    private readonly AvrInterruptConfig _txc;
    
    private int _cachedCyclesPerChar;
    private int _cachedRxMask;

    private readonly Action _txCompleteAction;
    private readonly Action _rxCompleteAction;
    private ushort _incomingRxBuffer;
    private bool _incomingFrameError;
    private bool _incomingParityError;

    public Action<byte>? OnByteTransmit { get; set; } = null;
    public Action<string>? OnLineTransmit { get; set; } = null;
    public Action? OnRxComplete { get; set; } = null;
    public Action? OnConfigurationChange { get; set; } = null;

    public bool RxBusy
    {
        get { return _rxBusyValue; }
    }

    public int CyclesPerChar
    {
        get
        {
            var symbolsPerChar = 1 + BitsPerChar + StopBits + (ParityEnabled ? 1 : 0);
            return (UBRR + 1) * Multiplier * symbolsPerChar;
        }
    }

    public int UBRR
    {
        get { return _cpu.Mmio.Data[_config.UBRRL] | _cpu.Mmio.Data[_config.UBRRH] << 8; }
    }

    /// <summary>
    /// True when the USART is in synchronous master mode (UMSEL1:0 = 01). In this mode
    /// the baud divisor is f/(2·(UBRR+1)) and the U2X bit is ignored.
    /// </summary>
    public bool SyncMode
    {
        get { return (_cpu.Mmio.Data[_config.UCSRC] & UCSRC_UMSEL_MASK) == UCSRC_UMSEL0; }
    }

    public int Multiplier
    {
        get
        {
            // Synchronous mode: fixed divisor of 2 (datasheet §19.3.1). Async normal = 16,
            // async double-speed (U2X) = 8.
            if (SyncMode) return 2;
            return (_cpu.Mmio.Data[_config.UCSRA] & UCSRA_U2X) != 0 ? 8 : 16;
        }
    }

    public bool RxEnable
    {
        get { return (_cpu.Mmio.Data[_config.UCSRB] & UCSRB_RXEN) != 0; }
    }

    public bool TxEnable
    {
        get { return (_cpu.Mmio.Data[_config.UCSRB] & UCSRB_TXEN) != 0; }
    }

    public long BaudRate
    {
        get { return _freqHz / (Multiplier * (1 + UBRR)); }
    }

    public int BitsPerChar
    {
        get
        {
            var ucsz = ((_cpu.Mmio.Data[_config.UCSRC] & (UCSRC_UCSZ1 | UCSRC_UCSZ0)) >> 1) |
                       (_cpu.Mmio.Data[_config.UCSRB] & UCSRB_UCSZ2);
            switch (ucsz)
            {
                case 0:
                    return 5;
                case 1:
                    return 6;
                case 2:
                    return 7;
                case 3:
                    return 8;
                default: // 4..6 are reserved
                case 7:
                    return 9;
            }
        }
    }

    public int StopBits
    {
        get { return (_cpu.Mmio.Data[_config.UCSRC] & UCSRC_USBS) != 0 ? 2 : 1; }
    }

    public bool ParityEnabled
    {
        get { return (_cpu.Mmio.Data[_config.UCSRC] & UCSRC_UPM1) != 0; }
    }

    public bool ParityOdd
    {
        get { return (_cpu.Mmio.Data[_config.UCSRC] & UCSRC_UPM0) != 0; }
    }

    public AvrUsart(Cpu cpu, AvrUsartConfig config, uint freqHz)
    {
        _cpu = cpu;
        _config = config;
        _freqHz = freqHz;

        _rxc = new AvrInterruptConfig(
            address: _config.RxCompleteInterrupt,
            flagRegister: _config.UCSRA,
            flagMask: UCSRA_RXC,
            enableRegister: _config.UCSRB,
            enableMask: UCSRB_RXCIE,
            constant: true
        );

        _udre = new AvrInterruptConfig(
            address: _config.DataRegisterEmptyInterrupt,
            flagRegister: _config.UCSRA,
            flagMask: UCSRA_UDRE,
            enableRegister: _config.UCSRB,
            enableMask: UCSRB_UDRIE
        );

        _txc = new AvrInterruptConfig(
            address: _config.TxCompleteInterrupt,
            flagRegister: _config.UCSRA,
            flagMask: UCSRA_TXC,
            enableRegister: _config.UCSRB,
            enableMask: UCSRB_TXCIE
        );
        
        _txCompleteAction = () =>
        {
            _cpu.SetInterruptFlag(_udre);
            _cpu.SetInterruptFlag(_txc);
        };

        _rxCompleteAction = () =>
        {
            _rxBusyValue = false;
            WriteByte(_incomingRxBuffer, true, _incomingFrameError, _incomingParityError);
        };
        
        Reset();
        UpdateCalculatedValues();
        
        cpu.Mmio.RegisterWrite(_config.UCSRA, (value, oldValue, _, _) =>
        {
            _cpu.Mmio.Data[_config.UCSRA] = (byte)(value & (UCSRA_MPCM | UCSRA_U2X));
            UpdateCalculatedValues();
            _cpu.ClearInterruptByFlag(_txc, value);
            if ((value & UCSRA_CFG_MASK) != (oldValue & UCSRA_CFG_MASK))
            {
                OnConfigurationChange?.Invoke();
            }

            return true;
        });

        cpu.Mmio.RegisterWrite(_config.UCSRB, (value, oldValue, _, _) =>
        {
            _cpu.UpdateInterruptEnable(_rxc, value);
            _cpu.UpdateInterruptEnable(_udre, value);
            _cpu.UpdateInterruptEnable(_txc, value);
            if ((value & UCSRB_RXEN) != 0 && (oldValue & UCSRB_RXEN) != 0)
            {
                _cpu.ClearInterrupt(_rxc);
            }

            if ((value & UCSRB_TXEN) != 0 && (oldValue & UCSRB_TXEN) == 0)
            {
                _cpu.SetInterruptFlag(_udre);
            }

            value = (byte)((value & ~UCSRB_RXB8) | (oldValue & UCSRB_RXB8));

            _cpu.Mmio.Data[_config.UCSRB] = value;
            UpdateCalculatedValues();
            if ((value & UCSRB_CFG_MASK) != (oldValue & UCSRB_CFG_MASK))
            {
                OnConfigurationChange?.Invoke();
            }

            return true;
        });

        cpu.Mmio.RegisterWrite(_config.UCSRC, (value, _, _, _) =>
        {
            _cpu.Mmio.Data[_config.UCSRC] = value;
            UpdateCalculatedValues();
            OnConfigurationChange?.Invoke();
            return true;
        });

        _cpu.Mmio.RegisterRead(_config.UDR, _ =>
        {
            var result = _rxBuffer & _cachedRxMask & 0xFF;
            _rxBuffer = 0;
            // FE/DOR/UPE are tied to the word in the receive buffer; reading UDR consumes
            // that word, so the error flags are cleared (in a deeper FIFO they would update
            // to the next word's status — here the single buffered word is now empty).
            _cpu.Mmio.Data[_config.UCSRA] &= unchecked((byte)~(UCSRA_FE | UCSRA_DOR | UCSRA_UPE));
            _cpu.ClearInterrupt(_rxc);
            return (byte)result;
        });

        cpu.Mmio.RegisterWrite(_config.UDR, (value, _, _, _) =>
        {
            OnByteTransmit?.Invoke(value);
            if (OnLineTransmit != null)
            {
                var ch = (char)value;
                if (ch == '\n')
                {
                    OnLineTransmit(_lineBuffer.ToString());
                    _lineBuffer.Clear();
                }
                else
                {
                    _lineBuffer.Append(ch);
                }
            }

            _cpu.AddClockEvent(_txCompleteAction, _cachedCyclesPerChar);
            _cpu.ClearInterrupt(_txc);
            _cpu.ClearInterrupt(_udre);
            return false;
        });

        cpu.Mmio.RegisterWrite(_config.UBRRH, (value, _, _, _) =>
        {
            _cpu.Mmio.Data[_config.UBRRH] = value;
            UpdateCalculatedValues();
            OnConfigurationChange?.Invoke();
            return true;
        });

        cpu.Mmio.RegisterWrite(_config.UBRRL, (value, _, _, _) =>
        {
            _cpu.Mmio.Data[_config.UBRRL] = value;
            UpdateCalculatedValues();
            OnConfigurationChange?.Invoke();
            return true;
        });
    }
    
    private void UpdateCalculatedValues()
    {
        _cachedRxMask = RxMasks.GetValueOrDefault(BitsPerChar, 0xff);

        var symbolsPerChar = 1 + BitsPerChar + StopBits + (ParityEnabled ? 1 : 0);
        _cachedCyclesPerChar = (UBRR + 1) * Multiplier * symbolsPerChar;
    }

    public void Reset()
    {
        _cpu.Mmio.Data[_config.UCSRA] = UCSRA_UDRE;
        _cpu.Mmio.Data[_config.UCSRB] = 0;
        _cpu.Mmio.Data[_config.UCSRC] = UCSRC_UCSZ1 | UCSRC_UCSZ0; // default: 8 bits per byte
        _rxBusyValue = false;
        _rxBuffer = 0;
        _lineBuffer.Clear();
    }

    /// <summary>
    /// Delivers a received byte to the USART. <paramref name="frameError"/> models a stop-bit
    /// violation (FE) and <paramref name="parityError"/> a parity mismatch (UPE) for the frame.
    /// If a previously received byte has not yet been read from UDR, the new byte is lost and
    /// the Data OverRun flag (DOR) is set, per the datasheet.
    /// </summary>
    public bool WriteByte(ushort value, bool immediate = false, bool frameError = false, bool parityError = false)
    {
        if (_rxBusyValue || !RxEnable) return false;

        if (immediate)
        {
            // Overrun: a frame completed while the previous one is still unread (RXC set).
            // The buffered word is kept and the incoming word is discarded; DOR is raised.
            if ((_cpu.Mmio.Data[_config.UCSRA] & UCSRA_RXC) != 0)
            {
                _cpu.Mmio.Data[_config.UCSRA] |= UCSRA_DOR;
                OnRxComplete?.Invoke();
                return false;
            }

            _rxBuffer = value;

            // Frame Error / Parity Error accompany this word in the receive buffer and stay
            // valid until UDR is read. They are read-only to firmware, so set them directly.
            var ucsra = _cpu.Mmio.Data[_config.UCSRA];
            ucsra = frameError ? (byte)(ucsra | UCSRA_FE) : (byte)(ucsra & ~UCSRA_FE);
            ucsra = parityError ? (byte)(ucsra | UCSRA_UPE) : (byte)(ucsra & ~UCSRA_UPE);
            _cpu.Mmio.Data[_config.UCSRA] = ucsra;

            var ucsrb = _cpu.Mmio.Data[_config.UCSRB];
            if ((value & 0x100) != 0)
            {
                ucsrb |= UCSRB_RXB8;
            }
            else
            {
                ucsrb &= ~UCSRB_RXB8 & 0xFF;
            }
            _cpu.Mmio.Data[_config.UCSRB] = ucsrb;

            _cpu.SetInterruptFlag(_rxc);
            OnRxComplete?.Invoke();
        }
        else
        {
            _rxBusyValue = true;
            _incomingRxBuffer = value;
            _incomingFrameError = frameError;
            _incomingParityError = parityError;
            _cpu.AddClockEvent(_rxCompleteAction, _cachedCyclesPerChar);
        }

        return true;
    }
}

public class AvrUsartConfig
{
    public byte RxCompleteInterrupt { get; set; }
    public byte DataRegisterEmptyInterrupt { get; set; }
    public byte TxCompleteInterrupt { get; set; }

    public ushort UCSRA { get; set; }
    public ushort UCSRB { get; set; }
    public ushort UCSRC { get; set; }
    public ushort UBRRL { get; set; }
    public ushort UBRRH { get; set; }
    public ushort UDR { get; set; }
}