namespace AVR8Sharp.Core;

/// <summary>
/// Thrown when the program reaches an instruction the modelled <see cref="AvrCore"/>
/// does not have -- <c>XCH</c>, <c>LAC</c>, <c>LAS</c> or <c>LAT</c> on a classic
/// (AVRe) core, for instance. On silicon the opcode is undefined and what happens is
/// unspecified; executing it in the emulator would let a program compiled for the
/// wrong part look like it works. The emulator stops instead and names the
/// instruction, so the mismatch between the toolchain's <c>-mmcu</c> and the simulated
/// part is obvious. Set <see cref="Cpu.Core"/> to the core the firmware was built for
/// to enable the instruction.
/// </summary>
public sealed class AvrUnsupportedInstructionException : Exception
{
    /// <summary>Program counter (word address) of the offending instruction.</summary>
    public uint Pc { get; }

    /// <summary>The 16-bit opcode that was fetched.</summary>
    public ushort Opcode { get; }

    /// <summary>The instruction's mnemonic.</summary>
    public string Mnemonic { get; }

    /// <summary>The core the <see cref="Cpu"/> models, which lacks the instruction.</summary>
    public AvrCore Core { get; }

    public AvrUnsupportedInstructionException(uint pc, ushort opcode, string mnemonic, AvrCore core)
        : base($"{mnemonic} (opcode 0x{opcode:X4} at PC=0x{pc:X4} word) is not an instruction of " +
               $"the {core} AVR core. It is an XMEGA (AVRxm) instruction; on the modelled part the " +
               $"opcode is undefined. Check the toolchain's target part, or set Cpu.Core to the core " +
               $"the firmware was built for.")
    {
        Pc = pc;
        Opcode = opcode;
        Mnemonic = mnemonic;
        Core = core;
    }
}
