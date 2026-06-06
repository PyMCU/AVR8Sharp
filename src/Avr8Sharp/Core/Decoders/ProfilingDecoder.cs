namespace AVR8Sharp.Core.Decoders;

/// <summary>
/// Slow-path decoder that fires a required callback before each instruction.
/// Use only for profiling/tracing. Hot-path simulation should use LutDecoder or NativeLutDecoder.
/// </summary>
public sealed class ProfilingDecoder : IInstructionDecoder
{
    private NativeLutDecoder _inner = new();
    private readonly Action<uint, ulong> _onInstruction;

    public ProfilingDecoder(Action<uint, ulong> onInstruction)
        => _onInstruction = onInstruction
            ?? throw new ArgumentNullException(nameof(onInstruction));

    public void Decode(Cpu cpu)
    {
        _onInstruction(cpu.Pc, cpu.Cycles);
        _inner.Decode(cpu);
    }
}
