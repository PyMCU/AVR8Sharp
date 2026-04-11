using AVR8Sharp.Core.Utils;

namespace Avr8Sharp.Tests.Utils;

public class AsmProgram(string source)
{
    public CompileResult Compile()
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