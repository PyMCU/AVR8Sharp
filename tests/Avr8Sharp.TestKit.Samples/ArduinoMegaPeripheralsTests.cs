using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.TestKit.Boards;

namespace Avr8Sharp.TestKit.Samples;

/// <summary>
/// ATmega2560 peripheral wiring (compare-output pins, external/pin-change interrupts, SPI/USART
/// vectors, input capture) driven by small assembled programs. Interrupt vector "word" values are
/// avr-libc vector number x 2; <c>.org</c> takes byte addresses, so a vector at word W sits at byte 2*W.
/// </summary>
[TestFixture]
public class ArduinoMegaPeripheralsTests
{
    // Reset jumps to main (placed past the 57-entry vector table); the handler body is
    // appended at the vector under test and just records a marker in r24 then breaks.
    private static string Program(string main, int vectorWord, string handler = "ldi r24, 0xA5\nbreak")
    {
        return $@"
            jmp main
            .org {vectorWord * 2}
            {handler}
            .org 0x200
        main:
            ldi r16, 0x21
            out 0x3E, r16       ; SPH = RAMEND high
            ldi r16, 0xFF
            out 0x3D, r16       ; SPL
            {main}
        ";
    }

    private static List<bool> RecordPin(AvrIoPort port, int bit)
    {
        var levels = new List<bool>();
        port.AddListener((value, _) => levels.Add((value & (1 << bit)) != 0));
        return levels;
    }

    private static void AssertToggles(List<bool> levels)
    {
        Assert.That(levels, Does.Contain(true), "pin never went high");
        Assert.That(levels, Does.Contain(false), "pin never went low");
    }

    // ── Compare-output pins ───────────────────────────────────────────────────

    [Test]
    public void Timer0_FastPwm_DrivesPB7_ForOC0A()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(@"
            ldi r16, 0x80
            out 0x04, r16       ; DDRB.7 output
            ldi r16, 0x40
            sts 0x0047, r16     ; OCR0A
            ldi r16, 0x83
            out 0x24, r16       ; TCCR0A = COM0A1 | WGM01 | WGM00
            ldi r16, 0x01
            out 0x25, r16       ; TCCR0B = clk/1
        loop:
            rjmp loop
        ");
        var pb7 = RecordPin(mega.PortB, 7);
        var pd6 = RecordPin(mega.PortD, 6);

        mega.RunCycles(2000);

        AssertToggles(pb7);
        Assert.That(pd6, Is.Empty, "OC0A must not use the ATmega328P pin (PD6)");
    }

    [Test]
    public void Timer0_FastPwm_DrivesPG5_ForOC0B()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(@"
            ldi r16, 0x20
            sts 0x0033, r16     ; DDRG.5 output
            ldi r16, 0x40
            sts 0x0048, r16     ; OCR0B
            ldi r16, 0x23
            out 0x24, r16       ; TCCR0A = COM0B1 | WGM01 | WGM00
            ldi r16, 0x01
            out 0x25, r16
        loop:
            rjmp loop
        ");
        var pg5 = RecordPin(mega.PortG, 5);

        mega.RunCycles(2000);

        AssertToggles(pg5);
    }

    [Test]
    public void Timer1_ChannelC_DrivesPB7_ForOC1C()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(@"
            ldi r16, 0x80
            out 0x04, r16       ; DDRB.7 output
            ldi r16, 0x40
            sts 0x008C, r16     ; OCR1CL
            ldi r16, 0x09
            sts 0x0080, r16     ; TCCR1A = COM1C1 | WGM10
            ldi r16, 0x09
            sts 0x0081, r16     ; TCCR1B = WGM12 | clk/1  (8-bit fast PWM)
        loop:
            rjmp loop
        ");
        var pb7 = RecordPin(mega.PortB, 7);

        mega.RunCycles(2000);

        AssertToggles(pb7);
    }

