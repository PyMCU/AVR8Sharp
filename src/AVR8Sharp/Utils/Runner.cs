using AVR8Sharp.Peripherals;
namespace AVR8Sharp.Utils;

public class AvrRunner
{
	public const int FLASH = 0x8000;

	public readonly AVR8Sharp.Cpu.Cpu Cpu;
	private uint speed = 16_000_000U; // 16 MHz
	private int workUnitCycles = 500000;
	
	public uint Speed {
		get {
			return speed;
		}
	}

	public AvrRunner (byte[] program, int sramBytes)
	{
		Cpu = new AVR8Sharp.Cpu.Cpu (program, sramBytes);
	}
	
	public void SetSpeed (uint _speed)
	{
		this.speed = _speed;
	}
	
	public void SetWorkUnitCycles (int cycles)
	{
		workUnitCycles = cycles;
	}
	
	public void LoadProgram (byte[] program)
	{
		Cpu.LoadProgram (program);
	}
	
	public void LoadProgram (ushort[] program)
	{
		Cpu.LoadProgram (program);
	}
	
	public void LoadHex (string source)
	{
		var target = new byte[FLASH];
		foreach (var line in source.Split ('\n')) {
			if (!string.IsNullOrEmpty (line) && line[0] == ':' && line.Substring (7, 2) == "00") {
				var bytes = Convert.ToInt32 (line.Substring (1, 2), 16);
				var addr = Convert.ToInt32 (line.Substring (3, 4), 16);
				for (var i = 0; i < bytes; i++) {
					target[addr + i] = Convert.ToByte (line.Substring (9 + i * 2, 2), 16);
				}
			}
		}
		Cpu.LoadProgram (target);
	}

	public void Execute (Action<AVR8Sharp.Cpu.Cpu>? callback = null)
	{
		var cyclesToRun = Cpu.Cycles + workUnitCycles;
		while (Cpu.Cycles < cyclesToRun) {
			AVR8Sharp.Cpu.Instruction.AvrInstruction (Cpu);
			Cpu.Tick ();
		}
		callback?.Invoke (Cpu);
	}
}
