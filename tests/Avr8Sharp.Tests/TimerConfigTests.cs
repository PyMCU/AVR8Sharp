using AVR8Sharp.Core.Peripherals;

namespace Avr8Sharp.Tests;

[TestFixture]
public class TimerConfigTests
{
    [Test]
    public void CreateNew_KeepsSourceValues_WhenNothingIsPassed()
    {
        var copy = AvrTimer.Timer1Config.CreateNew();

        Assert.That(copy.ComparatorPinA, Is.EqualTo(AvrTimer.Timer1Config.ComparatorPinA));
        Assert.That(copy.ComparatorPortB, Is.EqualTo(AvrTimer.Timer1Config.ComparatorPortB));
        Assert.That(copy.ICF, Is.EqualTo(AvrTimer.Timer1Config.ICF));
        Assert.That(copy.TCNT, Is.EqualTo(AvrTimer.Timer1Config.TCNT));
    }

    [Test]
    public void CreateNew_HonoursExplicitZero_ForPinAndBit()
    {
        var copy = AvrTimer.Timer1Config.CreateNew(comparatorPinA: 0, externalClockPin: 0, tov: 0);

        Assert.That(AvrTimer.Timer1Config.ComparatorPinA, Is.Not.Zero);
        Assert.That(copy.ComparatorPinA, Is.Zero);
        Assert.That(copy.ExternalClockPin, Is.Zero);
        Assert.That(copy.TOV, Is.Zero);
    }

    [Test]
    public void Mega2560_TimerPins_FollowTheDatasheet()
    {
        var portB = AvrIoPort.Mega2560PortBConfig.PORT;

        Assert.That(AvrTimer.Mega2560Timer0Config.ComparatorPortA, Is.EqualTo(portB));
        Assert.That(AvrTimer.Mega2560Timer0Config.ComparatorPinA, Is.EqualTo(7));
        Assert.That(AvrTimer.Mega2560Timer0Config.ComparatorPortB, Is.EqualTo(AvrIoPort.Mega2560PortGConfig.PORT));
        Assert.That(AvrTimer.Mega2560Timer0Config.ComparatorPinB, Is.EqualTo(5));
        Assert.That(AvrTimer.Mega2560Timer1Config.ComparatorPinC, Is.EqualTo(7));
        Assert.That(AvrTimer.Mega2560Timer1Config.OCRC, Is.EqualTo(0x8c));
        Assert.That(AvrTimer.Mega2560Timer2Config.ComparatorPortB, Is.EqualTo(AvrIoPort.Mega2560PortHConfig.PORT));
        Assert.That(AvrTimer.Mega2560Timer2Config.ComparatorPinB, Is.EqualTo(6));
        Assert.That(AvrTimer.Mega2560Timer4Config.ExternalClockPin, Is.EqualTo(7));
        Assert.That(AvrTimer.Mega2560Timer3Config.IcpPin, Is.EqualTo(7));
        Assert.That(AvrTimer.Mega2560Timer4Config.IcpPort, Is.EqualTo(AvrIoPort.Mega2560PortLConfig.PORT));
        foreach (var t in new[] { AvrTimer.Mega2560Timer1Config, AvrTimer.Mega2560Timer3Config, AvrTimer.Mega2560Timer4Config, AvrTimer.Mega2560Timer5Config })
        {
            Assert.That(t.ICF, Is.EqualTo(0x20));
            Assert.That(t.ICIE, Is.EqualTo(0x20));
        }
    }
}
