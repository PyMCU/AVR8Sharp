using System.Runtime.CompilerServices;
using AVR8Sharp.Core;

namespace AVR8Sharp.Core.Decoders;

public interface IInstructionDecoder
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Decode(Cpu cpu);
}