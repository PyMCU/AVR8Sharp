using System.Runtime.CompilerServices;
namespace AVR8Sharp.Core.Cpu;

public static class Instruction
{
	public static void AvrInstruction (Cpu cpu)
	{
		var opcode = cpu.ProgramMemory[(int)cpu.PC];
		switch (opcode & 0xfc00) {
			case 0x1c00:
			{
				/* ADC, 0001 11rd dddd rrrr */
				ADC (ref cpu, ref opcode);
				break;
			}
			case 0xc00:
			{
				/* ADD, 0000 11rd dddd rrrr */
				ADD (ref cpu, ref opcode);
				break;
			}
			default:
			{

				if (InstructionBatchOne (ref cpu, ref opcode))
					break;
				
				if (InstructionBatchTwo (ref cpu, ref opcode))
					break;
				
				if (InstructionBatchThree (ref cpu, ref opcode))
					break;
				
				if (InstructionBatchFour (ref cpu, ref opcode))
					break;
				
				if (InstructionBatchFive (ref cpu, ref opcode))
					break;
				
				if (InstructionBatchSix (ref cpu, ref opcode))
					break;

				InstructionBatchSeven (ref cpu, ref opcode);
				break;
			}
		}
		cpu.PC = (uint)((cpu.PC + 1) % cpu.ProgramMemory.Length);
		cpu.Cycles++;
	}

