namespace Avr8Sharp.Tests.Utils;

public class CompileResult
{
    public byte[] Program { get; set; }
    public int InstructionCount { get; set; }
    public Dictionary<string, int> Labels { get; set; }
}