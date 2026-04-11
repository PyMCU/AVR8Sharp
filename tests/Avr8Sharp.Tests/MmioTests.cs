using AVR8Sharp.Core.Memory;

namespace Avr8Sharp.Tests;

[TestFixture]
public class MmioTests
{
    [Test(Description = "Should respect write masks and preserve non-masked bits")]
    public void WriteRegister_RespectsMask()
    {
        var mmio = new MmioController(0x100);
        const byte addr = 0x50;
        mmio.Data[addr] = 0b1010_1010;

        mmio.WriteData(addr, 0b1111_1111, 0b1000_0001);

        Assert.That(mmio.Data[addr], Is.EqualTo(0xAB));
    }

    [Test(Description = "Hooks should be able to veto a write by returning true")]
    public void WriteRegister_HookCanVetoWrite()
    {
        var mmio = new MmioController(0x100);
        const byte addr = 0x60;
        mmio.Data[addr] = 0x11;

        mmio.RegisterWrite(addr, (val, old, a, m) => true);

        mmio.WriteData(addr, 0xFF);

        Assert.That(mmio.Data[addr], Is.EqualTo(0x11), "La escritura debió ser ignorada por el hook");
    }

    [Test(Description = "Hooks should receive the correct mask and old value")]
    public void WriteRegister_HookReceivesCorrectContext()
    {
        var mmio = new MmioController(0x100);
        const byte addr = 0x70;
        mmio.Data[addr] = 0xAA;

        byte capturedOldValue = 0;
        byte capturedMask = 0;

        mmio.RegisterWrite(addr, (val, old, a, m) => {
            capturedOldValue = old;
            capturedMask = m;
            return false;
        });

        mmio.WriteData(addr, 0xBB, 0x0F);

        Assert.Multiple(() => {
            Assert.That(capturedOldValue, Is.EqualTo(0xAA));
            Assert.That(capturedMask, Is.EqualTo(0x0F));
            Assert.That(mmio.Data[addr], Is.EqualTo(0xAB));
        });
    }
}