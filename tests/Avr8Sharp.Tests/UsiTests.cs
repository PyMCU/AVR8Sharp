using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

[TestFixture]
public class Usi : AvrTestBase
{
    // USI registers (data-space addresses)
    const int USICR = 0x2d;
    const int USISR = 0x2e;
    const int USIDR = 0x2f;
    const int USIBR = 0x30;

    // USISR bits
    const int USICNT_MASK = 0x0f;
    const int USIOIF = 1 << 6;
    const int USISIF = 1 << 7;

    // USICR bits
    const int USICLK = 1 << 1;
    const int USIWM0 = 1 << 4; // 3-wire mode
    const int USIWM1 = 1 << 5; // two-wire mode

    // PortB registers (data-space addresses from PortBConfig: pin=0x23, ddr=0x24, port=0x25)
    const int PINB = 0x23;
    const int DDRB = 0x24;
    const int PORTB = 0x25;

    const int DATA_PIN = 0; // bit 0 = SDA
    const int CLOCK_PIN = 2; // bit 2 = SCL

    private AvrIoPort _portB;
    private AvrUsi _usi;

    protected override void SetupPeripherals()
    {
        _portB = new AvrIoPort(Cpu, AvrIoPort.PortBConfig);
        _usi = new AvrUsi(Cpu, _portB, (int)AvrIoPort.PortBConfig.PIN,
            dataPin: DATA_PIN, clockPin: CLOCK_PIN);
    }

    [Test(Description = "3-wire mode: USIDR shifts and USIOIF fires after 16 software clocks")]
    public void ThreeWire_ShiftAndOverflow()
    {
        // Seed USIDR with a known pattern; data pin is 0 (input low), so each
        // shift appends a 0 at bit 0.
        Cpu.Mmio.Data[USIDR] = 0b10101010;

        // 16 software-clock edges: USICLK=1, 3-wire mode (USIWM0), software clock source (USICS=00)
        for (var i = 0; i < 16; i++)
            Cpu.WriteData((ushort)USICR, (byte)(USICLK | USIWM0));

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Mmio.Data[USISR] & USIOIF, Is.EqualTo(USIOIF),
                "USIOIF must be set after 16 shift-clock edges");
            Assert.That(Cpu.Mmio.Data[USISR] & USICNT_MASK, Is.Zero,
                "USICNT must have wrapped to 0 on overflow");
            // USIBR is latched from USIDR at overflow
            Assert.That(Cpu.Mmio.Data[USIBR], Is.EqualTo(Cpu.Mmio.Data[USIDR]),
                "USIBR must be latched from USIDR on overflow");
        });
    }

    [Test(Description = "Two-wire mode: USISIF is set when start condition (SCL=1, SDA=0) is detected")]
    public void TwoWire_StartCondition()
    {
        // Enable two-wire mode
        Cpu.WriteData((ushort)USICR, (byte)USIWM1);

        // Configure SCL and SDA as outputs so WriteGpio sees the values
        Cpu.WriteData((ushort)DDRB, (byte)((1 << CLOCK_PIN) | (1 << DATA_PIN)));

        // Set SCL=1, SDA=1 (idle state)
        Cpu.WriteData((ushort)PORTB, (byte)((1 << CLOCK_PIN) | (1 << DATA_PIN)));

        bool callbackFired = false;
        _usi.OnStartCondition = () => callbackFired = true;

        // Drive SDA low while SCL is high → I2C start condition
        Cpu.WriteData((ushort)PORTB, (byte)(1 << CLOCK_PIN)); // SCL=1, SDA=0

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Mmio.Data[USISR] & USISIF, Is.EqualTo(USISIF),
                "USISIF must be set on start condition detection");
            Assert.That(callbackFired, Is.True,
                "OnStartCondition callback must be invoked");
        });
    }

    [Test(Description = "Two-wire mode: USIPF is set and OnStopCondition fires on stop condition (SCL=1, SDA=1)")]
    public void TwoWire_StopCondition()
    {
        Cpu.WriteData((ushort)USICR, (byte)USIWM1);
        Cpu.WriteData((ushort)DDRB, (byte)((1 << CLOCK_PIN) | (1 << DATA_PIN)));

        // Start with SCL=1, SDA=0 (after a data bit)
        Cpu.WriteData((ushort)PORTB, (byte)(1 << CLOCK_PIN));

        bool callbackFired = false;
        _usi.OnStopCondition = () => callbackFired = true;

        const int USIPF = 1 << 5;

        // Drive SDA high while SCL is high → stop condition
        Cpu.WriteData((ushort)PORTB, (byte)((1 << CLOCK_PIN) | (1 << DATA_PIN)));

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Mmio.Data[USISR] & USIPF, Is.EqualTo(USIPF),
                "USIPF must be set on stop condition detection");
            Assert.That(callbackFired, Is.True,
                "OnStopCondition callback must be invoked");
        });
    }

    [Test(Description = "Two-wire mode: Start condition must be edge-triggered and not fire repeatedly")]
    public void TwoWire_StartCondition_FiresOnlyOnEdge()
    {
        Cpu.WriteData((ushort)USICR, (byte)USIWM1);
        Cpu.WriteData((ushort)DDRB, (byte)((1 << CLOCK_PIN) | (1 << DATA_PIN)));

        Cpu.WriteData((ushort)PORTB, (byte)((1 << CLOCK_PIN) | (1 << DATA_PIN)));

        int startCount = 0;
        _usi.OnStartCondition = () => startCount++;

        Cpu.WriteData((ushort)PORTB, (byte)(1 << CLOCK_PIN));

        Cpu.WriteData((ushort)PORTB, (byte)((1 << CLOCK_PIN) | 0b10000));

        Assert.Multiple(() =>
        {
            Assert.That(startCount, Is.EqualTo(1),
                "OnStartCondition debe dispararse exactamente UNA vez en el flanco");
            Assert.That(Cpu.Mmio.Data[USISR] & USISIF, Is.EqualTo(USISIF),
                "El flag USISIF debe estar activo");
        });
    }
}