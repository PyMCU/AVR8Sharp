using System.Text;
using AVR8Sharp.Core.Peripherals;
using AVR8Sharp.Core.Utils;
using SiliconTwin.Abstractions;

namespace AVR8Sharp.Core.Abstractions;

/// <summary>
/// Thin adapter that exposes an <see cref="AvrRunner"/> and its peripherals through the
/// SiliconTwin contracts (<see cref="IMcu"/>, <see cref="IGpio"/>, <see cref="IPwmSource"/>,
/// <see cref="IAdcInput"/>, <see cref="IPeripheralMap"/>). It holds no emulation state of its own
/// and does not change how the core behaves.
/// </summary>
/// <remarks>
/// Peripherals are passed in because <see cref="AvrBuilder"/> hands them back through <c>out</c>
/// parameters and the core keeps no registry of them (GPIO ports are the exception: they register
/// themselves in <see cref="Cpu.GpioPorts"/> and are discovered automatically).
/// <code>
/// var runner = AvrBuilder.Create()
///     .AddGpioPort(AvrIoPort.PortBConfig, out _)
///     .AddUsart(AvrUsart.Usart0Config, out var usart)
///     .Build();
/// var mcu = runner.AsMcu("atmega328p", usarts: [usart]);
/// </code>
/// </remarks>
public sealed class AvrMcu : IMcu, IGpio, IPwmSource, IAdcInput, IPeripheralMap
{
	readonly AvrRunner _runner;
	readonly AvrClock? _clock;
	readonly AvrWatchdog? _watchdog;
	readonly AvrAdc? _adc;
	readonly AvrTimer[] _timers;
	readonly AvrPinBank[] _banks;
	readonly AvrUartPort[] _uarts;
	readonly AvrSpiPort[] _spis;
	readonly AvrI2cPort[] _i2cs;
	Func<int, double>? _adcCallback;
	Action? _clockChanged;

	/// <summary>Value of <see cref="Cpu.Cycles"/> at the last power reset; <see cref="Cycles"/> counts from there.</summary>
	internal ulong CycleBase { get; private set; }

	/// <param name="runner">The machine, normally from <see cref="AvrBuilder.Build"/>.</param>
	/// <param name="name">Device name reported by <see cref="Name"/>.</param>
	/// <param name="clock">Clock prescaler peripheral, if mounted: makes <see cref="ClockHz"/> follow CLKPR.</param>
	/// <param name="watchdog">Watchdog, if mounted: a power reset restores its power-on MCUSR.</param>
	/// <param name="adc">ADC, if mounted.</param>
	/// <param name="timers">Timers, used by <see cref="IPwmSource"/>.</param>
	/// <param name="usarts">USARTs, in order: index 0 is UART0.</param>
	/// <param name="spis">SPI controllers, in order.</param>
	/// <param name="twis">TWI controllers, in order. Each one gets its <see cref="AvrTwi.EventHandler"/> replaced.</param>
	public AvrMcu (AvrRunner runner, string name = "avr", AvrClock? clock = null, AvrWatchdog? watchdog = null,
		AvrAdc? adc = null, IEnumerable<AvrTimer>? timers = null, IEnumerable<AvrUsart>? usarts = null,
		IEnumerable<AvrSpi>? spis = null, IEnumerable<AvrTwi>? twis = null)
	{
		ArgumentNullException.ThrowIfNull (runner);
		_runner = runner;
		Name = name;
		_clock = clock;
		_watchdog = watchdog;
		_adc = adc;
		_timers = timers?.ToArray () ?? [];

		// Chip order (PORTA, PORTB, ...) regardless of the order the ports were mounted in
		_banks = runner.Cpu.GpioPorts.OrderBy (p => p.Config.PORT).Select (p => new AvrPinBank (this, p)).ToArray ();
		_uarts = (usarts ?? []).Select ((u, i) => new AvrUartPort (i, u)).ToArray ();
		_spis = (spis ?? []).Select ((s, i) => new AvrSpiPort (i, s)).ToArray ();
		_i2cs = (twis ?? []).Select ((t, i) => new AvrI2cPort (i, t)).ToArray ();

		if (_clock != null) _clock.Changed += () => _clockChanged?.Invoke ();
	}

	/// <summary>The wrapped machine.</summary>
	public AvrRunner Runner => _runner;

	/// <summary>The wrapped CPU.</summary>
	public Cpu Cpu => _runner.Cpu;

	public string Name { get; }

	public long Cycles => (long)(_runner.Cpu.Cycles - CycleBase);

