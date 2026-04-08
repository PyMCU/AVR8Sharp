using AVR8Sharp.Core.Cpu;

namespace AVR8Sharp.Core.Peripherals;

public class AvrIoPort
{
	public static readonly AvrExternalInterrupt INT0 = new AvrExternalInterrupt (
		eicr: 0x69,
		eimsk: 0x3d,
		eifr: 0x3c,
		iscOffset: 0,
		index: 0,
		interrupt: 2
	);
	
	public static readonly AvrExternalInterrupt INT1 = new AvrExternalInterrupt (
		eicr: 0x69,
		eimsk: 0x3d,
		eifr: 0x3c,
		iscOffset: 2,
		index: 1,
		interrupt: 4
	);
	
	public static readonly AvrPinChangeInterrupt PCINT0 = new AvrPinChangeInterrupt (
		pcie: 0,
		pcicr: 0x68,
		pcifr: 0x3b,
		pcmsk: 0x6b,
		pinChangeInterrupt: 6,
		mask: 0xFF,
		offset: 0
	);

	public static readonly AvrPinChangeInterrupt PCINT1 = new AvrPinChangeInterrupt (
		pcie:1,
		pcicr:0x68,
		pcifr:0x3b,
		pcmsk:0x6c,
		pinChangeInterrupt:8,
		mask:0xFF,
		offset:0
	);
	
	public static readonly AvrPinChangeInterrupt PCINT2 = new AvrPinChangeInterrupt (
		pcie: 2,
		pcicr: 0x68,
		pcifr: 0x3b,
		pcmsk: 0x6c,
		pinChangeInterrupt: 10,
		mask: 0xFF,
		offset: 0
	);
	
	public static readonly AvrPortConfig PortAConfig = new AvrPortConfig (
		pin: 0x20,
		ddr: 0x21,
		port: 0x22,
		externalInterrupts: []
	);
	
	public static readonly AvrPortConfig PortBConfig = new AvrPortConfig (
		pin: 0x23,
		ddr: 0x24,
		port: 0x25,
		
		// Interrupt settings
		pinChange: PCINT0,
		externalInterrupts: []
	);
	
	public static readonly AvrPortConfig PortCConfig = new AvrPortConfig (
		pin: 0x26,
		ddr: 0x27,
		port: 0x28,
		
		// Interrupt settings
		pinChange: PCINT1,
		externalInterrupts: []
	);
	
	public static readonly AvrPortConfig PortDConfig = new AvrPortConfig (
		pin: 0x29,
		ddr: 0x2a,
		port: 0x2b,
		
		// Interrupt settings
		pinChange: PCINT2,
		externalInterrupts: [null, null, INT0, INT1, ]
	);

	public static readonly AvrPortConfig PortEConfig = new AvrPortConfig (
		pin: 0x2c,
		ddr: 0x2d,
		port: 0x2e,
		externalInterrupts: []
	);
	
	public static readonly AvrPortConfig PortFConfig = new AvrPortConfig (
		pin: 0x2f,
		ddr: 0x30,
		port: 0x31,
		externalInterrupts: []
	);
	
	public static readonly AvrPortConfig PortGConfig = new AvrPortConfig (
		pin: 0x32,
		ddr: 0x33,
		port: 0x34,
		externalInterrupts: []
	);
	
	public static readonly AvrPortConfig PortHConfig = new AvrPortConfig (
		pin: 0x100,
		ddr: 0x101,
		port: 0x102,
		externalInterrupts: []
	);
	
	public static readonly AvrPortConfig PortJConfig = new AvrPortConfig (
		pin: 0x103,
		ddr: 0x104,
		port: 0x105,
		externalInterrupts: []
	);
	
	public static readonly AvrPortConfig PortKConfig = new AvrPortConfig (
		pin: 0x106,
		ddr: 0x107,
		port: 0x108,
		externalInterrupts: []
	);
	
	public static readonly AvrPortConfig PortLConfig = new AvrPortConfig (
		pin: 0x109,
		ddr: 0x10a,
		port: 0x10b,
		externalInterrupts: []
	);
	
