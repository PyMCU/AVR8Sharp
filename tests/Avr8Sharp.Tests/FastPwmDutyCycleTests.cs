using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

/// <summary>
/// Regression tests for issue #17: in fast PWM the compare output stays active for
/// OCR + 1 counts, not OCR. Measured on an Arduino Uno with an FNIRSI 2C53T on D6
/// on 2026-09-14: TCCR0A 0x83, TCCR0B 0x03, OCR0A 128, 16 MHz reads 50.32 % of a
/// 1024 us period, that is 129 of 256 counts.
/// <para>
/// Every measurement here runs the timer at prescaler 1 and samples the compare
/// output pin once per timer count, so a count of high samples over one full period
/// is the pulse width in timer counts.
/// </para>
/// </summary>
[TestFixture]
public class FastPwmDutyCycle : AvrTestBase
{
    // Port D
    private const int DDRD = 0x2a;
    // Port B
    private const int DDRB = 0x24;

    // Timer 0
    private const int TCCR0A = 0x44;
    private const int TCCR0B = 0x45;
    private const int OCR0A = 0x47;
    private const int OCR0B = 0x48;

    // Timer 1
    private const int TCCR1A = 0x80;
    private const int TCCR1B = 0x81;
    private const int OCR1A = 0x88;
    private const int OCR1AH = 0x89;

    // Timer 2
    private const int TCCR2A = 0xb0;
    private const int TCCR2B = 0xb1;
    private const int OCR2A = 0xb3;

    private const int COM_NON_INVERTING_A = 0x80; // COM0A1
    private const int COM_INVERTING_A = 0xc0;     // COM0A1 | COM0A0
    private const int COM_NON_INVERTING_B = 0x20; // COM0B1
    private const int FAST_PWM_8BIT = 0x03;       // WGM01 | WGM00
    private const int CS_1 = 0x01;

    private AvrTimer _timer0;
    private AvrTimer _timer1;
    private AvrTimer _timer2;
    private AvrIoPort _portB;
    private AvrIoPort _portD;

    protected override void SetupPeripherals()
    {
        _timer0 = new AvrTimer(Cpu, AvrTimer.Timer0Config);
        _timer1 = new AvrTimer(Cpu, AvrTimer.Timer1Config);
        _timer2 = new AvrTimer(Cpu, AvrTimer.Timer2Config);
        _portB = new AvrIoPort(Cpu, AvrIoPort.PortBConfig);
        _portD = new AvrIoPort(Cpu, AvrIoPort.PortDConfig);
    }

    [TestCase(128, 129, TestName = "OC0A high for 129 counts when OCR0A is 128 (the value the scope read)")]
    [TestCase(0, 1, TestName = "OC0A high for a single count when OCR0A is BOTTOM")]
    [TestCase(255, 256, TestName = "OC0A constantly high when OCR0A is MAX")]
    [TestCase(19, 20, TestName = "OC0A high for 20 counts when OCR0A is 19 (a 50 Hz servo pulse)")]
    [TestCase(64, 65, TestName = "OC0A high for 65 counts when OCR0A is 64")]
    [TestCase(192, 193, TestName = "OC0A high for 193 counts when OCR0A is 192")]
    public void Timer0_NonInverting_HighForOcrPlusOne(int ocr, int expectedHigh)
    {
        Cpu.WriteData(DDRD, 0x40); // PD6 (OC0A) as output
        Cpu.WriteData(OCR0A, (byte)ocr);
        Cpu.WriteData(TCCR0A, COM_NON_INVERTING_A | FAST_PWM_8BIT);
        Cpu.WriteData(TCCR0B, CS_1);

        Assert.That(MeasureHighCounts(_portD, 6, 256), Is.EqualTo(expectedHigh));
    }

    [TestCase(128, 127, TestName = "OC0A low for 129 counts when OCR0A is 128 and the output is inverted")]
    [TestCase(0, 255, TestName = "OC0A high for all but one count when OCR0A is BOTTOM and the output is inverted")]
    [TestCase(255, 0, TestName = "OC0A constantly low when OCR0A is MAX and the output is inverted")]
    public void Timer0_Inverting_LowForOcrPlusOne(int ocr, int expectedHigh)
    {
        Cpu.WriteData(DDRD, 0x40);
        Cpu.WriteData(OCR0A, (byte)ocr);
        Cpu.WriteData(TCCR0A, COM_INVERTING_A | FAST_PWM_8BIT);
        Cpu.WriteData(TCCR0B, CS_1);

        Assert.That(MeasureHighCounts(_portD, 6, 256), Is.EqualTo(expectedHigh));
    }