    [Test]
    public void Timer1_ChannelC_CompareMatchSetsOcf1c()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(@"
            ldi r16, 0x10
            sts 0x008C, r16     ; OCR1CL = 16
            ldi r16, 0x01
            sts 0x0081, r16     ; TCCR1B = clk/1, normal mode
        loop:
            rjmp loop
        ");

        mega.RunCycles(200);

        Assert.That(mega.Cpu.ReadData(0x36) & 0x08, Is.EqualTo(0x08), "TIFR1.OCF1C");
    }

    [Test]
    public void Timer2_FastPwm_DrivesPH6_ForOC2B()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(@"
            ldi r16, 0x40
            sts 0x0101, r16     ; DDRH.6 output
            ldi r16, 0x40
            sts 0x00B4, r16     ; OCR2B
            ldi r16, 0x23
            sts 0x00B0, r16     ; TCCR2A = COM2B1 | WGM21 | WGM20
            ldi r16, 0x01
            sts 0x00B1, r16     ; TCCR2B = clk/1
        loop:
            rjmp loop
        ");
        var ph6 = RecordPin(mega.PortH, 6);
        var pd3 = RecordPin(mega.PortD, 3);

        mega.RunCycles(2000);

        AssertToggles(ph6);
        Assert.That(pd3, Is.Empty, "OC2B must not use the ATmega328P pin (PD3)");
    }

    [Test]
    public void Timer2_FastPwm_DrivesPB4_ForOC2A()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(@"
            ldi r16, 0x10
            out 0x04, r16       ; DDRB.4 output
            ldi r16, 0x40
            sts 0x00B3, r16     ; OCR2A
            ldi r16, 0x83
            sts 0x00B0, r16     ; TCCR2A = COM2A1 | WGM21 | WGM20
            ldi r16, 0x01
            sts 0x00B1, r16
        loop:
            rjmp loop
        ");
        var pb4 = RecordPin(mega.PortB, 4);

        mega.RunCycles(2000);

        AssertToggles(pb4);
    }

    [Test]
    public void Timer3_ChannelC_DrivesPE5()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(@"
            ldi r16, 0x20
            out 0x0D, r16       ; DDRE.5 output
            ldi r16, 0x40
            sts 0x009C, r16     ; OCR3CL
            ldi r16, 0x09
            sts 0x0090, r16     ; TCCR3A = COM3C1 | WGM30
            ldi r16, 0x09
            sts 0x0091, r16     ; TCCR3B = WGM32 | clk/1
        loop:
            rjmp loop
        ");
        var pe5 = RecordPin(mega.PortE, 5);

        mega.RunCycles(2000);

        AssertToggles(pe5);
    }

    // ── External interrupts (INT0-7) ──────────────────────────────────────────

    [Test]
    public void Int4_FallingEdge_SetsEifrBit4()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(@"
            ldi r16, 0x02
            sts 0x006A, r16     ; EICRB: ISC41 = falling edge on INT4
            ldi r16, 0x10
            out 0x1D, r16       ; EIMSK = INT4 (interrupts left globally disabled)
        loop:
            rjmp loop
        ");
        mega.PortE.SetPinValue(4, true);
        mega.RunInstructions(20);
        Assert.That(mega.Cpu.ReadData(0x3C) & 0x10, Is.Zero);

        mega.PortE.SetPinValue(4, false);

        Assert.That(mega.Cpu.ReadData(0x3C) & 0x10, Is.EqualTo(0x10), "EIFR.INTF4");
    }

