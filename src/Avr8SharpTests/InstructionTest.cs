namespace AVr8SharpTests;

[TestFixture]
public class Instruction
{
	const int r0 = 0;
	const int r1 = 1;
	const int r2 = 2;
	const int r3 = 3;
	const int r4 = 4;
	const int r5 = 5;
	const int r6 = 6;
	const int r7 = 7;
	const int r8 = 8;
	const int r9 = 9;
	const int r10 = 10;
	const int r11 = 11;
	const int r12 = 12;
	const int r13 = 13;
	const int r14 = 14;
	const int r15 = 15;
	const int r16 = 16;
	const int r17 = 17;
	const int r18 = 18;
	const int r19 = 19;
	const int r20 = 20;
	const int r21 = 21;
	const int r22 = 22;
	const int r23 = 23;
	const int r24 = 24;
	const int r25 = 25;
	const int r26 = 26;
	const int r27 = 27;
	const int r31 = 31;
	const int X = 26;
	const int Y = 28;
	const int Z = 30;
	const int RAMPZ = 0x5B;
	const int EIND = 0x5C;
	const int SP = 93;
	const int SPH = 94;
	const int SREG = 95;
	
	// SREG Bits: I-HSVNZC
	const int SREG_C = 0b00000001;
	const int SREG_Z = 0b00000010;
	const int SREG_N = 0b00000100;
	const int SREG_V = 0b00001000;
	const int SREG_S = 0b00010000;
	const int SREG_H = 0b00100000;
	const int SREG_I = 0b10000000;
	
	private AVR8Sharp.Cpu.Cpu cpu;
	
	[SetUp]
	public void Setup()
	{
		cpu = new AVR8Sharp.Cpu.Cpu(new ushort[0x8000]);
	}
	
	[Test(Description = "Should execute ADC r0, r1 instruction when carry is on")]
	public void ADC()
	{
		LoadProgram ([
			"adc r0, r1"
		]);
		cpu.Data[r0] = 10;
		cpu.Data[r1] = 20;
		cpu.Data[SREG] = SREG_C;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
        Assert.Multiple(() =>
        {
            Assert.That(cpu.PC, Is.EqualTo(1));
            Assert.That(cpu.Cycles, Is.EqualTo(1));
            Assert.That(cpu.Data[r0], Is.EqualTo(31));
            Assert.That(cpu.Data[SREG], Is.EqualTo(0));
        });
    }
	
	[Test(Description = "Should execute ADC r0, r1 instruction when carry is on and the result overflows")]
	public void ADC_Overflow()
	{
		LoadProgram ([
			"adc r0, r1"
		]);
		cpu.Data[r0] = 10;
		cpu.Data[r1] = 245;
		cpu.Data[SREG] = SREG_C;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple(() =>
		{
			Assert.That(cpu.PC, Is.EqualTo(1));
			Assert.That(cpu.Cycles, Is.EqualTo(1));
			Assert.That(cpu.Data[r0], Is.EqualTo(0));
			Assert.That(cpu.Data[SREG], Is.EqualTo(SREG_H | SREG_Z | SREG_C));
		});
	}
	
	[Test(Description = "Should execute ADD r0, r1 instruction when result overflows")]
	public void ADD_Overflow()
	{
		LoadProgram ([
			"add r0, r1"
		]);
		cpu.Data[r0] = 11;
		cpu.Data[r1] = 245;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple(() =>
		{
			Assert.That(cpu.PC, Is.EqualTo(1));
			Assert.That(cpu.Cycles, Is.EqualTo(1));
			Assert.That(cpu.Data[r0], Is.EqualTo(0));
			Assert.That(cpu.Data[SREG], Is.EqualTo(SREG_H | SREG_Z | SREG_C));
		});
	}
	
	[Test(Description = "Should execute ADD r0, r1 instruction when carry is on")]
	public void ADD()
	{
		LoadProgram ([
			"add r0, r1"
		]);
		cpu.Data[r0] = 11;
		cpu.Data[r1] = 244;
		cpu.Data[SREG] = SREG_C;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple(() =>
		{
			Assert.That(cpu.PC, Is.EqualTo(1));
			Assert.That(cpu.Cycles, Is.EqualTo(1));
			Assert.That(cpu.Data[r0], Is.EqualTo(255));
			Assert.That(cpu.Data[SREG], Is.EqualTo(SREG_S | SREG_N));
		});
	}
	
	[Test(Description = "Should execute ADD r0, r1 instruction when carry is on and the result overflows")]
	public void ADD_Overflow_Carry()
	{
		LoadProgram ([
			"add r0, r1"
		]);
		cpu.Data[r0] = 11;
		cpu.Data[r1] = 245;
		cpu.Data[SREG] = SREG_C;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple(() =>
		{
			Assert.That(cpu.PC, Is.EqualTo(1));
			Assert.That(cpu.Cycles, Is.EqualTo(1));
			Assert.That(cpu.Data[r0], Is.EqualTo(0));
			Assert.That(cpu.Data[SREG], Is.EqualTo(SREG_H | SREG_Z | SREG_C));
		});
	}
	
	[Test(Description = "Should execute BCLR 2 instruction")]
	public void BCLR()
	{
		LoadProgram ([
			"bclr 2"
		]);
		cpu.Data[SREG] = 0xff;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple(() =>
		{
			Assert.That(cpu.PC, Is.EqualTo(1));
			Assert.That(cpu.Cycles, Is.EqualTo(1));
			Assert.That(cpu.Data[SREG], Is.EqualTo(0xfb));
		});
	}
	