	public static bool InstructionBatchOne (ref Cpu cpu, ref ushort opcode)
	{
		if ((opcode & 0xff00) == 0x9600) {
			// switched
			/* ADIW, 1001 0110 KKdd KKKK */
			ADIW (ref cpu, ref opcode);
			return true;
		}
		else if ((opcode & 0xfc00) == 0x2000) {
			/* AND, 0010 00rd dddd rrrr */
			AND (ref cpu, ref opcode);
			return true;
		}
		else if ((opcode & 0xf000) == 0x7000) {
			/* ANDI, 0111 KKKK dddd KKKK */
			ANDI (ref cpu, ref opcode);
			return true;
		}
		else if ((opcode & 0xfe0f) == 0x9405) {
			/* ASR, 1001 010d dddd 0101 */
			ASR (ref cpu, ref opcode);
			return true;
		}
		else if ((opcode & 0xff8f) == 0x9488) {
			/* BCLR, 1001 0100 1sss 1000 */
			BCLR (ref cpu, ref opcode);
			return true;
		}
		else if ((opcode & 0xfe08) == 0xf800) {
			/* BLD, 1111 100d dddd 0bbb */
			BLD (ref cpu, ref opcode);
			return true;
		}
		else if ((opcode & 0xfc00) == 0xf400) {
			/* BRBC, 1111 01kk kkkk ksss */
			BRBC (ref cpu, ref opcode);
			return true;
		}
		else if ((opcode & 0xfc00) == 0xf000) {
			/* BRBS, 1111 00kk kkkk ksss */
			BRBS (ref cpu, ref opcode);
			return true;
		}
		else if ((opcode & 0xff8f) == 0x9408) {
			/* BSET, 1001 0100 0sss 1000 */
			BSET (ref cpu, ref opcode);
			return true;
		}
		else if ((opcode & 0xfe08) == 0xfa00) {
			/* BST, 1111 101d dddd 0bbb */
			BST (ref cpu, ref opcode);
			return true;
		}
		else if ((opcode & 0xfe0e) == 0x940e) {
			/* CALL, 1001 010k kkkk 111k kkkk kkkk kkkk kkkk */
			CALL (ref cpu, ref opcode);
			return true;
		}
		else if ((opcode & 0xff00) == 0x9800) {
			// switched
			/* CBI, 1001 1000 AAAA Abbb */
			CBI (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9400) {
			/* COM, 1001 010d dddd 0000 */
			COM (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfc00) == 0x1400) {
			/* CP, 0001 01rd dddd rrrr */
			CP (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfc00) == 0x400) {
			/* CPC, 0000 01rd dddd rrrr */
			CPC (ref cpu, ref opcode);
			return true;
		}
		return false;
	}

	public static bool InstructionBatchTwo (ref Cpu cpu, ref ushort opcode)
	{
		if ((opcode & 0xf000) == 0x3000) {
			/* CPI, 0011 KKKK dddd KKKK */
			CPI (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfc00) == 0x1000) {
			/* CPSE, 0001 00rd dddd rrrr */
			CPSE (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x940a) {
			/* DEC, 1001 010d dddd 1010 */
			DEC (ref cpu, ref opcode);
			return true;
		} 
		else if (opcode == 0x9519) {
			/* EICALL, 1001 0101 0001 1001 */
			EICALL (ref cpu);
			return true;
		} 
		else if (opcode == 0x9419) {
			/* EIJMP, 1001 0100 0001 1001 */
			EIJMP (ref cpu);
			return true;
		} 
		else if (opcode == 0x95d8) {
			/* ELPM, 1001 0101 1101 1000 */
			ELPM (ref cpu);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9006) {
			/* ELPM(REG), 1001 000d dddd 0110 */
			ELPM_REG (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9007) {
			/* ELPM(INC), 1001 000d dddd 0111 */
			ELPM_INC (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfc00) == 0x2400) {
			/* EOR, 0010 01rd dddd rrrr */
			EOR (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xff88) == 0x308) {
			/* FMUL, 0000 0011 0ddd 1rrr */
			FMUL (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xff88) == 0x380) {
			/* FMULS, 0000 0011 1ddd 0rrr */
			FMULS (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xff88) == 0x388) {
			/* FMULSU, 0000 0011 1ddd 1rrr */
			FMULSU (ref cpu, ref opcode);
			return true;
		} 
		else if (opcode == 0x9509) {
			/* ICALL, 1001 0101 0000 1001 */
			ICALL (ref cpu);
			return true;
		} 
		else if (opcode == 0x9409) {
			/* IJMP, 1001 0100 0000 1001 */
			IJMP (ref cpu);
			return true;
		} 
		else if ((opcode & 0xf800) == 0xb000) {
			/* IN, 1011 0AAd dddd AAAA */
			IN (ref cpu, ref opcode);
			return true;
		} 
		
		return false;
	}

	public static bool InstructionBatchThree (ref Cpu cpu, ref ushort opcode)
	{
		if ((opcode & 0xfe0f) == 0x9403) {
			/* INC, 1001 010d dddd 0011 */
			INC (ref cpu, ref opcode);
			return true;
		} else if ((opcode & 0xfe0e) == 0x940c) {
			/* JMP, 1001 010k kkkk 110k kkkk kkkk kkkk kkkk */
			JMP (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9206) {
			/* LAC, 1001 001r rrrr 0110 */
			LAC (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9205) {
			/* LAS, 1001 001r rrrr 0101 */
			LAS (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9207) {
			/* LAT, 1001 001r rrrr 0111 */
			LAT (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xf000) == 0xe000) {
			/* LDI, 1110 KKKK dddd KKKK */
			LDI (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9000) {
			/* LDS, 1001 000d dddd 0000 kkkk kkkk kkkk kkkk */
			LDS (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x900c) {
			/* LDX, 1001 000d dddd 1100 */
			LDX (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x900d) {
			/* LDX(INC), 1001 000d dddd 1101 */
			LDX_INC (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x900e) {
			/* LDX(DEC), 1001 000d dddd 1110 */
			LDX_DEC (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x8008) {
			/* LDY, 1000 000d dddd 1000 */
			LDY (ref cpu, ref opcode);
			return true;
		} 
		return false;
	}

	public static bool InstructionBatchFour (ref Cpu cpu, ref ushort opcode)
	{
		if ((opcode & 0xfe0f) == 0x9009) {
			/* LDY(INC), 1001 000d dddd 1001 */
			LDY_INC (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x900a) {
			/* LDY(DEC), 1001 000d dddd 1010 */
			LDY_DEC (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xd208) == 0x8008 && ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)) != 0) {
			/* LDDY, 10q0 qq0d dddd 1qqq */
			LDDY (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x8000) {
			/* LDZ, 1000 000d dddd 0000 */
			LDZ (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9001) {
			/* LDZ(INC), 1001 000d dddd 0001 */
			LDZ_INC (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9002) {
			/* LDZ(DEC), 1001 000d dddd 0010 */
			LDZ_DEC (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xd208) == 0x8000 && ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)) != 0) {
			/* LDDZ, 10q0 qq0d dddd 0qqq */
			LDDZ (ref cpu, ref opcode);
			return true;
		}
		else if (opcode == 0x95c8) {
			/* LPM, 1001 0101 1100 1000 */
			LPM (ref cpu);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9004) {
			/* LPM(REG), 1001 000d dddd 0100 */
			LPM_REG (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9005) {
			/* LPM(INC), 1001 000d dddd 0101 */
			LPM_INC (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9406) {
			/* LSR, 1001 010d dddd 0110 */
			LSR (ref cpu, ref opcode);
			return true;
		}
		else if ((opcode & 0xfc00) == 0x2c00) {
			/* MOV, 0010 11rd dddd rrrr */
			MOV (ref cpu, ref opcode);
			return true;
		}
		else if ((opcode & 0xff00) == 0x100) {
			/* MOVW, 0000 0001 dddd rrrr */
			MOVW (ref cpu, ref opcode);
			return true;
		} 
		
		return false;
	}

	public static bool InstructionBatchFive (ref Cpu cpu, ref ushort opcode)
	{
		if ((opcode & 0xfc00) == 0x9c00) {
			/* MUL, 1001 11rd dddd rrrr */
			MUL (ref cpu, ref opcode);
			return true;
		} 
		if ((opcode & 0xff00) == 0x200) {
			/* MULS, 0000 0010 dddd rrrr */
			MULS (ref cpu, ref opcode);
			return true;
		}
		else if ((opcode & 0xff88) == 0x300) {
			/* MULSU, 0000 0011 0ddd 0rrr */
			MULSU (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9401) {
			/* NEG, 1001 010d dddd 0001 */
			NEG (ref cpu, ref opcode);
			return true;
		} 
		else if (opcode == 0) {
			/* NOP, 0000 0000 0000 0000 */
			/* NOP */
			return true;
		} 
		else if ((opcode & 0xfc00) == 0x2800) {
			/* OR, 0010 10rd dddd rrrr */
			OR (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xf000) == 0x6000) {
			/* SBR, 0110 KKKK dddd KKKK */
			SBR (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xf800) == 0xb800) {
			/* OUT, 1011 1AAr rrrr AAAA */
			OUT (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x900f) {
			/* POP, 1001 000d dddd 1111 */
			POP (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x920f) {
			/* PUSH, 1001 001d dddd 1111 */
			PUSH (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xf000) == 0xd000) {
			/* RCALL, 1101 kkkk kkkk kkkk */
			RCALL (ref cpu, ref opcode);
			return true;
		} 
		else if (opcode == 0x9508) {
			/* RET, 1001 0101 0000 1000 */
			RET (ref cpu);
			return true;
		} 
		else if (opcode == 0x9518) {
			/* RETI, 1001 0101 0001 1000 */
			RETI (ref cpu);
			return true;
		} 
		else if ((opcode & 0xf000) == 0xc000) {
			/* RJMP, 1100 kkkk kkkk kkkk */
			RJMP (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9407) {
			/* ROR, 1001 010d dddd 0111 */
			ROR (ref cpu, ref opcode);
			return true;
		}
		return false;
	}

	public static bool InstructionBatchSix (ref Cpu cpu, ref ushort opcode)
	{
		if ((opcode & 0xfc00) == 0x800) {
			/* SBC, 0000 10rd dddd rrrr */
			SBC (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xf000) == 0x4000) {
			/* SBCI, 0100 KKKK dddd KKKK */
			SBCI (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xff00) == 0x9a00) {
			/* SBI, 1001 1010 AAAA Abbb */
			SBI (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xff00) == 0x9900) {
			/* SBIC, 1001 1001 AAAA Abbb */
			SBIC (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xff00) == 0x9b00) {
			/* SBIS, 1001 1011 AAAA Abbb */
			SBIS (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xff00) == 0x9700) {
			/* SBIW, 1001 0111 KKdd KKKK */
			SBIW (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe08) == 0xfc00) {
			/* SBRC, 1111 110r rrrr 0bbb */
			SBRC (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe08) == 0xfe00) {
			/* SBRS, 1111 111r rrrr 0bbb */
			SBRS (ref cpu, ref opcode);
			return true;
		} 
		else if (opcode == 0x9588) {
			/* SLEEP, 1001 0101 1000 1000 */
			/* not implemented */
			return true;
		} 
		else if (opcode == 0x95e8) {
			/* SPM, 1001 0101 1110 1000 */
			/* not implemented */
			return true;
		} 
		else if (opcode == 0x95f8) {
			/* SPM(INC), 1001 0101 1111 1000 */
			/* not implemented */
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9200) {
			/* STS, 1001 001d dddd 0000 kkkk kkkk kkkk kkkk */
			STS (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x920c) {
			/* STX, 1001 001r rrrr 1100 */
			STX (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x920d) {
			/* STX(INC), 1001 001r rrrr 1101 */
			STX_INC (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x920e) {
			/* STX(DEC), 1001 001r rrrr 1110 */
			STX_DEC (ref cpu, ref opcode);
			return true;
		} 
		return false;
	}

	public static bool InstructionBatchSeven (ref Cpu cpu, ref ushort opcode)
	{
		if ((opcode & 0xfe0f) == 0x8208) {
			/* STY, 1000 001r rrrr 1000 */
			STY (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9209) {
			/* STY(INC), 1001 001r rrrr 1001 */
			STY_INC (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x920a) {
			/* STY(DEC), 1001 001r rrrr 1010 */
			STY_DEC (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xd208) == 0x8208 && ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)) != 0) {
			/* STDY, 10q0 qq1r rrrr 1qqq */
			STDY (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x8200) {
			/* STZ, 1000 001r rrrr 0000 */
			STZ (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9201) {
			/* STZ(INC), 1001 001r rrrr 0001 */
			STZ_INC (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9202) {
			/* STZ(DEC), 1001 001r rrrr 0010 */
			STZ_DEC (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xd208) == 0x8200 && ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)) != 0) {
			/* STDZ, 10q0 qq1r rrrr 0qqq */
			STDZ (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfc00) == 0x1800) {
			/* SUB, 0001 10rd dddd rrrr */
			SUB (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xf000) == 0x5000) {
			/* SUBI, 0101 KKKK dddd KKKK */
			SUBI (ref cpu, ref opcode);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9402) {
			/* SWAP, 1001 010d dddd 0010 */
			SWAP (ref cpu, ref opcode);
			return true;
		} 
		else if (opcode == 0x95a8) {
			/* WDR, 1001 0101 1010 1000 */
			WDR (ref cpu);
			return true;
		} 
		else if ((opcode & 0xfe0f) == 0x9204) {
			/* XCH, 1001 001r rrrr 0100 */
			XCH (ref cpu, ref opcode);
			return true;
		}
		return false;
	}

	private static void ADC (ref Cpu cpu, ref ushort opcode)
	{
		var d = cpu.Data[(opcode & 0x1f0) >> 4];
		var r = cpu.Data[(opcode & 0xf) | (opcode & 0x200) >> 5];
		var sum = d + r + (cpu.Data[95] & 1);
		var R = (byte)(sum & 255);
		cpu.Data[(opcode & 0x1f0) >> 4] = R;
		var sreg = cpu.Data[95] & 0xc0;
		sreg |= R == 0 ? 2 : 0;
		sreg |= (128 & R) != 0 ? 4 : 0;
		sreg |= ((R ^ r) & (d ^ R) & 128) != 0 ? 8 : 0;
		sreg |= (sreg >> 2 & 1 ^ sreg >> 3 & 1) != 0 ? 0x10 : 0;
		sreg |= (sum & 256) != 0 ? 1 : 0;
		sreg |= (1 & (d & r | r & ~R | ~R & d)) != 0 ? 0x20 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void ADD (ref Cpu cpu, ref ushort opcode)
	{
		var d = cpu.Data[(opcode & 0x1f0) >> 4];
		var r = cpu.Data[(opcode & 0xf) | (opcode & 0x200) >> 5];
		var R = (byte)(d + r);
		cpu.Data[(opcode & 0x1f0) >> 4] = R;
		var sreg = cpu.Data[95] & 0xc0;
		sreg |= R == 0 ? 2 : 0;
		sreg |= (128 & R) != 0 ? 4 : 0;
		sreg |= ((R ^ r) & (R ^ d) & 128) != 0 ? 8 : 0;
		sreg |= (sreg >> 2 & 1 ^ sreg >> 3 & 1) != 0 ? 0x10 : 0;
		sreg |= (d + r & 256) != 0 ? 1 : 0;
		sreg |= (1 & (d & r | r & ~R | ~R & d)) != 0 ? 0x20 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void ADIW (ref Cpu cpu, ref ushort opcode)
	{
		var addr = (ushort)(2 * ((opcode & 0x30) >> 4) + 24);
		var value = cpu.DataView.GetUint16(addr, true);
		var R = (ushort)(value + ((opcode & 0xf) | ((opcode & 0xc0) >> 2)) & 0xffff);
		cpu.DataView.SetUint16(addr, R, true);
		var sreg = cpu.Data[95] & 0xe0;
		sreg |= R == 0 ? 2 : 0;
		sreg |= (0x8000 & R) != 0 ? 4 : 0;
		sreg |= (~value & R & 0x8000) != 0 ? 8 : 0;
		sreg |= ((sreg >> 2 & 1) ^ (sreg >> 3 & 1)) != 0 ? 0x10 : 0;
		sreg |= (~R & value & 0x8000) != 0 ? 1 : 0;
		cpu.Data[95] = (byte)sreg;
		cpu.Cycles++;
	}
	
	private static void AND (ref Cpu cpu, ref ushort opcode)
	{
		var R = (byte)(cpu.Data[(opcode & 0x1f0) >> 4] & cpu.Data[(opcode & 0xf) | (opcode & 0x200) >> 5]);
		cpu.Data[(opcode & 0x1f0) >> 4] = R;
		var sreg = cpu.Data[95] & 0xe1;
		sreg |= R == 0 ? 2 : 0;	
		sreg |= (128 & R) != 0 ? 4 : 0;
		sreg |= ((sreg >> 2 & 1) ^ (sreg >> 3 & 1)) != 0 ? 0x10 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void ANDI (ref Cpu cpu, ref ushort opcode)
	{
		var R = (byte)(cpu.Data[((opcode & 0xf0) >> 4) + 16] & ((opcode & 0xf) | ((opcode & 0xf00) >> 4)));
		cpu.Data[((opcode & 0xf0) >> 4) + 16] = R;
		var sreg = cpu.Data[95] & 0xe1;
		sreg |= R == 0 ? 2 : 0;
		sreg |= (128 & R) != 0 ? 4 : 0;
		sreg |= ((sreg >> 2 & 1) ^ (sreg >> 3 & 1)) != 0 ? 0x10 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void ASR (ref Cpu cpu, ref ushort opcode)
	{
		var value = cpu.Data[(opcode & 0x1f0) >> 4];
		var R = (byte)((value >> 1) | (128 & value));
		cpu.Data[(opcode & 0x1f0) >> 4] = R;
		var sreg = cpu.Data[95] & 0xe0;
		sreg |= R == 0 ? 2 : 0;
		sreg |= (128 & R) != 0 ? 4 : 0;
		sreg |= value & 1;
		sreg |= (((sreg >> 2) & 1) ^ (sreg & 1)) != 0 ? 8 : 0;
		sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void BCLR (ref Cpu cpu, ref ushort opcode)
	{
		cpu.Data[95] &= (byte)~(1 << ((opcode & 0x70) >> 4));
	}
	
	private static void BLD (ref Cpu cpu, ref ushort opcode)
	{
		var b = opcode & 7;
		var d = (opcode & 0x1f0) >> 4;
		cpu.Data[d] = (byte)((~(1 << b) & cpu.Data[d]) | (((cpu.Data[95] >> 6) & 1) << b));
	}
	
	private static void BRBC (ref Cpu cpu, ref ushort opcode)
	{
		if ((cpu.Data[95] & (1 << (opcode & 7))) == 0) {
			cpu.PC = (ushort)(cpu.PC + (((opcode & 0x1f8) >> 3) - ((opcode & 0x200) != 0 ? 0x40 : 0)));
			cpu.Cycles++;
		}
	}
	
	private static void BRBS (ref Cpu cpu, ref ushort opcode)
	{
		if ((cpu.Data[95] & (1 << (opcode & 7))) != 0) {
			cpu.PC = (ushort)(cpu.PC + (((opcode & 0x1f8) >> 3) - ((opcode & 0x200) != 0 ? 0x40 : 0)));
			cpu.Cycles++;
		}
	}
	
	private static void BSET (ref Cpu cpu, ref ushort opcode)
	{
		cpu.Data[95] |= (byte)(1 << ((opcode & 0x70) >> 4));
	}
	
	private static void BST (ref Cpu cpu, ref ushort opcode)
	{
		var d = cpu.Data[(opcode & 0x1f0) >> 4];
		var b = opcode & 7;
		cpu.Data[95] = (byte)(((cpu.Data[95] & 0xbf) | ((d >> b) & 1)) != 0 ? 0x40 : 0);
	}
	
	private static void CALL (ref Cpu cpu, ref ushort opcode)
	{
		var k = (ushort)(cpu.ProgramMemory[(int)(cpu.PC + 1)] | ((opcode & 1) << 16) | ((opcode & 0x1f0) << 13));
		var ret = cpu.PC + 2;
		var sp = cpu.DataView.GetUint16(93, true);
		cpu.Data[sp] = (byte)(ret & 255);
		cpu.Data[sp - 1] = (byte)((ret >> 8) & 255);
		if (cpu.PC22Bits) {
			cpu.Data[sp - 2] = (byte)((ret >> 16) & 255);
		}
		cpu.DataView.SetUint16(93, (ushort)(sp - (cpu.PC22Bits ? 3 : 2)), true);
		cpu.PC = (ushort)(k - 1);
		cpu.Cycles += cpu.PC22Bits ? 4 : 3;
	}
	
	private static void CBI (ref Cpu cpu, ref ushort opcode)
	{
		var A = opcode & 0xf8;
		var b = opcode & 7;
		var R = cpu.ReadData((ushort)((A >> 3) + 32));
		var mask = (byte)(1 << b);
		cpu.WriteData((ushort)((A >> 3) + 32), (byte)(R & ~mask), mask);
	}
	
	private static void COM (ref Cpu cpu, ref ushort opcode)
	{
		var d = (opcode & 0x1f0) >> 4;
		var R = (byte)(255 - cpu.Data[d]);
		cpu.Data[d] = R;
		var sreg = (cpu.Data[95] & 0xe1) | 1;
		sreg |= R == 0 ? 2 : 0;
		sreg |= (128 & R) != 0 ? 4 : 0;
		sreg |= ((sreg >> 2) & 1 ^ (sreg >> 3) & 1) != 0 ? 0x10 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void CP (ref Cpu cpu, ref ushort opcode)
	{
		var val1 = cpu.Data[(opcode & 0x1f0) >> 4];
		var val2 = cpu.Data[(opcode & 0xf) | ((opcode & 0x200) >> 5)];
		var R = val1 - val2;
		var sreg = cpu.Data[95] & 0xc0;
		sreg |= R == 0 ? 2 : 0;
		sreg |= (128 & R) != 0 ? 4 : 0;
		sreg |= ((val1 ^ val2) & (val1 ^ R) & 128) != 0 ? 8 : 0;
		sreg |= ((sreg >> 2 & 1) ^ (sreg >> 3 & 1)) != 0 ? 0x10 : 0;
		sreg |= val2 > val1 ? 1 : 0;
		sreg |= (1 & (~val1 & val2 | val2 & R | R & ~val1)) != 0 ? 0x20 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void CPC (ref Cpu cpu, ref ushort opcode)
	{
		var arg1 = cpu.Data[(opcode & 0x1f0) >> 4];
		var arg2 = cpu.Data[(opcode & 0xf) | ((opcode & 0x200) >> 5)];
		int sreg = cpu.Data[95];
		var r = arg1 - arg2 - (sreg & 1);
		sreg = (sreg & 0xc0) | ((~r & (sreg >> 1 & 1)) != 0 ? 2 : 0) | (arg2 + (sreg & 1) > arg1 ? 1 : 0);
		sreg |= ((128 & r) != 0 ? 4 : 0);
		sreg |= ((arg1 ^ arg2) & (arg1 ^ r) & 128) != 0 ? 8 : 0;
		sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
		sreg |= (1 & ((~arg1 & arg2) | (arg2 & r) | (r & ~arg1))) != 0 ? 0x20 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void CPI (ref Cpu cpu, ref ushort opcode)
	{
		var arg1 = cpu.Data[((opcode & 0xf0) >> 4) + 16];
		var arg2 = (opcode & 0xf) | ((opcode & 0xf00) >> 4);
		var r = arg1 - arg2;
		var sreg = cpu.Data[95] & 0xc0;
		sreg |= r == 0 ? 2 : 0;
		sreg |= (128 & r) != 0 ? 4 : 0;
		sreg |= ((arg1 ^ arg2) & (arg1 ^ r) & 128) != 0 ? 8 : 0;
		sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
		sreg |= arg2 > arg1 ? 1 : 0;
		sreg |= (1 & ((~arg1 & arg2) | (arg2 & r) | (r & ~arg1))) != 0 ? 0x20 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void CPSE (ref Cpu cpu, ref ushort opcode)
	{
		if (cpu.Data[(opcode & 0x1f0) >> 4] == cpu.Data[(opcode & 0xf) | ((opcode & 0x200) >> 5)]) {
			var nextOpcode = cpu.ProgramMemory[(int)(cpu.PC + 1)];
			var skipSize = IsTwoWordInstruction(nextOpcode) ? 2 : 1;
			cpu.PC += (ushort)skipSize;
			cpu.Cycles += skipSize;
		}
	}
	
	private static void DEC (ref Cpu cpu, ref ushort opcode)
	{
		var value = cpu.Data[(opcode & 0x1f0) >> 4];
		var r = (byte)(value - 1);
		cpu.Data[(opcode & 0x1f0) >> 4] = r;
		var sreg = cpu.Data[95] & 0xe1;
		sreg |= r == 0 ? 2 : 0;
		sreg |= (128 & r) != 0 ? 4 : 0;
		sreg |= 128 == value ? 8 : 0;
		sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void EICALL (ref Cpu cpu)
	{
		var retAddr = cpu.PC + 1;
		var sp = cpu.DataView.GetUint16(93, true);
		var eind = cpu.Data[0x5c];
		cpu.Data[sp] = (byte)(retAddr & 255);
		cpu.Data[sp - 1] = (byte)((retAddr >> 8) & 255);
		cpu.Data[sp - 2] = (byte)((retAddr >> 16) & 255);
		cpu.DataView.SetUint16(93, (ushort)(sp - 3), true);
		cpu.PC = (uint)((eind << 16) | cpu.DataView.GetUint16(30, true) - 1);
		cpu.Cycles += 3;
	}
	
	private static void EIJMP (ref Cpu cpu)
	{
		var eind = cpu.Data[0x5c];
		cpu.PC = (uint)((eind << 16) | cpu.DataView.GetUint16(30, true) - 1);
		cpu.Cycles++;
	}
	
	private static void ELPM (ref Cpu cpu)
	{
		var rampz = cpu.Data[0x5b];
		cpu.Data[0] = cpu.ProgBytes[(rampz << 16) | cpu.DataView.GetUint16(30, true)];
		cpu.Cycles += 2;
	}
	
	private static void ELPM_REG (ref Cpu cpu, ref ushort opcode)
	{
		var rampz = cpu.Data[0x5b];
		cpu.Data[(opcode & 0x1f0) >> 4] = cpu.ProgBytes[rampz << 16 | cpu.DataView.GetUint16(30, true)];
		cpu.Cycles += 2;
	}
	
	private static void ELPM_INC (ref Cpu cpu, ref ushort opcode)
	{
		var rampz = cpu.Data[0x5b];
		var i = cpu.DataView.GetUint16(30, true);
		cpu.Data[(opcode & 0x1f0) >> 4] = cpu.ProgBytes[rampz << 16 | i];
		cpu.DataView.SetUint16(30, (ushort)(i + 1), true);
		if (i == 0xffff) {
			cpu.Data[0x5b] = (byte)((rampz + 1) % (cpu.ProgBytes.Length >> 16));
		}
		cpu.Cycles += 2;
	}
	
	private static void EOR (ref Cpu cpu, ref ushort opcode)
	{
		var R = (byte)(cpu.Data[(opcode & 0x1f0) >> 4] ^ cpu.Data[(opcode & 0xf) | ((opcode & 0x200) >> 5)]);
		cpu.Data[(opcode & 0x1f0) >> 4] = R;
		var sreg = cpu.Data[95] & 0xe1;
		sreg |= R == 0 ? 2 : 0;
		sreg |= (128 & R) != 0 ? 4 : 0;
		sreg |= ((sreg >> 2 & 1) ^ (sreg >> 3 & 1)) != 0 ? 0x10 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void FMUL (ref Cpu cpu, ref ushort opcode)
	{
		var v1 = cpu.Data[((opcode & 0x70) >> 4) + 16];
		var v2 = cpu.Data[(opcode & 7) + 16];
		var R = (v1 * v2) << 1;
		cpu.DataView.SetUint16(0, (ushort)R, true);
		cpu.Data[95] = (byte)(cpu.Data[95] & 0xfc | ((0xffff & R) != 0 ? 0 : 2) | ((v1 * v2 & 0x8000) != 0 ? 1 : 0));
		cpu.Cycles++;
	}
	
	private static void FMULS (ref Cpu cpu, ref ushort opcode)
	{
		var v1 = cpu.DataView.GetInt8(((opcode & 0x70) >> 4) + 16);
		var v2 = cpu.DataView.GetInt8((opcode & 7) + 16);
		var R = (v1 * v2) << 1;
		cpu.DataView.SetInt16(0, (short)R, true);
		cpu.Data[95] = (byte)(cpu.Data[95] & 0xfc | ((0xffff & R) != 0 ? 0 : 2) | ((v1 * v2 & 0x8000) != 0 ? 1 : 0));
		cpu.Cycles++;
	}
	
	private static void FMULSU (ref Cpu cpu, ref ushort opcode)
	{
		var v1 = cpu.DataView.GetInt8(((opcode & 0x70) >> 4) + 16);
		var v2 = cpu.Data[(opcode & 7) + 16];
		var R = (v1 * v2) << 1;
		cpu.DataView.SetInt16(0, (short)R, true);
		cpu.Data[95] = (byte)((((cpu.Data[95] & 0xfc) | (0xffff & R)) != 0 ? 0 : 2) | ((v1 * v2 & 0x8000) != 0 ? 1 : 0));
		cpu.Cycles++;
	}
	
	private static void ICALL (ref Cpu cpu)
	{
		var retAddr = cpu.PC + 1;
		var sp = cpu.DataView.GetUint16(93, true);
		cpu.Data[sp] = (byte)(retAddr & 255);
		cpu.Data[sp - 1] = (byte)((retAddr >> 8) & 255);
		if (cpu.PC22Bits) {
			cpu.Data[sp - 2] = (byte)((retAddr >> 16) & 255);
		}
		cpu.DataView.SetUint16(93, (ushort)(sp - (cpu.PC22Bits ? 3 : 2)), true);
		cpu.PC = (uint)(cpu.DataView.GetUint16(30, true) - 1);
		cpu.Cycles += cpu.PC22Bits ? 3 : 2;
	}
	
	private static void IJMP (ref Cpu cpu)
	{
		cpu.PC = (uint)(cpu.DataView.GetUint16(30, true) - 1);
		cpu.Cycles++;
	}
	
	private static void IN (ref Cpu cpu, ref ushort opcode)
	{
		var i = cpu.ReadData((ushort)((opcode & 0xf) | ((opcode & 0x600) >> 5) + 32));
		cpu.Data[(opcode & 0x1f0) >> 4] = i;
	}
	
	private static void INC (ref Cpu cpu, ref ushort opcode)
	{
		var d = cpu.Data[(opcode & 0x1f0) >> 4];
		var r = (d + 1) & 255;
		cpu.Data[(opcode & 0x1f0) >> 4] = (byte)r;
		var sreg = cpu.Data[95] & 0xe1;
		sreg |= r == 0 ? 2 : 0;
		sreg |= (128 & r) != 0 ? 4 : 0;
		sreg |= 127 == d ? 8 : 0;
		sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void JMP (ref Cpu cpu, ref ushort opcode)
	{
		cpu.PC = (uint)(cpu.ProgramMemory[(int)(cpu.PC + 1)] | (opcode & 1) << 16 | (opcode & 0x1f0) << 13) - 1;
		cpu.Cycles += 2;
	}
	
	private static void LAC (ref Cpu cpu, ref ushort opcode)
	{
		var r = (opcode & 0x1f0) >> 4;
		var clear = cpu.Data[r];
		var value = cpu.ReadData(cpu.DataView.GetUint16(30, true));
		cpu.WriteData(cpu.DataView.GetUint16(30, true), (byte)(value & (255 - clear)));
		cpu.Data[r] = value;
	}
	
	private static void LAS (ref Cpu cpu, ref ushort opcode)
	{
		var r = (opcode & 0x1f0) >> 4;
		var set = cpu.Data[r];
		var value = cpu.ReadData(cpu.DataView.GetUint16(30, true));
		cpu.WriteData(cpu.DataView.GetUint16(30, true), (byte)(value | set));
		cpu.Data[r] = value;
	}
	
	private static void LAT (ref Cpu cpu, ref ushort opcode)
	{
		var r = cpu.Data[(opcode & 0x1f0) >> 4];
		var R = cpu.ReadData(cpu.DataView.GetUint16(30, true));
		cpu.WriteData(cpu.DataView.GetUint16(30, true), (byte)(r ^ R));
		cpu.Data[(opcode & 0x1f0) >> 4] = R;
	}
	
	private static void LDI (ref Cpu cpu, ref ushort opcode)
	{
		cpu.Data[((opcode & 0xf0) >> 4) + 16] = (byte)(opcode & 0xf | (opcode & 0xf00) >> 4);
	}
	
	private static void LDS (ref Cpu cpu, ref ushort opcode)
	{
		cpu.Cycles++;
		var value = cpu.ReadData(cpu.ProgramMemory[(int)(cpu.PC + 1)]);
		cpu.Data[(opcode & 0x1f0) >> 4] = value;
		cpu.PC++;
	}
	
	private static void LDX (ref Cpu cpu, ref ushort opcode)
	{
		cpu.Cycles++;
		cpu.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData(cpu.DataView.GetUint16(26, true));
	}
	
	private static void LDX_INC (ref Cpu cpu, ref ushort opcode)
	{
		var x = cpu.DataView.GetUint16(26, true);
		cpu.Cycles++;
		cpu.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData(x);
		cpu.DataView.SetUint16(26, (ushort)(x + 1), true);
	}
	
	private static void LDX_DEC (ref Cpu cpu, ref ushort opcode)
	{
		var x = cpu.DataView.GetUint16(26, true) - 1;
		cpu.DataView.SetUint16(26, (ushort)x, true);
		cpu.Cycles++;
		cpu.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData((ushort)x);
	}
	
	private static void LDY (ref Cpu cpu, ref ushort opcode)
	{
		cpu.Cycles++;
		cpu.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData(cpu.DataView.GetUint16(28, true));
	}
	
	private static void LDY_INC (ref Cpu cpu, ref ushort opcode)
	{
		var y = cpu.DataView.GetUint16(28, true);
		cpu.Cycles++;
		cpu.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData(y);
		cpu.DataView.SetUint16(28, (ushort)(y + 1), true);
	}
	
	private static void LDY_DEC (ref Cpu cpu, ref ushort opcode)
	{
		var y = cpu.DataView.GetUint16(28, true) - 1;
		cpu.DataView.SetUint16(28, (ushort)y, true);
		cpu.Cycles++;
		cpu.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData((ushort)y);
	}
	
	private static void LDDY (ref Cpu cpu, ref ushort opcode)
	{
		cpu.Cycles++;
		cpu.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData(
			(ushort)(cpu.DataView.GetUint16(28, true) + ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)))
		);
	}
	
	private static void LDZ (ref Cpu cpu, ref ushort opcode)
	{
		cpu.Cycles++;
		cpu.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData(cpu.DataView.GetUint16(30, true));
	}
	
	private static void LDZ_INC (ref Cpu cpu, ref ushort opcode)
	{
		var z = cpu.DataView.GetUint16(30, true);
		cpu.Cycles++;
		cpu.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData(z);
		cpu.DataView.SetUint16(30, (ushort)(z + 1), true);
	}
	
	private static void LDZ_DEC (ref Cpu cpu, ref ushort opcode)
	{
		var z = cpu.DataView.GetUint16(30, true) - 1;
		cpu.DataView.SetUint16(30, (ushort)z, true);
		cpu.Cycles++;
		cpu.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData((ushort)z);
	}
	
	private static void LDDZ (ref Cpu cpu, ref ushort opcode)
	{
		cpu.Cycles++;
		cpu.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData(
			(ushort)(cpu.DataView.GetUint16(30, true) + ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)))
		);
	}
	
	private static void LPM (ref Cpu cpu)
	{
		cpu.Data[0] = cpu.ProgBytes[cpu.DataView.GetUint16(30, true)];
		cpu.Cycles += 2;
	}
	
	private static void LPM_REG (ref Cpu cpu, ref ushort opcode)
	{
		cpu.Data[(opcode & 0x1f0) >> 4] = cpu.ProgBytes[cpu.DataView.GetUint16(30, true)];
		cpu.Cycles += 2;
	}
	
	private static void LPM_INC (ref Cpu cpu, ref ushort opcode)
	{
		var i = cpu.DataView.GetUint16(30, true);
		cpu.Data[(opcode & 0x1f0) >> 4] = cpu.ProgBytes[i];
		cpu.DataView.SetUint16(30, (ushort)(i + 1), true);
		cpu.Cycles += 2;
	}
	
	private static void LSR (ref Cpu cpu, ref ushort opcode)
	{
		var value = cpu.Data[(opcode & 0x1f0) >> 4];
		var R = (byte)(value >> 1);
		cpu.Data[(opcode & 0x1f0) >> 4] = R;
		var sreg = cpu.Data[95] & 0xe0;
		sreg |= R == 0 ? 2 : 0;
		sreg |= value & 1;
		sreg |= ((sreg >> 2) & 1 ^ (sreg & 1)) != 0 ? 8 : 0;
		sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void MOV (ref Cpu cpu, ref ushort opcode)
	{
		cpu.Data[(opcode & 0x1f0) >> 4] = cpu.Data[(opcode & 0xf) | ((opcode & 0x200) >> 5)];
	}
	
	private static void MOVW (ref Cpu cpu, ref ushort opcode)
	{
		var r2 = 2 * (opcode & 0xf);
		var d2 = 2 * ((opcode & 0xf0) >> 4);
		cpu.Data[d2] = cpu.Data[r2];
		cpu.Data[d2 + 1] = cpu.Data[r2 + 1];
	}
	
	private static void MUL (ref Cpu cpu, ref ushort opcode)
	{
		var R = cpu.Data[(opcode & 0x1f0) >> 4] * cpu.Data[(opcode & 0xf) | ((opcode & 0x200) >> 5)];
		cpu.DataView.SetUint16(0, (ushort)R, true);
		cpu.Data[95] = (byte)(cpu.Data[95] & 0xfc | ((0xffff & R) != 0 ? 0 : 2) | ((0x8000 & R) != 0 ? 1 : 0));
		cpu.Cycles++;
	}
	
	private static void MULS (ref Cpu cpu, ref ushort opcode)
	{
		var R = cpu.DataView.GetInt8(((opcode & 0xf0) >> 4) + 16) * cpu.DataView.GetInt8((opcode & 0xf) + 16);
		cpu.DataView.SetInt16(0, (short)R, true);
		cpu.Data[95] = (byte)(cpu.Data[95] & 0xfc | ((0xffff & R) != 0 ? 0 : 2) | ((0x8000 & R) != 0 ? 1 : 0));
		cpu.Cycles++;
	}
	
	private static void MULSU (ref Cpu cpu, ref ushort opcode)
	{
		var R = cpu.DataView.GetInt8(((opcode & 0x70) >> 4) + 16) * cpu.Data[(opcode & 7) + 16];
		cpu.DataView.SetInt16(0, (short)R, true);
		cpu.Data[95] = (byte)(cpu.Data[95] & 0xfc | ((0xffff & R) != 0 ? 0 : 2) | ((0x8000 & R) != 0 ? 1 : 0));
		cpu.Cycles++;
	}
	
	private static void NEG (ref Cpu cpu, ref ushort opcode)
	{
		var d = (opcode & 0x1f0) >> 4;
		var value = cpu.Data[d];
		var R = (byte)(0 - value);
		cpu.Data[d] = R;
		var sreg = cpu.Data[95] & 0xc0;
		sreg |= R == 0 ? 2 : 0;
		sreg |= (128 & R) != 0 ? 4 : 0;
		sreg |= 128 == R ? 8 : 0;
		sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
		sreg |= R == 0 ? 0 : 1;
		sreg |= (1 & (R | value)) != 0 ? 0x20 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void OR (ref Cpu cpu, ref ushort opcode)
	{
		var R = cpu.Data[(opcode & 0x1f0) >> 4] | cpu.Data[(opcode & 0xf) | ((opcode & 0x200) >> 5)];
		cpu.Data[(opcode & 0x1f0) >> 4] = (byte)R;
		var sreg = cpu.Data[95] & 0xe1;
		sreg |= R == 0 ? 2 : 0;
		sreg |= (128 & R) != 0 ? 4 : 0;
		sreg |= ((sreg >> 2 & 1) ^ (sreg >> 3 & 1)) != 0 ? 0x10 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void SBR (ref Cpu cpu, ref ushort opcode)
	{
		var R = cpu.Data[((opcode & 0xf0) >> 4) + 16] | ((opcode & 0xf) | ((opcode & 0xf00) >> 4));
		cpu.Data[((opcode & 0xf0) >> 4) + 16] = (byte)R;
		var sreg = cpu.Data[95] & 0xe1;
		sreg |= R == 0 ? 2 : 0;
		sreg |= (128 & R) != 0 ? 4 : 0;
		sreg |= ((sreg >> 2 & 1) ^ (sreg >> 3 & 1)) != 0 ? 0x10 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void OUT (ref Cpu cpu, ref ushort opcode)
	{
		cpu.WriteData ((ushort)(((opcode & 0xf) | ((opcode & 0x600) >> 5)) + 32), cpu.Data[(opcode & 0x1f0) >> 4]);
	}
	
	private static void POP (ref Cpu cpu, ref ushort opcode)
	{
		var value = cpu.DataView.GetUint16(93, true) + 1;
		cpu.DataView.SetUint16(93, (ushort)value, true);
		cpu.Data[(opcode & 0x1f0) >> 4] = cpu.Data[value];
		cpu.Cycles++;
	}
	
	private static void PUSH (ref Cpu cpu, ref ushort opcode)
	{
		var value = cpu.DataView.GetUint16(93, true);
		cpu.Data[value] = cpu.Data[(opcode & 0x1f0) >> 4];
		cpu.DataView.SetUint16(93, (ushort)(value - 1), true);
		cpu.Cycles++;
	}
	
	private static void RCALL (ref Cpu cpu, ref ushort opcode)
	{
		var k = (opcode & 0x7ff) - ((opcode & 0x800) != 0 ? 0x800 : 0);
		var retAddr = cpu.PC + 1;
		var sp = cpu.DataView.GetUint16(93, true);
		cpu.Data[sp] = (byte)(retAddr & 255);
		cpu.Data[sp - 1] = (byte)((retAddr >> 8) & 255);
		if (cpu.PC22Bits) {
			cpu.Data[sp - 2] = (byte)((retAddr >> 16) & 255);
		}
		cpu.DataView.SetUint16(93, (ushort)(sp - (cpu.PC22Bits ? 3 : 2)), true);
		cpu.PC += (ushort)k;
		cpu.Cycles += cpu.PC22Bits ? 3 : 2;
	}
	
	private static void RET (ref Cpu cpu)
	{
		var i = cpu.DataView.GetUint16(93, true) + (cpu.PC22Bits ? 3 : 2);
		cpu.DataView.SetUint16(93, (ushort)i, true);
		cpu.PC = (uint)((cpu.Data[i - 1] << 8) + cpu.Data[i] - 1);
		if (cpu.PC22Bits) {
			cpu.PC |= (uint)(cpu.Data[i - 2] << 16);
		}
		cpu.Cycles += cpu.PC22Bits ? 4 : 3;
	}
	
	private static void RETI (ref Cpu cpu)
	{
		var i = cpu.DataView.GetUint16(93, true) + (cpu.PC22Bits ? 3 : 2);
		cpu.DataView.SetUint16(93, (ushort)i, true);
		cpu.PC = (uint)((cpu.Data[i - 1] << 8) + cpu.Data[i] - 1);
		if (cpu.PC22Bits) {
			cpu.PC |= (uint)(cpu.Data[i - 2] << 16);
		}
		cpu.Cycles += cpu.PC22Bits ? 4 : 3;
		cpu.Data[95] |= 0x80; // Enable interrupts
	}
	
	private static void RJMP (ref Cpu cpu, ref ushort opcode)
	{
		cpu.PC = (uint)(cpu.PC + ((opcode & 0x7ff) - ((opcode & 0x800) != 0 ? 0x800 : 0)));
		cpu.Cycles++;
	}
	
	private static void ROR (ref Cpu cpu, ref ushort opcode)
	{
		var d = cpu.Data[(opcode & 0x1f0) >> 4];
		var r = (byte)((d >> 1) | ((cpu.Data[95] & 1) << 7));
		cpu.Data[(opcode & 0x1f0) >> 4] = r;
		var sreg = cpu.Data[95] & 0xe0;
		sreg |= r == 0 ? 2 : 0;
		sreg |= (128 & r) != 0 ? 4 : 0;
		sreg |= d & 1;
		sreg |= ((sreg >> 2) & 1 ^ (sreg & 1)) != 0 ? 8 : 0;
		sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void SBC (ref Cpu cpu, ref ushort opcode)
	{
		var val1 = cpu.Data[(opcode & 0x1f0) >> 4];
		var val2 = cpu.Data[(opcode & 0xf) | ((opcode & 0x200) >> 5)];
		int sreg = cpu.Data[95];
		var R = (byte)(val1 - val2 - (sreg & 1));
		cpu.Data[(opcode & 0x1f0) >> 4] = R;
		sreg = ((sreg & 0xc0) | ((~R & (sreg >> 1) & 1) != 0 ? 2 : 0) | (val2 + (sreg & 1) > val1 ? 1 : 0));
		sreg |= ((128 & R) != 0 ? 4 : 0);
		sreg |= ((val1 ^ val2) & (val1 ^ R) & 128) != 0 ? 8 : 0;
		sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
		sreg |= (1 & ((~val1 & val2) | (val2 & R) | (R & ~val1))) != 0 ? 0x20 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void SBCI (ref Cpu cpu, ref ushort opcode)
	{
		var val1 = cpu.Data[((opcode & 0xf0) >> 4) + 16];
		var val2 = (opcode & 0xf) | ((opcode & 0xf00) >> 4);
		int sreg = cpu.Data[95];
		var R = (byte)(val1 - val2 - (sreg & 1));
		cpu.Data[((opcode & 0xf0) >> 4) + 16] = R;
		sreg = (sreg & 0xc0) | ((~R & (sreg >> 1) & 1) != 0 ? 2 : 0) | (val2 + (sreg & 1) > val1 ? 1 : 0);
		sreg |= ((128 & R) != 0 ? 4 : 0);
		sreg |= ((val1 ^ val2) & (val1 ^ R) & 128) != 0 ? 8 : 0;
		sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
		sreg |= (1 & ((~val1 & val2) | (val2 & R) | (R & ~val1))) != 0 ? 0x20 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void SBI (ref Cpu cpu, ref ushort opcode)
	{
		var target = ((opcode & 0xf8) >> 3) + 32;
		var mask = 1 << (opcode & 7);
		cpu.WriteData ((ushort)target, (byte)(cpu.ReadData ((ushort)target) | mask), (byte)mask);
		cpu.Cycles++;
	}
	
	private static void SBIC (ref Cpu cpu, ref ushort opcode)
	{
		var value = cpu.ReadData((ushort)(((opcode & 0xf8) >> 3) + 32));
		if ((value & (1 << (opcode & 7))) == 0) {
			var nextOpcode = cpu.ProgBytes[cpu.PC + 1];
			var skipSize = IsTwoWordInstruction(nextOpcode) ? 2 : 1;
			cpu.PC += (ushort)skipSize;
			cpu.Cycles += skipSize;
		}
	}
	
	private static void SBIS (ref Cpu cpu, ref ushort opcode)
	{
		var value = cpu.ReadData((ushort)(((opcode & 0xf8) >> 3) + 32));
		if ((value & (1 << (opcode & 7))) != 0) {
			var nextOpcode = cpu.ProgramMemory[cpu.PC + 1];
			var skipSize = IsTwoWordInstruction(nextOpcode) ? 2 : 1;
			cpu.PC += (ushort)skipSize;
			cpu.Cycles += skipSize;
		}
	}
	
	private static void SBIW (ref Cpu cpu, ref ushort opcode)
	{
		var i = 2 * ((opcode & 0x30) >> 4) + 24;
		var a = cpu.DataView.GetUint16((ushort)i, true);
		var l = (opcode & 0xf) | ((opcode & 0xc0) >> 2);
		var R = a - l;
		cpu.DataView.SetUint16((ushort)i, (ushort)R, true);
		var sreg = cpu.Data[95] & 0xc0;
		sreg |= R == 0 ? 2 : 0;
		sreg |= (0x8000 & R) != 0 ? 4 : 0;
		sreg |= (a & ~R & 0x8000) != 0 ? 8 : 0;
		sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
		sreg |= l > a ? 1 : 0;
		sreg |= (1 & ((~a & l) | (l & R) | (R & ~a))) != 0 ? 0x20 : 0;
		cpu.Data[95] = (byte)sreg;
		cpu.Cycles++;
	}
	
	private static void SBRC (ref Cpu cpu, ref ushort opcode)
	{
		if ((cpu.Data[(opcode & 0x1f0) >> 4] & (1 << (opcode & 7))) == 0) {
			var nextOpcode = cpu.ProgBytes[cpu.PC + 1];
			var skipSize = IsTwoWordInstruction(nextOpcode) ? 2 : 1;
			cpu.PC += (ushort)skipSize;
			cpu.Cycles += skipSize;
		}
	}
	
	private static void SBRS (ref Cpu cpu, ref ushort opcode)
	{
		if ((cpu.Data[(opcode & 0x1f0) >> 4] & (1 << (opcode & 7))) != 0) {
			var nextOpcode = cpu.ProgBytes[cpu.PC + 1];
			var skipSize = IsTwoWordInstruction(nextOpcode) ? 2 : 1;
			cpu.PC += (ushort)skipSize;
			cpu.Cycles += skipSize;
		}
	}
	
	private static void STS (ref Cpu cpu, ref ushort opcode)
	{
		var value = cpu.Data[(opcode & 0x1f0) >> 4];
		var addr = cpu.ProgramMemory[(int)(cpu.PC + 1)];
		cpu.WriteData(addr, value);
		cpu.PC++;
		cpu.Cycles++;
	}
	
	private static void STX (ref Cpu cpu, ref ushort opcode)
	{
		cpu.WriteData (cpu.DataView.GetUint16(26, true), cpu.Data[(opcode & 0x1f0) >> 4]);
		cpu.Cycles++;
	}
	
	private static void STX_INC (ref Cpu cpu, ref ushort opcode)
	{
		var x = cpu.DataView.GetUint16(26, true);
		cpu.WriteData(x, cpu.Data[(opcode & 0x1f0) >> 4]);
		cpu.DataView.SetUint16(26, (ushort)(x + 1), true);
		cpu.Cycles++;
	}
	
	private static void STX_DEC (ref Cpu cpu, ref ushort opcode)
	{
		var i = cpu.Data[(opcode & 0x1f0) >> 4];
		var x = cpu.DataView.GetUint16(26, true) - 1;
		cpu.DataView.SetUint16(26, (ushort)x, true);
		cpu.WriteData((ushort)x, i);
		cpu.Cycles++;
	}
	
	private static void STY (ref Cpu cpu, ref ushort opcode)
	{
		cpu.WriteData (cpu.DataView.GetUint16(28, true), cpu.Data[(opcode & 0x1f0) >> 4]);
		cpu.Cycles++;
	}
	
	private static void STY_INC (ref Cpu cpu, ref ushort opcode)
	{
		var i = cpu.Data[(opcode & 0x1f0) >> 4];
		var y = cpu.DataView.GetUint16(28, true);
		cpu.WriteData(y, i);
		cpu.DataView.SetUint16(28, (ushort)(y + 1), true);
		cpu.Cycles++;
	}
	
	private static void STY_DEC (ref Cpu cpu, ref ushort opcode)
	{
		var i = cpu.Data[(opcode & 0x1f0) >> 4];
		var y = cpu.DataView.GetUint16(28, true) - 1;
		cpu.DataView.SetUint16(28, (ushort)y, true);
		cpu.WriteData((ushort)y, i);
		cpu.Cycles++;
	}
	
	private static void STDY (ref Cpu cpu, ref ushort opcode)
	{
		cpu.WriteData (
			(ushort)(cpu.DataView.GetUint16(28, true) + ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8))),
			cpu.Data[(opcode & 0x1f0) >> 4]
		);
		cpu.Cycles++;
	}
	
	private static void STZ (ref Cpu cpu, ref ushort opcode)
	{
		cpu.WriteData (cpu.DataView.GetUint16(30, true), cpu.Data[(opcode & 0x1f0) >> 4]);
		cpu.Cycles++;
	}
	
	private static void STZ_INC (ref Cpu cpu, ref ushort opcode)
	{
		var z = cpu.DataView.GetUint16(30, true);
		cpu.WriteData(z, cpu.Data[(opcode & 0x1f0) >> 4]);
		cpu.DataView.SetUint16(30, (ushort)(z + 1), true);
		cpu.Cycles++;
	}
	
	private static void STZ_DEC (ref Cpu cpu, ref ushort opcode)
	{
		var i = cpu.Data[(opcode & 0x1f0) >> 4];
		var z = cpu.DataView.GetUint16(30, true) - 1;
		cpu.DataView.SetUint16(30, (ushort)z, true);
		cpu.WriteData((ushort)z, i);
		cpu.Cycles++;
	}
	
	private static void STDZ (ref Cpu cpu, ref ushort opcode)
	{
		cpu.WriteData (
			(ushort)(cpu.DataView.GetUint16(30, true) + ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8))),
			cpu.Data[(opcode & 0x1f0) >> 4]
		);
		cpu.Cycles++;
	}
	
	private static void SUB (ref Cpu cpu, ref ushort opcode)
	{
		var val1 = cpu.Data[(opcode & 0x1f0) >> 4];
		var val2 = cpu.Data[(opcode & 0xf) | ((opcode & 0x200) >> 5)];
		var R = (byte)(val1 - val2);
			
		cpu.Data[(opcode & 0x1f0) >> 4] = R;
		var sreg = cpu.Data[95] & 0xc0;
		sreg |= R == 0 ? 2 : 0;
		sreg |= (128 & R) != 0 ? 4 : 0;
		sreg |= ((val1 ^ val2) & (val1 ^ R) & 128) != 0 ? 8 : 0;
		sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
		sreg |= val2 > val1 ? 1 : 0;
		sreg |= (1 & ((~val1 & val2) | (val2 & R) | (R & ~val1))) != 0 ? 0x20 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void SUBI (ref Cpu cpu, ref ushort opcode)
	{
		var val1 = cpu.Data[((opcode & 0xf0) >> 4) + 16];
		var val2 = (opcode & 0xf) | ((opcode & 0xf00) >> 4);
		var R = (byte)(val1 - val2);
		cpu.Data[((opcode & 0xf0) >> 4) + 16] = R;
		var sreg = cpu.Data[95] & 0xc0;
		sreg |= R == 0 ? 2 : 0;
		sreg |= (128 & R) != 0 ? 4 : 0;
		sreg |= ((val1 ^ val2) & (val1 ^ R) & 128) != 0 ? 8 : 0;
		sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
		sreg |= val2 > val1 ? 1 : 0;
		sreg |= (1 & ((~val1 & val2) | (val2 & R) | (R & ~val1))) != 0 ? 0x20 : 0;
		cpu.Data[95] = (byte)sreg;
	}
	
	private static void SWAP (ref Cpu cpu, ref ushort opcode)
	{
		var d = (opcode & 0x1f0) >> 4;
		var i = cpu.Data[d];
		cpu.Data[d] = (byte)(((15 & i) << 4) | ((240 & i) >> 4));
	}
	
	private static void WDR (ref Cpu cpu)
	{
		cpu.OnWatchdogReset();
	}
	
	private static void XCH (ref Cpu cpu, ref ushort opcode)
	{
		var r = (opcode & 0x1f0) >> 4;
		var val1 = cpu.Data[r];
		var val2 = cpu.Data[cpu.DataView.GetUint16(30, true)];
		cpu.Data[cpu.DataView.GetUint16(30, true)] = val1;
		cpu.Data[r] = val2;
	}
	
	private static bool IsTwoWordInstruction(ushort opcode)
	{
		return 
			/* LDS */
			(opcode & 0xfe0f) == 0x9000 ||
			/* STS */
			(opcode & 0xfe0f) == 0x9200 || 
			/* CALL */
			(opcode & 0xfe0e) == 0x940e || 
			/* JMP */
			(opcode & 0xfe0e) == 0x940c;
	}
}