	public uint ClockHz => _clock?.Frequency ?? _runner.Speed;

	/// <summary>Raised when CLKPR changes the clock. Never raised without an <see cref="AvrClock"/>.</summary>
	public event Action? ClockChanged {
		add => _clockChanged += value;
		remove => _clockChanged -= value;
	}

	public long Run (long cycleBudget) => _runner.Run (cycleBudget);

	public long RunUntil (long absoluteCycle)
	{
		if (absoluteCycle <= Cycles) return 0;
		return _runner.RunUntil ((ulong)absoluteCycle + CycleBase);
	}

	/// <summary>
	/// Soft: <see cref="Core.Cpu.Reset"/> (registers, peripherals and pending events; SRAM and
	/// flash are kept, <see cref="Cycles"/> keeps counting). Power: the same, after clearing the
	/// general registers and SRAM, then MCUSR gets its power-on value (if a watchdog is mounted)
	/// and <see cref="Cycles"/> restarts from zero. Flash is kept.
	/// </summary>
	public void Reset (ResetKind kind = ResetKind.Soft)
	{
		var cpu = _runner.Cpu;
		if (kind == ResetKind.Power) {
			Array.Clear (cpu.Mmio.Data, 0, 32);
			Array.Clear (cpu.Mmio.Data, cpu.RamStart, cpu.Mmio.Data.Length - cpu.RamStart);
		}
		cpu.Reset ();
		if (kind == ResetKind.Power) {
			_watchdog?.PowerOnReset ();
			CycleBase = cpu.Cycles;
		}
	}

	/// <summary>
	/// Loads Intel HEX (strict: malformed records, bad checksums or an image past the end of flash
	/// throw <see cref="FormatException"/> and leave flash untouched) or a raw binary at
	/// <see cref="FirmwareImage.Offset"/> (bytes outside the image are kept). Other formats throw
	/// <see cref="NotSupportedException"/>.
	/// </summary>
	public void Load (in FirmwareImage image)
	{
		switch (image.Format) {
			case FirmwareFormat.IntelHex:
				if (!_runner.TryLoadHex (Encoding.ASCII.GetString (image.Data.Span), out var info))
					throw new FormatException ("Invalid Intel HEX: " + string.Join ("; ", info.Errors));
				break;
			case FirmwareFormat.Bin:
				var flash = _runner.Cpu.ProgBytes;
				if ((long)image.Offset + image.Data.Length > flash.Length)
					throw new ArgumentOutOfRangeException (nameof (image), "The image does not fit in flash.");
				var merged = (byte[])flash.Clone ();
				image.Data.Span.CopyTo (merged.AsSpan ((int)image.Offset));
				_runner.Cpu.LoadProgram (merged);
				break;
			default:
				throw new NotSupportedException ($"Firmware format {image.Format} is not supported by AVR.");
		}
	}

	public IReadOnlyList<IPinBank> Banks => _banks;

	public IReadOnlyList<IUartPort> Uarts => _uarts;
	public IReadOnlyList<ISpiPort> Spis => _spis;
	public IReadOnlyList<II2cPort> I2cs => _i2cs;

	/// <summary>Reference voltage currently selected by ADMUX; 0 when no ADC is mounted.</summary>
	public double ReferenceVolts => _adc?.ReferenceVoltage ?? 0;

	/// <summary>Maps to <see cref="AvrAdc.ReadChannelVolts"/>. Ignored when no ADC is mounted.</summary>
	public Func<int, double>? ReadChannelVolts {
		get => _adcCallback;
		set {
			_adcCallback = value;
			if (_adc != null) _adc.ReadChannelVolts = value;
		}
	}

	/// <summary>
	/// PWM of timer compare outputs (fast PWM and phase correct, non-inverting or inverting), from the
	/// current timer registers. Returns false for pins that no mounted timer's OCnx output uses.
	/// </summary>
	public bool TryGetPwm (int bank, int pin, out PwmInfo info)
	{
		info = default;
		if ((uint)bank >= (uint)_banks.Length || (uint)pin >= 8) return false;
		var portAddress = _banks[bank].Port.Config.PORT;
		foreach (var timer in _timers) {
			var c = timer.Config;
			for (var ch = 0; ch < 3; ch++) {
				var (port, cpin) = ch switch {
					0 => (c.ComparatorPortA, c.ComparatorPinA),
					1 => (c.ComparatorPortB, c.ComparatorPinB),
					_ => (c.ComparatorPortC, c.ComparatorPinC),
				};
				if (port != portAddress || cpin != pin) continue;
				if (!timer.TryGetPwm (ch, ClockHz, out var enabled, out var freq, out var duty)) continue;
				info = new PwmInfo (enabled, freq, duty);
				return true;
			}
		}
		return false;
	}

