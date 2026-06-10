using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

[TestFixture]
public class Instruction : AvrTestBase
{
	protected override int FlashByteCount => 0x8000;

	private AVR8Sharp.Core.Decoders.SwitchDecoder decoder;

	protected override void SetupPeripherals()
	{
		decoder = new AVR8Sharp.Core.Decoders.SwitchDecoder();
	}
	
	[Test(Description = "Should execute ADC r0, r1 instruction when carry is on")]
	public void ADC()
	{
		LoadProgram ([
			"adc r0, r1"
		]);
		Cpu.Mmio.Data[R0] = 10;
		Cpu.Mmio.Data[R1] = 20;
		Cpu.WriteData((ushort)SREG, (byte)(SREG_C));
		decoder.Decode(Cpu);
        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Pc, Is.EqualTo(1));
            Assert.That(Cpu.Cycles, Is.EqualTo(1));
            Assert.That(Cpu.Mmio.Data[R0], Is.EqualTo(31));
            Assert.That(Cpu.Sreg, Is.EqualTo(0));
        });
    }
	
	[Test(Description = "Should execute ADC r0, r1 instruction when carry is on and the result overflows")]
	public void ADC_Overflow()
	{
		LoadProgram ([
			"adc r0, r1"
		]);
		Cpu.Mmio.Data[R0] = 10;
		Cpu.Mmio.Data[R1] = 245;
		Cpu.WriteData((ushort)SREG, (byte)(SREG_C));
		decoder.Decode(Cpu);
		Assert.Multiple(() =>
		{
			Assert.That(Cpu.Pc, Is.EqualTo(1));
			Assert.That(Cpu.Cycles, Is.EqualTo(1));
			Assert.That(Cpu.Mmio.Data[R0], Is.EqualTo(0));
			Assert.That(Cpu.Sreg, Is.EqualTo(SREG_H | SREG_Z | SREG_C));
		});
	}
	
	[Test(Description = "Should execute ADD r0, r1 instruction when result overflows")]
	public void ADD_Overflow()
	{
		LoadProgram ([
			"add r0, r1"
		]);
		Cpu.Mmio.Data[R0] = 11;
		Cpu.Mmio.Data[R1] = 245;
		decoder.Decode(Cpu);
		Assert.Multiple(() =>
		{
			Assert.That(Cpu.Pc, Is.EqualTo(1));
			Assert.That(Cpu.Cycles, Is.EqualTo(1));
			Assert.That(Cpu.Mmio.Data[R0], Is.EqualTo(0));
			Assert.That(Cpu.Sreg, Is.EqualTo(SREG_H | SREG_Z | SREG_C));
		});
	}
	
	[Test(Description = "Should execute ADD r0, r1 instruction when carry is on")]
	public void ADD()
	{
		LoadProgram ([
			"add r0, r1"
		]);
		Cpu.Mmio.Data[R0] = 11;
		Cpu.Mmio.Data[R1] = 244;
		Cpu.WriteData((ushort)SREG, (byte)(SREG_C));
		decoder.Decode(Cpu);
		Assert.Multiple(() =>
		{
			Assert.That(Cpu.Pc, Is.EqualTo(1));
			Assert.That(Cpu.Cycles, Is.EqualTo(1));
			Assert.That(Cpu.Mmio.Data[R0], Is.EqualTo(255));
			Assert.That(Cpu.Sreg, Is.EqualTo(SREG_S | SREG_N));
		});
	}
	
	[Test(Description = "Should execute ADD r0, r1 instruction when carry is on and the result overflows")]
	public void ADD_Overflow_Carry()
	{
		LoadProgram ([
			"add r0, r1"
		]);
		Cpu.Mmio.Data[R0] = 11;
		Cpu.Mmio.Data[R1] = 245;
		Cpu.WriteData((ushort)SREG, (byte)(SREG_C));
		decoder.Decode(Cpu);
		Assert.Multiple(() =>
		{
			Assert.That(Cpu.Pc, Is.EqualTo(1));
			Assert.That(Cpu.Cycles, Is.EqualTo(1));
			Assert.That(Cpu.Mmio.Data[R0], Is.EqualTo(0));
			Assert.That(Cpu.Sreg, Is.EqualTo(SREG_H | SREG_Z | SREG_C));
		});
	}
	
	[Test(Description = "Should execute BCLR 2 instruction")]
	public void BCLR()
	{
		LoadProgram ([
			"bclr 2"
		]);
		Cpu.WriteData((ushort)SREG, (byte)(0xff));
		decoder.Decode(Cpu);
		Assert.Multiple(() =>
		{
			Assert.That(Cpu.Pc, Is.EqualTo(1));
			Assert.That(Cpu.Cycles, Is.EqualTo(1));
			Assert.That(Cpu.Sreg, Is.EqualTo(0xfb));
		});
	}
	
	[Test(Description = "Should execute BLD r4, 7 instruction")]
	public void BLD()
	{
		LoadProgram ([
			"bld r4, 7"
		]);
		Cpu.Mmio.Data[R4] = 0x15;
		Cpu.WriteData((ushort)SREG, (byte)(0x40));
		decoder.Decode(Cpu);
		Assert.Multiple(() =>
		{
			Assert.That(Cpu.Pc, Is.EqualTo(1));
			Assert.That(Cpu.Cycles, Is.EqualTo(1));
			Assert.That(Cpu.Mmio.Data[R4], Is.EqualTo(0x95));
			Assert.That(Cpu.Sreg, Is.EqualTo(0x40));
		});
	}

	[Test (Description = "Should execute BRBC 0, +8 instruction when SREG.C is clear")]
	public void BRBC ()
	{
		LoadProgram ([
			"brbc 0, +8",
		]);
		Cpu.WriteData((ushort)SREG, (byte)(SREG_V));
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1 + 8 / 2));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
		});
	}
	
	[Test (Description = "Should execute BRBC 0, +8 instruction when SREG.C is set")]
	public void BRBC_SREG_C ()
	{
		LoadProgram ([
			"brbc 0, +8",
		]);
		Cpu.WriteData((ushort)SREG, (byte)(SREG_C));
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
		});
	}
	
	[Test (Description = "Should execute BRBS 3, 92 instruction when SREG.V is set")]
	public void BRBS ()
	{
		LoadProgram ([
			"brbs 3, +92",
		]);
		Cpu.WriteData((ushort)SREG, (byte)(SREG_V));
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1 + 92 / 2));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
		});
	}
	
	[Test (Description = "Should execute BRBS 3, -4 instruction when SREG.V is set")]
	public void BRBS_Negative ()
	{
		LoadProgram ([
			"brbs 3, -4",
		]);
		Cpu.WriteData((ushort)SREG, (byte)(SREG_V));
		decoder.Decode(Cpu);
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (0));
			Assert.That (Cpu.Cycles, Is.EqualTo (3));
		});
	}
	
	[Test (Description = "Should execute BRBS 3, -4 instruction when SREG.V is clear")]
	public void BRBS_Clear ()
	{
		LoadProgram ([
			"brbs 3, -4",
		]);
		Cpu.WriteData((ushort)SREG, (byte)(0x0));
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
		});
	}
	
	[Test (Description = "Should execute CALL instruction")]
	public void CALL ()
	{
		LoadProgram ([
			"call 0xb8",
		]);
		Cpu.Mmio.Data[SPH] = 0x00;
		Cpu.Mmio.Data[SP] = 150;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (0x5c));
			Assert.That (Cpu.Cycles, Is.EqualTo (4));
			Assert.That (Cpu.Mmio.Data[150], Is.EqualTo (2)); // Return address low byte
			Assert.That (Cpu.Mmio.Data[SP], Is.EqualTo (148)); // SP should be decremented 
		});
	}
	
	[Test (Description = "Should throw AvrStackUnderflowException on RET with an empty (underflowed) stack")]
	public void RET_StackUnderflow ()
	{
		LoadProgram ([
			"ret",
		]);
		// SP parked at the very top of SRAM: a RET pops a return address from past
		// the end of memory -- the empty-stack case. Should surface as a clear
		// stack-underflow diagnostic, not a bare IndexOutOfRangeException.
		Cpu.Mmio.DataView.SetUint16 (93, (ushort)(Cpu.Mmio.Data.Length - 1), true);
		Assert.Throws<AVR8Sharp.Core.AvrStackUnderflowException> (() => decoder.Decode (Cpu));
	}

	[Test (Description = "Should throw AvrStackOverflowException when a PUSH crosses the stack limit")]
	public void PUSH_StackOverflow ()
	{
		LoadProgram ([
			"push r0",
		]);
		// Park SP just below a configured RAMSTART: the push writes into the I/O space.
		Cpu.StackLowLimit = 0x100;
		Cpu.Mmio.DataView.SetUint16 (93, 0xFF, true);
		Assert.Throws<AVR8Sharp.Core.AvrStackOverflowException> (() => decoder.Decode (Cpu));
	}

	[Test (Description = "Should throw AvrStackOverflowException when a CALL would push below the stack limit")]
	public void CALL_StackOverflow ()
	{
		LoadProgram ([
			"call 0xb8",
		]);
		Cpu.StackLowLimit = 0x100;
		// SP == limit: a 2-byte return-address push reaches 0xFF, one below the limit.
		Cpu.Mmio.DataView.SetUint16 (93, 0x100, true);
		Assert.Throws<AVR8Sharp.Core.AvrStackOverflowException> (() => decoder.Decode (Cpu));
	}

	[Test (Description = "Should push 3-byte return address when executing CALL instruction on device with >128k flash")]
	public void CALL_3Byte ()
	{
		Cpu = new AVR8Sharp.Core.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"call 0xb8",
		]);
		Cpu.Mmio.Data[SPH] = 0x00;
		Cpu.Mmio.Data[SP] = 150;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (0x5c));
			Assert.That (Cpu.Cycles, Is.EqualTo (5));
			Assert.That (Cpu.Mmio.Data[150], Is.EqualTo (2)); // Return address low byte
			Assert.That (Cpu.Mmio.Data[SP], Is.EqualTo (147)); // SP should be incremented by 3 
		});
	}
	
	[Test (Description = "Should execute CBI 0x0c, 5 instruction")]
	public void CBI ()
	{
		LoadProgram ([
			"cbi 0x0c, 5",
		]);
		Cpu.Mmio.Data[0x2c] = 0b11111111;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Mmio.Data[0x2c], Is.EqualTo (0b11011111));
		});
	}
	
	[Test (Description = "Should execute CPC r27, r18 instruction")]
	public void CPC ()
	{
		LoadProgram ([
			"cpc r27, r18",
		]);
		Cpu.Mmio.Data[R18] = 0x1;
		Cpu.Mmio.Data[R27] = 0x1;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Sreg, Is.EqualTo (0));
		});
	}
	
	[Test (Description = "Should execute CPC r24, r1 instruction and set")]
	public void CPC_Negative ()
	{
		LoadProgram ([
			"cpc r24, r1",
		]);
		Cpu.Mmio.Data[R1] = 0;
		Cpu.Mmio.Data[R24] = 0;
		Cpu.WriteData((ushort)SREG, (byte)(SREG_I | SREG_C));
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Sreg, Is.EqualTo (SREG_I | SREG_H | SREG_S | SREG_N | SREG_C));
		});
	}
	
	[Test (Description = "Should execute CPI r26, 0x9 instruction")]
	public void CPI ()
	{
		LoadProgram ([
			"cpi r26, 0x9",
		]);
		Cpu.Mmio.Data[R26] = 0x8;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Sreg, Is.EqualTo (SREG_H | SREG_S | SREG_N | SREG_C));
		});
	}
	
	[Test (Description = "Should execute CPSE r2, r3 instruction when r2 != r3")]
	public void CPSE ()
	{
		LoadProgram ([
			"cpse r2, r3"
		]);
		Cpu.Mmio.Data[R2] = 10;
		Cpu.Mmio.Data[R3] = 11;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
		});
	}
	
	[Test (Description = "Should execute CPSE r2, r3 instruction when r2 == r3")]
	public void CPSE_Equal ()
	{
		LoadProgram ([
			"cpse r2, r3"
		]);
		Cpu.Mmio.Data[R2] = 10;
		Cpu.Mmio.Data[R3] = 10;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (2));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
		});
	}
	
	[Test (Description = "Should execute CPSE r2, r3 when r2 == r3 and followed by 2-word instruction")]
	public void CPSE_2Word ()
	{
		LoadProgram ([
			"cpse r2, r3",
			"call 8",
		]);
		Cpu.Mmio.Data[R2] = 10;
		Cpu.Mmio.Data[R3] = 10;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (3));
			Assert.That (Cpu.Cycles, Is.EqualTo (3));
		});
	}
	
	[Test (Description = "Should execute EICALL instruction")]
	public void EICALL ()
	{
		LoadProgram ([
			"eicall",
		]);
		Cpu.Mmio.Data[SPH] = 0x00;
		Cpu.Mmio.Data[SP] = 0x80;
		Cpu.Mmio.Data[EIND] = 0x01;
		Cpu.Mmio.DataView.SetUint16 (Z, 0x1234, true);
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (0x1234));
			Assert.That (Cpu.Cycles, Is.EqualTo (4));
			Assert.That (Cpu.Mmio.Data[0x80], Is.EqualTo (1)); // Return address low byte
			Assert.That (Cpu.Mmio.Data[SP], Is.EqualTo (0x80 - 3)); // SP should be decremented 
		});
	}
	
	[Test (Description = "Should execute EIJMP instruction")]
	public void EIJMP ()
	{
		Cpu = new AVR8Sharp.Core.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"eijmp",
		]);
		Cpu.Mmio.Data[EIND] = 0x01;
		Cpu.Mmio.DataView.SetUint16 (Z, 0x1040, true);
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (0x11040));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
		});
	}
	
	[Test (Description = "Should execute ELPM instruction")]
	public void ELPM ()
	{
		Cpu = new AVR8Sharp.Core.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"elpm",
		]);
		Cpu.Mmio.Data[Z] = 0x50;
		Cpu.Mmio.Data[RAMPZ] = 0x2;
		Cpu.SetProgramByte (0x20050, 0x62);
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (3));
			Assert.That (Cpu.Mmio.Data[R0], Is.EqualTo (0x62));
		});
	}
	
	[Test (Description = "Should execute ELPM r5, Z instruction")]
	public void ELPM_Register ()
	{
		Cpu = new AVR8Sharp.Core.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"elpm r5, Z",
		]);
		Cpu.Mmio.Data[Z] = 0x11;
		Cpu.Mmio.Data[RAMPZ] = 0x1;
		Cpu.SetProgramByte (0x10011, 0x99);
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (3));
			Assert.That (Cpu.Mmio.Data[R5], Is.EqualTo (0x99));
		});
	}
	
	[Test (Description = "Should execute ELPM r6, Z+ instruction")]
	public void ELPM_Register_PostIncrement ()
	{
		Cpu = new AVR8Sharp.Core.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"elpm r6, Z+",
		]);
		Cpu.Mmio.DataView.SetUint16 (Z, 0xffff, true);
		Cpu.Mmio.Data[RAMPZ] = 0x2;
		Cpu.SetProgramByte (0x2ffff, 0x22);
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (3));
			Assert.That (Cpu.Mmio.Data[R6], Is.EqualTo (0x22)); // Check that the value was loaded to r6
			Assert.That (Cpu.Mmio.DataView.GetUint16 (Z, true), Is.EqualTo (0x0000)); // Check that Z was incremented
			Assert.That (Cpu.Mmio.Data[RAMPZ], Is.EqualTo (3)); // Check that RAMPZ was incremented
		});
	}
	
	[Test (Description = "Should clamp RAMPZ when executing ELPM r6, Z+ instruction")]
	public void ELPM_Register_PostIncrement_RAMPZ ()
	{
		Cpu = new AVR8Sharp.Core.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"elpm r6, Z+",
		]);
		Cpu.Mmio.DataView.SetUint16 (Z, 0xffff, true);
		Cpu.Mmio.Data[RAMPZ] = 0x3;
		Cpu.SetProgramByte (0x2ffff, 0x22);
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Mmio.Data[RAMPZ], Is.EqualTo (0x0)); // Verify that RAMPZ was reset to zero
		});
	}
	
	[Test (Description = "Should execute ICALL instruction")]
	public void ICALL ()
	{
		LoadProgram ([
			"icall",
		]);
		Cpu.Mmio.Data[SPH] = 0x00;
		Cpu.Mmio.Data[SP] = 0x80;
		Cpu.Mmio.DataView.SetUint16 (Z, 0x2020, true);
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (0x2020));
			Assert.That (Cpu.Cycles, Is.EqualTo (3));
			Assert.That (Cpu.Mmio.Data[0x80], Is.EqualTo (1)); // Return address low byte
			Assert.That (Cpu.Mmio.Data[SP], Is.EqualTo (0x7e)); // SP should be decremented 
		});
	}
	
	[Test (Description = "Should push 3-byte return address when executing ICALL instruction on device with >128k flash")]
	public void ICALL_3Byte ()
	{
		Cpu = new AVR8Sharp.Core.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"icall",
		]);
		Cpu.Mmio.Data[SPH] = 0x00;
		Cpu.Mmio.Data[SP] = 0x80;
		Cpu.Mmio.DataView.SetUint16 (Z, 0x2020, true);
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (0x2020));
			Assert.That (Cpu.Cycles, Is.EqualTo (4));
			Assert.That (Cpu.Mmio.Data[0x80], Is.EqualTo (1)); // Return address low byte
			Assert.That (Cpu.Mmio.Data[SP], Is.EqualTo (0x7d)); // SP should be decremented by 3 
		});
	}
	
	[Test (Description = "Should execute IJMP instruction")]
	public void IJMP ()
	{
		LoadProgram ([
			"ijmp",
		]);
		Cpu.Mmio.DataView.SetUint16 (Z, 0x1040, true);
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (0x1040));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
		});
	}
	
	[Test (Description = "Should execute IN r5, 0xb instruction")]
	public void IN ()
	{
		LoadProgram ([
			"in r5, 0xb",
		]);
		Cpu.Mmio.Data[0x2b] = 0xaf;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Mmio.Data[R5], Is.EqualTo (0xaf));
		});
	}
	
	[Test (Description = "Should execute INC r5 instruction")]
	public void INC ()
	{
		LoadProgram ([
			"inc r5",
		]);
		Cpu.Mmio.Data[R5] = 0x7f;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Mmio.Data[R5], Is.EqualTo (0x80));
			Assert.That (Cpu.Sreg, Is.EqualTo (SREG_N | SREG_V));
		});
	}
	
	[Test (Description = "Should execute INC r5 instruction when r5 == 0xff")]
	public void INC_Overflow ()
	{
		LoadProgram ([
			"inc r5",
		]);
		Cpu.Mmio.Data[R5] = 0xff;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Mmio.Data[R5], Is.EqualTo (0x00));
			Assert.That (Cpu.Sreg, Is.EqualTo ( SREG_Z));
		});
	}
	
	[Test (Description = "Should execute JMP 0xb8 instruction")]
	public void JMP ()
	{
		LoadProgram ([
			"jmp 0xb8",
		]);
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (0x5c));
			Assert.That (Cpu.Cycles, Is.EqualTo (3));
		});
	}
	
	[Test (Description = "Should execute LAC Z, r19 instruction")]
	public void LAC ()
	{
		LoadProgram ([
			"lac Z, r19",
		]);
		Cpu.Mmio.Data[R19] = 0x02;
		Cpu.Mmio.DataView.SetUint16 (Z, 0x100, true);
		Cpu.Mmio.Data[0x100] = 0x96;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Mmio.Data[R19], Is.EqualTo (0x96));
			Assert.That (Cpu.Mmio.DataView.GetUint16 (Z, true), Is.EqualTo (0x100));
			Assert.That (Cpu.Mmio.Data[0x100], Is.EqualTo (0x94));
		});
	}
	
	[Test (Description = "Should execute LAS Z, r17 instruction")]
	public void LAS ()
	{
		LoadProgram ([
			"las Z, r17",
		]);
		Cpu.Mmio.Data[R17] = 0x11;
		Cpu.Mmio.Data[Z] = 0x80;
		Cpu.Mmio.Data[0x80] = 0x44;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Mmio.Data[R17], Is.EqualTo (0x44));
			Assert.That (Cpu.Mmio.Data[Z], Is.EqualTo (0x80));
			Assert.That (Cpu.Mmio.Data[0x80], Is.EqualTo (0x55));
		});
	}
	
	[Test (Description = "Should execute LAT Z, r0 instruction")]
	public void LAT ()
	{
		LoadProgram ([
			"lat Z, r0",
		]);
		Cpu.Mmio.Data[R0] = 0x33;
		Cpu.Mmio.Data[Z] = 0x80;
		Cpu.Mmio.Data[0x80] = 0x66;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Mmio.Data[R0], Is.EqualTo (0x66));
			Assert.That (Cpu.Mmio.Data[Z], Is.EqualTo (0x80));
			Assert.That (Cpu.Mmio.Data[0x80], Is.EqualTo (0x55));
		});
	}
	
	[Test (Description = "Should execute LD r1, X instruction")]
	public void LD ()
	{
		LoadProgram ([
			"ld r1, X",
		]);
		Cpu.Mmio.Data[0xc0] = 0x15;
		Cpu.Mmio.Data[X] = 0xc0;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[R1], Is.EqualTo (0x15));
			Assert.That (Cpu.Mmio.Data[X], Is.EqualTo (0xc0)); // X should not be modified
		});
	}
	
	[Test (Description = "Should execute LD r17, X+ instruction")]
	public void LD_PostIncrement ()
	{
		LoadProgram ([
			"ld r17, X+",
		]);
		Cpu.Mmio.Data[0xc0] = 0x15;
		Cpu.Mmio.Data[X] = 0xc0;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[R17], Is.EqualTo (0x15));
			Assert.That (Cpu.Mmio.Data[X], Is.EqualTo (0xc1)); // X should be incremented
		});
	}
	
	[Test (Description = "Should execute LD r1, -X instruction")]
	public void LD_PreDecrement ()
	{
		LoadProgram ([
			"ld r1, -X",
		]);
		Cpu.Mmio.Data[0x98] = 0x22;
		Cpu.Mmio.Data[X] = 0x99;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[R1], Is.EqualTo (0x22));
			Assert.That (Cpu.Mmio.Data[X], Is.EqualTo (0x98)); // X should be decremented
		});
	}
	
	[Test (Description = "Should execute LD r8, Y instruction")]
	public void LD_Y ()
	{
		LoadProgram ([
			"ld r8, Y",
		]);
		Cpu.Mmio.Data[0xc0] = 0x15;
		Cpu.Mmio.Data[Y] = 0xc0;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[R8], Is.EqualTo (0x15));
			Assert.That (Cpu.Mmio.Data[Y], Is.EqualTo (0xc0)); // Y should not be modified
		});
	}
	
	[Test (Description = "Should execute LD r3, Y+ instruction")]
	public void LD_Y_PostIncrement ()
	{
		LoadProgram ([
			"ld r3, Y+",
		]);
		Cpu.Mmio.Data[0xc0] = 0x15;
		Cpu.Mmio.Data[Y] = 0xc0;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[R3], Is.EqualTo (0x15));
			Assert.That (Cpu.Mmio.Data[Y], Is.EqualTo (0xc1)); // Y should be incremented
		});
	}
	
	[Test (Description = "Should execute LD r0, -Y instruction")]
	public void LD_Y_PreDecrement ()
	{
		LoadProgram ([
			"ld r0, -Y",
		]);
		Cpu.Mmio.Data[0x98] = 0x22;
		Cpu.Mmio.Data[Y] = 0x99;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[R0], Is.EqualTo (0x22));
			Assert.That (Cpu.Mmio.Data[Y], Is.EqualTo (0x98)); // Y should be decremented
		});
	}
	
	[Test (Description = "Should execute LDD r4, Y+2 instruction")]
	public void LDD_Y ()
	{
		LoadProgram ([
			"ldd r4, Y+2",
		]);
		Cpu.Mmio.Data[0x82] = 0x33;
		Cpu.Mmio.Data[Y] = 0x80;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[R4], Is.EqualTo (0x33));
			Assert.That (Cpu.Mmio.Data[Y], Is.EqualTo (0x80)); // Y should not be modified
		});
	}
	
	[Test (Description = "Should execute LD r5, Z instruction")]
	public void LD_Z ()
	{
		LoadProgram ([
			"ld r5, Z",
		]);
		Cpu.Mmio.Data[0xcc] = 0xf5;
		Cpu.Mmio.Data[Z] = 0xcc;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[R5], Is.EqualTo (0xf5));
			Assert.That (Cpu.Mmio.Data[Z], Is.EqualTo (0xcc)); // Z should not be modified
		});
	}
	
	[Test (Description = "Should execute LD r7, Z+ instruction")]
	public void LD_Z_PostIncrement ()
	{
		LoadProgram ([
			"ld r7, Z+",
		]);
		Cpu.Mmio.Data[0xc0] = 0x25;
		Cpu.Mmio.Data[Z] = 0xc0;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[R7], Is.EqualTo (0x25));
			Assert.That (Cpu.Mmio.Data[Z], Is.EqualTo (0xc1)); // Z should be incremented
		});
	}
	
	[Test (Description = "Should execute LD r0, -Z instruction")]
	public void LD_Z_PreDecrement ()
	{
		LoadProgram ([
			"ld r0, -Z",
		]);
		Cpu.Mmio.Data[0x9e] = 0x66;
		Cpu.Mmio.Data[Z] = 0x9f;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[R0], Is.EqualTo (0x66));
			Assert.That (Cpu.Mmio.Data[Z], Is.EqualTo (0x9e)); // Z should be decremented
		});
	}
	
	[Test (Description = "Should execute LDD r15, Z+31 instruction")]
	public void LDD_Z ()
	{
		LoadProgram ([
			"ldd r15, Z+31",
		]);
		Cpu.Mmio.Data[0x9f] = 0x33;
		Cpu.Mmio.Data[Z] = 0x80;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[R15], Is.EqualTo (0x33));
			Assert.That (Cpu.Mmio.Data[Z], Is.EqualTo (0x80)); // Z should not be modified
		});
	}
	
	[Test (Description = "Should execute LDI r28, 0xff instruction")]
	public void LDI ()
	{
		LoadProgram ([
			"ldi r28, 0xff",
		]);
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Mmio.Data[Y], Is.EqualTo (0xff));
		});
	}
	
	[Test (Description = "Should execute LDS r5, 0x150 instruction")]
	public void LDS ()
	{
		LoadProgram ([
			"lds r5, 0x150",
		]);
		Cpu.Mmio.Data[0x150] = 0x7a;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (2));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[R5], Is.EqualTo (0x7a));
		});
	}
	
	[Test (Description = "Should execute LPM instruction")]
	public void LPM ()
	{
		Cpu = new AVR8Sharp.Core.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"lpm",
		]);
		Cpu.SetProgramWord (0x40, 0xa0);
		Cpu.Mmio.Data[Z] = 0x80;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (3));
			Assert.That (Cpu.Mmio.Data[R0], Is.EqualTo (0xa0));
			Assert.That (Cpu.Mmio.Data[Z], Is.EqualTo (0x80)); // Z should not be modified
		});
	}
	
	[Test (Description = "Should execute LPM r2, Z instruction")]
	public void LPM_Register ()
	{
		LoadProgram ([
			"lpm r2, Z",
		]);
		Cpu.Mmio.Data[Z] = 0x80;
		Cpu.SetProgramWord (0x40, 0xa0);
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (3));
			Assert.That (Cpu.Mmio.Data[R2], Is.EqualTo (0xa0));
			Assert.That (Cpu.Mmio.Data[Z], Is.EqualTo (0x80)); // Z should not be modified
		});
	}
	
	[Test (Description = "Should execute LPM r1, Z+ instruction")]
	public void LPM_Register_PostIncrement ()
	{
		LoadProgram ([
			"lpm r1, Z+",
		]);
		Cpu.Mmio.Data[Z] = 0x80;
		Cpu.SetProgramWord (0x40, 0xa0);
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (3));
			Assert.That (Cpu.Mmio.Data[R1], Is.EqualTo (0xa0));
			Assert.That (Cpu.Mmio.Data[Z], Is.EqualTo (0x81)); // Z should be incremented
		});
	}
	
	[Test (Description = "Should execute LSR r7 instruction")]
	public void LSR ()
	{
		LoadProgram ([
			"lsr r7",
		]);
		Cpu.Mmio.Data[R7] = 0x45;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Mmio.Data[R7], Is.EqualTo (0x22));
			Assert.That (Cpu.Sreg, Is.EqualTo (SREG_S | SREG_V | SREG_C));
		});
	}
	
	[Test (Description = "Should execute MOV r7, r8 instruction")]
	public void MOV ()
	{
		LoadProgram ([
			"mov r7, r8",
		]);
		Cpu.Mmio.Data[R8] = 0x45;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Mmio.Data[R7], Is.EqualTo (0x45));
		});
	}
	
	[Test (Description = "Should execute MOVW r26, r22 instruction")]
	public void MOVW ()
	{
		LoadProgram ([
			"movw r26, r22",
		]);
		Cpu.Mmio.Data[R22] = 0x45;
		Cpu.Mmio.Data[R23] = 0x9a;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Mmio.Data[R26], Is.EqualTo (0x45));
			Assert.That (Cpu.Mmio.Data[R27], Is.EqualTo (0x9a));
		});
	}
	
	[Test (Description = "Should execute MUL r5, r6 instruction")]
	public void MUL ()
	{
		LoadProgram ([
			"mul r5, r6",
		]);
		Cpu.Mmio.Data[R5] = 100;
		Cpu.Mmio.Data[R6] = 5;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.DataView.GetUint16 (0, true), Is.EqualTo (500));
			Assert.That (Cpu.Sreg, Is.EqualTo (0));
		});
	}
	
	[Test (Description = "Should execute MUL r5, r6 instruction and update carry flag when numbers are big")]
	public void MUL_Carry ()
	{
		LoadProgram ([
			"mul r5, r6",
		]);
		Cpu.Mmio.Data[R5] = 200;
		Cpu.Mmio.Data[R6] = 200;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.DataView.GetUint16 (0, true), Is.EqualTo (40000));
			Assert.That (Cpu.Sreg, Is.EqualTo (SREG_C));
		});
	}
	
	[Test (Description = "Should execute MUL r0, r1 and update the zero flag")]
	public void MUL_Zero ()
	{
		LoadProgram ([
			"mul r0, r1",
		]);
		Cpu.Mmio.Data[R0] = 0;
		Cpu.Mmio.Data[R1] = 9;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.DataView.GetUint16 (0, true), Is.EqualTo (0));
			Assert.That (Cpu.Sreg, Is.EqualTo (SREG_Z));
		});
	}
	
	[Test (Description = "Should execute MULS r18, r19 instruction")]
	public void MULS ()
	{
		LoadProgram ([
			"muls r18, r19",
		]);
		Cpu.Mmio.Data[R18] = (-5) & 0xff;
		Cpu.Mmio.Data[R19] = 100;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.DataView.GetInt16 (0, true), Is.EqualTo (-500));
			Assert.That (Cpu.Sreg, Is.EqualTo (SREG_C));
		});
	}
	
	[Test (Description = "Should execute MULSU r16, r17 instruction")]
	public void MULSU ()
	{
		LoadProgram ([
			"mulsu r16, r17",
		]);
		Cpu.Mmio.Data[R16] = (-5) & 0xff;
		Cpu.Mmio.Data[R17] = 200;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.DataView.GetInt16 (0, true), Is.EqualTo (-1000));
			Assert.That (Cpu.Sreg, Is.EqualTo (SREG_C));
		});
	}
	
	[Test (Description = "Should execute NEG r20 instruction")]
	public void NEG ()
	{
		LoadProgram ([
			"neg r20",
		]);
		Cpu.Mmio.Data[R20] = 0x56;
		decoder.Decode(Cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Mmio.Data[R20], Is.EqualTo (0xaa));
			Assert.That (Cpu.Sreg, Is.EqualTo (SREG_H | SREG_S | SREG_N | SREG_C));
		});
	}

	[Test (Description = "NEG: half-carry set when bit 3 of result is set (regression: 1& vs 8&)")]
	public void NEG_HalfCarry ()
	{
		LoadProgram ([ "neg r20" ]);
		Cpu.Mmio.Data[R20] = 0x08;   // R = (byte)(0 - 0x08) = 0xF8; R3=1, d3=1 → H=1
		decoder.Decode(Cpu);
		Assert.That (Cpu.Sreg, Is.EqualTo (SREG_H | SREG_S | SREG_N | SREG_C));
	}

	[Test (Description = "ADC: half-carry set on nibble carry (regression: 1& vs 8&)")]
	public void ADC_HalfCarry ()
	{
		LoadProgram ([ "adc r0, r1" ]);
		Cpu.Mmio.Data[R0] = 0x08;
		Cpu.Mmio.Data[R1] = 0x08;    // 0x08 + 0x08 = 0x10: carry from bit 3 → H=1
		decoder.Decode(Cpu);
		Assert.That (Cpu.Sreg, Is.EqualTo (SREG_H));
	}

	[Test (Description = "CPI: half-carry set on nibble borrow (regression: 1& vs 8&)")]
	public void CPI_HalfCarry ()
	{
		LoadProgram ([ "cpi r16, 0x08" ]);
		Cpu.Mmio.Data[R16] = 0x10;   // 0x10 - 0x08 = 0x08: borrow from bit 3 → H=1
		decoder.Decode(Cpu);
		Assert.That (Cpu.Sreg, Is.EqualTo (SREG_H));
	}

	[Test (Description = "SUBI: half-carry set on nibble borrow (regression: 1& vs 8&)")]
	public void SUBI_HalfCarry ()
	{
		LoadProgram ([ "subi r16, 0x08" ]);
		Cpu.Mmio.Data[R16] = 0x10;   // 0x10 - 0x08 = 0x08: borrow from bit 3 → H=1
		decoder.Decode(Cpu);
		Assert.Multiple(() =>
		{
			Assert.That (Cpu.Mmio.Data[R16], Is.EqualTo (0x08));
			Assert.That (Cpu.Sreg, Is.EqualTo (SREG_H));
		});
	}

	[Test (Description = "SBIW: half-carry set on nibble borrow (regression: 1& vs 8&)")]
	public void SBIW_HalfCarry ()
	{
		LoadProgram ([ "sbiw r24, 8" ]);
		Cpu.Mmio.Data[R24] = 0x10;
		Cpu.Mmio.Data[R25] = 0x00;   // r25:r24 = 0x0010; 0x0010 - 8 = 0x0008, borrow at bit 3 → H=1
		decoder.Decode(Cpu);
		Assert.Multiple(() =>
		{
			Assert.That (Cpu.Mmio.Data[R24], Is.EqualTo (0x08));
			Assert.That (Cpu.Mmio.Data[R25], Is.EqualTo (0x00));
			Assert.That (Cpu.Sreg, Is.EqualTo (SREG_H));
		});
	}

	[Test (Description = "Should execute NOP instruction")]
	public void NOP ()
	{
		LoadProgram ([
			"nop",
		]);
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
		});
	}

	[Test (Description = "Should execute OUT 0x3f, r1 instruction")]
	public void OUT ()
	{
		LoadProgram ([
			"out 0x3f, r1",
		]);
		Cpu.Mmio.Data[R1] = 0x5a;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Mmio.Data[0x5f], Is.EqualTo (0x5a));
		});
	}
	
	[Test (Description = "Should execute POP r26 instruction")]
	public void POP ()
	{
		LoadProgram ([
			"pop r26",
		]);
		Cpu.Mmio.Data[SPH] = 0;
		Cpu.Mmio.Data[SP] = 0xff;
		Cpu.Mmio.Data[0x100] = 0x1a;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[X], Is.EqualTo (0x1a));
			Assert.That (Cpu.Mmio.DataView.GetUint16 (SP, true), Is.EqualTo (0x100));
		});
	}
	
	[Test (Description = "Should execute PUSH r11 instruction")]
	public void PUSH ()
	{
		LoadProgram ([
			"push r11",
		]);
		Cpu.Mmio.Data[SPH] = 0;
		Cpu.Mmio.Data[SP] = 0xff;
		Cpu.Mmio.Data[R11] = 0x2a;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[0xff], Is.EqualTo (0x2a));
			Assert.That (Cpu.Mmio.DataView.GetUint16 (SP, true), Is.EqualTo (0xfe));
		});
	}
	
	[Test (Description = "Should execute RCALL .+6 instruction")]
	public void RCALL ()
	{
		LoadProgram ([
			"rcall 6"
		]);
		Cpu.Mmio.Data[SPH] = 0;
		Cpu.Mmio.Data[SP] = 0x80;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (4));
			Assert.That (Cpu.Cycles, Is.EqualTo (3));
			Assert.That (Cpu.Mmio.Data[SP], Is.EqualTo (0x7e)); // Return address low byte
			Assert.That (Cpu.Mmio.DataView.GetUint16 (0x80, true), Is.EqualTo (1)); // SP should be decremented 
		});
	}
	
	[Test (Description = "Should execute RCALL .-4 instruction")]
	public void RCALL_Negative ()
	{
		LoadProgram ([
			"nop",
			"rcall -4"
		]);
		Cpu.Mmio.Data[SPH] = 0;
		Cpu.Mmio.Data[SP] = 0x80;
		decoder.Decode(Cpu);
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (0));
			Assert.That (Cpu.Cycles, Is.EqualTo (4));
			Assert.That (Cpu.Mmio.Data[SP], Is.EqualTo (0x7e)); // Return address low byte
			Assert.That (Cpu.Mmio.DataView.GetUint16 (0x80, true), Is.EqualTo (2)); // SP should be decremented 
		});
	}
	
	[Test (Description = "Should push 3-byte return address when executing RCALL instruction on device with >128k flash")]
	public void RCALL_3Byte ()
	{
		Cpu = new AVR8Sharp.Core.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"rcall 6"
		]);
		Cpu.Mmio.Data[SPH] = 0;
		Cpu.Mmio.Data[SP] = 0x80;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (4));
			Assert.That (Cpu.Cycles, Is.EqualTo (4));
			Assert.That (Cpu.Mmio.Data[SP], Is.EqualTo (0x7d)); // Return address low byte
			Assert.That (Cpu.Mmio.DataView.GetUint16 (0x80, true), Is.EqualTo (1)); // SP should be decremented by 3 
		});
	}
	
	[Test (Description = "Should execute RET instruction")]
	public void RET ()
	{
		LoadProgram ([
			"ret",
		]);
		Cpu.Mmio.Data[SPH] = 0;
		Cpu.Mmio.Data[SP] = 0x90;
		Cpu.Mmio.Data[0x92] = 16;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (16));
			Assert.That (Cpu.Cycles, Is.EqualTo (4));
			Assert.That (Cpu.Mmio.Data[SP], Is.EqualTo (0x92));
		});
	}
	
	[Test (Description = "Should execute `RET` instruction on device with >128k flash")]
	public void RET_3Byte ()
	{
		Cpu = new AVR8Sharp.Core.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"ret",
		]);
		Cpu.Mmio.Data[SPH] = 0;
		Cpu.Mmio.Data[SP] = 0x90;
		Cpu.Mmio.Data[0x91] = 0x1;
		Cpu.Mmio.Data[0x93] = 0x16;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (0x10016));
			Assert.That (Cpu.Cycles, Is.EqualTo (5));
			Assert.That (Cpu.Mmio.Data[SP], Is.EqualTo (0x93));
		});
	}
	
	[Test (Description = "Should execute RETI instruction")]
	public void RETI ()
	{
		LoadProgram ([
			"reti",
		]);
		Cpu.Mmio.Data[SPH] = 0;
		Cpu.Mmio.Data[SP] = 0xc0;
		Cpu.Mmio.Data[0xc2] = 200;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (200));
			Assert.That (Cpu.Cycles, Is.EqualTo (4));
			Assert.That (Cpu.Mmio.Data[SP], Is.EqualTo (0xc2));
			Assert.That (Cpu.Sreg, Is.EqualTo (SREG_I));
		});
	}
	
	[Test (Description = "Should execute `RETI` instruction on device with >128k flash")]
	public void RETI_3Byte ()
	{
		Cpu = new AVR8Sharp.Core.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"reti",
		]);
		Cpu.Mmio.Data[SPH] = 0;
		Cpu.Mmio.Data[SP] = 0xc0;
		Cpu.Mmio.Data[0xc1] = 0x1;
		Cpu.Mmio.Data[0xc3] = 0x30;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (0x10030));
			Assert.That (Cpu.Cycles, Is.EqualTo (5));
			Assert.That (Cpu.Mmio.Data[SP], Is.EqualTo (0xc3));
			Assert.That (Cpu.Sreg, Is.EqualTo (SREG_I));
		});
	}
	
	[Test (Description = "Should execute RJMP 2 instruction")]
	public void RJMP ()
	{
		LoadProgram ([
			"rjmp 2"
		]);
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (2));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
		});
	}
	
	[Test (Description = "Should execute ROR r0 instruction")]
	public void ROR ()
	{
		LoadProgram ([
			"ror r0",
		]);
		Cpu.Mmio.Data[R0] = 0x11;
		decoder.Decode(Cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Mmio.Data[R0], Is.EqualTo (0x08));
			Assert.That (Cpu.Sreg, Is.EqualTo (SREG_S | SREG_V | SREG_C));
		});
	}
	
	[Test (Description = "Should execute SBC r0, r1 instruction when carry is on and result overflows")]
	public void SBC_Overflow ()
	{
		LoadProgram ([
			"sbc r0, r1",
		]);
		Cpu.Mmio.Data[R0] = 0x00;
		Cpu.Mmio.Data[R1] = 10;
		Cpu.WriteData(95, (byte)(SREG_C));
		decoder.Decode(Cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Mmio.Data[R0], Is.EqualTo (245));
			Assert.That (Cpu.Sreg, Is.EqualTo (SREG_H | SREG_S | SREG_N | SREG_C));
		});
	}
	
	[Test (Description = "Should execute SBCI r23, 3")]
	public void SBCI ()
	{
		LoadProgram ([
			"sbci r23, 3",
		]);
		Cpu.Mmio.Data[R23] = 3;
		Cpu.WriteData((ushort)SREG, (byte)(SREG_I | SREG_C));
		decoder.Decode(Cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Sreg, Is.EqualTo (SREG_I | SREG_H | SREG_S | SREG_N | SREG_C));
		});
	}
	
	[Test (Description = "Should execute SBI 0x0c, 5 instruction")]
	public void SBI ()
	{
		LoadProgram ([
			"sbi 0x0c, 5",
		]);
		Cpu.Mmio.Data[0x2c] = 0b00001111;
		decoder.Decode(Cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[0x2c], Is.EqualTo (0b00101111));
		});
	}
	
	[Test (Description = "Should execute SBIS 0x0c, 5 when bit is clear")]
	public void SBIS_Clear ()
	{
		LoadProgram ([
			"sbis 0x0c, 5",
		]);
		Cpu.Mmio.Data[0x2c] = 0b00001111;
		decoder.Decode(Cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
		});
	}
	
	[Test (Description = "Should execute SBIS 0x0c, 5 when bit is set")]
	public void SBIS_Set ()
	{
		LoadProgram ([
			"sbis 0x0c, 5",
		]);
		Cpu.Mmio.Data[0x2c] = 0b00101111;
		decoder.Decode(Cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (2));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
		});
	}
	
	[Test (Description = "Should execute SBIS 0x0c, 5 when bit is set and followed by 2-word instruction")]
	public void SBIS_Set_Two_Words ()
	{
		LoadProgram ([
			"sbis 0x0c, 5",
			"call 0xb8"
		]);
		Cpu.Mmio.Data[0x2c] = 0b00101111;
		decoder.Decode(Cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (3));
			Assert.That (Cpu.Cycles, Is.EqualTo (3));
		});
	}
	
	[Test (Description = "Should execute ST X, r1 instruction")]
	public void ST ()
	{
		LoadProgram ([
			"st X, r1",
		]);
		Cpu.Mmio.Data[R1] = 0x5a;
		Cpu.Mmio.Data[X] = 0x9a;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[0x9a], Is.EqualTo (0x5a));
			Assert.That (Cpu.Mmio.Data[X], Is.EqualTo (0x9a)); // X should not be modified
		});
	}
	
	[Test (Description = "Should execute ST X+, r1 instruction")]
	public void ST_PostIncrement ()
	{
		LoadProgram ([
			"st X+, r1",
		]);
		Cpu.Mmio.Data[R1] = 0x5a;
		Cpu.Mmio.Data[X] = 0x9a;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[0x9a], Is.EqualTo (0x5a));
			Assert.That (Cpu.Mmio.Data[X], Is.EqualTo (0x9b)); // X should be incremented
		});
	}
	
	[Test (Description = "Should execute ST -X, r17 instruction")]
	public void ST_PreDecrement ()
	{
		LoadProgram ([
			"st -X, r17",
		]);
		Cpu.Mmio.Data[R17] = 0x88;
		Cpu.Mmio.Data[X] = 0x99;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[0x98], Is.EqualTo (0x88));
			Assert.That (Cpu.Mmio.Data[X], Is.EqualTo (0x98)); // X should be decremented
		});
	}
	
	[Test (Description = "Should execute ST Y, r2 instruction")]
	public void ST_Y ()
	{
		LoadProgram ([
			"st Y, r2",
		]);
		Cpu.Mmio.Data[R2] = 0x5b;
		Cpu.Mmio.Data[Y] = 0x9a;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[0x9a], Is.EqualTo (0x5b));
			Assert.That (Cpu.Mmio.Data[Y], Is.EqualTo (0x9a)); // Y should not be modified
		});
	}
	
	[Test (Description = "Should execute ST Y+, r1 instruction")]
	public void ST_Y_PostIncrement ()
	{
		LoadProgram ([
			"st Y+, r1",
		]);
		Cpu.Mmio.Data[R1] = 0x5a;
		Cpu.Mmio.Data[Y] = 0x9a;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[0x9a], Is.EqualTo (0x5a));
			Assert.That (Cpu.Mmio.Data[Y], Is.EqualTo (0x9b)); // Y should be incremented
		});
	}
	
	[Test (Description = "Should execute ST -Y, r1 instruction")]
	public void ST_Y_PreDecrement ()
	{
		LoadProgram ([
			"st -Y, r1",
		]);
		Cpu.Mmio.Data[R1] = 0x5a;
		Cpu.Mmio.Data[Y] = 0x9a;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[0x99], Is.EqualTo (0x5a));
			Assert.That (Cpu.Mmio.Data[Y], Is.EqualTo (0x99)); // Y should be decremented
		});
	}
	
	[Test (Description = "Should execute STD Y+17, r0 instruction")]
	public void STD_Y ()
	{
		LoadProgram ([
			"std Y+17, r0",
		]);
		Cpu.Mmio.Data[R0] = 0xba;
		Cpu.Mmio.Data[Y] = 0x9a;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[0x9a + 17], Is.EqualTo (0xba));
			Assert.That (Cpu.Mmio.Data[Y], Is.EqualTo (0x9a)); // Y should not be modified
		});
	}
	
	[Test (Description = "Should execute ST Z, r16 instruction")]
	public void ST_Z ()
	{
		LoadProgram ([
			"st Z, r16",
		]);
		Cpu.Mmio.Data[R16] = 0xdf;
		Cpu.Mmio.Data[Z] = 0x40;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[0x40], Is.EqualTo (0xdf));
			Assert.That (Cpu.Mmio.Data[Z], Is.EqualTo (0x40)); // Z should not be modified
		});
	}
	
	[Test (Description = "Should execute ST Z+, r0 instruction")]
	public void ST_Z_PostIncrement ()
	{
		LoadProgram ([
			"st Z+, r0",
		]);
		Cpu.Mmio.Data[R0] = 0x55;
		Cpu.Mmio.DataView.SetUint16 (Z, 0x155, true);
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[0x155], Is.EqualTo (0x55));
			Assert.That (Cpu.Mmio.DataView.GetUint16 (Z, true), Is.EqualTo (0x156)); // Z should be incremented
		});
	}
	
	[Test (Description = "Should execute ST -Z, r16 instruction")]
	public void ST_Z_PreDecrement ()
	{
		LoadProgram ([
			"st -Z, r16",
		]);
		Cpu.Mmio.Data[R16] = 0x5a;
		Cpu.Mmio.Data[Z] = 0xff;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[0xfe], Is.EqualTo (0x5a));
			Assert.That (Cpu.Mmio.Data[Z], Is.EqualTo (0xfe)); // Z should be decremented
		});
	}
	
	[Test (Description = "Should execute STD Z+1, r0 instruction")]
	public void STD_Z ()
	{
		LoadProgram ([
			"std Z+1, r0",
		]);
		Cpu.Mmio.Data[R0] = 0xcc;
		Cpu.Mmio.Data[Z] = 0x50;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[0x51], Is.EqualTo (0xcc));
			Assert.That (Cpu.Mmio.Data[Z], Is.EqualTo (0x50)); // Z should not be modified
		});
	}
	
	[Test (Description = "Should execute STS 0x151, r31 instruction")]
	public void STS ()
	{
		LoadProgram ([
			"sts 0x151, r31",
		]);
		Cpu.Mmio.Data[R31] = 0x80;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (2));
			Assert.That (Cpu.Cycles, Is.EqualTo (2));
			Assert.That (Cpu.Mmio.Data[0x151], Is.EqualTo (0x80));
		});
	}
	
	[Test (Description = "Should execute SUB r0, r1 instruction")]
	public void SUB ()
	{
		LoadProgram ([
			"sub r0, r1",
		]);
		Cpu.Mmio.Data[R0] = 0;
		Cpu.Mmio.Data[R1] = 10;
		decoder.Decode(Cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Mmio.Data[R0], Is.EqualTo (246));
			Assert.That (Cpu.Sreg, Is.EqualTo (SREG_S | SREG_N | SREG_C | SREG_H));
		});
	}
	
	[Test (Description = "Should execute SWAP r1 instruction")]
	public void SWAP ()
	{
		LoadProgram ([
			"swap r1",
		]);
		Cpu.Mmio.Data[R1] = 0xa5;
		decoder.Decode(Cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Mmio.Data[R1], Is.EqualTo (0x5a));
		});
	}
	
	[Test (Description = "Should execute WDR instruction and call `cpu.onWatchdogReset`")]
	public void WDR ()
	{
		LoadProgram ([
			"wdr",
		]);
		Cpu.OnWatchdogReset = () => {
			Cpu.Mmio.Data[0x100] = 0x1;
		};
		decoder.Decode(Cpu);
		Assert.That (Cpu.Mmio.Data[0x100], Is.EqualTo (0x1));
	}
	
	[Test (Description = "Should execute XCH Z, r21 instruction")]
	public void XCH ()
	{
		LoadProgram ([
			"xch Z, r21",
		]);
		Cpu.Mmio.Data[R21] = 0xa1;
		Cpu.Mmio.Data[Z] = 0x50;
		Cpu.Mmio.Data[0x50] = 0xb9;
		decoder.Decode(Cpu);
		Assert.Multiple (() =>
		{
			Assert.That (Cpu.Pc, Is.EqualTo (1));
			Assert.That (Cpu.Cycles, Is.EqualTo (1));
			Assert.That (Cpu.Mmio.Data[R21], Is.EqualTo (0xb9));
			Assert.That (Cpu.Mmio.Data[0x50], Is.EqualTo (0xa1));
		});
	}

	private void LoadProgram (string[] instructions)
	{
		var code = string.Join ("\n", instructions);
		var assembler = new AVR8Sharp.Core.Utils.AvrAssembler ();
		var program = assembler.Assemble (code);
		if (assembler.Errors.Count > 0) {
			throw new Exception (string.Join ("\n", assembler.Errors));
		}
		Cpu.LoadProgram (program);
	}
}
