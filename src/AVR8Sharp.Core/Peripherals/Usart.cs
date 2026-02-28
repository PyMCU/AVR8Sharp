using System.Text;
using AVR8Sharp.Core.Cpu;

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
	const int UCSRC_UPM1 = 0x20; // Parity Mode 1
	const int UCSRC_UPM0 = 0x10; // Parity Mode 0
	const int UCSRC_USBS = 0x08; // Stop Bit Select
	const int UCSRC_UCSZ1 = 0x04; // Character Size
	const int UCSRC_UCSZ0 = 0x02; // Character Size
	const int UCSRC_UCPOL = 0x01; // Clock Polarity
	
	
	public static readonly AvrUsartConfig Usart0Config = new AvrUsartConfig {
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
	public static Dictionary<int, int> RxMasks { get; } = new Dictionary<int, int> {
		{ 5, 0x1f },
		{ 6, 0x3f },
		{ 7, 0x7f },
		{ 8, 0xff },
		{ 9, 0xff },
	};
	
	private readonly Cpu.Cpu _cpu;
	private readonly AvrUsartConfig _config;
	private readonly uint _freqHz;
	
	private bool _rxBusyValue = false;
	private byte _rxByte = 0;
	private readonly StringBuilder _lineBuffer = new StringBuilder();
	
	private readonly AvrInterruptConfig _rxc;
	private readonly AvrInterruptConfig _udre;
	private readonly AvrInterruptConfig _txc;
	
	public Action<byte>? OnByteTransmit { get; set; } = null;
	public Action<string>? OnLineTransmit { get; set; } = null;
	public Action? OnRxComplete { get; set; } = null;
	public Action? OnConfigurationChange { get; set; } = null;
	
	public bool RxBusy {
		get {
			return _rxBusyValue;
		}
	}
	public int CyclesPerChar {
		get {
			var symbolsPerChar = 1 + BitsPerChar + StopBits + (ParityEnabled ? 1 : 0);
			return (UBRR + 1) * Multiplier * symbolsPerChar;
		}
	}
	public int UBRR {
		get {
			return _cpu.Data[_config.UBRRL] | _cpu.Data[_config.UBRRH] << 8;
		}
	}
	public int Multiplier {
		get {
			return (_cpu.Data[_config.UCSRA] & UCSRA_U2X) != 0 ? 8 : 16;
		}
	}
	public bool RxEnable {
		get {
			return (_cpu.Data[_config.UCSRB] & UCSRB_RXEN) != 0;
		}
	}
	public bool TxEnable {
		get {
			return (_cpu.Data[_config.UCSRB] & UCSRB_TXEN) != 0;
		}
	}
	public long BaudRate {
		get {
			return _freqHz / (Multiplier * (1 + UBRR));
		}
	}
	public int BitsPerChar {
		get {
			var ucsz = ((_cpu.Data[_config.UCSRC] & (UCSRC_UCSZ1 | UCSRC_UCSZ0)) >> 1) | (_cpu.Data[_config.UCSRB] & UCSRB_UCSZ2);
			switch (ucsz) {
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
	public int StopBits {
		get {
			return (_cpu.Data[_config.UCSRC] & UCSRC_USBS) != 0 ? 2 : 1;
		}
	}
	public bool ParityEnabled {
		get {
			return (_cpu.Data[_config.UCSRC] & UCSRC_UPM1) != 0;
		}
	}
	public bool ParityOdd {
		get {
			return (_cpu.Data[_config.UCSRC] & UCSRC_UPM0) != 0;
		}
	}

	public AvrUsart (Cpu.Cpu cpu, AvrUsartConfig config, uint freqHz)
	{
		_cpu = cpu;
		_config = config;
		_freqHz = freqHz;

		_rxc = new AvrInterruptConfig (
			address: _config.RxCompleteInterrupt,
			flagRegister: _config.UCSRA,
			flagMask: UCSRA_RXC,
			enableRegister: _config.UCSRB,
			enableMask: UCSRB_RXCIE,
			constant: true
		);
		
		_udre = new AvrInterruptConfig (
			address: _config.DataRegisterEmptyInterrupt,
			flagRegister: _config.UCSRA,
			flagMask: UCSRA_UDRE,
			enableRegister: _config.UCSRB,
			enableMask: UCSRB_UDRIE
		);
		
		_txc = new AvrInterruptConfig (
			address: _config.TxCompleteInterrupt,
			flagRegister: _config.UCSRA,
			flagMask: UCSRA_TXC,
			enableRegister: _config.UCSRB,
			enableMask: UCSRB_TXCIE
		);
		
		Reset ();
		
		_cpu.WriteHooks[_config.UCSRA] = (value, oldValue, _, _) => {
			_cpu.Data[_config.UCSRA] = (byte)(value & (UCSRA_MPCM | UCSRA_U2X));
			_cpu.ClearInterruptByFlag (_txc, value);
			if ((value & UCSRA_CFG_MASK) != (oldValue & UCSRA_CFG_MASK)) {
				OnConfigurationChange?.Invoke ();
			}
			return true;
		};
		
		_cpu.WriteHooks[_config.UCSRB] = (value, oldValue, _, _) => {
			_cpu.UpdateInterruptEnable (_rxc, value);
			_cpu.UpdateInterruptEnable (_udre, value);
			_cpu.UpdateInterruptEnable (_txc, value);
			if ((value & UCSRB_RXEN) != 0 && (oldValue & UCSRB_RXEN) != 0) {
				_cpu.ClearInterrupt (_rxc);
			}
			if ((value & UCSRB_TXEN) != 0 && (oldValue & UCSRB_TXEN) == 0) {
				_cpu.SetInterruptFlag (_udre);
			}
			_cpu.Data[_config.UCSRB] = value;
			if ((value & UCSRB_CFG_MASK) != (oldValue & UCSRB_CFG_MASK)) {
				OnConfigurationChange?.Invoke ();
			}
			return true;
		};
		
		_cpu.WriteHooks[_config.UCSRC] = (value, _, _, _) => {
			_cpu.Data[_config.UCSRC] = value;
			OnConfigurationChange?.Invoke ();
			return true;
		};
		
		_cpu.ReadHooks[_config.UDR] = _ => {
			var mask = RxMasks.TryGetValue (BitsPerChar, out var m) ? m : 0xff;
			var result = _rxByte & mask;
			_rxByte = 0;
			_cpu.ClearInterrupt (_rxc);
			return (byte)result;
		};
		
		_cpu.WriteHooks[_config.UDR] = (value, _, _, _) => {
			OnByteTransmit?.Invoke (value);
			if (OnLineTransmit != null) {
				var ch = (char)value;
				if (ch == '\n') {
					OnLineTransmit (_lineBuffer.ToString ());
					_lineBuffer.Clear ();
				} else {
					_lineBuffer.Append (ch);
				}
			}
			_cpu.AddClockEvent (() => {
				_cpu.SetInterruptFlag (_udre);
				_cpu.SetInterruptFlag (_txc);
			}, CyclesPerChar);
			_cpu.ClearInterrupt (_txc);
			_cpu.ClearInterrupt (_udre);
			return false;
		};
		
		_cpu.WriteHooks[_config.UBRRH] = (value, _, _, _) => {
			_cpu.Data[_config.UBRRH] = value;
			OnConfigurationChange?.Invoke ();
			return true;
		};
		
		_cpu.WriteHooks[_config.UBRRL] = (value, _, _, _) => {
			_cpu.Data[_config.UBRRL] = value;
			OnConfigurationChange?.Invoke ();
			return true;
		};
	}
	
	public void Reset ()
	{
		_cpu.Data[_config.UCSRA] = UCSRA_UDRE;
		_cpu.Data[_config.UCSRB] = 0;
		_cpu.Data[_config.UCSRC] = UCSRC_UCSZ1 | UCSRC_UCSZ0; // default: 8 bits per byte
		_rxBusyValue = false;
		_rxByte = 0;
		_lineBuffer.Clear ();
	}
	
	public bool WriteByte (byte value, bool immediate = false)
	{
		if (_rxBusyValue || !RxEnable) {
			return false;
		}
		if (immediate) {
			_rxByte = value;
			_cpu.SetInterruptFlag (_rxc);
			OnRxComplete?.Invoke ();
		} else {
			_rxBusyValue = true;
			_cpu.AddClockEvent (() => {
				_rxBusyValue = false;
				WriteByte (value, true);
			}, CyclesPerChar);
			return true;
		}
		return false;
	}
}

public class AvrUsartConfig
{
	public byte RxCompleteInterrupt { get; set; }
	public byte DataRegisterEmptyInterrupt { get; set; }
	public byte TxCompleteInterrupt { get; set; }
	
	public byte UCSRA { get; set; }
	public byte UCSRB { get; set; }
	public byte UCSRC { get; set; }
	public byte UBRRL { get; set; }
	public byte UBRRH { get; set; }
	public byte UDR { get; set; }
}
