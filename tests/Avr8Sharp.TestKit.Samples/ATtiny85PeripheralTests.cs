using Avr8Sharp.TestKit;
using Avr8Sharp.TestKit.Boards;

namespace Avr8Sharp.TestKit.Samples;

/// <summary>
/// ATtiny85 peripheral wiring: EEPROM address registers, Timer1 and INT0.
/// </summary>
[TestFixture]
public class ATtiny85PeripheralTests
{
    private const int EECR = 0x3C;
    private const int EEDR = 0x3D;
    private const int TIFR = 0x58;
    private const int GIFR = 0x5A;
    private const int TCNT1 = 0x4F;

    [Test]
    public void Eeprom_WriteWithInterruptsEnabled_DoesNotReadR0AsAddressHigh()
    {
        // avr-libc's eeprom_write_byte saves SREG in r0 before the timed sequence. With
        // interrupts enabled r0 is 0x80, which must not leak into the EEPROM address.
        var tiny = new ATtiny85Simulation();
        tiny.WithAsm(@"
            sei
            in   r0, 0x3F        ; r0 = SREG = 0x80
            ldi  r16, 0x00
            out  0x1F, r16       ; EEARH
            ldi  r16, 0x05
            out  0x1E, r16       ; EEARL = 5
            ldi  r16, 0x42
            out  0x1D, r16       ; EEDR
            ldi  r16, 0x04
            out  0x1C, r16       ; EEMPE
            ldi  r16, 0x02
            out  0x1C, r16       ; EEPE
            done: rjmp done
        ");

        Assert.DoesNotThrow(() => tiny.RunInstructions(20));
        tiny.RunMilliseconds(10);

        tiny.Cpu.WriteData(EECR, 0x01); // EERE
        Assert.That(tiny.Data[EEDR], Is.EqualTo(0x42));
    }

    [Test]
    public void Eeprom_AddressAboveTheArray_WrapsInsteadOfThrowing()
    {
        var tiny = new ATtiny85Simulation();
        tiny.WithAsm(@"
            ldi  r16, 0x82
            out  0x1F, r16       ; EEARH with unused high bits set
            ldi  r16, 0x05
            out  0x1E, r16
            ldi  r16, 0x01
            out  0x1C, r16       ; EERE
            break
        ");

        Assert.DoesNotThrow(() => tiny.RunToBreak());
    }

    [Test]
    public void Timer1_TccrWrite_DoesNotFreezePb1High()
    {
        // TCCR1 used to be mapped as TCCRB with TCCRA at address 0, so r0 was read as COM1A.
        var tiny = new ATtiny85Simulation();
        tiny.WithAsm(@"
            ldi  r16, 0xFF
            mov  r0, r16
            ldi  r16, 0x02
            out  0x17, r16       ; DDRB: PB1 output
            ldi  r16, 0x01
            out  0x30, r16       ; TCCR1 = CS10
            done: rjmp done
        ");

        tiny.RunInstructions(20);
        tiny.RunMicroseconds(100);

        tiny.PortB.Should().HavePinLow(1);
        Assert.That(tiny.Cpu.ReadData(TCNT1), Is.Not.Zero);
    }

    [Test]
    public void Timer1_CtcMode_SetsTov1AtOcr1cAndNotTheTimer0Bit()
    {
        var tiny = new ATtiny85Simulation();
        tiny.WithAsm(@"
            ldi  r16, 99
            out  0x2D, r16       ; OCR1C = 99
            ldi  r16, 0x81
            out  0x30, r16       ; TCCR1 = CTC1 | CS10
            done: rjmp done
        ");

        tiny.RunInstructions(20);
        tiny.RunCycles(150);

        Assert.Multiple(() =>
        {
            Assert.That(tiny.Data[TIFR] & 0x04, Is.EqualTo(0x04), "TOV1");
            Assert.That(tiny.Data[TIFR] & 0x10, Is.Zero, "OCF0A must stay clear");
        });
    }

    [Test]
    public void Timer1_Prescaler16384_IsSupported()
    {
        var tiny = new ATtiny85Simulation();
        tiny.WithAsm(@"
            ldi  r16, 0x0F
            out  0x30, r16       ; CS1 = 15 -> /16384
            done: rjmp done
        ");

        tiny.RunInstructions(20);
        tiny.RunCycles(16384 * 3 + 16);

        Assert.That(tiny.Cpu.ReadData(TCNT1), Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void Int0_FallingEdgeOnPb2_SetsIntf0()
    {
        var tiny = new ATtiny85Simulation();
        tiny.WithAsm(@"
            ldi  r16, 0x40
            out  0x3B, r16       ; GIMSK: INT0
            ldi  r16, 0x02
            out  0x35, r16       ; MCUCR: ISC01 = falling edge
            break
        ");

        tiny.RunToBreak();
        tiny.PortB.SetPinValue(2, true);
        Assert.That(tiny.Data[GIFR] & 0x40, Is.Zero);
        tiny.PortB.SetPinValue(2, false);

        Assert.That(tiny.Data[GIFR] & 0x40, Is.EqualTo(0x40));
    }
}
