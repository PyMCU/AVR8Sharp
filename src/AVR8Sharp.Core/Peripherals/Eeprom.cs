using AVR8Sharp.Core.Cpu;

namespace AVR8Sharp.Core.Peripherals;

public class AvrEeprom
{
	public const byte EERE = 1 << 0;
	public const byte EEPE = 1 << 1;
	public const byte EEMPE = 1 << 2;
	public const byte EERIE = 1 << 3;
	public const byte EEPM0 = 1 << 4;
	public const byte EEPM1 = 1 << 5;
	public const byte EECR_WRITE_MASK = EEPE | EEMPE | EERIE | EEPM0 | EEPM1;
	
	public static readonly AvrEepromConfig EepromConfig = new AvrEepromConfig (
		eepromReadyInterrupt: 0x2c,
		eecr: 0x3f,
		eedr: 0x40,
		eearl: 0x41,
		eearh: 0x42,
		eraseCycles: 28800,
		writeCycles: 28800
	);
	
	private uint _writeEnabledCycles = 0;
	private uint _writeCompleteCycles = 0;
	readonly AvrEepromConfig _config;
	readonly AvrInterruptConfig _eer; 
	readonly IEepromBackend _backend;

	public AvrEeprom (Cpu.Cpu cpu, IEepromBackend backend, AvrEepromConfig? config = null)
	{
		_backend = backend;
		_config = config ?? EepromConfig;
		_eer = new AvrInterruptConfig (
			address: _config.EepromReadyInterrupt,
			flagRegister: _config.EECR,
			flagMask: EEPE,
			enableRegister: _config.EECR,
			enableMask: EERIE,
			constant: true,
			inverseFlag: true
		);
		
		cpu.WriteHooks[_config.EECR] = (eecr, _, _, _) => {
			var addr = (ushort)((cpu.Data[_config.EEARH] << 8) | cpu.Data[_config.EEARL]);
			
			cpu.Data[_config.EECR] = (byte)((cpu.Data[_config.EECR] & ~EECR_WRITE_MASK) | (eecr & EECR_WRITE_MASK));
			cpu.UpdateInterruptEnable (_eer, eecr);
			
			if ((eecr & EERE) != 0) {
				cpu.ClearInterrupt (_eer);
			}
			
			if ((eecr & EEMPE) != 0) {
				var eempeCycles = 4;
				_writeEnabledCycles = (uint)(cpu.Cycles + eempeCycles);
				cpu.AddClockEvent (() => {
					cpu.Data[_config.EECR] &= ~EEMPE & 0xFF;
				}, eempeCycles);
			}
			
			// Read
			if ((eecr & EERE) != 0) {
				cpu.Data[_config.EEDR] = _backend.ReadMemory (addr);
				// When the EEPROM is read, the CPU is halted for four cycles before the
				// next instruction is executed.
				cpu.Cycles += 4;
				return true;
			}
			
			// Write
			if ((eecr & EEPE) != 0) {
				// If EEMPE is zero, setting EEPE will have no effect.
				if (cpu.Cycles >= _writeEnabledCycles) {
					cpu.Data[_config.EECR] &= ~EEPE & 0xFF;
					return true;
				}
				
				if (cpu.Cycles < _writeCompleteCycles) {
					return true;
				}
				
				var eedr = cpu.Data[_config.EEDR];
				
				_writeCompleteCycles = (uint)cpu.Cycles;
				
				// Erase
				if ((eecr & EEPM1) == 0) {
					_backend.EraseMemory (addr);
					_writeCompleteCycles += _config.EraseCycles;
				}
				
				// Write
				if ((eecr & EEPM0) == 0) {
					_backend.WriteMemory (addr, eedr);
					_writeCompleteCycles += _config.WriteCycles;
				}
				
				cpu.Data[_config.EECR] |= EEPE;
				
				cpu.AddClockEvent (() => {
					cpu.SetInterruptFlag (_eer);
				}, (int)(_writeCompleteCycles - cpu.Cycles));
				
				cpu.Cycles += 2;
			}
			
			return true;
			
		};
	}
}

public interface IEepromBackend
{
	byte ReadMemory (uint address);
	void WriteMemory (uint address, byte value);
	void EraseMemory (uint address);
}

public class EepromMemoryBackend : IEepromBackend
{
	private readonly byte[] _memory;
	public EepromMemoryBackend (uint size)
	{
		_memory = new byte[size];
		// Fill with 0xFF 
		for (var i = 0; i < size; i++) {
			_memory[i] = 0xFF;
		}
	}
	
	public byte ReadMemory (uint address)
	{
		return _memory[address];
	}
	
	public void WriteMemory (uint address, byte value)
	{
		_memory[address] &= value;
	}
	
	public void EraseMemory (uint address)
	{
		_memory[address] = 0xFF;
	}
}

public class AvrEepromConfig (byte eepromReadyInterrupt = 0, byte eecr = 0, byte eedr = 0, byte eearl = 0, byte eearh = 0, uint eraseCycles = 0, uint writeCycles = 0)
{
	public readonly byte EepromReadyInterrupt = eepromReadyInterrupt;
	
	public readonly byte EECR = eecr;
	public readonly byte EEDR = eedr;
	public readonly byte EEARL = eearl;
	public readonly byte EEARH = eearh;
	
	public readonly uint EraseCycles = eraseCycles;
	public readonly uint WriteCycles = writeCycles;

}