	private readonly List<AvrInterruptConfig?> _externalInts = [];
	public event Action<byte, byte>? OnGpioChange;
	private readonly AvrInterruptConfig? _pcint;
	private readonly Cpu.Cpu _cpu;
	private readonly AvrPortConfig _portConfig;
	private int _pinValue;
	private byte _overrideMask = 0xff;
	private byte _overrideValue = 0;
	private byte _lastValue = 0;
	private byte _lastDdr = 0;
	private byte _lastPin = 0;

	public byte OpenCollector { get; set; } = 0;

	public Action<bool>?[] ExternalClockListeners { get; } = new Action<bool>?[8];
	
	public Action<byte, PinOverrideMode> TimerOverridePin { get; set; }

	public AvrIoPort (Cpu.Cpu cpu, AvrPortConfig portConfig)
	{
		_cpu = cpu;
		_portConfig = portConfig;
		
		_cpu.GpioPorts.Add (this);
		_cpu.GpioByPort[_portConfig.PORT] = this;
		
		cpu.Mmio.RegisterWrite(portConfig.DDR, (value, _, _, _) => {
			var portValue = _cpu.Mmio.Data[portConfig.PORT];
			_cpu.Mmio.Data[portConfig.DDR] = value;
			WriteGpio (portValue, value);
			UpdatePinRegister (value);
			return true;
		});
		
		cpu.Mmio.RegisterWrite(portConfig.PORT, (value, _, _, _) => {
			var ddrMask = _cpu.Mmio.Data[portConfig.DDR];
			_cpu.Mmio.Data[portConfig.PORT] = value;
			WriteGpio (value, ddrMask);
			UpdatePinRegister (ddrMask);
			return true;
		});

		cpu.Mmio.RegisterWrite(portConfig.PIN, (value, _, _, mask) =>
		{
			// Writing to 1 PIN toggles PORT bits
			var oldPortValue = _cpu.Mmio.Data[portConfig.PORT];
			var ddrMask = _cpu.Mmio.Data[portConfig.DDR];
			var portValue = (byte)(oldPortValue ^ (value & mask));
			_cpu.Mmio.Data[portConfig.PORT] = portValue;
			WriteGpio(portValue, ddrMask);
			UpdatePinRegister(ddrMask);
			return true;
		});
		
		// External interrupts

		if (portConfig.ExternalInterrupts != null) {
			_externalInts = portConfig.ExternalInterrupts.Select (externalConfig => {
				if (externalConfig != null) {
					return new AvrInterruptConfig (
						address: externalConfig.Interrupt,
						flagRegister: externalConfig.EIFR,
						flagMask: (byte)(1 << externalConfig.Index),
						enableRegister: externalConfig.EIMSK,
						enableMask: (byte)(1 << externalConfig.Index)
					);
				}
				return null;
			}).ToList ();
			
			AssignExternalInterrupts (portConfig.ExternalInterrupts);
		}
		
		_pcint = portConfig.PinChange != null ? new AvrInterruptConfig (
			address: portConfig.PinChange.PinChangeInterrupt,
			flagRegister: portConfig.PinChange.PCIFR,
			flagMask: 1 << portConfig.PinChange.PCIE,
			enableRegister: portConfig.PinChange.PCICR,
			enableMask: 1 << portConfig.PinChange.PCIE
		) : null;
		
		if (portConfig.PinChange != null) {
			var pcifr = portConfig.PinChange.PCIFR;
			cpu.Mmio.RegisterWrite(pcifr, DelegateWritePcifr);
			
			var pcmsk = portConfig.PinChange.PCMSK;
			cpu.Mmio.RegisterWrite(pcmsk, DelegateWritePcmsk);
		}
		
		// Move here to be able to test the TimerOverridePin
		TimerOverridePin = DelegateTimerOverridePin;
	}

