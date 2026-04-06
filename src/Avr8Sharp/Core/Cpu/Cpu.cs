#nullable enable
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avr8Sharp.Core.Memory;
using AVR8Sharp.Core.Peripherals;

namespace AVR8Sharp.Core.Cpu;

public class Cpu
{
	#region Constants
	const int RegisterSpace = 0x100;
	const int MaxInterrupts = 128;
	#endregion

	#region Private Properties
	readonly AvrInterruptConfig?[] _pendingInterrupts = new AvrInterruptConfig?[MaxInterrupts];
	private ClockEventEntry[] _clockEvents = new ClockEventEntry[64];
	private int _clockEventCount = 0;
	private readonly byte[] _ram;
	int _nextEventCycle = int.MaxValue;
	short _nextInterrupt = -1;
	short _maxInterrupt = 0;
    #endregion

	#region Public Properties
	public Action OnWatchdogReset { get; set; } = () => { };
	public MmioController Mmio { get; }
	public ushort[] ProgramMemory { get; }
	public byte[] ProgBytes { get; }
	public bool Pc22Bits { get; }
	public ushort Sp {
		get => Mmio.DataView.GetUint16(93, true);
		private set => Mmio.DataView.SetUint16(93, value, true);
	}
	public byte Sreg => _ram[95];
	public bool InterruptsEnabled => (_ram[95] & 0x80) != 0;

	public uint Pc;
	public int Cycles;

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

		// Reset the CPU
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

		// Reset the CPU
		Reset ();
	}
	
	public void Reset ()
	{
		// Reset the CPU
		Sp = (ushort)(Mmio.Data.Length - 1);
		Pc = 0;
		for (var i = 0; i < _pendingInterrupts.Length; i++) {
			_pendingInterrupts[i] = null;
		}
		_nextInterrupt = -1;
		_clockEventCount = 0;
		_nextEventCycle = int.MaxValue;
		Array.Clear(_clockEvents, 0, _clockEvents.Length);
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
		var targetCycles = Cycles + Math.Max(1, cycles);

		if (_clockEventCount == _clockEvents.Length) {
			Array.Resize(ref _clockEvents, _clockEvents.Length * 2);
		}
		
		var i = _clockEventCount - 1;
		while (i >= 0 && _clockEvents[i].Cycles > targetCycles)
		{
			_clockEvents[i + 1] = _clockEvents[i];
			i--;
		}
		
		_clockEvents[i + 1] = new ClockEventEntry { Callback = callback, Cycles = targetCycles };
		_clockEventCount++;

		_nextEventCycle = _clockEvents[0].Cycles;

		return callback;
	}
	
	public void UpdateClockEvent(Action callback, int cycles)
	{
		ClearClockEvent(callback);
		AddClockEvent(callback, cycles);
	}
	
	public bool ClearClockEvent(Action callback)
	{
		for (var i = 0; i < _clockEventCount; i++)
		{
			if (_clockEvents[i].Callback != callback) continue;
			for (var j = i; j < _clockEventCount - 1; j++)
			{
				_clockEvents[j] = _clockEvents[j + 1];
			}
            
			_clockEventCount--;
			_clockEvents[_clockEventCount] = default;
            
			_nextEventCycle = _clockEventCount > 0 ? _clockEvents[0].Cycles : int.MaxValue;
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
		while (_clockEventCount > 0 && _clockEvents[0].Cycles <= Cycles)
		{
			var callback = _clockEvents[0].Callback;

			for (int i = 0; i < _clockEventCount - 1; i++)
			{
				_clockEvents[i] = _clockEvents[i + 1];
			}
			_clockEventCount--;
			_clockEvents[_clockEventCount] = default;

			callback();
		}

		_nextEventCycle = _clockEventCount > 0 ? _clockEvents[0].Cycles : int.MaxValue;
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
	public int Cycles;
}
