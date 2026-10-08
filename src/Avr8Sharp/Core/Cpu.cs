#nullable enable
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AVR8Sharp.Core.Memory;
using AVR8Sharp.Core.Peripherals;

namespace AVR8Sharp.Core;

public class Cpu
{
	#region Constants
	private const int RegisterSpace = 0x100;
	private const int MaxInterrupts = 256;
	#endregion

	#region Private Properties
	readonly AvrInterruptConfig?[] _pendingInterrupts = new AvrInterruptConfig?[MaxInterrupts];
	private ClockEventEntry[] _clockEvents = new ClockEventEntry[64];
	private int _clockEventCount = 0;
	private int _clockHead = 0;
	private readonly byte[] _ram;
	internal byte _sregArith;
	private ulong _nextEventCycle = ulong.MaxValue;
	private short _nextInterrupt = -1;
	private short _maxInterrupt = 0;
	// Set by SEI/RETI/anything that raises I: the next Tick must not dispatch an interrupt
	// (the instruction after it always runs first). It is paired with _nextEventCycle = 0
	// so the next Tick takes the existing slow path and consumes it; the hot path pays nothing.
	private bool _interruptHold;
    #endregion

	#region Public Properties
	public Action OnWatchdogReset { get; set; } = () => { };
	public event Action? OnPeripheralReset;
	/// <summary>
	/// Raised when this CPU dispatches an interrupt. Arguments: vector address, PC that was
	/// pushed. Instance-level replacement for the process-global <c>AvrInterrupt.OnInterruptDispatch</c>.
	/// </summary>
	public event Action<int, uint>? OnInterruptDispatch;
	/// <summary>
	/// Raised when this CPU executes a BREAK instruction (0x9598). The argument is the word
	/// address of the BREAK. Instance-level replacement for <c>AvrInterrupt.OnBreakpoint</c>.
	/// </summary>
	public event Action<uint>? OnBreakpoint;
	/// <summary>
	/// Raised when this CPU executes a SLEEP instruction (0x9588). The argument is the
	/// SM2:SM1:SM0 sleep mode bits from SMCR (bits 3:1). Instance-level replacement for
	/// <c>AvrInterrupt.OnSleep</c>.
	/// </summary>
	public event Action<byte>? OnSleep;
	public MmioController Mmio { get; }
	public ushort[] ProgramMemory { get; }
	public byte[] ProgBytes { get; }
	public bool Pc22Bits { get; }
	public ushort Sp {
		get => Mmio.DataView.GetUint16(93, true);
		private set => Mmio.DataView.SetUint16(93, value, true);
	}
	/// <summary>
	/// Lowest data address the stack may occupy (a chip's RAMSTART). A PUSH/CALL that
	/// would write below it has overflowed the stack into the I/O/register space and
	/// throws <see cref="AvrStackOverflowException"/>. Default 0 disables the check (no
	/// address is below 0), so raw cores and unit tests that park SP low are unaffected;
	/// a board/simulation sets it to the chip's SRAM start to catch overflow.
	/// </summary>
	public int StackLowLimit { get; set; } = 0;

	private int _ramStart = -1;
	/// <summary>
	/// The chip's first SRAM address, i.e. the end of the register/I/O space that
	/// <see cref="Reset"/> returns to reset values. When not set it is 0x200 for cores
	/// with a 22-bit PC (ATmega2560/2561) and 0x100 otherwise, never above the data size.
	/// Independent of <see cref="StackLowLimit"/>. Boards set it explicitly (ATtiny: 0x60).
	/// </summary>
	public int RamStart {
		get => Math.Min (_ramStart >= 0 ? _ramStart : (Pc22Bits ? 0x200 : RegisterSpace), Mmio.Data.Length);
		set => _ramStart = value;
	}
	/// <summary>
	/// The core variant this CPU models. Instructions the variant does not have throw
	/// <see cref="AvrUnsupportedInstructionException"/> instead of executing; instructions
	/// whose cost differs between variants are charged the modelled variant's cycles.
	/// Defaults to <see cref="AvrCore.Classic"/>, the ATmega328P / ATmega2560 / ATtiny core.
	/// </summary>
	public AvrCore Core { get; set; } = AvrCore.Classic;
	public byte Sreg
	{
		get
		{
			_ram[95] = (byte)((_ram[95] & 0xc0) | _sregArith);
			return _ram[95];
		}
	}
	public bool InterruptsEnabled => (_ram[95] & 0x80) != 0;

