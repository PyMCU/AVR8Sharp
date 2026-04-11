using AVR8Sharp.Core;
using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

[TestFixture]
public class Interrupt : AvrTestBase
{
	protected override int FlashByteCount => 0x80000;

	[Test(Description = "The interrupt handler should be executed")]
	public void Interrupt_Handler ()
	{
		Cpu = new AVR8Sharp.Core.Cpu(new ushort[0x8000])
		{
			Pc = 0x520
		};

		Cpu.Mmio.Data[94] = 0;
		Cpu.Mmio.Data[93] = 0x80; // SP <- 0x80
		Cpu.Mmio.Data[95] = 0b10000001; // SREG <- I------C

		AvrInterrupt.DoAvrInterrupt (Cpu, 5);

		Assert.Multiple(() =>
		{
			Assert.That(Cpu.Cycles, Is.EqualTo(3)); // 2 for PC push + 1 for vector fetch (AVR spec: 4 cycles total)
			Assert.That(Cpu.Pc, Is.EqualTo(5));
			Assert.That(Cpu.Mmio.Data[93], Is.EqualTo(0x7E)); // SP <- 0x7E
			Assert.That(Cpu.Mmio.Data[0x80], Is.EqualTo(0x20)); // Return address low byte
			Assert.That(Cpu.Mmio.Data[0x7F], Is.EqualTo(0x5)); // Return address high byte
			Assert.That(Cpu.Mmio.Data[95], Is.EqualTo(0b00000001)); // SREG <- -------C
		});
	}

	[Test(Description = "Push a 3-byte return address when running in 22-bit PC mode (issue #58)")]
	public void AVRJS_Issue_58 ()
	{
		Assert.That(Cpu.Pc22Bits, Is.True);

		Cpu.Pc = 0x10520;
		Cpu.Mmio.Data[94] = 0;
		Cpu.Mmio.Data[93] = 0x80; // SP <- 0x80
		Cpu.Mmio.Data[95] = 0b10000001; // SREG <- I------C

		AvrInterrupt.DoAvrInterrupt (Cpu, 5);

		Assert.Multiple(() =>
		{
			Assert.That(Cpu.Cycles, Is.EqualTo(3)); // 2 for PC push + 1 for vector fetch (AVR spec: 4 cycles total)
			Assert.That(Cpu.Pc, Is.EqualTo(5));
			Assert.That(Cpu.Mmio.Data[93], Is.EqualTo(0x7D)); // SP <- 0x7D
			Assert.That(Cpu.Mmio.Data[0x80], Is.EqualTo(0x20)); // Return address low byte
			Assert.That(Cpu.Mmio.Data[0x7F], Is.EqualTo(0x5)); // Return address high byte
			Assert.That(Cpu.Mmio.Data[0x7E], Is.EqualTo(0x1)); // Return address high byte
			Assert.That(Cpu.Mmio.Data[95], Is.EqualTo(0b00000001)); // SREG <- -------C
		});
	}

	[Test(Description = "Interrupt latency must be exactly 3 cycles from DoAvrInterrupt (4 total incl. executing instruction)")]
	public void InterruptLatency_ThreeCycles ()
	{
		// AVR spec: after a pending interrupt is recognized, the current instruction
		// finishes (1 cycle), then the PC is pushed onto the stack (2 cycles),
		// then the vector address is fetched (1 cycle) = 4 cycles total.
		// DoAvrInterrupt is called *after* the instruction cycle, so it must add exactly 3.
		Cpu.Mmio.Data[93] = 0x80;
		Cpu.Mmio.Data[95] = 0b10000001; // I flag + C flag

		var cyclesBefore = Cpu.Cycles;
		AvrInterrupt.DoAvrInterrupt(Cpu, 10);
		Assert.That(Cpu.Cycles - cyclesBefore, Is.EqualTo(3));
	}

	[Test(Description = "SREG is cleared to zero on CPU Reset")]
	public void SregClearedOnReset ()
	{
		// Dirty SREG with all bits set
		Cpu.Mmio.Data[95] = 0xFF;
		Cpu.Reset();
		Assert.That(Cpu.Mmio.Data[95], Is.EqualTo(0),
			"SREG must be 0 after Reset (AVR spec: all I/O registers return to reset state)");
	}

