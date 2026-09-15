namespace AVR8Sharp.Core;

/// <summary>
/// The AVR core variant a <see cref="Cpu"/> models. The instruction set differs per
/// variant (AVR Instruction Set Manual, DS40002198, Appendix A), and so does the cycle
/// cost of some instructions. The emulator uses it to refuse opcodes the modelled part
/// does not have instead of executing them silently, and to charge the right cycles
/// where the variants disagree.
/// </summary>
public enum AvrCore
{
    /// <summary>
    /// The classic core: AVRe / AVRe+ in the manual's tables. The ATmega328P, the
    /// ATmega2560 and the ATtiny85 are all classic cores. This is the default.
    /// </summary>
    Classic,

    /// <summary>
    /// The XMEGA core: AVRxm in the manual's tables. Adds <c>XCH</c>, <c>LAC</c>,
    /// <c>LAS</c> and <c>LAT</c>, and <c>DES</c> (which the emulator does not
    /// implement).
    /// </summary>
    Xmega,
}
