using AVR8Sharp.Core.Cpu;

namespace AVR8Sharp.Core.Peripherals;

public class AvrSpi
{
	const int SPCR_SPIE = 0x80; // SPI Interrupt Enable
	const int SPCR_SPE = 0x40; // SPI Enable
	const int SPCR_DORD = 0x20; // Data Order (0:MSB first / 1:LSB first)
	const int SPCR_MSTR = 0x10; // Master/Slave select
	const int SPCR_CPOL = 0x08; // Clock Polarity
	const int SPCR_CPHA = 0x04; // Clock Phase
	const int SPCR_SPR1 = 0x02; // SPI Clock Rate Select 1
	const int SPCR_SPR0 = 0x01; // SPI Clock Rate Select 0
	const int SPSR_SPR_MASK = SPCR_SPR1 | SPCR_SPR0; // SPI Clock Rate Select Mask
	
	const int SPSR_SPIF = 0x80; // SPI Interrupt Flag
	const int SPSR_WCOL = 0x40; // Write COLlision Flag
	const int SPSR_SPI2X = 0x01; // Double SPI Speed Bit
	
	const byte BitsPerByte = 8;
	
	public static readonly AvrSpiConfig SpiConfig = new AvrSpiConfig {
		SpiInterrupt = 0x22,
		
		SPCR = 0x4c,
		SPSR = 0x4d,
		SPDR = 0x4e
	};
	
	readonly Cpu.Cpu _cpu;
	readonly AvrSpiConfig _config;
	readonly uint _freqHz;
	
	bool _transmissionActive = false;
	byte _shiftRegister = 0;

	readonly AvrInterruptConfig _spi;

	public Func<byte, int> OnTransfer { get; set; }
	public Action<byte> OnByte { get; set; }
	public bool IsMaster {
		get {
			return (_cpu.Mmio.Data[_config.SPCR] & SPCR_MSTR) != 0;
		}
	}
	public SpiDataOrder DataOrder {
		get {
			return (_cpu.Mmio.Data[_config.SPCR] & SPCR_DORD) != 0 ? SpiDataOrder.LsbFirst : SpiDataOrder.MsbFirst;
		}
	}
	public int SpiMode {
		get {
			var cpha = _cpu.Mmio.Data[_config.SPCR] & SPCR_CPHA;
			var cpol = _cpu.Mmio.Data[_config.SPCR] & SPCR_CPOL;
			return (cpha != 0 ? 2 : 0) | (cpol != 0 ? 1 : 0);
		}
	}
	public int ClockDivider {
		get {
			var baseDivider = (_cpu.Mmio.Data[_config.SPSR] & SPSR_SPI2X) != 0 ? 2 : 4;
			switch (_cpu.Mmio.Data[_config.SPCR] & SPSR_SPR_MASK) {
				case 0b00:
					return baseDivider;
				case 0b01:
					return baseDivider * 4;
				case 0b10:
					return baseDivider * 16;
				case 0b11:
					return baseDivider * 32;
				default:
					return 0;
			}
		}
	}
	public int TransferCycles {
		get {
			return BitsPerByte * ClockDivider;
		}
	}
	public long SpiFrequency {
		get {
			return _freqHz / ClockDivider;
		}
	}

	public AvrSpi (Cpu.Cpu cpu, AvrSpiConfig config, uint freqHz)
	{
		_cpu = cpu;
		_config = config;
		_freqHz = freqHz;
		
		_spi = new AvrInterruptConfig (
			address: _config.SpiInterrupt,
			flagRegister: _config.SPSR,
			flagMask: SPSR_SPIF,
			enableRegister: _config.SPCR,
			enableMask: SPCR_SPIE
		);
		
		OnByte = value => {
			var valueIn = OnTransfer?.Invoke(value) ?? 0;
			_cpu.AddClockEvent (() => CompleteTransfer (valueIn), TransferCycles);
		};
		
		OnTransfer = _ => 0;
		
		cpu.Mmio.RegisterWrite(_config.SPDR, (value,_ ,_ ,_ ) => {
			if ((_cpu.Mmio.Data[_config.SPCR] & SPCR_SPE) == 0) {
				// SPI not enabled, ignore write
				return false;
			}
			
			// Write collision
			if (_transmissionActive) {
				_cpu.Mmio.Data[_config.SPSR] |= SPSR_WCOL;
				return true;
			}
			
			// Clear write collision / interrupt flags
			_cpu.Mmio.Data[_config.SPSR] &= ~SPSR_WCOL & 0xFF;
			_cpu.ClearInterrupt (_spi);
			
			_transmissionActive = true;
			OnByte(value);
			return true;
		});
		
		cpu.Mmio.RegisterWrite(_config.SPCR, (value, _, _, _) => {
			_cpu.UpdateInterruptEnable (_spi, value);
			return false;
		});

		cpu.Mmio.RegisterWrite(_config.SPSR, (value, _, _, _) =>
		{
			_cpu.Mmio.Data[_config.SPSR] = value;
			_cpu.ClearInterruptByFlag(_spi, value);
			return false;
		});
	}

	public void CompleteTransfer (int receivedByte)
	{
		// Two-stage model: shift register receives bits during transfer,
		// then the byte is moved to SPDR (the read buffer) on completion.
		_shiftRegister = (byte)receivedByte;
		_cpu.Mmio.Data[_config.SPDR] = _shiftRegister;
		_cpu.SetInterruptFlag (_spi);
		_transmissionActive = false;
	}
}

public class AvrSpiConfig
{
	public byte SpiInterrupt { get; set; }
	
	public byte SPCR { get; set; }
	public byte SPSR { get; set; }
	public byte SPDR { get; set; }
}

public enum SpiDataOrder
{
	MsbFirst,
	LsbFirst
}