	public uint Pc;
	public ulong Cycles;

	public List<AvrIoPort> GpioPorts { get; } = [];
	public Dictionary<uint, AvrIoPort> GpioByPort { get; } = [];
	#endregion

	public Cpu (ushort[] program, int sramBytes = 8192)
	{
		Mmio = new MmioController (sramBytes + RegisterSpace);
		_ram = Mmio.Data;

		ProgramMemory = new ushort[program.Length];
		ProgBytes = new byte[program.Length * 2];

		LoadProgram(program);

		Pc22Bits = (program.Length * 2) > 0x20000;

		RegisterSregHooks();
		Reset ();
	}

	public Cpu (byte[] program, int sramBytes = 8192)
	{
		Mmio = new MmioController (sramBytes + RegisterSpace);
		_ram = Mmio.Data;

		ProgBytes = new byte[program.Length];
		ProgramMemory = new ushort[program.Length / 2];

		LoadProgram(program);

		Pc22Bits = program.Length > 0x20000;

		RegisterSregHooks();
		Reset ();
	}

	private void RegisterSregHooks()
	{
		Mmio.RegisterRead(95, _ => {
			_ram[95] = (byte)((_ram[95] & 0xc0) | _sregArith);
			return _ram[95];
		});
		Mmio.RegisterWrite(95, (value, oldValue, _, mask) => {
			var masked = (byte)((oldValue & ~mask) | (value & mask));
			_sregArith = (byte)(masked & 0x3f);
			return false;
		});
	}
	
	#region Instance hooks
	// Instance handlers run first, then the obsolete process-global ones.
	#pragma warning disable CS0618
	internal void RaiseInterruptDispatch (int address, uint pc)
	{
		OnInterruptDispatch?.Invoke (address, pc);
		AvrInterrupt.OnInterruptDispatch?.Invoke (address, pc);
	}

	internal void RaiseBreakpoint ()
	{
		OnBreakpoint?.Invoke (Pc);
		AvrInterrupt.OnBreakpoint?.Invoke (Pc);
	}

	internal void RaiseSleep ()
	{
		var mode = (byte)((Mmio.Data[0x53] >> 1) & 0x07);
		OnSleep?.Invoke (mode);
		AvrInterrupt.OnSleep?.Invoke (mode);
	}
	#pragma warning restore CS0618
	#endregion

	/// <summary>
	/// Makes the next <see cref="Tick"/> skip interrupt dispatch, so the instruction after
	/// SEI / RETI / a write that sets I always runs before a pending interrupt is served.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void HoldInterrupts ()
	{
		_interruptHold = true;
		_nextEventCycle = 0;
	}

	/// <summary>
	/// Data write issued by an instruction. Identical to <see cref="WriteData"/> except that a
	/// write to SREG (0x5F) that sets I from 0 holds interrupts for one instruction.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void WriteDataSreg (ushort address, byte value)
	{
		if (address != 95) {
			Mmio.WriteData (address, value);
			return;
		}
		var wasEnabled = (_ram[95] & 0x80) != 0;
		Mmio.WriteData (address, value);
		if (!wasEnabled && (value & 0x80) != 0) HoldInterrupts ();
	}

	private readonly List<int> _preservedOnReset = [];

	/// <summary>
	/// Registers a data address that <see cref="Reset"/> must not zero (a register that
	/// survives a reset on silicon, such as MCUSR which holds the reset-cause flags).
	/// </summary>
	public void PreserveOnReset (int address)
	{
		if (!_preservedOnReset.Contains (address)) _preservedOnReset.Add (address);
	}

