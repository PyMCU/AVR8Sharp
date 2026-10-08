using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

/// <summary>
/// ATtiny25/45/85 Timer/Counter1. The first group is ported from avr8js timer-attiny.spec.ts
/// (commit 82f9fa5); the rest covers the register layout and the PWM and output-compare
/// behaviour that the generic ATmega timer cannot model.
/// </summary>
[TestFixture]
public class TimerAttiny : AvrTestBase
{
    const int PINB = 0x36;
    const int DDRB = 0x37;
    const int PORTB = 0x38;

    const int TCCR1 = 0x50;
    const int GTCCR = 0x4c;
    const int TCNT1 = 0x4f;
    const int OCR1A = 0x4e;
    const int OCR1B = 0x4b;
    const int OCR1C = 0x4d;
    const int TIFR = 0x58;
    const int TIMSK = 0x59;

    const int TOV1 = 1 << 2;
    const int OCF1A = 1 << 6;
    const int OCF1B = 1 << 5;
    const int OCIE1A = 1 << 6;
    const int TOIE1 = 1 << 2;

    const int CTC1 = 1 << 7;
    const int PWM1A = 1 << 6;
    const int COM1A1 = 1 << 5;
    const int COM1A0 = 1 << 4;
    const int CS10 = 1;
    const int CS13 = 1 << 3;

    const int PWM1B = 1 << 6;
    const int COM1B1 = 1 << 5;
    const int FOC1A = 1 << 2;
    const int PSR1 = 1 << 1;

    const int PB1 = 1;
    const int PB4 = 4;

    private AvrIoPort _portB = null!;
    private AvrAttinyTimer1 _timer = null!;

    protected override int FlashByteCount => 0x1000;

    protected override void SetupPeripherals()
    {
        _portB = new AvrIoPort(Cpu, new AvrPortConfig(pin: PINB, ddr: DDRB, port: PORTB));
        _timer = new AvrAttinyTimer1(Cpu);
    }

    private void Start(ulong atCycle = 1)
    {
        Cpu.Cycles = atCycle;
        Cpu.Tick();
    }

    // Advances one cycle at a time, like the CPU loop does, so that no compare match is skipped.
    private void RunTo(ulong cycle)
    {
        while (Cpu.Cycles < cycle)
        {
            Cpu.Cycles++;
            Cpu.Tick();
        }
    }

    // ── Ported from avr8js ────────────────────────────────────────────────────

    [Test(Description = "Should update timer every tick when prescaler is 1 (CS=1)")]
    public void Prescaler1()
    {
        Cpu.WriteData(TCCR1, CS10);
        Start(1);
        RunTo(2);
        Assert.That(Cpu.ReadData(TCNT1), Is.EqualTo(1));
    }

    [Test(Description = "Should update timer every 128 ticks when prescaler is 128 (CS=8)")]
    public void Prescaler128()
    {
        Cpu.WriteData(TCCR1, CS13);
        Start(1);
        RunTo(1 + 128);
        Assert.That(Cpu.ReadData(TCNT1), Is.EqualTo(1));
    }

    [Test(Description = "Should not update timer when disabled (CS=0)")]
    public void Disabled()
    {
        Cpu.WriteData(TCCR1, 0);
        Start(1);
        RunTo(100000);
        Assert.That(Cpu.ReadData(TCNT1), Is.Zero);
    }

    [Test(Description = "Should clear timer on OCR1C match when CTC1 is set")]
    public void CtcClearsOnOcr1c()
    {
        Cpu.WriteData(OCR1C, 9);
        Cpu.WriteData(TCCR1, CTC1 | CS10);
        Cpu.WriteData(TCNT1, 8);
        Start(1);
        RunTo(1 + 3);
        Assert.That(Cpu.ReadData(TCNT1), Is.EqualTo(1));
    }

