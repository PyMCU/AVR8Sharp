using AVR8Sharp.Core.Cpu;
namespace Avr8Sharp.Tests;

[TestFixture]
public class Interrupt
{
	[Test(Description = "The interrupt handler should be executed")]
	public void Interrupt_Handler ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu(new ushort[0x8000]);

		cpu.Pc = 0x520;
		cpu.Mmio.Data[94] = 0;
		cpu.Mmio.Data[93] = 0x80; // SP <- 0x80
		cpu.Mmio.Data[95] = 0b10000001; // SREG <- I------C

		AvrInterrupt.DoAvrInterrupt (cpu, 5);

		Assert.Multiple(() =>
		{
			Assert.That(cpu.Cycles, Is.EqualTo(3)); // 2 for PC push + 1 for vector fetch (AVR spec: 4 cycles total)
			Assert.That(cpu.Pc, Is.EqualTo(5));
			Assert.That(cpu.Mmio.Data[93], Is.EqualTo(0x7E)); // SP <- 0x7E
			Assert.That(cpu.Mmio.Data[0x80], Is.EqualTo(0x20)); // Return address low byte
			Assert.That(cpu.Mmio.Data[0x7F], Is.EqualTo(0x5)); // Return address high byte
			Assert.That(cpu.Mmio.Data[95], Is.EqualTo(0b00000001)); // SREG <- -------C
		});
	}

	[Test(Description = "Push a 3-byte return address when running in 22-bit PC mode (issue #58)")]
	public void AVRJS_Issue_58 ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu(new ushort[0x80000]);

		Assert.That(cpu.Pc22Bits, Is.True);

		cpu.Pc = 0x10520;
		cpu.Mmio.Data[94] = 0;
		cpu.Mmio.Data[93] = 0x80; // SP <- 0x80
		cpu.Mmio.Data[95] = 0b10000001; // SREG <- I------C

		AvrInterrupt.DoAvrInterrupt (cpu, 5);

		Assert.Multiple(() =>
		{
			Assert.That(cpu.Cycles, Is.EqualTo(3)); // 2 for PC push + 1 for vector fetch (AVR spec: 4 cycles total)
			Assert.That(cpu.Pc, Is.EqualTo(5));
			Assert.That(cpu.Mmio.Data[93], Is.EqualTo(0x7D)); // SP <- 0x7D
			Assert.That(cpu.Mmio.Data[0x80], Is.EqualTo(0x20)); // Return address low byte
			Assert.That(cpu.Mmio.Data[0x7F], Is.EqualTo(0x5)); // Return address high byte
			Assert.That(cpu.Mmio.Data[0x7E], Is.EqualTo(0x1)); // Return address high byte
			Assert.That(cpu.Mmio.Data[95], Is.EqualTo(0b00000001)); // SREG <- -------C
		});
	}

	[Test(Description = "Interrupt latency must be exactly 3 cycles from DoAvrInterrupt (4 total incl. executing instruction)")]
	public void InterruptLatency_ThreeCycles ()
	{
		// AVR spec: after a pending interrupt is recognized, the current instruction
		// finishes (1 cycle), then the PC is pushed onto the stack (2 cycles),
		// then the vector address is fetched (1 cycle) = 4 cycles total.
		// DoAvrInterrupt is called *after* the instruction cycle, so it must add exactly 3.
		var cpu = new AVR8Sharp.Core.Cpu.Cpu(new ushort[0x8000]);
		cpu.Mmio.Data[93] = 0x80;
		cpu.Mmio.Data[95] = 0b10000001; // I flag + C flag

		var cyclesBefore = cpu.Cycles;
		AvrInterrupt.DoAvrInterrupt(cpu, 10);
		Assert.That(cpu.Cycles - cyclesBefore, Is.EqualTo(3));
	}

	[Test(Description = "SREG is cleared to zero on CPU Reset")]
	public void SregClearedOnReset ()
	{
		var cpu = new AVR8Sharp.Core.Cpu.Cpu(new ushort[0x1000]);
		// Dirty SREG with all bits set
		cpu.Mmio.Data[95] = 0xFF;
		cpu.Reset();
		Assert.That(cpu.Mmio.Data[95], Is.EqualTo(0),
			"SREG must be 0 after Reset (AVR spec: all I/O registers return to reset state)");
	}

	[Test(Description = "BREAK instruction (0x9598) invokes OnBreakpoint callback in SwitchDecoder")]
	public void BreakInstruction_SwitchDecoder ()
	{
		// 0x9598 = BREAK opcode
		var program = new ushort[] { 0x9598 };
		var cpu = new AVR8Sharp.Core.Cpu.Cpu(program);

		uint? capturedPc = null;
		AvrInterrupt.OnBreakpoint = pc => capturedPc = pc;
		try
		{
			var decoder = new AVR8Sharp.Core.Cpu.Decoders.SwitchDecoder();
			decoder.Decode(cpu);
			Assert.That(capturedPc, Is.Not.Null, "OnBreakpoint must be called by BREAK instruction");
		}
		finally
		{
			AvrInterrupt.OnBreakpoint = null;
		}
	}

	[Test(Description = "BREAK instruction (0x9598) invokes OnBreakpoint callback in LutDecoder")]
	public void BreakInstruction_LutDecoder ()
	{
		var program = new ushort[] { 0x9598 };
		var cpu = new AVR8Sharp.Core.Cpu.Cpu(program);

		uint? capturedPc = null;
		AvrInterrupt.OnBreakpoint = pc => capturedPc = pc;
		try
		{
			var decoder = new AVR8Sharp.Core.Cpu.Decoders.LutDecoder();
			decoder.Decode(cpu);
			Assert.That(capturedPc, Is.Not.Null);
		}
		finally
		{
			AvrInterrupt.OnBreakpoint = null;
		}
	}

	[Test(Description = "BREAK instruction (0x9598) invokes OnBreakpoint callback in NativeLutDecoder")]
	public void BreakInstruction_NativeLutDecoder ()
	{
		var program = new ushort[] { 0x9598 };
		var cpu = new AVR8Sharp.Core.Cpu.Cpu(program);

		uint? capturedPc = null;
		AvrInterrupt.OnBreakpoint = pc => capturedPc = pc;
		try
		{
			var decoder = new AVR8Sharp.Core.Cpu.Decoders.NativeLutDecoder();
			decoder.Decode(cpu);
			Assert.That(capturedPc, Is.Not.Null);
		}
		finally
		{
			AvrInterrupt.OnBreakpoint = null;
		}
	}

	[Test(Description = "BREAK without OnBreakpoint registered does not crash")]
	public void BreakInstruction_NoCallback ()
	{
		var program = new ushort[] { 0x9598 };
		var cpu = new AVR8Sharp.Core.Cpu.Cpu(program);
		AvrInterrupt.OnBreakpoint = null;

		Assert.DoesNotThrow(() =>
		{
			var decoder = new AVR8Sharp.Core.Cpu.Decoders.SwitchDecoder();
			decoder.Decode(cpu);
		});
	}
}