	/// <summary>
	/// Resets the CPU and every peripheral, as the RESET pin would. The I/O register space
	/// (0x20 up to <see cref="RamStart"/>)
	/// returns to its reset value: zero here, then each peripheral subscribed to
	/// <see cref="OnPeripheralReset"/> resets its internal state and writes its non-zero
	/// reset values. SRAM and the general registers r0-r31 are kept, as on silicon. The
	/// zeroing writes the data array directly, so no MMIO write hook runs.
	/// </summary>
	public void Reset ()
	{
		var ioEnd = RamStart;
		if (ioEnd > 0x20) {
			var preserved = new byte[_preservedOnReset.Count];
			for (var i = 0; i < preserved.Length; i++) preserved[i] = Mmio.Data[_preservedOnReset[i]];
			Array.Clear (Mmio.Data, 0x20, ioEnd - 0x20);
			for (var i = 0; i < preserved.Length; i++) Mmio.Data[_preservedOnReset[i]] = preserved[i];
		}
		Sp = (ushort)(Mmio.Data.Length - 1);
		Mmio.Data[95] = 0;
		_sregArith = 0;
		Pc = 0;
		for (var i = 0; i < _pendingInterrupts.Length; i++) {
			_pendingInterrupts[i] = null;
		}
		_nextInterrupt = -1;
		_interruptHold = false;
		_clockHead = 0;
		_clockEventCount = 0;
		_nextEventCycle = ulong.MaxValue;
		Array.Clear(_clockEvents, 0, _clockEvents.Length);
		OnPeripheralReset?.Invoke();
	}
	
	public void LoadProgram (ushort[] program)
	{
		Array.Copy(program, ProgramMemory, program.Length);
		var spanBytes = MemoryMarshal.Cast<ushort, byte>(ProgramMemory.AsSpan());
		spanBytes.CopyTo(ProgBytes.AsSpan());
	}
	public void LoadProgram (byte[] program)
	{
		Buffer.BlockCopy(program, 0, ProgBytes, 0, program.Length);
		var spanUshorts = MemoryMarshal.Cast<byte, ushort>(ProgBytes.AsSpan());
		spanUshorts.CopyTo(ProgramMemory.AsSpan());
	}
	
	public void SetProgramByte (int address, byte value)
	{
		ProgBytes[address] = value;
		ProgramMemory[address / 2] = (ushort)(ProgBytes[address] | ProgBytes[address + 1] << 8);
	}
	
	public void SetProgramWord (int address, ushort value)
	{
		ProgramMemory[address] = value;
		ProgBytes[address * 2] = (byte)(value & 0xff);
		ProgBytes[address * 2 + 1] = (byte)(value >> 8);
	}
	
	public byte ReadData (ushort address)
	{
		return Mmio.ReadData(address);
	}
	
	public void WriteData (ushort address, byte value, byte mask = 0xff)
	{
		Mmio.WriteData(address, value, mask);
	}
	
	public void SetInterruptFlag (AvrInterruptConfig interrupt)
	{
		if (interrupt.InverseFlag) {
			Mmio.Data[interrupt.FlagRegister] &= (byte)~interrupt.FlagMask;
		}
		else {
			Mmio.Data[interrupt.FlagRegister] |= (byte)interrupt.FlagMask;
		}
		if ((Mmio.Data[interrupt.EnableRegister] & interrupt.EnableMask) != 0) {
			QueueInterrupt (interrupt);
		}
	}
	
	public void UpdateInterruptEnable (AvrInterruptConfig interrupt, byte registerValue)
	{
		if ((registerValue & interrupt.EnableMask) != 0) {
			var bitSet = (Mmio.Data[interrupt.FlagRegister] & interrupt.FlagMask) != 0;
			if (interrupt.InverseFlag ? !bitSet : bitSet) {
				QueueInterrupt (interrupt);
			}
		} else {
			ClearInterrupt (interrupt, false);
		}
	}
	
	public void QueueInterrupt (AvrInterruptConfig interrupt)
	{
		_pendingInterrupts[interrupt.Address] = interrupt;
		if (_nextInterrupt == -1 || _nextInterrupt > interrupt.Address) {
			_nextInterrupt = interrupt.Address;
		}
		if (interrupt.Address > _maxInterrupt) {
			_maxInterrupt = interrupt.Address;
		}
	}
	
	public void ClearInterrupt (AvrInterruptConfig interrupt, bool clearFlag = true)
	{
		if (clearFlag) {
			Mmio.Data[interrupt.FlagRegister] &= (byte)~interrupt.FlagMask;
		}
		if (_pendingInterrupts[interrupt.Address] == null) {
			return;
		}
		_pendingInterrupts[interrupt.Address] = null;
		if (_nextInterrupt != interrupt.Address) return;
		_nextInterrupt = -1;
		for (var i = interrupt.Address + 1; i <= _maxInterrupt; i++) {
			if (_pendingInterrupts[i] == null) continue;
			_nextInterrupt = (short)i;
			break;
		}
	}
	