    [Test]
    public void Int4_FallingEdge_JumpsToVector0x0A()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(Program(@"
            ldi r16, 0x02
            sts 0x006A, r16     ; EICRB: INT4 falling edge
            ldi r16, 0x10
            out 0x1D, r16       ; EIMSK
            sei
        loop:
            rjmp loop
        ", 0x0A));
        mega.PortE.SetPinValue(4, true);
        mega.RunInstructions(50);

        mega.PortE.SetPinValue(4, false);
        mega.RunToBreak();

        Assert.That(mega.Cpu.ReadData(24), Is.EqualTo(0xA5));
        Assert.That(mega.Cpu.Pc, Is.InRange(0x0Au, 0x0Du));
    }

    [Test]
    public void Int0_FallingEdgeOnPD0_JumpsToVector0x02()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(Program(@"
            ldi r16, 0x02
            sts 0x0069, r16     ; EICRA: INT0 falling edge
            ldi r16, 0x01
            out 0x1D, r16       ; EIMSK
            sei
        loop:
            rjmp loop
        ", 0x02));
        mega.PortD.SetPinValue(0, true);
        mega.RunInstructions(50);

        mega.PortD.SetPinValue(0, false);
        mega.RunToBreak();

        Assert.That(mega.Cpu.ReadData(24), Is.EqualTo(0xA5));
        Assert.That(mega.Cpu.Pc, Is.InRange(0x02u, 0x05u));
    }

    [Test]
    public void Int7_RisingEdgeOnPE7_UsesEicrbIsc7()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(Program(@"
            ldi r16, 0xC0
            sts 0x006A, r16     ; EICRB: ISC71:70 = rising edge
            ldi r16, 0x80
            out 0x1D, r16       ; EIMSK = INT7
            sei
        loop:
            rjmp loop
        ", 0x10));
        mega.PortE.SetPinValue(7, false);
        mega.RunInstructions(50);

        mega.PortE.SetPinValue(7, true);
        mega.RunToBreak();

        Assert.That(mega.Cpu.Pc, Is.InRange(0x10u, 0x13u));
    }

    // ── Pin-change interrupts ─────────────────────────────────────────────────

    [Test]
    public void Pcint2_OnPK0_JumpsToVector0x16()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(Program(@"
            ldi r16, 0x01
            sts 0x006D, r16     ; PCMSK2 = PCINT16 (PK0)
            ldi r16, 0x04
            sts 0x0068, r16     ; PCICR = PCIE2
            sei
        loop:
            rjmp loop
        ", 0x16));
        mega.PortK.SetPinValue(0, false);
        mega.RunInstructions(50);

        mega.PortK.SetPinValue(0, true);
        mega.RunToBreak();

        Assert.That(mega.Cpu.ReadData(24), Is.EqualTo(0xA5));
        Assert.That(mega.Cpu.Pc, Is.InRange(0x16u, 0x19u));
    }

    [Test]
    public void Pcint0_OnPB4_JumpsToVector0x12()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(Program(@"
            ldi r16, 0x10
            sts 0x006B, r16     ; PCMSK0 = PCINT4 (PB4)
            ldi r16, 0x01
            sts 0x0068, r16     ; PCICR = PCIE0
            sei
        loop:
            rjmp loop
        ", 0x12));
        mega.PortB.SetPinValue(4, false);
        mega.RunInstructions(50);

        mega.PortB.SetPinValue(4, true);
        mega.RunToBreak();

        Assert.That(mega.Cpu.Pc, Is.InRange(0x12u, 0x15u));
    }

    [Test]
    public void Pcint1_PE0IsBit0_AndPJ0IsBit1_OfPcmsk1()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(Program(@"
            ldi r16, 0x02
            sts 0x006C, r16     ; PCMSK1 = PCINT9 (PJ0) only
            ldi r16, 0x02
            sts 0x0068, r16     ; PCICR = PCIE1
        loop:
            rjmp loop
        ", 0x14));
        mega.RunInstructions(20);

        mega.PortE.SetPinValue(0, true);            // PCINT8, not enabled in PCMSK1
        Assert.That(mega.Cpu.ReadData(0x3B) & 0x02, Is.Zero, "PE0 is masked out");

        mega.PortJ.SetPinValue(0, true);            // PCINT9, enabled
        Assert.That(mega.Cpu.ReadData(0x3B) & 0x02, Is.EqualTo(0x02), "PCIFR.PCIF1 from PJ0");
    }

    [Test]
    public void Pcint1_OnPE0_SetsPcif1_WhenEnabledInBit0()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(@"
            ldi r16, 0x01
            sts 0x006C, r16     ; PCMSK1 = PCINT8 (PE0)
            ldi r16, 0x02
            sts 0x0068, r16
        loop:
            rjmp loop
        ");
        mega.RunInstructions(20);

        mega.PortE.SetPinValue(0, true);

        Assert.That(mega.Cpu.ReadData(0x3B) & 0x02, Is.EqualTo(0x02));
    }

    // ── SPI / USART ───────────────────────────────────────────────────────────

    [Test]
    public void Spi_TransferComplete_JumpsToVector0x30()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(Program(@"
            ldi r16, 0xD0
            out 0x2C, r16       ; SPCR = SPIE | SPE | MSTR
            sei
            ldi r16, 0x55
            out 0x2E, r16       ; SPDR: start transfer
        loop:
            rjmp loop
        ", 0x30));

        mega.RunToBreak();

        Assert.That(mega.Cpu.ReadData(24), Is.EqualTo(0xA5));
        Assert.That(mega.Cpu.Pc, Is.InRange(0x30u, 0x33u));
    }

    [Test]
    public void Usart1_TransmitsByte_OnSerial1Only()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(@"
            ldi r16, 0x08
            sts 0x00C9, r16     ; UCSR1B = TXEN1
            ldi r16, 0x06
            sts 0x00CA, r16     ; UCSR1C = 8N1
            ldi r16, 103
            sts 0x00CC, r16     ; UBRR1L (9600 baud @16 MHz)
            ldi r16, 0x4B       ; 'K'
            sts 0x00CE, r16     ; UDR1
            break
        ");

        mega.RunToBreak();
        mega.RunMilliseconds(5);

        Assert.That(mega.Serial1.Text, Is.EqualTo("K"));
        Assert.That(mega.Serial0.Text, Is.Empty);
    }

    [Test]
    public void Usart1_DataRegisterEmpty_JumpsToVector0x4A()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(Program(@"
            ldi r16, 0x20
            sts 0x00C9, r16     ; UCSR1B = UDRIE1
            sei
        loop:
            rjmp loop
        ", 0x4A));

        mega.RunToBreak();

        Assert.That(mega.Cpu.Pc, Is.InRange(0x4Au, 0x4Du));
    }

    // ── Input capture ─────────────────────────────────────────────────────────

    [Test]
    public void Timer3_TriggerCapture_SetsIcf3_AndJumpsToVector0x3E()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(Program(@"
            ldi r16, 0x01
            sts 0x0091, r16     ; TCCR3B = clk/1
            ldi r16, 0x20
            sts 0x0071, r16     ; TIMSK3 = ICIE3
            sei
        loop:
            rjmp loop
        ", 0x3E));
        mega.RunInstructions(50);

        mega.Timer3.TriggerCapture();
        mega.RunToBreak();

        Assert.That(mega.Cpu.Pc, Is.InRange(0x3Eu, 0x41u));
    }

    [Test]
    public void Timer3_TriggerCapture_SetsIcf3InTifr3()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm("ldi r16, 0x01\nsts 0x0091, r16\nloop:\nrjmp loop");
        mega.RunInstructions(20);

        mega.Timer3.TriggerCapture();

        Assert.That(mega.Cpu.ReadData(0x38) & 0x20, Is.EqualTo(0x20));
    }

    [Test]
    public void Timer4_InputCaptureFlag_IsConfigured()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm("ldi r16, 0x01\nsts 0x00A1, r16\nloop:\nrjmp loop");
        mega.RunInstructions(20);

        mega.Timer4.TriggerCapture();

        Assert.That(mega.Cpu.ReadData(0x39) & 0x20, Is.EqualTo(0x20), "TIFR4.ICF4");
    }

    [Test]
    public void Timer3_IcpPin_PE7_RisingEdgeCaptures()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm("ldi r16, 0x41\nsts 0x0091, r16\nloop:\nrjmp loop");   // ICES3 = rising, clk/1
        mega.PortE.SetPinValue(7, false);
        mega.RunInstructions(20);
        Assert.That(mega.Cpu.ReadData(0x38) & 0x20, Is.Zero);

        mega.PortE.SetPinValue(7, true);

        Assert.That(mega.Cpu.ReadData(0x38) & 0x20, Is.EqualTo(0x20), "TIFR3.ICF3 from ICP3 = PE7");
    }
}
