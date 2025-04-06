using AVR8Sharp.Cpu;
namespace AVr8SharpTests;

[TestFixture]
public class Interrupt
{
	[Test(Description = "The interrupt handler should be executed")]
	public void Interrupt_Handler ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[0x8000]);
		
		cpu.PC = 0x520;
		cpu.Data[94] = 0;
		cpu.Data[93] = 0x80; // SP <- 0x80
		cpu.Data[95] = 0b10000001; // SREG <- I------C
		
		AvrInterrupt.DoAvrInterrupt (cpu, 5);
		
		Assert.Multiple(() =>
		{
			Assert.That(cpu.Cycles, Is.EqualTo(2));
			Assert.That(cpu.PC, Is.EqualTo(5));
			Assert.That(cpu.Data[93], Is.EqualTo(0x7E)); // SP <- 0x7E
			Assert.That(cpu.Data[0x80], Is.EqualTo(0x20)); // Return address low byte 
			Assert.That(cpu.Data[0x7F], Is.EqualTo(0x5)); // Return address high byte
			Assert.That(cpu.Data[95], Is.EqualTo(0b00000001)); // SREG <- -------C
		});
	}

	[Test(Description = "Push a 3-byte return address when running in 22-bit PC mode (issue #58)")]
	public void AVRJS_Issue_58 ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[0x80000]);
		
		Assert.That(cpu.PC22Bits, Is.True);
		
		cpu.PC = 0x10520;
		cpu.Data[94] = 0;
		cpu.Data[93] = 0x80; // SP <- 0x80
		cpu.Data[95] = 0b10000001; // SREG <- I------C
		
		AvrInterrupt.DoAvrInterrupt (cpu, 5);
		
		Assert.Multiple(() =>
		{
			Assert.That(cpu.Cycles, Is.EqualTo(2));
			Assert.That(cpu.PC, Is.EqualTo(5));
			Assert.That(cpu.Data[93], Is.EqualTo(0x7D)); // SP <- 0x7D
			Assert.That(cpu.Data[0x80], Is.EqualTo(0x20)); // Return address low byte 
			Assert.That(cpu.Data[0x7F], Is.EqualTo(0x5)); // Return address high byte
			Assert.That(cpu.Data[0x7E], Is.EqualTo(0x1)); // Return address high byte
			Assert.That(cpu.Data[95], Is.EqualTo(0b00000001)); // SREG <- -------C
		});
	}
}