	[Test(Description = "BREAK instruction (0x9598) invokes OnBreakpoint callback in SwitchDecoder")]
	public void BreakInstruction_SwitchDecoder ()
	{
		// 0x9598 = BREAK opcode
		var program = new ushort[] { 0x9598 };
		Cpu.LoadProgram(program);

		uint? capturedPc = null;
		AvrInterrupt.OnBreakpoint = pc => capturedPc = pc;
		try
		{
			var decoder = new AVR8Sharp.Core.Decoders.SwitchDecoder();
			decoder.Decode(Cpu);
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
		Cpu.LoadProgram(program);

		uint? capturedPc = null;
		AvrInterrupt.OnBreakpoint = pc => capturedPc = pc;
		try
		{
			var decoder = new AVR8Sharp.Core.Decoders.LutDecoder();
			decoder.Decode(Cpu);
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
		Cpu.LoadProgram(program);

		uint? capturedPc = null;
		AvrInterrupt.OnBreakpoint = pc => capturedPc = pc;
		try
		{
			var decoder = new AVR8Sharp.Core.Decoders.NativeLutDecoder();
			decoder.Decode(Cpu);
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
		Cpu.LoadProgram(program);
		AvrInterrupt.OnBreakpoint = null;

		Assert.DoesNotThrow(() =>
		{
			var decoder = new AVR8Sharp.Core.Decoders.SwitchDecoder();
			decoder.Decode(Cpu);
		});
	}

	[Test(Description = "SLEEP instruction (0x9588) invokes OnSleep callback in SwitchDecoder")]
	public void SleepInstruction_SwitchDecoder_InvokesCallback ()
	{
		var program = new ushort[] { 0x9588 };
		Cpu.LoadProgram(program);

		// Set SM1:SM0 bits in SMCR (0x53): SM bits are bits 3:1, value 0b0000_0010 → SM0=1 (idle)
		Cpu.Mmio.Data[0x53] = 0b0000_0010; // SM0=1, SM1=0 → sleep mode bits = 0b001 = 1

		byte? capturedMode = null;
		AvrInterrupt.OnSleep = mode => capturedMode = mode;
		try
		{
			var decoder = new AVR8Sharp.Core.Decoders.SwitchDecoder();
			decoder.Decode(Cpu);
			Assert.That(capturedMode, Is.Not.Null, "OnSleep must be called by SLEEP instruction");
			Assert.That(capturedMode, Is.EqualTo(1), "Sleep mode bits SM2:SM1:SM0 must match SMCR");
		}
		finally
		{
			AvrInterrupt.OnSleep = null;
		}
	}

	[Test(Description = "SLEEP instruction (0x9588) invokes OnSleep callback in LutDecoder")]
	public void SleepInstruction_LutDecoder_InvokesCallback ()
	{
		var program = new ushort[] { 0x9588 };
		Cpu.LoadProgram(program);
		Cpu.Mmio.Data[0x53] = 0b0000_0100; // SM1=1, SM0=0 → sleep mode bits = 0b010 = 2

		byte? capturedMode = null;
		AvrInterrupt.OnSleep = mode => capturedMode = mode;
		try
		{
			var decoder = new AVR8Sharp.Core.Decoders.LutDecoder();
			decoder.Decode(Cpu);
			Assert.That(capturedMode, Is.Not.Null, "OnSleep must be called by SLEEP instruction in LutDecoder");
			Assert.That(capturedMode, Is.EqualTo(2));
		}
		finally
		{
			AvrInterrupt.OnSleep = null;
		}
	}

	[Test(Description = "SLEEP instruction (0x9588) invokes OnSleep callback in NativeLutDecoder")]
	public void SleepInstruction_NativeLutDecoder_InvokesCallback ()
	{
		var program = new ushort[] { 0x9588 };
		Cpu.LoadProgram(program);
		Cpu.Mmio.Data[0x53] = 0b0000_0110; // SM1=1, SM0=1 → sleep mode bits = 0b011 = 3

		byte? capturedMode = null;
		AvrInterrupt.OnSleep = mode => capturedMode = mode;
		try
		{
			var decoder = new AVR8Sharp.Core.Decoders.NativeLutDecoder();
			decoder.Decode(Cpu);
			Assert.That(capturedMode, Is.Not.Null, "OnSleep must be called by SLEEP instruction in NativeLutDecoder");
			Assert.That(capturedMode, Is.EqualTo(3));
		}
		finally
		{
			AvrInterrupt.OnSleep = null;
		}
	}

	[Test(Description = "SLEEP without OnSleep registered does not crash")]
	public void SleepInstruction_NoCallback_DoesNotCrash ()
	{
		var program = new ushort[] { 0x9588 };
		Cpu.LoadProgram(program);
		AvrInterrupt.OnSleep = null;

		Assert.DoesNotThrow(() =>
		{
			var decoder = new AVR8Sharp.Core.Decoders.SwitchDecoder();
			decoder.Decode(Cpu);
		});
	}
}
