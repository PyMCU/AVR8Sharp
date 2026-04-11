namespace Avr8Sharp.Tests.Utils;

public class TestProgramRunner(AVR8Sharp.Core.Cpu.Cpu cpu, Action<AVR8Sharp.Core.Cpu.Cpu>? onBreak = null)
{
	private const int BREAK_OPCODE = 0x9598;
	
	private static readonly Action<AVR8Sharp.Core.Cpu.Cpu> DefaultOnBreak = (cpu) => throw new Exception("BREAK instruction encountered");

	private readonly Action<AVR8Sharp.Core.Cpu.Cpu> _onBreak = onBreak ?? DefaultOnBreak;
	private AVR8Sharp.Core.Cpu.Decoders.SwitchDecoder _decoder = new();

	public void RunInstructions (int count)
	{
		for (var i = 0; i < count; i++)
		{
			if (cpu.ProgramMemory[cpu.Pc] == BREAK_OPCODE)
			{
				_onBreak(cpu);
			}
			_decoder.Decode(cpu);
			cpu.Tick ();
		}
	}

	private void RunUntil (Func<AVR8Sharp.Core.Cpu.Cpu, bool> predicate, int maxInstructions = 5000)
	{
		for (var i = 0; i < maxInstructions; i++) {
			if (cpu.ProgramMemory[cpu.Pc] == BREAK_OPCODE)
				_onBreak(cpu);
			
			if (predicate(cpu))
				return;
			
			_decoder.Decode(cpu);
			cpu.Tick ();
		}
		throw new Exception("Max instructions reached");
	}
	
	public void RunToBreak ()
	{
		RunUntil(cpu0 => cpu0.ProgramMemory[cpu0.Pc] == BREAK_OPCODE);
	}
	
	public void RunToAddress (int address)
	{
		RunUntil(cpu0 => cpu0.Pc * 2 == address);
	}
}
