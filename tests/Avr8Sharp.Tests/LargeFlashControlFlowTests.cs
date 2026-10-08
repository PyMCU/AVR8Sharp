using AvrCpu = AVR8Sharp.Core.Cpu;
using AVR8Sharp.Core.Decoders;

namespace Avr8Sharp.Tests;

/// <summary>
/// RCALL/RET and friends on parts whose program counter is wider than 16 bits
/// (ATmega2560-sized flash), where a truncated displacement leaves a phantom bit 16 in Pc.
/// </summary>
[TestFixture]
public class LargeFlashControlFlow
{
	private const ushort RET = 0x9508;
	private const int Words256K = 0x20000; // 128K words = 256 KB, ATmega2560
	private const int Words130K = 0x10100; // just over 64K words, still a 22-bit PC

	private static ushort Rcall (int k) => (ushort)(0xd000 | (k & 0xfff));
	private static ushort Rjmp (int k) => (ushort)(0xc000 | (k & 0xfff));

	private static void Step (AvrCpu cpu, bool lut)
	{
		if (lut) new LutDecoder ().Decode (cpu);
		else new SwitchDecoder ().Decode (cpu);
	}

	private static AvrCpu NewCpu (int words)
	{
		var cpu = new AvrCpu (new ushort[words]);
		Assert.That (cpu.Pc22Bits, Is.True);
		return cpu;
	}

	[TestCase (false)]
	[TestCase (true)]
	public void BackwardRcallThenRetReturnsToCaller (bool lut)
	{
		var cpu = NewCpu (Words256K);
		cpu.ProgramMemory[0x20] = RET;
		cpu.ProgramMemory[0x30] = Rcall (-0x11); // target 0x30 + 1 - 0x11 = 0x20
		cpu.Pc = 0x30;
		var sp = cpu.Sp;

		Step (cpu, lut);
		Assert.That (cpu.Pc, Is.EqualTo (0x20u));
		Assert.That (cpu.Sp, Is.EqualTo ((ushort)(sp - 3)));

		Step (cpu, lut);
		Assert.That (cpu.Pc, Is.EqualTo (0x31u));
		Assert.That (cpu.Sp, Is.EqualTo (sp));
	}

	[TestCase (false)]
	[TestCase (true)]
	public void ForwardRcallThenRetReturnsToCaller (bool lut)
	{
		var cpu = NewCpu (Words256K);
		cpu.ProgramMemory[0x30] = Rcall (0x0f); // target 0x40
		cpu.ProgramMemory[0x40] = RET;
		cpu.Pc = 0x30;

		Step (cpu, lut);
		Assert.That (cpu.Pc, Is.EqualTo (0x40u));
		Step (cpu, lut);
		Assert.That (cpu.Pc, Is.EqualTo (0x31u));
	}

	[TestCase (false)]
	[TestCase (true)]
	public void BackwardRcallAboveTheFirst64KWords (bool lut)
	{
		var cpu = NewCpu (Words256K);
		cpu.ProgramMemory[0x12000] = RET;
		cpu.ProgramMemory[0x12010] = Rcall (-0x11);
		cpu.Pc = 0x12010;

		Step (cpu, lut);
		Assert.That (cpu.Pc, Is.EqualTo (0x12000u));
		Step (cpu, lut);
		Assert.That (cpu.Pc, Is.EqualTo (0x12011u));
	}

	[TestCase (false)]
	[TestCase (true)]
	public void RcallNearTheTopOf128KWordFlash (bool lut)
	{
		var cpu = NewCpu (Words256K);
		cpu.ProgramMemory[0x1ffe0] = RET;
		cpu.ProgramMemory[0x1fff0] = Rcall (-0x11); // backward to 0x1ffe0
		cpu.ProgramMemory[0x1fff2] = Rcall (0x03);  // forward to 0x1fff6
		cpu.ProgramMemory[0x1fff6] = RET;

		cpu.Pc = 0x1fff0;
		Step (cpu, lut);
		Assert.That (cpu.Pc, Is.EqualTo (0x1ffe0u));
		Step (cpu, lut);
		Assert.That (cpu.Pc, Is.EqualTo (0x1fff1u));

		cpu.Pc = 0x1fff2;
		Step (cpu, lut);
		Assert.That (cpu.Pc, Is.EqualTo (0x1fff6u));
		Step (cpu, lut);
		Assert.That (cpu.Pc, Is.EqualTo (0x1fff3u));
	}

	[TestCase (false)]
	[TestCase (true)]
	public void RetToAReturnAddressWithZeroLow16Bits (bool lut)
	{
		var cpu = NewCpu (Words130K);
		cpu.ProgramMemory[0xffff] = Rcall (0x04); // return address 0x10000, target 0x10004
		cpu.ProgramMemory[0x10004] = RET;
		cpu.Pc = 0xffff;

		Step (cpu, lut);
		Assert.That (cpu.Pc, Is.EqualTo (0x10004u));
		Step (cpu, lut);
		Assert.That (cpu.Pc, Is.EqualTo (0x10000u));
	}

	[TestCase (false)]
	[TestCase (true)]
	public void BackwardRjmpAndBranchStayInRange (bool lut)
	{
		var cpu = NewCpu (Words256K);
		cpu.ProgramMemory[0x14000] = Rjmp (-0x11);
		cpu.Pc = 0x14000;
		Step (cpu, lut);
		Assert.That (cpu.Pc, Is.EqualTo (0x13ff0u));

		cpu.ProgramMemory[0x14100] = 0xf7e9; // brne .-6 (Z clear)
		cpu.Pc = 0x14100;
		Step (cpu, lut);
		Assert.That (cpu.Pc, Is.EqualTo (0x140feu));
	}
}