	private void AssignExternalInterrupts (AvrExternalInterrupt?[] externalInts)
	{
		var eicr = new HashSet<byte> (externalInts.Select (item => item?.EICR ?? 0));
		foreach (var eicrx in eicr) {
			if (eicrx != 0)
				AttachInterruptHook (eicrx);
		}
		
		var eimsk = externalInts.FirstOrDefault (item => item != null && item.EIMSK != 0)?.EIMSK ?? 0;
		if (eimsk != 0) {
			AttachInterruptHook (eimsk, "mask");
		}
		
		var eifr = externalInts.FirstOrDefault (item => item != null && item.EIFR != 0)?.EIFR ?? 0;
		if (eifr != 0) {
			AttachInterruptHook (eifr, "flag");
		}
	}
	
	private bool DelegateWritePcifr(byte value, byte oldValue, ushort v1, byte v2)
	{
		if (_portConfig.PinChange == null) return false;
    
		foreach (var gpio in _cpu.GpioPorts) 
		{
			if (gpio._pcint != null) 
			{
				_cpu.ClearInterruptByFlag(gpio._pcint, value);
			}
		}
		return true;
	}

	private bool DelegateWritePcmsk(byte value, byte oldValue, ushort v1, byte v2)
	{
		if (_portConfig.PinChange == null || _pcint == null) return false;

		_cpu.Mmio.Data[_portConfig.PinChange.PCMSK] = value;
		// Re-evaluate only this port group's interrupt enable using PCICR (not PCMSK).
		// Previously iterated all GPIO ports with the PCMSK value, which incorrectly
		// cleared/queued other port groups' PCINT interrupts.
		_cpu.UpdateInterruptEnable(_pcint, _cpu.Mmio.Data[_portConfig.PinChange.PCICR]);
		return true;
	}
	
	private void DelegateTimerOverridePin (byte pin, PinOverrideMode mode)
	{
		var bitMask = 1 << pin;
		if (mode == PinOverrideMode.None) {
			_overrideMask |= (byte)bitMask;
			_overrideValue &= (byte)~bitMask;
		} else {
			_overrideMask &= (byte)~bitMask;
			switch (mode) {
				case PinOverrideMode.Enable:
					_overrideValue &= (byte)~bitMask;
					_overrideValue |= (byte)(_cpu.Mmio.Data[_portConfig.PORT] & bitMask);
					break;
				case PinOverrideMode.Set:
					_overrideValue |= (byte)bitMask;
					break;
				case PinOverrideMode.Clear:
					_overrideValue &= (byte)~bitMask;
					break;
				case PinOverrideMode.Toggle:
					_overrideValue ^= (byte)bitMask;
					break;
			}
		}
		
		var ddrMask = _cpu.Mmio.Data[_portConfig.DDR];
		WriteGpio (_cpu.Mmio.Data[_portConfig.PORT], ddrMask);
		UpdatePinRegister (ddrMask);
	}
	
	public void AddListener (Action<byte, byte> listener) => OnGpioChange += listener;
	
	public void RemoveListener (Action<byte, byte> listener) => OnGpioChange -= listener;

	/// <summary>
	/// Get the state of a given pin
	/// </summary>
	/// <param name="index">Pin index to return from 0 to 7</param>
	/// <returns>inState.Low or PinState.High if the pin is set to output, PinState.Input if the pin is set
	/// to input, and PinState.InputPullUp if the pin is set to input and the internal pull-up resistor has
	/// been enabled.</returns>
	public PinState GetPinState (byte index)
	{
		var ddr = _cpu.Mmio.Data[_portConfig.DDR];
		var port = _cpu.Mmio.Data[_portConfig.PORT];
		var bitMask = (byte)(1 << index);
		var openState = (port & bitMask) != 0 ? PinState.InputPullup : PinState.Input;
		var highValue = (OpenCollector & bitMask) != 0 ? openState : PinState.High;
		if ((ddr & bitMask) != 0) {
			return (_lastValue & bitMask) != 0 ? highValue : PinState.Low;
		}
		return openState;
	}

