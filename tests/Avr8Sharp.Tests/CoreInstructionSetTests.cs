using AVR8Sharp.Core;
using AVR8Sharp.Core.Decoders;
using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

/// <summary>
/// Regression tests for issue #19: XCH, LAC, LAS and LAT are XMEGA (AVRxm) instructions.
/// The AVR Instruction Set Manual (DS40002198, tables 6-63/64/65/124) charges them two
/// cycles on AVRxm and lists them N/A on every other core, so a classic core (the
/// ATmega328P / ATmega2560 presets) must refuse them instead of executing them silently.
/// </summary>
[TestFixture]
public class CoreInstructionSet : AvrTestBase
{
    protected override int FlashByteCount => 0x1000;

    private static readonly (string Mnemonic, string Source)[] XmegaOnly =
    [
        ("XCH", "xch Z, r21"),
        ("LAC", "lac Z, r19"),
        ("LAS", "las Z, r17"),
        ("LAT", "lat Z, r0"),
    ];

    [Test(Description = "A Cpu models the classic core unless told otherwise")]
    public void Default_Core_Is_Classic()
    {
        Assert.That(Cpu.Core, Is.EqualTo(AvrCore.Classic));
    }

    [TestCaseSource(nameof(XmegaOnly))]
    [Description("On the classic core the opcode is undefined: the emulator stops and names the instruction (issue #19)")]
    public void Classic_Core_Refuses_The_Instruction((string Mnemonic, string Source) instruction)
    {
        LoadProgram(["nop", instruction.Source]);
        Cpu.Mmio.DataView.SetUint16(Z, 0x100, true);
        Cpu.Mmio.Data[0x100] = 0x5a;
        var decoder = new SwitchDecoder();
        decoder.Decode(Cpu); // the NOP, so the failing PC is not trivially 0

        var ex = Assert.Throws<AvrUnsupportedInstructionException>(() => decoder.Decode(Cpu));

        Assert.Multiple(() =>
        {
            Assert.That(ex.Mnemonic, Is.EqualTo(instruction.Mnemonic));
            Assert.That(ex.Pc, Is.EqualTo(1));
            Assert.That(ex.Opcode, Is.EqualTo(Cpu.ProgramMemory[1]));
            Assert.That(ex.Core, Is.EqualTo(AvrCore.Classic));
            Assert.That(ex.Message, Does.Contain(instruction.Mnemonic).And.Contain("XMEGA"));
            Assert.That(Cpu.Mmio.Data[0x100], Is.EqualTo(0x5a), "data space is untouched");
        });
    }

    [TestCaseSource(nameof(XmegaOnly))]
    [Description("Every decoder refuses it, not only the switch decoder")]
    public void All_Decoders_Refuse_The_Instruction_On_The_Classic_Core((string Mnemonic, string Source) instruction)
    {
        LoadProgram([instruction.Source]);
        Cpu.Mmio.DataView.SetUint16(Z, 0x100, true);

        Assert.Multiple(() =>
        {
            Assert.Throws<AvrUnsupportedInstructionException>(() => new SwitchDecoder().Decode(Cpu), "SwitchDecoder");
            Assert.Throws<AvrUnsupportedInstructionException>(() => new LutDecoder().Decode(Cpu), "LutDecoder");
            Assert.Throws<AvrUnsupportedInstructionException>(() => new NativeLutDecoder().Decode(Cpu), "NativeLutDecoder");
        });
    }

    [TestCaseSource(nameof(XmegaOnly))]
    [Description("On the XMEGA core the instruction executes and costs 2 cycles (DS40002198, AVRxm column)")]
    public void Xmega_Core_Charges_Two_Cycles((string Mnemonic, string Source) instruction)
    {
        LoadProgram([instruction.Source]);
        Cpu.Core = AvrCore.Xmega;
        Cpu.Mmio.DataView.SetUint16(Z, 0x100, true);

        new SwitchDecoder().Decode(Cpu);

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Cycles, Is.EqualTo(2));
            Assert.That(Cpu.Pc, Is.EqualTo(1));
        });
    }

    [Test(Description = "XCH on the XMEGA core swaps the register with the byte at Z")]
    public void Xmega_Core_Executes_XCH()
    {
        LoadProgram(["xch Z, r21"]);
        Cpu.Core = AvrCore.Xmega;
        Cpu.Mmio.Data[R21] = 0xa1;
        Cpu.Mmio.DataView.SetUint16(Z, 0x100, true);
        Cpu.Mmio.Data[0x100] = 0xb9;

        new NativeLutDecoder().Decode(Cpu);

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Mmio.Data[R21], Is.EqualTo(0xb9));
            Assert.That(Cpu.Mmio.Data[0x100], Is.EqualTo(0xa1));
            Assert.That(Cpu.Cycles, Is.EqualTo(2));
        });
    }

    private void LoadProgram(string[] instructions)
    {
        var code = string.Join("\n", instructions);
        var assembler = new AVR8Sharp.Core.Utils.AvrAssembler();
        var program = assembler.Assemble(code);
        if (assembler.Errors.Count > 0)
            throw new Exception(string.Join("\n", assembler.Errors));
        Cpu.LoadProgram(program);
    }
}