    [Test(Description = "Should set TOV1 when timer overflows past OCR1C")]
    public void CtcSetsTov1()
    {
        Cpu.WriteData(OCR1C, 9);
        Cpu.WriteData(TCNT1, 9);
        Cpu.WriteData(TCCR1, CTC1 | CS10);
        Start(1);
        RunTo(2);
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.ReadData(TCNT1), Is.Zero);
            Assert.That(Cpu.Mmio.Data[TIFR] & TOV1, Is.EqualTo(TOV1));
        });
    }

    [Test(Description = "Should set OCF1A when timer matches OCR1A")]
    public void CtcSetsOcf1a()
    {
        Cpu.WriteData(OCR1C, 249);
        Cpu.WriteData(OCR1A, 5);
        Cpu.WriteData(TCCR1, CTC1 | CS10);
        Cpu.WriteData(TCNT1, 4);
        Start(1);
        RunTo(3);
        Assert.That(Cpu.Mmio.Data[TIFR] & OCF1A, Is.EqualTo(OCF1A));
    }

    [Test(Description = "Should set OCF1B when timer matches OCR1B")]
    public void CtcSetsOcf1b()
    {
        Cpu.WriteData(OCR1C, 249);
        Cpu.WriteData(OCR1B, 10);
        Cpu.WriteData(TCCR1, CTC1 | CS10);
        Cpu.WriteData(TCNT1, 9);
        Start(1);
        RunTo(3);
        Assert.That(Cpu.Mmio.Data[TIFR] & OCF1B, Is.EqualTo(OCF1B));
    }

    [Test(Description = "Should fire the COMPA interrupt when enabled")]
    public void CompAInterrupt()
    {
        Cpu.WriteData(OCR1C, 249);
        Cpu.WriteData(OCR1A, 0);
        Cpu.WriteData(TCCR1, CTC1 | CS10);
        Cpu.WriteData(TCNT1, 248);
        Cpu.WriteData(TIMSK, OCIE1A);
        Cpu.Mmio.Data[SREG] = 0x80;
        Start(1);
        RunTo(3);
        Assert.That(Cpu.Pc, Is.EqualTo(0x03));
    }

    [Test(Description = "Should overflow after a full period with prescaler 128")]
    public void FullPeriodPrescaler128()
    {
        Cpu.WriteData(TCCR1, CTC1 | CS13);
        Cpu.WriteData(OCR1C, 249);
        Cpu.WriteData(TIMSK, OCIE1A);
        Cpu.Mmio.Data[SREG] = 0x80;

        // Full timer period: 250 * 128 = 32000 cycles
        Start(1);
        RunTo(32001);

        Assert.That(Cpu.Mmio.Data[TIFR] & TOV1, Is.Not.Zero);
    }

    [Test(Description = "Should clear TOV1 by writing 1 to TIFR")]
    public void ClearTov1()
    {
        Cpu.WriteData(OCR1C, 9);
        Cpu.WriteData(TCNT1, 9);
        Cpu.WriteData(TCCR1, CTC1 | CS10);
        Start(1);
        RunTo(2);
        Assert.That(Cpu.Mmio.Data[TIFR] & TOV1, Is.EqualTo(TOV1));
        Cpu.WriteData(TIFR, TOV1);
        Assert.That(Cpu.Mmio.Data[TIFR] & TOV1, Is.Zero);
    }

    // ── Register layout and normal mode ───────────────────────────────────────

    [Test(Description = "OCR1C resets to 0xFF")]
    public void Ocr1cResetValue()
    {
        Assert.That(Cpu.ReadData(OCR1C), Is.EqualTo(0xff));
    }

    [Test(Description = "Normal mode overflows from 0xFF and ignores OCR1C")]
    public void NormalModeOverflow()
    {
        Cpu.WriteData(OCR1C, 9);
        Cpu.WriteData(TCNT1, 0xff);
        Cpu.WriteData(TCCR1, CS10);
        Start(1);
        RunTo(2);
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.ReadData(TCNT1), Is.Zero);
            Assert.That(Cpu.Mmio.Data[TIFR] & TOV1, Is.EqualTo(TOV1));
        });
    }

    [Test(Description = "Overflow interrupt uses TOV1/TOIE1 at bit 2 and vector 4")]
    public void OverflowInterrupt()
    {
        Cpu.WriteData(TIMSK, TOIE1);
        Cpu.Mmio.Data[SREG] = 0x80;
        Cpu.WriteData(TCNT1, 0xff);
        Cpu.WriteData(TCCR1, CS10);
        Start(1);
        RunTo(2);
        Cpu.Tick();
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Pc, Is.EqualTo(0x04));
            Assert.That(Cpu.Mmio.Data[TIFR] & TOV1, Is.Zero);
        });
    }

    [Test(Description = "TOIE1 is TIMSK bit 2: bit 4 (OCIE0A) does not enable the Timer1 overflow")]
    public void Bit4DoesNotEnableOverflow()
    {
        Cpu.WriteData(TIMSK, 1 << 4);
        Cpu.Mmio.Data[SREG] = 0x80;
        Cpu.WriteData(TCNT1, 0xff);
        Cpu.WriteData(TCCR1, CS10);
        Start(1);
        RunTo(2);
        Cpu.Tick();
        Assert.That(Cpu.Pc, Is.Not.EqualTo(0x04));
    }

    [Test(Description = "Prescalers above /64 follow 2^(CS-1) up to /16384")]
    public void PrescalerTable()
    {
        for (var cs = 1; cs <= 15; cs++)
        {
            Cpu.Reset();
            Cpu.Cycles = 0;
            Cpu.WriteData(TCCR1, (byte)cs);
            Start(1);
            RunTo(1 + (1UL << (cs - 1)) * 3);
            Assert.That(Cpu.ReadData(TCNT1), Is.EqualTo(3), $"CS1={cs}");
        }
    }

    [Test(Description = "PSR1 restarts the prescaler without leaving a bit set in GTCCR")]
    public void Psr1ResetsPrescaler()
    {
        Cpu.WriteData(TCCR1, CS13); // /128
        Start(1);
        RunTo(100);
        Cpu.WriteData(GTCCR, PSR1);
        RunTo(100 + 127);
        Assert.That(Cpu.ReadData(TCNT1), Is.Zero);
        RunTo(100 + 128);
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.ReadData(TCNT1), Is.EqualTo(1));
            Assert.That(Cpu.Mmio.Data[GTCCR] & PSR1, Is.Zero);
        });
    }

    [Test(Description = "A TCNT1 above OCR1C counts up to 0xFF before wrapping")]
    public void CounterAboveTop()
    {
        Cpu.WriteData(OCR1C, 9);
        Cpu.WriteData(TCNT1, 250);
        Cpu.WriteData(TCCR1, CTC1 | CS10);
        Start(1);
        RunTo(1 + 5);
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.ReadData(TCNT1), Is.EqualTo(255));
            Assert.That(Cpu.Mmio.Data[TIFR] & TOV1, Is.Zero);
        });
        RunTo(1 + 6);
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.ReadData(TCNT1), Is.Zero);
            Assert.That(Cpu.Mmio.Data[TIFR] & TOV1, Is.EqualTo(TOV1));
        });
    }

    [Test(Description = "Clearing a flag with TIFR leaves the other TC1 flags and foreign bits alone")]
    public void TifrClearsOnlyWrittenFlags()
    {
        Cpu.WriteData(OCR1C, 9);
        Cpu.WriteData(OCR1A, 0);
        Cpu.WriteData(TCNT1, 9);
        Cpu.WriteData(TCCR1, CTC1 | CS10);
        Start(1);
        RunTo(2);
        Cpu.Mmio.Data[TIFR] |= 1 << 1; // TOV0, owned by Timer0
        Cpu.WriteData(TIFR, TOV1);
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Mmio.Data[TIFR] & TOV1, Is.Zero);
            Assert.That(Cpu.Mmio.Data[TIFR] & OCF1A, Is.EqualTo(OCF1A));
            Assert.That(Cpu.Mmio.Data[TIFR] & (1 << 1), Is.EqualTo(1 << 1));
        });
    }

    [Test(Description = "Sharing TIFR with an ATmega-style Timer0 does not lose TC1 flags")]
    public void SharedTifrWithTimer0()
    {
        // Timer0 first, like ATtiny85Simulation mounts them.
        Cpu = new AVR8Sharp.Core.Cpu(new ushort[0x1000], SramByteCount);
        var timer0 = new AvrTimer(Cpu, AvrTimer.Timer0Config.CreateNew(
            tov: 2, ocfa: 16, ocfb: 8, toie: 2, ociea: 16, ocieb: 8,
            tccra: 0x4a, tccrb: 0x53, tcnt: 0x52, ocra: 0x49, ocrb: 0x48,
            tifr: TIFR, timsk: TIMSK));
        _timer = new AvrAttinyTimer1(Cpu);
        Cpu.WriteData(TCNT1, 0xff);
        Cpu.WriteData(TCCR1, CS10);
        Start(1);
        RunTo(2);
        Assert.That(Cpu.Mmio.Data[TIFR] & TOV1, Is.EqualTo(TOV1));
        Cpu.WriteData(TIFR, 1 << 1); // clear TOV0 only
        Assert.That(Cpu.Mmio.Data[TIFR] & TOV1, Is.EqualTo(TOV1));
    }

    // ── Output compare ────────────────────────────────────────────────────────

    [Test(Description = "COM1A = 10 clears OC1A (PB1) on compare match in normal mode")]
    public void CompareMatchClearsPin()
    {
        Cpu.WriteData(DDRB, 1 << PB1);
        Cpu.WriteData(PORTB, 1 << PB1);
        Cpu.WriteData(OCR1A, 5);
        Cpu.WriteData(TCCR1, COM1A1 | CS10);
        Start(1);
        Assert.That(_portB.GetPinState(PB1), Is.EqualTo(PinState.High));
        RunTo(1 + 5);
        Assert.That(_portB.GetPinState(PB1), Is.EqualTo(PinState.Low));
    }

    [Test(Description = "COM1A = 01 toggles OC1A on compare match")]
    public void CompareMatchTogglesPin()
    {
        Cpu.WriteData(DDRB, 1 << PB1);
        Cpu.WriteData(OCR1C, 9);
        Cpu.WriteData(OCR1A, 3);
        Cpu.WriteData(TCCR1, CTC1 | COM1A0 | CS10);
        Start(1);
        var first = _portB.GetPinState(PB1);
        RunTo(1 + 3);
        var second = _portB.GetPinState(PB1);
        RunTo(1 + 13);
        var third = _portB.GetPinState(PB1);
        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(third, Is.EqualTo(first));
        });
    }

    [Test(Description = "COM1A = 00 leaves PB1 under PORTB control (TCCR1 writes do not read r0 as the mode)")]
    public void NoCompareOutputWithoutCom()
    {
        Cpu.WriteData(DDRB, 1 << PB1);
        Cpu.WriteData(PORTB, 0);
        Cpu.Mmio.Data[0] = 0xff; // r0, what a missing TCCRA mapping used to be read from
        Cpu.WriteData(TCCR1, CS10);
        Start(1);
        RunTo(300);
        Assert.That(_portB.GetPinState(PB1), Is.EqualTo(PinState.Low));
    }

    [Test(Description = "FOC1A forces a compare match on OC1A in non-PWM mode and reads back as zero")]
    public void ForceOutputCompare()
    {
        Cpu.WriteData(DDRB, 1 << PB1);
        Cpu.WriteData(TCCR1, COM1A1 | COM1A0 | CS10); // set on match
        Cpu.WriteData(GTCCR, FOC1A);
        Assert.Multiple(() =>
        {
            Assert.That(_portB.GetPinState(PB1), Is.EqualTo(PinState.High));
            Assert.That(Cpu.Mmio.Data[GTCCR] & FOC1A, Is.Zero);
        });
    }

    [Test(Description = "COM1B in GTCCR drives OC1B (PB4) on compare match")]
    public void CompareMatchPinB()
    {
        Cpu.WriteData(DDRB, 1 << PB4);
        Cpu.WriteData(PORTB, 1 << PB4);
        Cpu.WriteData(OCR1B, 4);
        Cpu.WriteData(GTCCR, COM1B1);
        Cpu.WriteData(TCCR1, CS10);
        Start(1);
        RunTo(1 + 4);
        Assert.That(_portB.GetPinState(PB4), Is.EqualTo(PinState.Low));
    }

    // ── PWM ───────────────────────────────────────────────────────────────────

    [Test(Description = "PWM1A with COM1A = 10 sets OC1A at the period start and clears it at OCR1A")]
    public void PwmA()
    {
        Cpu.WriteData(DDRB, 1 << PB1);
        Cpu.WriteData(OCR1C, 99);
        Cpu.WriteData(OCR1A, 25);
        Cpu.WriteData(TCCR1, PWM1A | COM1A1 | CS10);
        Start(1);
        RunTo(1 + 10);
        Assert.That(_portB.GetPinState(PB1), Is.EqualTo(PinState.High), "before the match");
        RunTo(1 + 25);
        Assert.That(_portB.GetPinState(PB1), Is.EqualTo(PinState.Low), "after the match");
        RunTo(1 + 100 + 5);
        Assert.That(_portB.GetPinState(PB1), Is.EqualTo(PinState.High), "next period");
    }

    [Test(Description = "PWM1A counts 0..OCR1C (single slope) and sets TOV1 at the wrap")]
    public void PwmSingleSlope()
    {
        Cpu.WriteData(OCR1C, 99);
        Cpu.WriteData(TCCR1, PWM1A | CS10);
        Start(1);
        RunTo(1 + 99);
        Assert.That(Cpu.ReadData(TCNT1), Is.EqualTo(99));
        Assert.That(Cpu.Mmio.Data[TIFR] & TOV1, Is.Zero);
        RunTo(1 + 100);
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.ReadData(TCNT1), Is.Zero);
            Assert.That(Cpu.Mmio.Data[TIFR] & TOV1, Is.EqualTo(TOV1));
        });
    }

    [Test(Description = "COM1A = 11 inverts the PWM output")]
    public void PwmInverting()
    {
        Cpu.WriteData(DDRB, 1 << PB1);
        Cpu.WriteData(OCR1C, 99);
        Cpu.WriteData(OCR1A, 25);
        Cpu.WriteData(TCCR1, PWM1A | COM1A1 | COM1A0 | CS10);
        Start(1);
        RunTo(1 + 10);
        Assert.That(_portB.GetPinState(PB1), Is.EqualTo(PinState.Low));
        RunTo(1 + 25);
        Assert.That(_portB.GetPinState(PB1), Is.EqualTo(PinState.High));
    }

    [Test(Description = "PWM1B (GTCCR) drives OC1B (PB4) with OCR1B and OCR1C as TOP")]
    public void PwmB()
    {
        Cpu.WriteData(DDRB, 1 << PB4);
        Cpu.WriteData(OCR1C, 99);
        Cpu.WriteData(OCR1B, 40);
        Cpu.WriteData(GTCCR, PWM1B | COM1B1);
        Cpu.WriteData(TCCR1, CS10);
        Start(1);
        RunTo(1 + 10);
        Assert.That(_portB.GetPinState(PB4), Is.EqualTo(PinState.High));
        RunTo(1 + 40);
        Assert.That(_portB.GetPinState(PB4), Is.EqualTo(PinState.Low));
    }

    [Test(Description = "A TCNT1 write on a stopped timer is visible when it is read back")]
    public void TcntWrite()
    {
        Cpu.WriteData(TCNT1, 77);
        Cpu.Tick();
        Assert.That(Cpu.ReadData(TCNT1), Is.EqualTo(77));
    }
}