	[Test(Description = "Should execute BLD r4, 7 instruction")]
	public void BLD()
	{
		LoadProgram ([
			"bld r4, 7"
		]);
		cpu.Data[r4] = 0x15;
		cpu.Data[SREG] = 0x40;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple(() =>
		{
			Assert.That(cpu.PC, Is.EqualTo(1));
			Assert.That(cpu.Cycles, Is.EqualTo(1));
			Assert.That(cpu.Data[r4], Is.EqualTo(0x95));
			Assert.That(cpu.Data[SREG], Is.EqualTo(0x40));
		});
	}

	[Test (Description = "Should execute BRBC 0, +8 instruction when SREG.C is clear")]
	public void BRBC ()
	{
		LoadProgram ([
			"brbc 0, +8",
		]);
		cpu.Data[SREG] = SREG_V;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1 + 8 / 2));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
		});
	}
	
	[Test (Description = "Should execute BRBC 0, +8 instruction when SREG.C is set")]
	public void BRBC_SREG_C ()
	{
		LoadProgram ([
			"brbc 0, +8",
		]);
		cpu.Data[SREG] = SREG_C;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
		});
	}
	
	[Test (Description = "Should execute BRBS 3, 92 instruction when SREG.V is set")]
	public void BRBS ()
	{
		LoadProgram ([
			"brbs 3, +92",
		]);
		cpu.Data[SREG] = SREG_V;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1 + 92 / 2));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
		});
	}
	
	[Test (Description = "Should execute BRBS 3, -4 instruction when SREG.V is set")]
	public void BRBS_Negative ()
	{
		LoadProgram ([
			"brbs 3, -4",
		]);
		cpu.Data[SREG] = SREG_V;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (0));
			Assert.That (cpu.Cycles, Is.EqualTo (3));
		});
	}
	
	[Test (Description = "Should execute BRBS 3, -4 instruction when SREG.V is clear")]
	public void BRBS_Clear ()
	{
		LoadProgram ([
			"brbs 3, -4",
		]);
		cpu.Data[SREG] = 0x0;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
		});
	}
	
	[Test (Description = "Should execute CALL instruction")]
	public void CALL ()
	{
		LoadProgram ([
			"call 0xb8",
		]);
		cpu.Data[SPH] = 0x00;
		cpu.Data[SP] = 150;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (0x5c));
			Assert.That (cpu.Cycles, Is.EqualTo (4));
			Assert.That (cpu.Data[150], Is.EqualTo (2)); // Return address low byte
			Assert.That (cpu.Data[SP], Is.EqualTo (148)); // SP should be decremented 
		});
	}
	
	[Test (Description = "Should push 3-byte return address when executing CALL instruction on device with >128k flash")]
	public void CALL_3Byte ()
	{
		cpu = new AVR8Sharp.Cpu.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"call 0xb8",
		]);
		cpu.Data[SPH] = 0x00;
		cpu.Data[SP] = 150;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (0x5c));
			Assert.That (cpu.Cycles, Is.EqualTo (5));
			Assert.That (cpu.Data[150], Is.EqualTo (2)); // Return address low byte
			Assert.That (cpu.Data[SP], Is.EqualTo (147)); // SP should be incremented by 3 
		});
	}
	
	[Test (Description = "Should execute CBI 0x0c, 5 instruction")]
	public void CBI ()
	{
		LoadProgram ([
			"cbi 0x0c, 5",
		]);
		cpu.Data[0x2c] = 0b11111111;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[0x2c], Is.EqualTo (0b11011111));
		});
	}
	
	[Test (Description = "Should execute CPC r27, r18 instruction")]
	public void CPC ()
	{
		LoadProgram ([
			"cpc r27, r18",
		]);
		cpu.Data[r18] = 0x1;
		cpu.Data[r27] = 0x1;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[SREG], Is.EqualTo (0));
		});
	}
	
	[Test (Description = "Should execute CPC r24, r1 instruction and set")]
	public void CPC_Negative ()
	{
		LoadProgram ([
			"cpc r24, r1",
		]);
		cpu.Data[r1] = 0;
		cpu.Data[r24] = 0;
		cpu.Data[SREG] = SREG_I | SREG_C;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[SREG], Is.EqualTo (SREG_I | SREG_H | SREG_S | SREG_N | SREG_C));
		});
	}
	
	[Test (Description = "Should execute CPI r26, 0x9 instruction")]
	public void CPI ()
	{
		LoadProgram ([
			"cpi r26, 0x9",
		]);
		cpu.Data[r26] = 0x8;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[SREG], Is.EqualTo (SREG_H | SREG_S | SREG_N | SREG_C));
		});
	}
	
	[Test (Description = "Should execute CPSE r2, r3 instruction when r2 != r3")]
	public void CPSE ()
	{
		LoadProgram ([
			"cpse r2, r3"
		]);
		cpu.Data[r2] = 10;
		cpu.Data[r3] = 11;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
		});
	}
	
	[Test (Description = "Should execute CPSE r2, r3 instruction when r2 == r3")]
	public void CPSE_Equal ()
	{
		LoadProgram ([
			"cpse r2, r3"
		]);
		cpu.Data[r2] = 10;
		cpu.Data[r3] = 10;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (2));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
		});
	}
	
	[Test (Description = "Should execute CPSE r2, r3 when r2 == r3 and followed by 2-word instruction")]
	public void CPSE_2Word ()
	{
		LoadProgram ([
			"cpse r2, r3",
			"call 8",
		]);
		cpu.Data[r2] = 10;
		cpu.Data[r3] = 10;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (3));
			Assert.That (cpu.Cycles, Is.EqualTo (3));
		});
	}
	
	[Test (Description = "Should execute EICALL instruction")]
	public void EICALL ()
	{
		LoadProgram ([
			"eicall",
		]);
		cpu.Data[SPH] = 0x00;
		cpu.Data[SP] = 0x80;
		cpu.Data[EIND] = 0x01;
		cpu.DataView.SetUint16 (Z, 0x1234, true);
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (0x1234));
			Assert.That (cpu.Cycles, Is.EqualTo (4));
			Assert.That (cpu.Data[0x80], Is.EqualTo (1)); // Return address low byte
			Assert.That (cpu.Data[SP], Is.EqualTo (0x80 - 3)); // SP should be decremented 
		});
	}
	
	[Test (Description = "Should execute EIJMP instruction")]
	public void EIJMP ()
	{
		cpu = new AVR8Sharp.Cpu.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"eijmp",
		]);
		cpu.Data[EIND] = 0x01;
		cpu.DataView.SetUint16 (Z, 0x1040, true);
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (0x11040));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
		});
	}
	
	[Test (Description = "Should execute ELPM instruction")]
	public void ELPM ()
	{
		cpu = new AVR8Sharp.Cpu.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"elpm",
		]);
		cpu.Data[Z] = 0x50;
		cpu.Data[RAMPZ] = 0x2;
		cpu.SetProgramByte (0x20050, 0x62);
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (3));
			Assert.That (cpu.Data[r0], Is.EqualTo (0x62));
		});
	}
	
	[Test (Description = "Should execute ELPM r5, Z instruction")]
	public void ELPM_Register ()
	{
		cpu = new AVR8Sharp.Cpu.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"elpm r5, Z",
		]);
		cpu.Data[Z] = 0x11;
		cpu.Data[RAMPZ] = 0x1;
		cpu.SetProgramByte (0x10011, 0x99);
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (3));
			Assert.That (cpu.Data[r5], Is.EqualTo (0x99));
		});
	}
	
	[Test (Description = "Should execute ELPM r6, Z+ instruction")]
	public void ELPM_Register_PostIncrement ()
	{
		cpu = new AVR8Sharp.Cpu.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"elpm r6, Z+",
		]);
		cpu.DataView.SetUint16 (Z, 0xffff, true);
		cpu.Data[RAMPZ] = 0x2;
		cpu.SetProgramByte (0x2ffff, 0x22);
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (3));
			Assert.That (cpu.Data[r6], Is.EqualTo (0x22)); // Check that the value was loaded to r6
			Assert.That (cpu.DataView.GetUint16 (Z, true), Is.EqualTo (0x0000)); // Check that Z was incremented
			Assert.That (cpu.Data[RAMPZ], Is.EqualTo (3)); // Check that RAMPZ was incremented
		});
	}
	
	[Test (Description = "Should clamp RAMPZ when executing ELPM r6, Z+ instruction")]
	public void ELPM_Register_PostIncrement_RAMPZ ()
	{
		cpu = new AVR8Sharp.Cpu.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"elpm r6, Z+",
		]);
		cpu.DataView.SetUint16 (Z, 0xffff, true);
		cpu.Data[RAMPZ] = 0x3;
		cpu.SetProgramByte (0x2ffff, 0x22);
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.Data[RAMPZ], Is.EqualTo (0x0)); // Verify that RAMPZ was reset to zero
		});
	}
	
	[Test (Description = "Should execute ICALL instruction")]
	public void ICALL ()
	{
		LoadProgram ([
			"icall",
		]);
		cpu.Data[SPH] = 0x00;
		cpu.Data[SP] = 0x80;
		cpu.DataView.SetUint16 (Z, 0x2020, true);
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (0x2020));
			Assert.That (cpu.Cycles, Is.EqualTo (3));
			Assert.That (cpu.Data[0x80], Is.EqualTo (1)); // Return address low byte
			Assert.That (cpu.Data[SP], Is.EqualTo (0x7e)); // SP should be decremented 
		});
	}
	
	[Test (Description = "Should push 3-byte return address when executing ICALL instruction on device with >128k flash")]
	public void ICALL_3Byte ()
	{
		cpu = new AVR8Sharp.Cpu.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"icall",
		]);
		cpu.Data[SPH] = 0x00;
		cpu.Data[SP] = 0x80;
		cpu.DataView.SetUint16 (Z, 0x2020, true);
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (0x2020));
			Assert.That (cpu.Cycles, Is.EqualTo (4));
			Assert.That (cpu.Data[0x80], Is.EqualTo (1)); // Return address low byte
			Assert.That (cpu.Data[SP], Is.EqualTo (0x7d)); // SP should be decremented by 3 
		});
	}
	
	[Test (Description = "Should execute IJMP instruction")]
	public void IJMP ()
	{
		LoadProgram ([
			"ijmp",
		]);
		cpu.DataView.SetUint16 (Z, 0x1040, true);
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (0x1040));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
		});
	}
	
	[Test (Description = "Should execute IN r5, 0xb instruction")]
	public void IN ()
	{
		LoadProgram ([
			"in r5, 0xb",
		]);
		cpu.Data[0x2b] = 0xaf;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[r5], Is.EqualTo (0xaf));
		});
	}
	
	[Test (Description = "Should execute INC r5 instruction")]
	public void INC ()
	{
		LoadProgram ([
			"inc r5",
		]);
		cpu.Data[r5] = 0x7f;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[r5], Is.EqualTo (0x80));
			Assert.That (cpu.Data[SREG], Is.EqualTo (SREG_N | SREG_V));
		});
	}
	
	[Test (Description = "Should execute INC r5 instruction when r5 == 0xff")]
	public void INC_Overflow ()
	{
		LoadProgram ([
			"inc r5",
		]);
		cpu.Data[r5] = 0xff;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[r5], Is.EqualTo (0x00));
			Assert.That (cpu.Data[SREG], Is.EqualTo ( SREG_Z));
		});
	}
	
	[Test (Description = "Should execute JMP 0xb8 instruction")]
	public void JMP ()
	{
		LoadProgram ([
			"jmp 0xb8",
		]);
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (0x5c));
			Assert.That (cpu.Cycles, Is.EqualTo (3));
		});
	}
	
	[Test (Description = "Should execute LAC Z, r19 instruction")]
	public void LAC ()
	{
		LoadProgram ([
			"lac Z, r19",
		]);
		cpu.Data[r19] = 0x02;
		cpu.DataView.SetUint16 (Z, 0x100, true);
		cpu.Data[0x100] = 0x96;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[r19], Is.EqualTo (0x96));
			Assert.That (cpu.DataView.GetUint16 (Z, true), Is.EqualTo (0x100));
			Assert.That (cpu.Data[0x100], Is.EqualTo (0x94));
		});
	}
	
	[Test (Description = "Should execute LAS Z, r17 instruction")]
	public void LAS ()
	{
		LoadProgram ([
			"las Z, r17",
		]);
		cpu.Data[r17] = 0x11;
		cpu.Data[Z] = 0x80;
		cpu.Data[0x80] = 0x44;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[r17], Is.EqualTo (0x44));
			Assert.That (cpu.Data[Z], Is.EqualTo (0x80));
			Assert.That (cpu.Data[0x80], Is.EqualTo (0x55));
		});
	}
	
	[Test (Description = "Should execute LAT Z, r0 instruction")]
	public void LAT ()
	{
		LoadProgram ([
			"lat Z, r0",
		]);
		cpu.Data[r0] = 0x33;
		cpu.Data[Z] = 0x80;
		cpu.Data[0x80] = 0x66;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[r0], Is.EqualTo (0x66));
			Assert.That (cpu.Data[Z], Is.EqualTo (0x80));
			Assert.That (cpu.Data[0x80], Is.EqualTo (0x55));
		});
	}
	
	[Test (Description = "Should execute LD r1, X instruction")]
	public void LD ()
	{
		LoadProgram ([
			"ld r1, X",
		]);
		cpu.Data[0xc0] = 0x15;
		cpu.Data[X] = 0xc0;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[r1], Is.EqualTo (0x15));
			Assert.That (cpu.Data[X], Is.EqualTo (0xc0)); // X should not be modified
		});
	}
	
	[Test (Description = "Should execute LD r17, X+ instruction")]
	public void LD_PostIncrement ()
	{
		LoadProgram ([
			"ld r17, X+",
		]);
		cpu.Data[0xc0] = 0x15;
		cpu.Data[X] = 0xc0;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[r17], Is.EqualTo (0x15));
			Assert.That (cpu.Data[X], Is.EqualTo (0xc1)); // X should be incremented
		});
	}
	
	[Test (Description = "Should execute LD r1, -X instruction")]
	public void LD_PreDecrement ()
	{
		LoadProgram ([
			"ld r1, -X",
		]);
		cpu.Data[0x98] = 0x22;
		cpu.Data[X] = 0x99;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[r1], Is.EqualTo (0x22));
			Assert.That (cpu.Data[X], Is.EqualTo (0x98)); // X should be decremented
		});
	}
	
	[Test (Description = "Should execute LD r8, Y instruction")]
	public void LD_Y ()
	{
		LoadProgram ([
			"ld r8, Y",
		]);
		cpu.Data[0xc0] = 0x15;
		cpu.Data[Y] = 0xc0;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[r8], Is.EqualTo (0x15));
			Assert.That (cpu.Data[Y], Is.EqualTo (0xc0)); // Y should not be modified
		});
	}
	
	[Test (Description = "Should execute LD r3, Y+ instruction")]
	public void LD_Y_PostIncrement ()
	{
		LoadProgram ([
			"ld r3, Y+",
		]);
		cpu.Data[0xc0] = 0x15;
		cpu.Data[Y] = 0xc0;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[r3], Is.EqualTo (0x15));
			Assert.That (cpu.Data[Y], Is.EqualTo (0xc1)); // Y should be incremented
		});
	}
	
	[Test (Description = "Should execute LD r0, -Y instruction")]
	public void LD_Y_PreDecrement ()
	{
		LoadProgram ([
			"ld r0, -Y",
		]);
		cpu.Data[0x98] = 0x22;
		cpu.Data[Y] = 0x99;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[r0], Is.EqualTo (0x22));
			Assert.That (cpu.Data[Y], Is.EqualTo (0x98)); // Y should be decremented
		});
	}
	
	[Test (Description = "Should execute LDD r4, Y+2 instruction")]
	public void LDD_Y ()
	{
		LoadProgram ([
			"ldd r4, Y+2",
		]);
		cpu.Data[0x82] = 0x33;
		cpu.Data[Y] = 0x80;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[r4], Is.EqualTo (0x33));
			Assert.That (cpu.Data[Y], Is.EqualTo (0x80)); // Y should not be modified
		});
	}
	
	[Test (Description = "Should execute LD r5, Z instruction")]
	public void LD_Z ()
	{
		LoadProgram ([
			"ld r5, Z",
		]);
		cpu.Data[0xcc] = 0xf5;
		cpu.Data[Z] = 0xcc;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[r5], Is.EqualTo (0xf5));
			Assert.That (cpu.Data[Z], Is.EqualTo (0xcc)); // Z should not be modified
		});
	}
	
	[Test (Description = "Should execute LD r7, Z+ instruction")]
	public void LD_Z_PostIncrement ()
	{
		LoadProgram ([
			"ld r7, Z+",
		]);
		cpu.Data[0xc0] = 0x25;
		cpu.Data[Z] = 0xc0;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[r7], Is.EqualTo (0x25));
			Assert.That (cpu.Data[Z], Is.EqualTo (0xc1)); // Z should be incremented
		});
	}
	
	[Test (Description = "Should execute LD r0, -Z instruction")]
	public void LD_Z_PreDecrement ()
	{
		LoadProgram ([
			"ld r0, -Z",
		]);
		cpu.Data[0x9e] = 0x66;
		cpu.Data[Z] = 0x9f;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[r0], Is.EqualTo (0x66));
			Assert.That (cpu.Data[Z], Is.EqualTo (0x9e)); // Z should be decremented
		});
	}
	
	[Test (Description = "Should execute LDD r15, Z+31 instruction")]
	public void LDD_Z ()
	{
		LoadProgram ([
			"ldd r15, Z+31",
		]);
		cpu.Data[0x9f] = 0x33;
		cpu.Data[Z] = 0x80;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[r15], Is.EqualTo (0x33));
			Assert.That (cpu.Data[Z], Is.EqualTo (0x80)); // Z should not be modified
		});
	}
	
	[Test (Description = "Should execute LDI r28, 0xff instruction")]
	public void LDI ()
	{
		LoadProgram ([
			"ldi r28, 0xff",
		]);
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[Y], Is.EqualTo (0xff));
		});
	}
	
	[Test (Description = "Should execute LDS r5, 0x150 instruction")]
	public void LDS ()
	{
		LoadProgram ([
			"lds r5, 0x150",
		]);
		cpu.Data[0x150] = 0x7a;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (2));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[r5], Is.EqualTo (0x7a));
		});
	}
	
	[Test (Description = "Should execute LPM instruction")]
	public void LPM ()
	{
		cpu = new AVR8Sharp.Cpu.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"lpm",
		]);
		cpu.SetProgramWord (0x40, 0xa0);
		cpu.Data[Z] = 0x80;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (3));
			Assert.That (cpu.Data[r0], Is.EqualTo (0xa0));
			Assert.That (cpu.Data[Z], Is.EqualTo (0x80)); // Z should not be modified
		});
	}
	
	[Test (Description = "Should execute LPM r2, Z instruction")]
	public void LPM_Register ()
	{
		LoadProgram ([
			"lpm r2, Z",
		]);
		cpu.Data[Z] = 0x80;
		cpu.SetProgramWord (0x40, 0xa0);
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (3));
			Assert.That (cpu.Data[r2], Is.EqualTo (0xa0));
			Assert.That (cpu.Data[Z], Is.EqualTo (0x80)); // Z should not be modified
		});
	}
	
	[Test (Description = "Should execute LPM r1, Z+ instruction")]
	public void LPM_Register_PostIncrement ()
	{
		LoadProgram ([
			"lpm r1, Z+",
		]);
		cpu.Data[Z] = 0x80;
		cpu.SetProgramWord (0x40, 0xa0);
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (3));
			Assert.That (cpu.Data[r1], Is.EqualTo (0xa0));
			Assert.That (cpu.Data[Z], Is.EqualTo (0x81)); // Z should be incremented
		});
	}
	
	[Test (Description = "Should execute LSR r7 instruction")]
	public void LSR ()
	{
		LoadProgram ([
			"lsr r7",
		]);
		cpu.Data[r7] = 0x45;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[r7], Is.EqualTo (0x22));
			Assert.That (cpu.Data[SREG], Is.EqualTo (SREG_S | SREG_V | SREG_C));
		});
	}
	
	[Test (Description = "Should execute MOV r7, r8 instruction")]
	public void MOV ()
	{
		LoadProgram ([
			"mov r7, r8",
		]);
		cpu.Data[r8] = 0x45;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[r7], Is.EqualTo (0x45));
		});
	}
	
	[Test (Description = "Should execute MOVW r26, r22 instruction")]
	public void MOVW ()
	{
		LoadProgram ([
			"movw r26, r22",
		]);
		cpu.Data[r22] = 0x45;
		cpu.Data[r23] = 0x9a;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[r26], Is.EqualTo (0x45));
			Assert.That (cpu.Data[r27], Is.EqualTo (0x9a));
		});
	}
	
	[Test (Description = "Should execute MUL r5, r6 instruction")]
	public void MUL ()
	{
		LoadProgram ([
			"mul r5, r6",
		]);
		cpu.Data[r5] = 100;
		cpu.Data[r6] = 5;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.DataView.GetUint16 (0, true), Is.EqualTo (500));
			Assert.That (cpu.Data[SREG], Is.EqualTo (0));
		});
	}
	
	[Test (Description = "Should execute MUL r5, r6 instruction and update carry flag when numbers are big")]
	public void MUL_Carry ()
	{
		LoadProgram ([
			"mul r5, r6",
		]);
		cpu.Data[r5] = 200;
		cpu.Data[r6] = 200;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.DataView.GetUint16 (0, true), Is.EqualTo (40000));
			Assert.That (cpu.Data[SREG], Is.EqualTo (SREG_C));
		});
	}
	
	[Test (Description = "Should execute MUL r0, r1 and update the zero flag")]
	public void MUL_Zero ()
	{
		LoadProgram ([
			"mul r0, r1",
		]);
		cpu.Data[r0] = 0;
		cpu.Data[r1] = 9;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.DataView.GetUint16 (0, true), Is.EqualTo (0));
			Assert.That (cpu.Data[SREG], Is.EqualTo (SREG_Z));
		});
	}
	
	[Test (Description = "Should execute MULS r18, r19 instruction")]
	public void MULS ()
	{
		LoadProgram ([
			"muls r18, r19",
		]);
		cpu.Data[r18] = (-5) & 0xff;
		cpu.Data[r19] = 100;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.DataView.GetInt16 (0, true), Is.EqualTo (-500));
			Assert.That (cpu.Data[SREG], Is.EqualTo (SREG_C));
		});
	}
	
	[Test (Description = "Should execute MULSU r16, r17 instruction")]
	public void MULSU ()
	{
		LoadProgram ([
			"mulsu r16, r17",
		]);
		cpu.Data[r16] = (-5) & 0xff;
		cpu.Data[r17] = 200;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.DataView.GetInt16 (0, true), Is.EqualTo (-1000));
			Assert.That (cpu.Data[SREG], Is.EqualTo (SREG_C));
		});
	}
	
	[Test (Description = "Should execute NEG r20 instruction")]
	public void NEG ()
	{
		LoadProgram ([
			"neg r20",
		]);
		cpu.Data[r20] = 0x56;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[r20], Is.EqualTo (0xaa));
			Assert.That (cpu.Data[SREG], Is.EqualTo (SREG_S | SREG_N | SREG_C));
		});
	}
	
	[Test (Description = "Should execute NOP instruction")]
	public void NOP ()
	{
		LoadProgram ([
			"nop",
		]);
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
		});
	}

	[Test (Description = "Should execute OUT 0x3f, r1 instruction")]
	public void OUT ()
	{
		LoadProgram ([
			"out 0x3f, r1",
		]);
		cpu.Data[r1] = 0x5a;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[0x5f], Is.EqualTo (0x5a));
		});
	}
	
	[Test (Description = "Should execute POP r26 instruction")]
	public void POP ()
	{
		LoadProgram ([
			"pop r26",
		]);
		cpu.Data[SPH] = 0;
		cpu.Data[SP] = 0xff;
		cpu.Data[0x100] = 0x1a;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[X], Is.EqualTo (0x1a));
			Assert.That (cpu.DataView.GetUint16 (SP, true), Is.EqualTo (0x100));
		});
	}
	
	[Test (Description = "Should execute PUSH r11 instruction")]
	public void PUSH ()
	{
		LoadProgram ([
			"push r11",
		]);
		cpu.Data[SPH] = 0;
		cpu.Data[SP] = 0xff;
		cpu.Data[r11] = 0x2a;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[0xff], Is.EqualTo (0x2a));
			Assert.That (cpu.DataView.GetUint16 (SP, true), Is.EqualTo (0xfe));
		});
	}
	
	[Test (Description = "Should execute RCALL .+6 instruction")]
	public void RCALL ()
	{
		LoadProgram ([
			"rcall 6"
		]);
		cpu.Data[SPH] = 0;
		cpu.Data[SP] = 0x80;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (4));
			Assert.That (cpu.Cycles, Is.EqualTo (3));
			Assert.That (cpu.Data[SP], Is.EqualTo (0x7e)); // Return address low byte
			Assert.That (cpu.DataView.GetUint16 (0x80, true), Is.EqualTo (1)); // SP should be decremented 
		});
	}
	
	[Test (Description = "Should execute RCALL .-4 instruction")]
	public void RCALL_Negative ()
	{
		LoadProgram ([
			"nop",
			"rcall -4"
		]);
		cpu.Data[SPH] = 0;
		cpu.Data[SP] = 0x80;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (0));
			Assert.That (cpu.Cycles, Is.EqualTo (4));
			Assert.That (cpu.Data[SP], Is.EqualTo (0x7e)); // Return address low byte
			Assert.That (cpu.DataView.GetUint16 (0x80, true), Is.EqualTo (2)); // SP should be decremented 
		});
	}
	
	[Test (Description = "Should push 3-byte return address when executing RCALL instruction on device with >128k flash")]
	public void RCALL_3Byte ()
	{
		cpu = new AVR8Sharp.Cpu.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"rcall 6"
		]);
		cpu.Data[SPH] = 0;
		cpu.Data[SP] = 0x80;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (4));
			Assert.That (cpu.Cycles, Is.EqualTo (4));
			Assert.That (cpu.Data[SP], Is.EqualTo (0x7d)); // Return address low byte
			Assert.That (cpu.DataView.GetUint16 (0x80, true), Is.EqualTo (1)); // SP should be decremented by 3 
		});
	}
	
	[Test (Description = "Should execute RET instruction")]
	public void RET ()
	{
		LoadProgram ([
			"ret",
		]);
		cpu.Data[SPH] = 0;
		cpu.Data[SP] = 0x90;
		cpu.Data[0x92] = 16;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (16));
			Assert.That (cpu.Cycles, Is.EqualTo (4));
			Assert.That (cpu.Data[SP], Is.EqualTo (0x92));
		});
	}
	
	[Test (Description = "Should execute `RET` instruction on device with >128k flash")]
	public void RET_3Byte ()
	{
		cpu = new AVR8Sharp.Cpu.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"ret",
		]);
		cpu.Data[SPH] = 0;
		cpu.Data[SP] = 0x90;
		cpu.Data[0x91] = 0x1;
		cpu.Data[0x93] = 0x16;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (0x10016));
			Assert.That (cpu.Cycles, Is.EqualTo (5));
			Assert.That (cpu.Data[SP], Is.EqualTo (0x93));
		});
	}
	
	[Test (Description = "Should execute RETI instruction")]
	public void RETI ()
	{
		LoadProgram ([
			"reti",
		]);
		cpu.Data[SPH] = 0;
		cpu.Data[SP] = 0xc0;
		cpu.Data[0xc2] = 200;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (200));
			Assert.That (cpu.Cycles, Is.EqualTo (4));
			Assert.That (cpu.Data[SP], Is.EqualTo (0xc2));
			Assert.That (cpu.Data[SREG], Is.EqualTo (SREG_I));
		});
	}
	
	[Test (Description = "Should execute `RETI` instruction on device with >128k flash")]
	public void RETI_3Byte ()
	{
		cpu = new AVR8Sharp.Cpu.Cpu(new ushort[0x20000]);
		LoadProgram ([
			"reti",
		]);
		cpu.Data[SPH] = 0;
		cpu.Data[SP] = 0xc0;
		cpu.Data[0xc1] = 0x1;
		cpu.Data[0xc3] = 0x30;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (0x10030));
			Assert.That (cpu.Cycles, Is.EqualTo (5));
			Assert.That (cpu.Data[SP], Is.EqualTo (0xc3));
			Assert.That (cpu.Data[SREG], Is.EqualTo (SREG_I));
		});
	}
	
	[Test (Description = "Should execute RJMP 2 instruction")]
	public void RJMP ()
	{
		LoadProgram ([
			"rjmp 2"
		]);
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (2));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
		});
	}
	
	[Test (Description = "Should execute ROR r0 instruction")]
	public void ROR ()
	{
		LoadProgram ([
			"ror r0",
		]);
		cpu.Data[r0] = 0x11;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[r0], Is.EqualTo (0x08));
			Assert.That (cpu.Data[SREG], Is.EqualTo (SREG_S | SREG_V | SREG_C));
		});
	}
	
	[Test (Description = "Should execute SBC r0, r1 instruction when carry is on and result overflows")]
	public void SBC_Overflow ()
	{
		LoadProgram ([
			"sbc r0, r1",
		]);
		cpu.Data[r0] = 0x00;
		cpu.Data[r1] = 10;
		cpu.Data[95] = SREG_C;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[r0], Is.EqualTo (245));
			Assert.That (cpu.Data[SREG], Is.EqualTo (SREG_H | SREG_S | SREG_N | SREG_C));
		});
	}
	
	[Test (Description = "Should execute SBCI r23, 3")]
	public void SBCI ()
	{
		LoadProgram ([
			"sbci r23, 3",
		]);
		cpu.Data[r23] = 3;
		cpu.Data[SREG] = SREG_I | SREG_C;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[SREG], Is.EqualTo (SREG_I | SREG_H | SREG_S | SREG_N | SREG_C));
		});
	}
	
	[Test (Description = "Should execute SBI 0x0c, 5 instruction")]
	public void SBI ()
	{
		LoadProgram ([
			"sbi 0x0c, 5",
		]);
		cpu.Data[0x2c] = 0b00001111;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[0x2c], Is.EqualTo (0b00101111));
		});
	}
	
	[Test (Description = "Should execute SBIS 0x0c, 5 when bit is clear")]
	public void SBIS_Clear ()
	{
		LoadProgram ([
			"sbis 0x0c, 5",
		]);
		cpu.Data[0x2c] = 0b00001111;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
		});
	}
	
	[Test (Description = "Should execute SBIS 0x0c, 5 when bit is set")]
	public void SBIS_Set ()
	{
		LoadProgram ([
			"sbis 0x0c, 5",
		]);
		cpu.Data[0x2c] = 0b00101111;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (2));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
		});
	}
	
	[Test (Description = "Should execute SBIS 0x0c, 5 when bit is set and followed by 2-word instruction")]
	public void SBIS_Set_Two_Words ()
	{
		LoadProgram ([
			"sbis 0x0c, 5",
			"call 0xb8"
		]);
		cpu.Data[0x2c] = 0b00101111;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (3));
			Assert.That (cpu.Cycles, Is.EqualTo (3));
		});
	}
	
	[Test (Description = "Should execute ST X, r1 instruction")]
	public void ST ()
	{
		LoadProgram ([
			"st X, r1",
		]);
		cpu.Data[r1] = 0x5a;
		cpu.Data[X] = 0x9a;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[0x9a], Is.EqualTo (0x5a));
			Assert.That (cpu.Data[X], Is.EqualTo (0x9a)); // X should not be modified
		});
	}
	
	[Test (Description = "Should execute ST X+, r1 instruction")]
	public void ST_PostIncrement ()
	{
		LoadProgram ([
			"st X+, r1",
		]);
		cpu.Data[r1] = 0x5a;
		cpu.Data[X] = 0x9a;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[0x9a], Is.EqualTo (0x5a));
			Assert.That (cpu.Data[X], Is.EqualTo (0x9b)); // X should be incremented
		});
	}
	
	[Test (Description = "Should execute ST -X, r17 instruction")]
	public void ST_PreDecrement ()
	{
		LoadProgram ([
			"st -X, r17",
		]);
		cpu.Data[r17] = 0x88;
		cpu.Data[X] = 0x99;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[0x98], Is.EqualTo (0x88));
			Assert.That (cpu.Data[X], Is.EqualTo (0x98)); // X should be decremented
		});
	}
	
	[Test (Description = "Should execute ST Y, r2 instruction")]
	public void ST_Y ()
	{
		LoadProgram ([
			"st Y, r2",
		]);
		cpu.Data[r2] = 0x5b;
		cpu.Data[Y] = 0x9a;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[0x9a], Is.EqualTo (0x5b));
			Assert.That (cpu.Data[Y], Is.EqualTo (0x9a)); // Y should not be modified
		});
	}
	
	[Test (Description = "Should execute ST Y+, r1 instruction")]
	public void ST_Y_PostIncrement ()
	{
		LoadProgram ([
			"st Y+, r1",
		]);
		cpu.Data[r1] = 0x5a;
		cpu.Data[Y] = 0x9a;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[0x9a], Is.EqualTo (0x5a));
			Assert.That (cpu.Data[Y], Is.EqualTo (0x9b)); // Y should be incremented
		});
	}
	
	[Test (Description = "Should execute ST -Y, r1 instruction")]
	public void ST_Y_PreDecrement ()
	{
		LoadProgram ([
			"st -Y, r1",
		]);
		cpu.Data[r1] = 0x5a;
		cpu.Data[Y] = 0x9a;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[0x99], Is.EqualTo (0x5a));
			Assert.That (cpu.Data[Y], Is.EqualTo (0x99)); // Y should be decremented
		});
	}
	
	[Test (Description = "Should execute STD Y+17, r0 instruction")]
	public void STD_Y ()
	{
		LoadProgram ([
			"std Y+17, r0",
		]);
		cpu.Data[r0] = 0xba;
		cpu.Data[Y] = 0x9a;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[0x9a + 17], Is.EqualTo (0xba));
			Assert.That (cpu.Data[Y], Is.EqualTo (0x9a)); // Y should not be modified
		});
	}
	
	[Test (Description = "Should execute ST Z, r16 instruction")]
	public void ST_Z ()
	{
		LoadProgram ([
			"st Z, r16",
		]);
		cpu.Data[r16] = 0xdf;
		cpu.Data[Z] = 0x40;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[0x40], Is.EqualTo (0xdf));
			Assert.That (cpu.Data[Z], Is.EqualTo (0x40)); // Z should not be modified
		});
	}
	
	[Test (Description = "Should execute ST Z+, r0 instruction")]
	public void ST_Z_PostIncrement ()
	{
		LoadProgram ([
			"st Z+, r0",
		]);
		cpu.Data[r0] = 0x55;
		cpu.DataView.SetUint16 (Z, 0x155, true);
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[0x155], Is.EqualTo (0x55));
			Assert.That (cpu.DataView.GetUint16 (Z, true), Is.EqualTo (0x156)); // Z should be incremented
		});
	}
	
	[Test (Description = "Should execute ST -Z, r16 instruction")]
	public void ST_Z_PreDecrement ()
	{
		LoadProgram ([
			"st -Z, r16",
		]);
		cpu.Data[r16] = 0x5a;
		cpu.Data[Z] = 0xff;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[0xfe], Is.EqualTo (0x5a));
			Assert.That (cpu.Data[Z], Is.EqualTo (0xfe)); // Z should be decremented
		});
	}
	
	[Test (Description = "Should execute STD Z+1, r0 instruction")]
	public void STD_Z ()
	{
		LoadProgram ([
			"std Z+1, r0",
		]);
		cpu.Data[r0] = 0xcc;
		cpu.Data[Z] = 0x50;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[0x51], Is.EqualTo (0xcc));
			Assert.That (cpu.Data[Z], Is.EqualTo (0x50)); // Z should not be modified
		});
	}
	
	[Test (Description = "Should execute STS 0x151, r31 instruction")]
	public void STS ()
	{
		LoadProgram ([
			"sts 0x151, r31",
		]);
		cpu.Data[r31] = 0x80;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (2));
			Assert.That (cpu.Cycles, Is.EqualTo (2));
			Assert.That (cpu.Data[0x151], Is.EqualTo (0x80));
		});
	}
	
	[Test (Description = "Should execute SUB r0, r1 instruction")]
	public void SUB ()
	{
		LoadProgram ([
			"sub r0, r1",
		]);
		cpu.Data[r0] = 0;
		cpu.Data[r1] = 10;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[r0], Is.EqualTo (246));
			Assert.That (cpu.Data[SREG], Is.EqualTo (SREG_S | SREG_N | SREG_C));
		});
	}
	
	[Test (Description = "Should execute SWAP r1 instruction")]
	public void SWAP ()
	{
		LoadProgram ([
			"swap r1",
		]);
		cpu.Data[r1] = 0xa5;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);     
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[r1], Is.EqualTo (0x5a));
		});
	}
	
	[Test (Description = "Should execute WDR instruction and call `cpu.onWatchdogReset`")]
	public void WDR ()
	{
		LoadProgram ([
			"wdr",
		]);
		cpu.OnWatchdogReset = () => {
			cpu.Data[0x100] = 0x1;
		};
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.That (cpu.Data[0x100], Is.EqualTo (0x1));
	}
	
	[Test (Description = "Should execute XCH Z, r21 instruction")]
	public void XCH ()
	{
		LoadProgram ([
			"xch Z, r21",
		]);
		cpu.Data[r21] = 0xa1;
		cpu.Data[Z] = 0x50;
		cpu.Data[0x50] = 0xb9;
		AVR8Sharp.Cpu.Instruction.AvrInstruction (cpu);
		Assert.Multiple (() =>
		{
			Assert.That (cpu.PC, Is.EqualTo (1));
			Assert.That (cpu.Cycles, Is.EqualTo (1));
			Assert.That (cpu.Data[r21], Is.EqualTo (0xb9));
			Assert.That (cpu.Data[0x50], Is.EqualTo (0xa1));
		});
	}

	private void LoadProgram (string[] instructions)
	{
		var code = string.Join ("\n", instructions);
		var assembler = new AVR8Sharp.Utils.AvrAssembler ();
		var program = assembler.Assemble (code);
		if (assembler.Errors.Count > 0) {
			throw new Exception (string.Join ("\n", assembler.Errors));
		}
		cpu.LoadProgram (program);
	}
}
