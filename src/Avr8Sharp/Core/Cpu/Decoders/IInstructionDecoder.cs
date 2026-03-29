using System.Runtime.CompilerServices;

namespace AVR8Sharp.Core.Cpu.Decoders;

public interface IInstructionDecoder
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Decode(Cpu cpu);
}