using AVR8Sharp.Core.Peripherals;
using AVR8Sharp.Core.Utils;

namespace AVR8Sharp.Core;

public class AvrBuilder (AvrRunner @base, int flashSize = 0x8000)
{
	readonly AvrRunner @base = @base;
	public static AvrBuilder Create (int flashSize = 0x8000, int ramSize = 8192)
	{
		return new AvrBuilder (new AvrRunner (new byte[flashSize], ramSize), flashSize);
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
		return this;
	}
	
	public AvrBuilder AddUsart(AvrUsartConfig config, out AvrUsart usart)
	{
		usart = new AvrUsart (@base.Cpu, config, @base.Speed);
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
		return this;
	}
	
	public AvrBuilder AddTwi(AvrTwiConfig config, out AvrTwi twim)
	{
		twim = new AvrTwi (@base.Cpu, config, @base.Speed);
		return this;
	}
	
	public AvrBuilder AddEeprom(AvrEepromConfig config, IEepromBackend backend, out AvrEeprom eeprom)
	{
		eeprom = new AvrEeprom (@base.Cpu, backend, config);
		return this;
	}
	
	public AvrBuilder AddAdc(AvrAdcConfig config, out AvrAdc adc)
	{
		adc = new AvrAdc (@base.Cpu, config);
		return this;
	}
	
	public AvrBuilder AddWatchdog(AvrWatchdogConfig config, AvrClock clock, out AvrWatchdog watchdog)
	{
		watchdog = new AvrWatchdog (@base.Cpu, config, clock);
		return this;
	}

	public AvrBuilder UseLutDecoder()
	{
		@base.SetDecoder(DecoderType.Lut);
		return this;
	}

	public AvrBuilder UseNativeDecoder()
	{
		@base.SetDecoder(DecoderType.NativeLut);
		return this;
	}

	public AvrBuilder UseSwitchDecoder()
	{
		@base.SetDecoder(DecoderType.Switch);
		return this;
	}
	
	public AvrRunner Build()
	{
		return @base;
	}
}