	public void ClearInterruptByFlag (AvrInterruptConfig interrupt, byte registerValue)
	{
		if ((registerValue & interrupt.FlagMask) == 0) return;
		Mmio.Data[interrupt.FlagRegister] &= (byte)~interrupt.FlagMask;
		ClearInterrupt (interrupt);
	}
	
	public Action AddClockEvent(Action callback, int cycles)
	{
		var targetCycles = Cycles + (ulong)Math.Max(1, cycles);

		if (_clockEventCount == _clockEvents.Length)
		{
			// Compact circular buffer into a fresh linear array before growing
			var newArray = new ClockEventEntry[_clockEvents.Length * 2];
			for (int k = 0; k < _clockEventCount; k++)
				newArray[k] = _clockEvents[(_clockHead + k) % _clockEvents.Length];
			_clockEvents = newArray;
			_clockHead = 0;
		}

		// Insertion sort: walk backward from tail toward head, shifting elements
		// toward the tail until we find the right slot for targetCycles.
		var i = _clockEventCount - 1;
		while (i >= 0)
		{
			var physIdx = (_clockHead + i) % _clockEvents.Length;
			if (_clockEvents[physIdx].Cycles <= targetCycles) break;
			_clockEvents[(_clockHead + i + 1) % _clockEvents.Length] = _clockEvents[physIdx];
			i--;
		}

		_clockEvents[(_clockHead + i + 1) % _clockEvents.Length] = new ClockEventEntry { Callback = callback, Cycles = targetCycles };
		_clockEventCount++;

		_nextEventCycle = _clockEvents[_clockHead].Cycles;
		return callback;
	}
	
	public void UpdateClockEvent(Action callback, int cycles)
	{
		ClearClockEvent(callback);
		AddClockEvent(callback, cycles);
	}
	
	public bool ClearClockEvent(Action callback)
	{
		for (var li = 0; li < _clockEventCount; li++)
		{
			var physIdx = (_clockHead + li) % _clockEvents.Length;
			if (_clockEvents[physIdx].Callback != callback) continue;

			// Shift elements after li one logical step toward head to fill the gap
			for (var lj = li; lj < _clockEventCount - 1; lj++)
			{
				var src = (_clockHead + lj + 1) % _clockEvents.Length;
				var dst = (_clockHead + lj) % _clockEvents.Length;
				_clockEvents[dst] = _clockEvents[src];
			}

			_clockEventCount--;
			_clockEvents[(_clockHead + _clockEventCount) % _clockEvents.Length] = default;

			_nextEventCycle = _clockEventCount > 0 ? _clockEvents[_clockHead].Cycles : ulong.MaxValue;
			return true;
		}
		return false;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Tick()
	{
		if (Cycles >= _nextEventCycle)
		{
			ProcessClockEvents();
			if (_interruptHold) {
				_interruptHold = false;
				return;
			}
		}

		if (!InterruptsEnabled || _nextInterrupt < 0) return;
    
		var interrupt = _pendingInterrupts[_nextInterrupt];
		if (interrupt == null) return;
    
		AvrInterrupt.DoAvrInterrupt(this, interrupt.Address);
		if (!interrupt.Constant) {
			ClearInterrupt(interrupt);
		}
	}
	
	private void ProcessClockEvents()
	{
		while (_clockEventCount > 0 && _clockEvents[_clockHead].Cycles <= Cycles)
		{
			var callback = _clockEvents[_clockHead].Callback;
			_clockEvents[_clockHead] = default;
			_clockHead = (_clockHead + 1) % _clockEvents.Length;
			_clockEventCount--;
			callback();
		}

		_nextEventCycle = _clockEventCount > 0 ? _clockEvents[_clockHead].Cycles : ulong.MaxValue;
	}
}

public class AvrInterruptConfig (byte address, ushort enableRegister, int enableMask, ushort flagRegister, int flagMask, bool constant = false, bool inverseFlag = false)
{
	public readonly byte Address = address;
	public readonly ushort EnableRegister = enableRegister;
	public readonly int EnableMask = enableMask;
	public readonly ushort FlagRegister = flagRegister;
	public readonly int FlagMask = flagMask;
	public readonly bool InverseFlag = inverseFlag;
	public bool Constant = constant;
}

public struct ClockEventEntry
{
	public Action Callback;
	public ulong Cycles;
}
