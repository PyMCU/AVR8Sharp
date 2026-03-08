#nullable enable
using CpuMemoryReadHooks = System.Collections.Generic.Dictionary<ushort, System.Func<ushort, byte>?>;
using CpuMemoryWriteHooks = System.Collections.Generic.Dictionary<ushort, System.Func<byte, byte, ushort, byte, bool>?>;
using System.Runtime.CompilerServices;
using AVR8Sharp.Core.Peripherals;

namespace AVR8Sharp.Core.Cpu;

public class Cpu
{
	#region Constants
	const int RegisterSpace = 0x100;
	const int MaxInterrupts = 128;
	#endregion

	#region Private Properties
	readonly byte[] _data;
	readonly AvrInterruptConfig?[] _pendingInterrupts = new AvrInterruptConfig?[MaxInterrupts];
	AvrClockEventEntry? _nextClockEvent = null;
	readonly Stack<AvrClockEventEntry?> _clockEventPool = [];
	short _nextInterrupt = -1;
	short _maxInterrupt = 0;
    #endregion

	#region Public Properties
	public Action OnWatchdogReset { get; set; } = () => { };
	public byte[] Data { get => _data; }
	public DataView DataView { get; }
	public ushort[] ProgramMemory { get; }
	public byte[] ProgBytes { get; }
	public CpuMemoryReadHooks ReadHooks { get; } = new CpuMemoryReadHooks ();
	public CpuMemoryWriteHooks WriteHooks { get; } = new CpuMemoryWriteHooks ();
	public bool PC22Bits { get; }
	public uint PC { get; set; }
	public int Cycles { get; set; }
	public ushort SP {
		get {
			return DataView.GetUint16 (93, true);
		}
		set {
			DataView.SetUint16 (93, value, true);
		}
	}
	public byte SREG {
		get {
			return _data[95];
		}
	}
	public bool InterruptsEnabled {
		get {
			return (SREG & 0x80) != 0;
		}
	}
	
	public List<AvrIoPort> GpioPorts { get; } = [];
	public Dictionary<uint, AvrIoPort> GpioByPort { get; } = [];
	#endregion

	public Cpu (ushort[] program, int sramBytes = 8192)
	{
		_data = new byte[sramBytes + RegisterSpace];
		DataView = new DataView (ref _data);
		ProgramMemory = program;
		ProgBytes = new byte[program.Length * 2];
		// Copy the values using array.copy
		Buffer.BlockCopy (program.ToArray (), 0, ProgBytes, 0, program.Length * 2);
		
		// Whether the program counter (PC) can address 22 bits (the default is 16)
		PC22Bits = (program.Length * 2) > 0x20000;
		
		// Reset the CPU
		Reset ();
	}
	
	public Cpu (byte[] program, int sramBytes = 8192)
	{
		_data = new byte[sramBytes + RegisterSpace];
		DataView = new DataView (ref _data);
		ProgBytes = program;
		ProgramMemory = new ushort[program.Length / 2];
		// Copy the values using array.copy
		Buffer.BlockCopy (program, 0, ProgramMemory, 0, program.Length);
		
		// Whether the program counter (PC) can address 22 bits (the default is 16)
		PC22Bits = program.Length > 0x20000;
		
		// Reset the CPU
		Reset ();
	}
	
	public void Reset ()
	{
		// Reset the CPU
		SP = (ushort)(_data.Length - 1);
		PC = 0;
		for (var i = 0; i < _pendingInterrupts.Length; i++) {
			_pendingInterrupts[i] = null;
		}
		_nextInterrupt = -1;
		_nextClockEvent = null;
	}
	
	public void LoadProgram (ushort[] program)
	{
		Array.Copy (program, ProgramMemory, program.Length);
		for (var i = 0; i < program.Length; i++) {
			ProgBytes[i * 2] = (byte)(program[i] & 0xff);
			ProgBytes[i * 2 + 1] = (byte)(program[i] >> 8);
		}
	}
	public void LoadProgram (byte[] program)
	{
		Array.Copy (program, ProgBytes, program.Length);
		for (var i = 0; i < program.Length / 2; i++) {
			ProgramMemory[i] = (ushort)(program[i * 2] | program[i * 2 + 1] << 8);
		}
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
		if (address >= 32 && ReadHooks.TryGetValue (address, out var hook) && hook != null) {
			return hook.Invoke(address);
		}
		return Data[address];
	}
	
	public void WriteData (ushort address, byte value, byte mask = 0xff)
	{
		if (WriteHooks.TryGetValue (address, out var hook) && hook != null && hook(value, Data[address], address, mask)) {
			return;
		} 
		Data[address] = value;
	}
	
	public void SetInterruptFlag (AvrInterruptConfig interrupt)
	{
		if (interrupt.InverseFlag) {
			_data[interrupt.FlagRegister] &= (byte)~interrupt.FlagMask;
		}
		else {
			_data[interrupt.FlagRegister] |= (byte)interrupt.FlagMask;
		}
		if ((_data[interrupt.EnableRegister] & interrupt.EnableMask) != 0) {
			QueueInterrupt (interrupt);
		}
	}
	