	public void Dispose ()
	{
	}
}

/// <summary>Factory extensions to wrap a built machine as an <see cref="AvrMcu"/>.</summary>
public static class AvrRunnerExtensions
{
	/// <summary>Wraps the runner as an <see cref="AvrMcu"/>; see its constructor for the parameters.</summary>
	public static AvrMcu AsMcu (this AvrRunner runner, string name = "avr", AvrClock? clock = null,
		AvrWatchdog? watchdog = null, AvrAdc? adc = null, IEnumerable<AvrTimer>? timers = null,
		IEnumerable<AvrUsart>? usarts = null, IEnumerable<AvrSpi>? spis = null, IEnumerable<AvrTwi>? twis = null)
		=> new (runner, name, clock, watchdog, adc, timers, usarts, spis, twis);
}

/// <summary>One <see cref="AvrIoPort"/> as an <see cref="IPinBank"/> of 8 pins.</summary>
public sealed class AvrPinBank : IPinBank
{
	readonly AvrMcu _mcu;
	readonly AvrPinChangeHandler _forward;
	PinChangeHandler? _changed;

	internal AvrPinBank (AvrMcu mcu, AvrIoPort port)
	{
		_mcu = mcu;
		Port = port;
		Name = "PORT" + PortLetter (port.Config.PORT);
		_forward = (changed, enable, levels, cycle) => {
			var e = new PinChange (changed, enable, levels, (long)(cycle - _mcu.CycleBase));
			_changed?.Invoke (in e);
		};
	}

	/// <summary>The wrapped port.</summary>
	public AvrIoPort Port { get; }

	public string Name { get; }
	public int PinCount => 8;
	public ulong OutputEnableMask => Port.OutputEnableMask;
	public ulong OutputLevels => Port.OutputLevels;
	public ulong PullUpMask => Port.PullUpMask;
	public ulong PullDownMask => 0;

	public void SetInputs (ulong mask, ulong levels) => Port.SetInputs ((byte)mask, (byte)levels);
	public void ReleaseInputs (ulong mask) => Port.ReleaseInputs ((byte)mask);

	public event PinChangeHandler PinChanged {
		add {
			if (_changed == null) Port.PinChanged += _forward;
			_changed += value;
		}
		remove {
			_changed -= value;
			if (_changed == null) Port.PinChanged -= _forward;
		}
	}

	public ulong WatchMask {
		get => Port.WatchMask;
		set => Port.WatchMask = (byte)value;
	}

	static string PortLetter (ushort portAddress) => portAddress switch {
		0x22 => "A", 0x25 => "B", 0x28 => "C", 0x2b => "D", 0x2e => "E", 0x31 => "F", 0x34 => "G",
		0x102 => "H", 0x105 => "J", 0x108 => "K", 0x10b => "L",
		_ => "@" + portAddress.ToString ("x"),
	};
}

/// <summary>One <see cref="AvrUsart"/> as an <see cref="IUartPort"/>.</summary>
public sealed class AvrUartPort : IUartPort
{
	readonly AvrUsart _usart;
	readonly Action<byte> _forward;
	Action<byte>? _previous;
	Action<byte>? _tx;

	internal AvrUartPort (int index, AvrUsart usart)
	{
		Index = index;
		_usart = usart;
		_forward = value => {
			_previous?.Invoke (value);
			_tx?.Invoke (value);
		};
	}

	public int Index { get; }

	public uint BaudRate => _usart.TxEnable || _usart.RxEnable ? (uint)_usart.BaudRate : 0;

	/// <summary>
	/// Raised for every transmitted byte. Wraps <see cref="AvrUsart.OnByteTransmit"/>: a handler
	/// already set there keeps being called.
	/// </summary>
	public event Action<byte>? TxByte {
		add {
			if (_tx == null) {
				_previous = _usart.OnByteTransmit;
				_usart.OnByteTransmit = _forward;
			}
			_tx += value;
		}
		remove {
			_tx -= value;
			if (_tx == null && ReferenceEquals (_usart.OnByteTransmit, _forward)) {
				_usart.OnByteTransmit = _previous;
				_previous = null;
			}
		}
	}

	/// <summary>
	/// Delivers a byte through <see cref="AvrUsart.WriteByte"/> (it arrives after one character
	/// time). False when the receiver is disabled or still busy with the previous byte.
	/// </summary>
	public bool TryInjectRx (byte value) => _usart.WriteByte (value);
}

