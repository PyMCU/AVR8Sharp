using AVR8Sharp.Core.Abstractions;
using AVR8Sharp.Core.Peripherals;
using AVR8Sharp.Core.Utils;

namespace AVR8Sharp.Core;

public class AvrBuilder (AvrRunner @base, int flashSize = 0x8000)
{
	readonly AvrRunner @base = @base;

	// What the Add* methods mounted, so BuildMcu can wrap the machine without being told again
	readonly List<AvrTimer> _timers = [];
	readonly List<AvrUsart> _usarts = [];
	readonly List<AvrSpi> _spis = [];
	readonly List<AvrTwi> _twis = [];
	AvrAdc? _adc;
	AvrClock? _clock;
	AvrWatchdog? _watchdog;

	public static AvrBuilder Create (int flashSize = 0x8000, int ramSize = 8192)
	{
		return new AvrBuilder (new AvrRunner (new byte[flashSize], ramSize), flashSize);
	}
	
	/// <summary>Sets the chip's first SRAM address (see <see cref="Cpu.RamStart"/>).</summary>
	public AvrBuilder WithRamStart (int ramStart)
	{
		@base.Cpu.RamStart = ramStart;
		return this;
	}

	public AvrBuilder SetSpeed(uint speed)
	{
		@base.SetSpeed (speed);
		return this;
	}
	
	public AvrBuilder SetWorkUnitCycles(int cycles)
	{
		@base.SetWorkUnitCycles (cycles);
		return this;
	}
	
	public AvrBuilder SetHex (string hex)
	{
		@base.LoadHex (hex);
		return this;
	}
	
	public AvrBuilder AddGpioPort(AvrPortConfig config, out AvrIoPort port)
	{
		port = new AvrIoPort (@base.Cpu, config);
		return this;
	}
	
	public AvrBuilder AddTimer(AvrTimerConfig config, out AvrTimer timer)
	{
		timer = new AvrTimer (@base.Cpu, config);
		_timers.Add (timer);
		return this;
	}
	
	public AvrBuilder AddUsart(AvrUsartConfig config, out AvrUsart usart)
	{
		usart = new AvrUsart (@base.Cpu, config, @base.Speed);
		_usarts.Add (usart);
		return this;
	}
	
	public AvrBuilder AddUsi(AvrIoPort portObj, int portPin, int dataPin, int clockPin, out AvrUsi usi)
	{
		usi = new AvrUsi (@base.Cpu, portObj, portPin, dataPin, clockPin);
		return this;
	}
	
	public AvrBuilder AddSpi(AvrSpiConfig config, out AvrSpi spi)
	{
		spi = new AvrSpi (@base.Cpu, config, @base.Speed);
		_spis.Add (spi);
		return this;
	}
	
	public AvrBuilder AddTwi(AvrTwiConfig config, out AvrTwi twim)
	{
		twim = new AvrTwi (@base.Cpu, config, @base.Speed);
		_twis.Add (twim);
		return this;
	}
	
	public AvrBuilder AddEeprom(AvrEepromConfig config, IEepromBackend backend, out AvrEeprom eeprom)
	{
		eeprom = new AvrEeprom (@base.Cpu, backend, config, @base.Speed);
		return this;
	}
	
	public AvrBuilder AddAdc(AvrAdcConfig config, out AvrAdc adc)
	{
		adc = new AvrAdc (@base.Cpu, config);
		_adc = adc;
		return this;
	}
	
	public AvrBuilder AddClock(AvrClockConfig config, out AvrClock clock)
	{
		clock = new AvrClock (@base.Cpu, @base.Speed, config);
		_clock = clock;
		return this;
	}

	public AvrBuilder AddWatchdog(AvrWatchdogConfig config, AvrClock clock, out AvrWatchdog watchdog)
	{
		watchdog = new AvrWatchdog (@base.Cpu, config, clock);
		_watchdog = watchdog;
		_clock ??= clock;
		return this;
	}

	public AvrBuilder UseLutDecoder()
	{
		@base.SetDecoder(DecoderType.Lut);
		return this;
	}

	public AvrBuilder UseNativeLutDecoder()
	{
		@base.SetDecoder(DecoderType.NativeLut);
		return this;
	}

	public AvrBuilder UseSwitchDecoder()
	{
		@base.SetDecoder(DecoderType.Switch);
		return this;
	}
	
	/// <summary>
	/// Builds the machine and wraps it as an <see cref="AvrMcu"/> (SiliconTwin contracts) with
	/// every peripheral mounted through this builder. USARTs, SPIs and TWIs are indexed in the
	/// order they were added.
	/// </summary>
	public AvrMcu BuildMcu(string name = "avr")
	{
		return new AvrMcu (@base, name, _clock, _watchdog, _adc, _timers, _usarts, _spis, _twis);
	}

	public AvrRunner Build()
	{
		return @base;
	}
}