	public void UpdateInterruptEnable (AvrInterruptConfig interrupt, byte registerValue)
	{
		if ((registerValue & interrupt.EnableMask) != 0) {
			var bitSet = (_data[interrupt.FlagRegister] & interrupt.FlagMask) != 0;
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
			_data[interrupt.FlagRegister] &= (byte)~interrupt.FlagMask;
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
		_data[interrupt.FlagRegister] &= (byte)~interrupt.FlagMask;
		ClearInterrupt (interrupt);
	}
	
	public Action AddClockEvent (Action callback, int cycles)
	{
		cycles = Cycles + Math.Max (1, cycles);
		var maybeEntry = _clockEventPool.Count > 0 ? _clockEventPool.Pop () : null;
		var entry = maybeEntry ?? new AvrClockEventEntry { Cycles = cycles, Callback = callback, Next = null, };
		entry.Cycles = cycles;
		entry.Callback = callback;
		var clockEvent = _nextClockEvent;
		AvrClockEventEntry? lastItem = null;
		while (clockEvent != null && clockEvent.Cycles < cycles) {
			lastItem = clockEvent;
			clockEvent = clockEvent.Next;
		}
		if (lastItem != null) {
			lastItem.Next = entry;
		} else {
			_nextClockEvent = entry;
		}
		entry.Next = clockEvent;
		return callback;
	}
	
	public bool UpdateClockEvent (Action callback, int cycles)
	{
		if (ClearClockEvent (callback)) {
			AddClockEvent (callback, cycles);
			return true;
		}
		return false;
	}
	
	public bool ClearClockEvent (Action callback)
	{
		var clockEvent = _nextClockEvent;
		if (clockEvent == null) {
			return false;
		}
		AvrClockEventEntry? lastItem = null;
		while (clockEvent != null) {
			if (clockEvent.Callback == callback) {
				if (lastItem != null) {
					lastItem.Next = clockEvent.Next;
				} else {
					_nextClockEvent = clockEvent.Next;
				}
				if (_clockEventPool.Count < 10) {
					_clockEventPool.Push (clockEvent);
				}
				return true;
			}
			lastItem = clockEvent;
			clockEvent = clockEvent.Next;
		}
		return false;
	}
	
	public void Tick ()
	{
		var nextClockEvent = _nextClockEvent;
		if (nextClockEvent != null && nextClockEvent.Cycles <= Cycles) {
			nextClockEvent.Callback ();
			_nextClockEvent = nextClockEvent.Next;
			if (_clockEventPool.Count < 10) {
				_clockEventPool.Push (nextClockEvent);
			}
		}

		if (!InterruptsEnabled || _nextInterrupt < 0) return;
		var interrupt = _pendingInterrupts[_nextInterrupt];
		if (interrupt == null) return;
		AvrInterrupt.DoAvrInterrupt (this, interrupt.Address);
		if (!interrupt.Constant) {
			ClearInterrupt (interrupt);
		}
	}
}

public class AvrInterruptConfig (byte address, ushort enableRegister, int enableMask, ushort flagRegister, int flagMask, bool constant = false, bool inverseFlag = false)
{
	public readonly byte Address = address;
	public readonly ushort EnableRegister = enableRegister;
	public readonly int EnableMask = enableMask;
	public readonly ushort FlagRegister = flagRegister;
	public readonly int FlagMask = flagMask;
	public readonly bool Constant = constant;
	public readonly bool InverseFlag = inverseFlag;
	
	public AvrInterruptConfig MakeConstant ()
	{
		return new AvrInterruptConfig (Address, EnableRegister, EnableMask, FlagRegister, FlagMask, true, InverseFlag);
	}

}

public class AvrClockEventEntry
{
	public int Cycles { get; set; }
	public Action Callback { get; set; } = () => { };
	public AvrClockEventEntry? Next { get; set; }
	
	public AvrClockEventEntry Clone ()
	{
		return new AvrClockEventEntry { Cycles = Cycles, Callback = Callback, Next = Next };
	}
}

public class DataView (ref byte[] data)
{
	private readonly byte[] _data = data;
	public sbyte GetInt8(int byteOffset)
	{
		return (sbyte)_data[byteOffset];
	}
	public void SetInt8(int byteOffset, sbyte value)
	{
		_data[byteOffset] = (byte)value;
	}
	public short GetInt16(int byteOffset, bool littleEndian = false)
	{
		if (littleEndian) {
			return (short)(_data[byteOffset] | _data[byteOffset + 1] << 8);
		}
		return (short)(_data[byteOffset] << 8 | _data[byteOffset + 1]);
	}
	public void SetInt16(int byteOffset, short value, bool littleEndian = false)
	{
		if (littleEndian) {
			_data[byteOffset] = (byte)(value & 0xff);
			_data[byteOffset + 1] = (byte)(value >> 8);
		} else {
			_data[byteOffset] = (byte)(value >> 8);
			_data[byteOffset + 1] = (byte)(value & 0xff);
		}
	}
	public ushort GetUint16(int byteOffset, bool littleEndian = false)
	{
		if (littleEndian) {
			return (ushort)(_data[byteOffset] | _data[byteOffset + 1] << 8);
		}
		return (ushort)(_data[byteOffset] << 8 | _data[byteOffset + 1]);
	}
	public void SetUint16(int byteOffset, ushort value, bool littleEndian = false)
	{
		if (littleEndian) {
			_data[byteOffset] = (byte)(value & 0xff);
			_data[byteOffset + 1] = (byte)(value >> 8);
		} else {
			_data[byteOffset] = (byte)(value >> 8);
			_data[byteOffset + 1] = (byte)(value & 0xff);
		}
	}
}