/// <summary>One <see cref="AvrSpi"/> (master mode) as an <see cref="ISpiPort"/>.</summary>
public sealed class AvrSpiPort : ISpiPort
{
	readonly AvrSpi _spi;
	Func<uint, uint>? _transfer;

	internal AvrSpiPort (int index, AvrSpi spi)
	{
		Index = index;
		_spi = spi;
		_spi.OnTransfer = _ => 0xff;
	}

	public int Index { get; }
	public int FrameBits => 8;

	/// <summary>Maps to <see cref="AvrSpi.OnTransfer"/>; null makes the bus read 0xFF.</summary>
	public Func<uint, uint>? Transfer {
		get => _transfer;
		set {
			_transfer = value;
			_spi.OnTransfer = value == null ? _ => 0xff : mosi => (int)(value (mosi) & 0xff);
		}
	}
}

/// <summary>
/// One <see cref="AvrTwi"/> as an <see cref="II2cPort"/>. Installs its own
/// <see cref="ITwiEventHandler"/> that dispatches the firmware's master transactions to the
/// attached targets by address (no target at the address: NACK).
/// </summary>
/// <remarks>
/// Slave side: <see cref="BeginMasterTransfer"/>, <see cref="MasterWrite"/> and <see cref="MasterStop"/>
/// map to the TWI slave simulation. <see cref="TryMasterRead"/> throws <see cref="NotSupportedException"/>
/// because <see cref="AvrTwi"/> does not model the slave-transmit byte sequence.
/// </remarks>
public sealed class AvrI2cPort : II2cPort
{
	const int StatusSlaveDataRxAck = 0x80;

	readonly AvrTwi _twi;
	readonly Dictionary<byte, II2cTarget> _targets = new ();
	II2cTarget? _active;
	Action<byte>? _slaveAddressChanged;

	internal AvrI2cPort (int index, AvrTwi twi)
	{
		Index = index;
		_twi = twi;
		_twi.EventHandler = new Handler (this);
	}

	public int Index { get; }

	public void AttachTarget (byte addr7, II2cTarget target)
	{
		ArgumentNullException.ThrowIfNull (target);
		_targets[addr7] = target;
	}

	public void DetachTarget (byte addr7) => _targets.Remove (addr7);

	public byte SlaveAddress => _twi.SlaveAddress;

	public event Action<byte>? SlaveAddressChanged {
		add {
			if (_slaveAddressChanged == null) _twi.SlaveAddressChanged += Forward;
			_slaveAddressChanged += value;
		}
		remove {
			_slaveAddressChanged -= value;
			if (_slaveAddressChanged == null) _twi.SlaveAddressChanged -= Forward;
		}
	}

	void Forward (byte address) => _slaveAddressChanged?.Invoke (address);

	public bool BeginMasterTransfer (byte addr7, bool write) => _twi.SimulateIncomingAddress (addr7, write);

	/// <summary>False when the firmware answered with NACK (TWEA clear) instead of status 0x80.</summary>
	public bool MasterWrite (byte data)
	{
		_twi.SimulateIncomingData (data);
		return _twi.Status == StatusSlaveDataRxAck;
	}

	public bool TryMasterRead (out byte data)
		=> throw new NotSupportedException ("AvrTwi does not model slave-transmit sequencing.");

	public void MasterStop () => _twi.SimulateIncomingStop ();

	sealed class Handler (AvrI2cPort port) : ITwiEventHandler
	{
		public void Start (bool repeated)
		{
			if (repeated) EndTransaction ();
			port._twi.CompleteStart ();
		}

		public void Stop ()
		{
			EndTransaction ();
			port._twi.CompleteStop ();
		}

		public void ConnectToSlave (byte address, bool write)
		{
			EndTransaction ();
			if (port._targets.TryGetValue (address, out var target) && target.Probe (address, write)) {
				port._active = target;
				port._twi.CompleteConnect (true);
			} else {
				port._twi.CompleteConnect (false);
			}
		}

		public void WriteByte (byte data)
		{
			if (port._active == null) {
				port._twi.CompleteWrite (false);
				return;
			}
			port._active.Write (data);
			port._twi.CompleteWrite (true);
		}

		public void ReadByte (bool ack) => port._twi.CompleteRead (port._active?.Read () ?? 0xff);

		void EndTransaction ()
		{
			port._active?.Stop ();
			port._active = null;
		}
	}
}
