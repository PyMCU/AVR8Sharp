using AVR8Sharp.Core.Utils;
namespace Avr8Sharp.Tests;

public class Utils
{
	// export function asmProgram(source: string) {
	//   const { bytes, errors, lines, labels } = assemble(source);
	//   if (errors.length) {
	//     throw new Error('Assembly failed: ' + errors);
	//   }
	//   return { program: new Uint16Array(bytes.buffer), lines, instructionCount: lines.length, labels };
	// }
	public static CompileResult AsmProgram(string source)
	{
		var assembler = new AvrAssembler();
		var bytes = assembler.Assemble(source);
		if (assembler.Errors.Count > 0)
		{
			throw new Exception("Assembly failed: " + string.Join(", ", assembler.Errors));
		}

		var result = new CompileResult {
			Program = bytes,
			InstructionCount = assembler.Lines.Count,
			Labels = assembler.Labels
		};
		
		return result;
	}
}

public class CompileResult
{
	public byte[] Program { get; set; }
	public int InstructionCount { get; set; }
	public Dictionary<string, int> Labels { get; set; }
}

public class TestProgramRunner
{
	private const int BREAK_OPCODE = 0x9598;
	
	private static Action<AVR8Sharp.Core.Cpu.Cpu> DefaultOnBreak = (cpu) => {
		throw new Exception("BREAK instruction encountered");
	};
	
	private AVR8Sharp.Core.Cpu.Cpu _cpu;
	private Action<AVR8Sharp.Core.Cpu.Cpu> _onBreak;
	private AVR8Sharp.Core.Cpu.Decoders.SwitchDecoder _decoder;
	
	public TestProgramRunner (AVR8Sharp.Core.Cpu.Cpu cpu, Action<AVR8Sharp.Core.Cpu.Cpu>? onBreak = null)
	{
		_cpu = cpu;
		_onBreak = onBreak ?? DefaultOnBreak;
		_decoder = new AVR8Sharp.Core.Cpu.Decoders.SwitchDecoder();
	}
	
	public void RunInstructions (int count)
	{
		for (var i = 0; i < count; i++)
		{
			if (_cpu.ProgramMemory[_cpu.Pc] == BREAK_OPCODE)
			{
				_onBreak(_cpu);
			}
			_decoder.Decode(_cpu);
			_cpu.Tick ();
		}
	}
	
	public void RunUntil (Func<AVR8Sharp.Core.Cpu.Cpu, bool> predicate, int maxInstructions = 5000)
	{
		for (var i = 0; i < maxInstructions; i++) {
			if (_cpu.ProgramMemory[_cpu.Pc] == BREAK_OPCODE)
				_onBreak(_cpu);
			
			if (predicate(_cpu))
				return;
			
			_decoder.Decode(_cpu);
			_cpu.Tick ();
		}
		throw new Exception("Max instructions reached");
	}
	
	public void RunToBreak ()
	{
		RunUntil(cpu => cpu.ProgramMemory[cpu.Pc] == BREAK_OPCODE);
	}
	
	public void RunToAddress (int address)
	{
		RunUntil(cpu => cpu.Pc * 2 == address);
	}
}
