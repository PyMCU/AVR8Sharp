using AVR8Sharp.Core;

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
		pcmsk: 0x6d,
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
	
	// ── ATmega2560 ──────────────────────────────────────────────────────────
	// Naming scheme for every Mega peripheral config in the core: Mega2560<Peripheral>Config
	// (Mega2560Timer3Config, Mega2560Usart1Config, Mega2560PortBConfig ...). Interrupt "address"
	// values are word indices = avr-libc vector number x 2 (the ATmega2560 uses 4-byte JMP vectors).
	// Checked against the ATmega2560 datasheet register summary and interrupt vector table.

	/// <summary>ATmega2560 INT0 (PD0): EICRA ISC00/01, vector 1 -> word 0x02.</summary>
	public static readonly AvrExternalInterrupt Mega2560INT0 = new AvrExternalInterrupt (eicr: 0x69, eimsk: 0x3d, eifr: 0x3c, iscOffset: 0, index: 0, interrupt: 0x02);
	/// <summary>ATmega2560 INT1 (PD1): EICRA ISC10/11, vector 2 -> word 0x04.</summary>
	public static readonly AvrExternalInterrupt Mega2560INT1 = new AvrExternalInterrupt (eicr: 0x69, eimsk: 0x3d, eifr: 0x3c, iscOffset: 2, index: 1, interrupt: 0x04);
	/// <summary>ATmega2560 INT2 (PD2): EICRA ISC20/21, vector 3 -> word 0x06.</summary>
	public static readonly AvrExternalInterrupt Mega2560INT2 = new AvrExternalInterrupt (eicr: 0x69, eimsk: 0x3d, eifr: 0x3c, iscOffset: 4, index: 2, interrupt: 0x06);
	/// <summary>ATmega2560 INT3 (PD3): EICRA ISC30/31, vector 4 -> word 0x08.</summary>
	public static readonly AvrExternalInterrupt Mega2560INT3 = new AvrExternalInterrupt (eicr: 0x69, eimsk: 0x3d, eifr: 0x3c, iscOffset: 6, index: 3, interrupt: 0x08);
	/// <summary>ATmega2560 INT4 (PE4): EICRB ISC40/41, vector 5 -> word 0x0A.</summary>
	public static readonly AvrExternalInterrupt Mega2560INT4 = new AvrExternalInterrupt (eicr: 0x6a, eimsk: 0x3d, eifr: 0x3c, iscOffset: 0, index: 4, interrupt: 0x0a);
	/// <summary>ATmega2560 INT5 (PE5): EICRB ISC50/51, vector 6 -> word 0x0C.</summary>
	public static readonly AvrExternalInterrupt Mega2560INT5 = new AvrExternalInterrupt (eicr: 0x6a, eimsk: 0x3d, eifr: 0x3c, iscOffset: 2, index: 5, interrupt: 0x0c);
	/// <summary>ATmega2560 INT6 (PE6): EICRB ISC60/61, vector 7 -> word 0x0E.</summary>
	public static readonly AvrExternalInterrupt Mega2560INT6 = new AvrExternalInterrupt (eicr: 0x6a, eimsk: 0x3d, eifr: 0x3c, iscOffset: 4, index: 6, interrupt: 0x0e);
	/// <summary>ATmega2560 INT7 (PE7): EICRB ISC70/71, vector 8 -> word 0x10.</summary>
	public static readonly AvrExternalInterrupt Mega2560INT7 = new AvrExternalInterrupt (eicr: 0x6a, eimsk: 0x3d, eifr: 0x3c, iscOffset: 6, index: 7, interrupt: 0x10);

	/// <summary>PCINT0 group: PORTB (PCINT0-7), PCMSK0 0x6B, vector 9 -> word 0x12.</summary>
	public static readonly AvrPinChangeInterrupt Mega2560PCINT0 = new AvrPinChangeInterrupt (pcie: 0, pcicr: 0x68, pcifr: 0x3b, pcmsk: 0x6b, pinChangeInterrupt: 0x12, mask: 0xFF, offset: 0);
	/// <summary>PCINT1 group, PE0 half (PCINT8 = PCMSK1 bit 0), vector 10 -> word 0x14.</summary>
	public static readonly AvrPinChangeInterrupt Mega2560PCINT1E = new AvrPinChangeInterrupt (pcie: 1, pcicr: 0x68, pcifr: 0x3b, pcmsk: 0x6c, pinChangeInterrupt: 0x14, mask: 0x01, offset: 0);
	/// <summary>PCINT1 group, PJ0-6 half (PCINT9-15 = PCMSK1 bits 1-7), vector 10 -> word 0x14.</summary>
	public static readonly AvrPinChangeInterrupt Mega2560PCINT1J = new AvrPinChangeInterrupt (pcie: 1, pcicr: 0x68, pcifr: 0x3b, pcmsk: 0x6c, pinChangeInterrupt: 0x14, mask: 0x7F, offset: 1);
	/// <summary>PCINT2 group: PORTK (PCINT16-23), PCMSK2 0x6D, vector 11 -> word 0x16.</summary>
	public static readonly AvrPinChangeInterrupt Mega2560PCINT2 = new AvrPinChangeInterrupt (pcie: 2, pcicr: 0x68, pcifr: 0x3b, pcmsk: 0x6d, pinChangeInterrupt: 0x16, mask: 0xFF, offset: 0);

	/// <summary>ATmega2560 Port A (digital 22-29).</summary>
	public static readonly AvrPortConfig Mega2560PortAConfig = new AvrPortConfig (
		pin: 0x20, ddr: 0x21, port: 0x22,
		externalInterrupts: []
	);

	/// <summary>ATmega2560 Port B (PCINT0-7, OC0A/OC1C/OC1A/OC1B/OC2A, SPI).</summary>
	public static readonly AvrPortConfig Mega2560PortBConfig = new AvrPortConfig (
		pin: 0x23, ddr: 0x24, port: 0x25,
		pinChange: Mega2560PCINT0
	);

	/// <summary>ATmega2560 Port C (digital 30-37).</summary>
	public static readonly AvrPortConfig Mega2560PortCConfig = new AvrPortConfig (
		pin: 0x26, ddr: 0x27, port: 0x28,
		externalInterrupts: []
	);

	/// <summary>ATmega2560 Port D (INT0-3, T0/T1, ICP1).</summary>
	public static readonly AvrPortConfig Mega2560PortDConfig = new AvrPortConfig (
		pin: 0x29, ddr: 0x2a, port: 0x2b,
		externalInterrupts: [Mega2560INT0, Mega2560INT1, Mega2560INT2, Mega2560INT3]
	);

	/// <summary>ATmega2560 Port E (INT4-7, PCINT8, OC3A/B/C, T3, ICP3).</summary>
	public static readonly AvrPortConfig Mega2560PortEConfig = new AvrPortConfig (
		pin: 0x2c, ddr: 0x2d, port: 0x2e,
		pinChange: Mega2560PCINT1E,
		externalInterrupts: [null, null, null, null, Mega2560INT4, Mega2560INT5, Mega2560INT6, Mega2560INT7]
	);

	/// <summary>ATmega2560 Port F (ADC0-7).</summary>
	public static readonly AvrPortConfig Mega2560PortFConfig = new AvrPortConfig (
		pin: 0x2f, ddr: 0x30, port: 0x31,
		externalInterrupts: []
	);

	/// <summary>ATmega2560 Port G (OC0B).</summary>
	public static readonly AvrPortConfig Mega2560PortGConfig = new AvrPortConfig (
		pin: 0x32, ddr: 0x33, port: 0x34,
		externalInterrupts: []
	);

	/// <summary>ATmega2560 Port H (OC4A/B/C, OC2B, T4).</summary>
	public static readonly AvrPortConfig Mega2560PortHConfig = new AvrPortConfig (
		pin: 0x100, ddr: 0x101, port: 0x102,
		externalInterrupts: []
	);

	/// <summary>ATmega2560 Port J (PCINT9-15).</summary>
	public static readonly AvrPortConfig Mega2560PortJConfig = new AvrPortConfig (
		pin: 0x103, ddr: 0x104, port: 0x105,
		pinChange: Mega2560PCINT1J
	);

	/// <summary>ATmega2560 Port K (ADC8-15, PCINT16-23).</summary>
	public static readonly AvrPortConfig Mega2560PortKConfig = new AvrPortConfig (
		pin: 0x106, ddr: 0x107, port: 0x108,
		pinChange: Mega2560PCINT2
	);

	/// <summary>ATmega2560 Port L (OC5A/B/C, T5, ICP4/5).</summary>
	public static readonly AvrPortConfig Mega2560PortLConfig = new AvrPortConfig (
		pin: 0x109, ddr: 0x10a, port: 0x10b,
		externalInterrupts: []
	);

	private readonly List<AvrInterruptConfig?> _externalInts = [];
	public event Action<byte, byte>? OnGpioChange;
	private readonly AvrInterruptConfig? _pcint;
	private readonly Cpu _cpu;
	private readonly AvrPortConfig _portConfig;
	private int _pinValue;
	private int _driven;
	private byte _overrideMask = 0xff;
	private byte _overrideValue = 0;
	private byte _lastValue = 0;
	private byte _lastDdr = 0;
	private byte _lastPin = 0;

	public byte OpenCollector { get; set; } = 0;

	public Action<bool>?[] ExternalClockListeners { get; } = new Action<bool>?[8];
	
	public Action<byte, PinOverrideMode> TimerOverridePin { get; set; }

	public AvrIoPort (Cpu cpu, AvrPortConfig portConfig)
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

		cpu.OnPeripheralReset += ResetState;
	}

	/// <summary>
	/// Returns the port to its reset state: every pin an input without pull-up, timer
	/// overrides released. The register bytes were already zeroed by <see cref="Cpu.Reset"/>.
	/// Listeners are notified so embedders see the pins go back to input. Pins held by
	/// <see cref="SetPinValue"/> belong to the outside world and stay driven.
	/// </summary>
	private void ResetState ()
	{
		_overrideMask = 0xff;
		_overrideValue = 0;
		foreach (var external in _externalInts) {
			if (external != null) external.Constant = false;
		}
		WriteGpio (0, 0);
		UpdatePinRegister (0);
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
	/// will be returned when reading from the PIN register. The pin counts as
	/// externally driven until <see cref="ReleasePin"/> is called.
	/// </summary>
	/// <param name="index">Pin index to set from 0 to 7</param>
	/// <param name="value">The value to set</param>
	public void SetPinValue (byte index, bool value)
	{ 
		var bitMask = 1 << index;
		var newPinValue = value ? _pinValue | bitMask : _pinValue & ~bitMask;
		var newDriven = _driven | bitMask;
		if (newPinValue == _pinValue && newDriven == _driven) return;
		_pinValue = newPinValue;
		_driven = newDriven;
		UpdatePinRegister (_cpu.Mmio.Data[_portConfig.DDR]);
	}

	/// <summary>
	/// Releases the given pin back to floating, undoing <see cref="SetPinValue"/>.
	/// While an input pin floats, the PIN register reads the internal pull-up
	/// (the PORT bit): 1 with the pull-up enabled, 0 without it.
	/// </summary>
	/// <param name="index">Pin index to release from 0 to 7</param>
	public void ReleasePin (byte index)
	{
		var bitMask = 1 << index;
		if ((_driven & bitMask) == 0) return;
		_driven &= ~bitMask;
		UpdatePinRegister (_cpu.Mmio.Data[_portConfig.DDR]);
	}

	/// <summary>
	/// Drives only the pins in <paramref name="mask"/> from outside to the matching bits of
	/// <paramref name="levels"/>; the other pins keep whatever they had (driven or released).
	/// Equivalent to <see cref="SetPinValue"/> for each bit of the mask, but recomputes PIN once
	/// and returns early when nothing changed.
	/// </summary>
	/// <param name="mask">Pins to drive from outside</param>
	/// <param name="levels">Levels of the pins in the mask (other bits are ignored)</param>
	public void SetInputs (byte mask, byte levels)
	{
		var newDriven = _driven | mask;
		var newPinValue = (_pinValue & ~mask) | (levels & mask);
		if (newDriven == _driven && newPinValue == _pinValue) return;
		_driven = newDriven;
		_pinValue = newPinValue;
		UpdatePinRegister (_cpu.Mmio.Data[_portConfig.DDR]);
	}

	/// <summary>
	/// Stops driving the pins in <paramref name="mask"/> from outside, as <see cref="ReleasePin"/>
	/// does for one pin, with a single PIN recompute and an early return when none was driven.
	/// </summary>
	public void ReleaseInputs (byte mask)
	{
		if ((_driven & mask) == 0) return;
		_driven &= ~mask;
		UpdatePinRegister (_cpu.Mmio.Data[_portConfig.DDR]);
	}

	/// <summary>
	/// Sets the full external state in one call: pins in <paramref name="drivenMask"/> are driven
	/// to <paramref name="levels"/> and every other pin is released. Same result as
	/// <c>SetInputs(drivenMask, levels)</c> followed by <c>ReleaseInputs(~drivenMask)</c>, with one
	/// PIN recompute.
	/// </summary>
	public void ApplyInputs (byte drivenMask, byte levels)
	{
		var newPinValue = levels & drivenMask;
		if (drivenMask == _driven && newPinValue == (_pinValue & drivenMask)) return;
		_driven = drivenMask;
		_pinValue = (_pinValue & ~drivenMask) | newPinValue;
		UpdatePinRegister (_cpu.Mmio.Data[_portConfig.DDR]);
	}

	/// <summary>
	/// Pins with the internal pull-up in effect, exactly the pins <see cref="GetPinState"/> reports
	/// as <see cref="PinState.InputPullup"/>: inputs with the PORT bit set, plus open-collector
	/// outputs that are released (high) with the PORT bit set.
	/// </summary>
	public byte PullUpMask {
		get {
			var port = _cpu.Mmio.Data[_portConfig.PORT];
			var released = _cpu.Mmio.Data[_portConfig.DDR] & OpenCollector & _lastValue;
			return (byte)(port & (~_cpu.Mmio.Data[_portConfig.DDR] | released));
		}
	}

	/// <summary>The register layout this port was built with.</summary>
	public AvrPortConfig Config => _portConfig;

	private AvrPinChangeHandler? _pinChanged;

	/// <summary>
	/// Raised when the effective pad level or output enable of a pin in <see cref="WatchMask"/>
	/// changes on the chip side (after timer overrides), with <see cref="Cpu.Cycles"/> at that
	/// instant. Costs nothing while nobody subscribes. A change of <see cref="OpenCollector"/>
	/// alone is not reported until the next write that moves a pin.
	/// </summary>
	public event AvrPinChangeHandler? PinChanged {
		add { _pinChanged += value; }
		remove { _pinChanged -= value; }
	}

	/// <summary>Pins that may raise <see cref="PinChanged"/>. All pins by default.</summary>
	public byte WatchMask { get; set; } = 0xff;

	/// <summary>
	/// Pins the chip drives (as <see cref="PinState.Low"/> or <see cref="PinState.High"/> in
	/// <see cref="GetPinState"/>): DDR bits, minus open-collector pins that are in their high
	/// (released) state. Timer overrides only change the level, never the direction.
	/// </summary>
	public byte OutputEnableMask {
		get {
			var ddr = _cpu.Mmio.Data[_portConfig.DDR];
			return (byte)(ddr & ~(OpenCollector & _lastValue));
		}
	}

	/// <summary>
	/// Driven level of the bits in <see cref="OutputEnableMask"/> (timer overrides included).
	/// Bits outside the mask read 0.
	/// </summary>
	public byte OutputLevels => (byte)(_lastValue & OutputEnableMask);

	private void UpdatePinRegister (byte ddr)
	{
		var pulled = _cpu.Mmio.Data[_portConfig.PORT] & ~_driven;
		var newPin = (byte)(((((_pinValue & _driven) | pulled) & ~ddr) | (_lastValue & ddr)) & 0xff);
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
		var pinChanged = _pinChanged;
		if (pinChanged == null) {
			_lastValue = newValue;
			_lastDdr = ddr;
			OnGpioChange?.Invoke(newValue, prevValue);
			return;
		}
		var oldEnable = (byte)(_lastDdr & ~(OpenCollector & prevValue));
		_lastValue = newValue;
		_lastDdr = ddr;
		var newEnable = (byte)(ddr & ~(OpenCollector & newValue));
		var changed = (byte)(((oldEnable ^ newEnable) | ((prevValue ^ newValue) & oldEnable & newEnable)) & WatchMask);
		OnGpioChange?.Invoke(newValue, prevValue);
		if (changed != 0) pinChanged (changed, newEnable, (byte)(newValue & newEnable), _cpu.Cycles);
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

/// <summary>Chip-side pin change: pins that changed, the pins the chip drives and their levels afterwards.</summary>
public delegate void AvrPinChangeHandler (byte changed, byte outputEnable, byte levels, ulong cycle);

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