	/// <summary>
	/// Sets the input value for the given pin. This is the value that
	/// will be returned when reading from the PIN register.
	/// </summary>
	/// <param name="index">Pin index to set from 0 to 7</param>
	/// <param name="value">The value to set</param>
	public void SetPinValue (byte index, bool value)
	{ 
		var bitMask = 1 << index;
		_pinValue &= ~bitMask;
		if (value) {
			_pinValue |= bitMask;
		}
		UpdatePinRegister (_cpu.Mmio.Data[_portConfig.DDR]);
	}
	
	private void UpdatePinRegister (byte ddr)
	{
		var newPin = (byte)(((_pinValue & ~ddr) | (_lastValue & ddr)) & 0xff);
		_cpu.Mmio.Data[_portConfig.PIN] = newPin;
		if (_lastPin == newPin) return;
		for (var index = 0; index < 8; index++)
		{
			if (((newPin & (1 << index)) == (_lastPin & (1 << index)))) continue;
			var value = (newPin & (1 << index)) != 0;
			ToggleInterrupt ((byte)index, value);
			var listener = ExternalClockListeners[index];
			if (listener != null)
			{
				listener(value);
			}
		}
		_lastPin = newPin;
	}

	private void ToggleInterrupt (byte index, bool risingEdge)
	{
		var external = GetExternalInterruptConfig (index);
		var externalConfig = GetExternalInterrupt (index);
		
		if (external != null && externalConfig != null) {
			var eimsk = externalConfig!.EIMSK;
			var eicr = externalConfig.EICR;
			var iscOffset = externalConfig.IscOffset;
			if ((_cpu.Mmio.Data[eimsk] & (1 << externalConfig.Index)) != 0) {
				var configuration = (InterruptMode)((_cpu.Mmio.Data[eicr] >> iscOffset) & 0x3);
				var generateInterrupt = false;
				var shouldBeConstant = false;
				switch (configuration) {
					case InterruptMode.LowLevel:
						generateInterrupt = !risingEdge;
						shouldBeConstant = !external!.Constant;
						break;
					case InterruptMode.Change:
						generateInterrupt = true;
						break;
					case InterruptMode.FallingEdge:
						generateInterrupt = !risingEdge;
						break;
					case InterruptMode.RisingEdge:
						generateInterrupt = risingEdge;
						break;
				}
				if (shouldBeConstant) {
					external.Constant = true;
					_externalInts[index] = external;
				}
				if (generateInterrupt) {
					_cpu.SetInterruptFlag (external!);
				} else if (external!.Constant) {
					_cpu.ClearInterrupt (external, true);
				}
			}
		}
		
		TogglePinChangeInterrupt (index);
	}
	
	private void TogglePinChangeInterrupt (byte index)
	{
		if (_pcint != null && _portConfig.PinChange != null && (_portConfig.PinChange.Mask & (1 << index)) != 0) {
			var pcmsk = _portConfig.PinChange.PCMSK;
			if ((_cpu.Mmio.Data[pcmsk] & (1 << (index + _portConfig.PinChange.Offset))) != 0) {
				_cpu.SetInterruptFlag (_pcint);
			}
		}
	}

	private AvrExternalInterrupt? GetExternalInterrupt (byte index)
	{
		if (_portConfig.ExternalInterrupts == null) {
			return null;
		}
		return _portConfig.ExternalInterrupts.Length == 0 || _portConfig.ExternalInterrupts.Length - 1 < index ? null : _portConfig.ExternalInterrupts[index];
	}
	
	private AvrInterruptConfig? GetExternalInterruptConfig (byte index)
	{
		return _externalInts.Count == 0 || _externalInts.Count - 1 < index ? null : _externalInts[index];
	}
	
