using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

/// <summary>
/// Regression tests for issue #18: instruction cycle costs on the classic AVR core
/// (AVRe+, the ATmega328P), measured against an Arduino Uno with an oscilloscope on
/// 2026-09-14.
/// </summary>
[TestFixture]
public class InstructionCycles : AvrTestBase
{
    protected override int FlashByteCount => 0x1000;

    private AVR8Sharp.Core.Decoders.SwitchDecoder _decoder;

    private const int DDRD = 0x0a;  // I/O address, 0x2a in data space
    private const int PORTD = 0x0b; // I/O address, 0x2b in data space

    protected override void SetupPeripherals()
    {
        _decoder = new AVR8Sharp.Core.Decoders.SwitchDecoder();
    }

    [Test(Description = "CBI takes 2 cycles on the classic AVR core; 1 cycle is the reduced-core timing (issue #18)")]
    public void CBI_Takes_Two_Cycles()
    {
        LoadProgram(["cbi 0x0c, 5"]);
        Cpu.Mmio.Data[0x2c] = 0b11111111;

        _decoder.Decode(Cpu);

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Cycles, Is.EqualTo(2));
            Assert.That(Cpu.Pc, Is.EqualTo(1));
            Assert.That(Cpu.Mmio.Data[0x2c], Is.EqualTo(0b11011111));
        });
    }

    [Test(Description = "SBI takes 2 cycles on the classic AVR core (issue #18)")]
    public void SBI_Takes_Two_Cycles()
    {
        LoadProgram(["sbi 0x0c, 5"]);
        Cpu.Mmio.Data[0x2c] = 0b00001111;

        _decoder.Decode(Cpu);

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Cycles, Is.EqualTo(2));
            Assert.That(Cpu.Mmio.Data[0x2c], Is.EqualTo(0b00101111));
        });
    }

    [Test(Description = "RJMP takes 2 cycles on the classic AVR core (issue #18)")]
    public void RJMP_Takes_Two_Cycles()
    {
        LoadProgram(["rjmp 2"]);

        _decoder.Decode(Cpu);

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Cycles, Is.EqualTo(2));
            Assert.That(Cpu.Pc, Is.EqualTo(2));
        });
    }

    /// <summary>
    /// The toggle loop of cp-digitalio-atmega/programs/test02_toggle_speed.py. On an
    /// Arduino Uno the scope reads 2.67 MHz on PD6 with a 30.9 % duty: a 6-cycle period
    /// (SBI 2 + CBI 2 + RJMP 2) with the pin high for the 2 cycles the CBI takes.
    /// </summary>
    [Test(Description = "The SBI/CBI/RJMP toggle loop runs at a 6-cycle period with the pin high for 2 (issue #18)")]
    public void Toggle_Loop_Has_Six_Cycle_Period()
    {
        var program = new AsmProgram(@"
        LDI r16, 0x40   ; DDRD = 1 << 6, PD6 as output
        OUT 0x0a, r16
      loop:
        SBI 0x0b, 6     ; PORTD |= 1 << 6
        CBI 0x0b, 6     ; PORTD &= ~(1 << 6)
        RJMP loop
").Compile();

        Cpu.LoadProgram(program.Program);
        var portD = new AvrIoPort(Cpu, AvrIoPort.PortDConfig);
        var runner = new TestProgramRunner(Cpu);

        runner.RunToAddress(program.Labels["loop"]);

        var atLoopTop = Cpu.Cycles;

        runner.RunInstructions(1); // SBI
        var afterSbi = Cpu.Cycles;
        var pinAfterSbi = portD.GetPinState(6);

        runner.RunInstructions(1); // CBI
        var afterCbi = Cpu.Cycles;
        var pinAfterCbi = portD.GetPinState(6);

        runner.RunInstructions(1); // RJMP
        var afterRjmp = Cpu.Cycles;

        Assert.Multiple(() =>
        {
            Assert.That(afterRjmp - atLoopTop, Is.EqualTo(6), "loop period in cycles");
            Assert.That(afterSbi - atLoopTop, Is.EqualTo(2), "SBI cycles");
            Assert.That(afterCbi - afterSbi, Is.EqualTo(2), "CBI cycles, the span PD6 stays high");
            Assert.That(afterRjmp - afterCbi, Is.EqualTo(2), "RJMP cycles");
            Assert.That(pinAfterSbi, Is.EqualTo(PinState.High));
            Assert.That(pinAfterCbi, Is.EqualTo(PinState.Low));
            Assert.That(Cpu.Pc * 2, Is.EqualTo(program.Labels["loop"]), "the loop closed");
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