    [Test(Description = "OC0B, the second channel of timer 0, is high for OCR0B + 1 counts (issue #17)")]
    public void Timer0_ChannelB_HighForOcrPlusOne()
    {
        Cpu.WriteData(DDRD, 0x20); // PD5 (OC0B) as output
        Cpu.WriteData(OCR0B, 64);
        Cpu.WriteData(TCCR0A, COM_NON_INVERTING_B | FAST_PWM_8BIT);
        Cpu.WriteData(TCCR0B, CS_1);

        Assert.That(MeasureHighCounts(_portD, 5, 256), Is.EqualTo(65));
    }

    [TestCase(128, 129, TestName = "OC1A high for 129 counts when OCR1A is 128")]
    [TestCase(255, 256, TestName = "OC1A constantly high when OCR1A is MAX of the 8-bit mode")]
    public void Timer1_FastPwm8Bit_HighForOcrPlusOne(int ocr, int expectedHigh)
    {
        Cpu.WriteData(DDRB, 0x02); // PB1 (OC1A) as output
        Cpu.WriteData(OCR1AH, 0x00);
        Cpu.WriteData(OCR1A, (byte)ocr);
        // WGM13:0 = 0101, fast PWM with TOP = 0x00ff
        Cpu.WriteData(TCCR1A, COM_NON_INVERTING_A | 0x01);
        Cpu.WriteData(TCCR1B, 0x08 | CS_1);

        Assert.That(MeasureHighCounts(_portB, 1, 256), Is.EqualTo(expectedHigh));
    }

    [TestCase(128, 129, TestName = "OC2A high for 129 counts when OCR2A is 128")]
    [TestCase(255, 256, TestName = "OC2A constantly high when OCR2A is MAX")]
    [TestCase(0, 1, TestName = "OC2A high for a single count when OCR2A is BOTTOM")]
    public void Timer2_FastPwm_HighForOcrPlusOne(int ocr, int expectedHigh)
    {
        Cpu.WriteData(DDRB, 0x08); // PB3 (OC2A) as output
        Cpu.WriteData(OCR2A, (byte)ocr);
        Cpu.WriteData(TCCR2A, COM_NON_INVERTING_A | FAST_PWM_8BIT);
        Cpu.WriteData(TCCR2B, CS_1);

        Assert.That(MeasureHighCounts(_portB, 3, 256), Is.EqualTo(expectedHigh));
    }

    /// <summary>
    /// Phase correct PWM is high for 2 x OCR of the 2 x TOP counts of its period, so
    /// the duty is OCR / TOP with both extremes saturating. This is what the datasheet
    /// asks for and what the emulator already did, so it is here as a guard that the
    /// fast PWM change left it alone.
    /// </summary>
    [TestCase(128, 256, TestName = "Phase correct PWM is high for 2 x OCR counts")]
    [TestCase(255, 510, TestName = "Phase correct PWM is constantly high when OCR0A is MAX")]
    [TestCase(0, 0, TestName = "Phase correct PWM is constantly low when OCR0A is BOTTOM")]
    public void Timer0_PhaseCorrect_HighForTwiceOcr(int ocr, int expectedHigh)
    {
        Cpu.WriteData(DDRD, 0x40);
        Cpu.WriteData(OCR0A, (byte)ocr);
        Cpu.WriteData(TCCR0A, COM_NON_INVERTING_A | 0x01); // WGM00 only: phase correct, TOP = 0xff
        Cpu.WriteData(TCCR0B, CS_1);

        Assert.That(MeasureHighCounts(_portD, 6, 510), Is.EqualTo(expectedHigh));
    }

    /// <summary>
    /// Runs the timer for two whole periods at one count per cycle and counts how many
    /// counts of the second period leave the pin high. The first period is discarded so
    /// the measurement starts from a settled waveform, exactly as a scope trigger would.
    /// </summary>
    private int MeasureHighCounts(AvrIoPort port, byte pin, int periodCounts)
    {
        var cycle = Cpu.Cycles;

        for (var i = 0; i < periodCounts; i++)
        {
            Cpu.Cycles = ++cycle;
            Cpu.Tick();
        }

        var high = 0;
        for (var i = 0; i < periodCounts; i++)
        {
            Cpu.Cycles = ++cycle;
            Cpu.Tick();
            if (port.GetPinState(pin) == PinState.High) high++;
        }

        return high;
    }
}