	private void AttachInterruptHook (byte register, string registerType = "other")
	{
		var isFlag = registerType == "flag";
		var isMask = registerType == "mask";
		_cpu.Mmio.RegisterWrite(register, (value, _, _, _) =>
		{
			if (!isFlag) _cpu.Mmio.Data[register] = value;

			foreach (var gpio in _cpu.GpioPorts)
			{
				for (var i = 0; i < gpio._externalInts.Count; i++)
				{
					var external = gpio._externalInts[i];
					if (external == null) continue;

					var shouldClear = !external.Constant && isFlag;
            
					if (isMask) _cpu.UpdateInterruptEnable(external, value);
					if (shouldClear) _cpu.ClearInterruptByFlag(external, value);
				}
				gpio.CheckExternalInterrupts();
			}
			return true;
		});
		
	}

	public void CheckExternalInterrupts ()
	{

		for (var pin = 0; pin < 8; pin++) {
			if (pin >= (_portConfig.ExternalInterrupts?.Length ?? -1)) 
				break;
			
			var external = _portConfig.ExternalInterrupts?[pin];
			if (external == null) continue;
			var pinValue = (_lastPin & (1 << pin)) != 0;
			var eifr = external.EIFR;
			var eimsk = external.EIMSK;
			var index = external.Index;
			var eicr = external.EICR;
			var iscOffset = external.IscOffset;
			var interrupt = external.Interrupt;
			if ((_cpu.Mmio.Data[eimsk] & (1 << index)) == 0 || pinValue) continue;
			var configuration = (byte)((_cpu.Mmio.Data[eicr] >> iscOffset) & 0x3);
			if (configuration == (byte)InterruptMode.LowLevel) {
				_cpu.QueueInterrupt (new AvrInterruptConfig (
					address: interrupt,
					flagRegister: eifr,
					flagMask: (byte)(1 << index),
					enableRegister: eimsk,
					enableMask: (byte)(1 << index),
					constant: true
				));
			}
		}
	}
	
	private void WriteGpio (byte value, byte ddr)
	{
		var newValue = (byte)((((value & _overrideMask) | _overrideValue) & ddr) | (value & ~ddr));
		var prevValue = _lastValue;
		if (newValue == prevValue && ddr == _lastDdr) return;
		_lastValue = newValue;
		_lastDdr = ddr;
		OnGpioChange?.Invoke(newValue, prevValue);
	}
}

public class AvrExternalInterrupt (byte eicr, byte eimsk, byte eifr, byte iscOffset, byte index, byte interrupt)
{
	public readonly byte EICR = eicr;
	public readonly byte EIMSK = eimsk;
	public readonly byte EIFR = eifr;
	
	public readonly byte IscOffset = iscOffset;
	public readonly byte Index = index;

	public readonly byte Interrupt = interrupt;
}

public class AvrPinChangeInterrupt (byte pcie, byte pcicr, byte pcifr, byte pcmsk, byte pinChangeInterrupt, byte mask = 0xff, byte offset = 0)
{
	public readonly byte PCIE = pcie;
	public readonly byte PCICR = pcicr;
	public readonly byte PCIFR = pcifr;
	public readonly byte PCMSK = pcmsk;
	public readonly byte PinChangeInterrupt = pinChangeInterrupt;
	public readonly byte Mask = mask;
	public readonly byte Offset = offset;
}

public class AvrPortConfig (ushort pin, ushort ddr, ushort port, AvrPinChangeInterrupt? pinChange = null, AvrExternalInterrupt?[]? externalInterrupts = null)
{
	public readonly ushort PIN = pin;
	public readonly ushort DDR = ddr;
	public readonly ushort PORT = port;
	
	public readonly AvrPinChangeInterrupt? PinChange = pinChange;
	public readonly AvrExternalInterrupt?[]? ExternalInterrupts = externalInterrupts;
}

public enum PinState
{
	Low = 0,
	High = 1,
	Input = 2,
	InputPullup = 3,
}

/* This mechanism allows timers to override specific GPIO pins */
public enum PinOverrideMode {
	None,
	Enable,
	Set,
	Clear,
	Toggle,
}

public enum InterruptMode {
	LowLevel,
	Change,
	FallingEdge,
	RisingEdge,
}
