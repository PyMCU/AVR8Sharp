using System.Runtime.CompilerServices;

namespace AVR8Sharp.Core.Decoders;

public struct SwitchDecoder : IInstructionDecoder
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Decode(Cpu cpu)
    {
        var opcode = cpu.ProgramMemory[(int)cpu.Pc];

        switch (opcode & 0xF000)
        {
            case 0x0000:
                if (opcode == 0x0000)
                {
                    /* NOP */
                    break;
                }

                switch (opcode & 0x0F00)
                {
                    case 0x0100: Opcodes.MOVW(ref cpu, ref opcode); break;
                    case 0x0200: Opcodes.MULS(ref cpu, ref opcode); break;

                    case 0x0300:
                        switch (opcode & 0x0088)
                        {
                            case 0x0000: Opcodes.MULSU(ref cpu, ref opcode); break;
                            case 0x0008: Opcodes.FMUL(ref cpu, ref opcode); break;
                            case 0x0080: Opcodes.FMULS(ref cpu, ref opcode); break;
                            case 0x0088: Opcodes.FMULSU(ref cpu, ref opcode); break;
                        }

                        break;

                    case 0x0400:
                    case 0x0500:
                    case 0x0600:
                    case 0x0700:
                        Opcodes.CPC(ref cpu, ref opcode); break;

                    case 0x0800:
                    case 0x0900:
                    case 0x0A00:
                    case 0x0B00:
                        Opcodes.SBC(ref cpu, ref opcode); break;

                    case 0x0C00:
                    case 0x0D00:
                    case 0x0E00:
                    case 0x0F00:
                        Opcodes.ADD(ref cpu, ref opcode); break;
                }

                break;

            case 0x1000:
                switch (opcode & 0x0C00)
                {
                    case 0x0000: Opcodes.CPSE(ref cpu, ref opcode); break;
                    case 0x0400: Opcodes.CP(ref cpu, ref opcode); break;
                    case 0x0800: Opcodes.SUB(ref cpu, ref opcode); break;
                    case 0x0C00: Opcodes.ADC(ref cpu, ref opcode); break;
                }

                break;

            case 0x2000:
                switch (opcode & 0x0C00)
                {
                    case 0x0000: Opcodes.AND(ref cpu, ref opcode); break;
                    case 0x0400: Opcodes.EOR(ref cpu, ref opcode); break;
                    case 0x0800: Opcodes.OR(ref cpu, ref opcode); break;
                    case 0x0C00: Opcodes.MOV(ref cpu, ref opcode); break;
                }

                break;

            case 0x3000: Opcodes.CPI(ref cpu, ref opcode); break;
            case 0x4000: Opcodes.SBCI(ref cpu, ref opcode); break;
            case 0x5000: Opcodes.SUBI(ref cpu, ref opcode); break;
            case 0x6000: Opcodes.SBR(ref cpu, ref opcode); break;
            case 0x7000: Opcodes.ANDI(ref cpu, ref opcode); break;

            case 0x8000:
                if ((opcode & 0xFE0F) == 0x8008)
                {
                    Opcodes.LDY(ref cpu, ref opcode);
                    break;
                }

                if ((opcode & 0xFE0F) == 0x8000)
                {
                    Opcodes.LDZ(ref cpu, ref opcode);
                    break;
                }

                if ((opcode & 0xFE0F) == 0x8208)
                {
                    Opcodes.STY(ref cpu, ref opcode);
                    break;
                }

                if ((opcode & 0xFE0F) == 0x8200)
                {
                    Opcodes.STZ(ref cpu, ref opcode);
                    break;
                }

                goto case 0xA000;

            case 0xA000:
                if ((opcode & 0xD208) == 0x8008 &&
                    ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)) != 0)
                {
                    Opcodes.LDDY(ref cpu, ref opcode);
                    break;
                }

                if ((opcode & 0xD208) == 0x8000 &&
                    ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)) != 0)
                {
                    Opcodes.LDDZ(ref cpu, ref opcode);
                    break;
                }

                if ((opcode & 0xD208) == 0x8208 &&
                    ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)) != 0)
                {
                    Opcodes.STDY(ref cpu, ref opcode);
                    break;
                }

                if ((opcode & 0xD208) == 0x8200 &&
                    ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)) != 0)
                {
                    Opcodes.STDZ(ref cpu, ref opcode);
                    break;
                }

                break;

            case 0x9000:
                switch (opcode & 0x0F00)
                {
                    case 0x0C00:
                    case 0x0D00:
                    case 0x0E00:
                    case 0x0F00:
                        Opcodes.MUL(ref cpu, ref opcode); break;

                    case 0x0000:
                    case 0x0100:
                        switch (opcode & 0x000F)
                        {
                            case 0x0000: Opcodes.LDS(ref cpu, ref opcode); break;
                            case 0x0001: Opcodes.LDZ_INC(ref cpu, ref opcode); break;
                            case 0x0002: Opcodes.LDZ_DEC(ref cpu, ref opcode); break;
                            case 0x0004: Opcodes.LPM_REG(ref cpu, ref opcode); break;
                            case 0x0005: Opcodes.LPM_INC(ref cpu, ref opcode); break;
                            case 0x0006: Opcodes.ELPM_REG(ref cpu, ref opcode); break;
                            case 0x0007: Opcodes.ELPM_INC(ref cpu, ref opcode); break;
                            case 0x0009: Opcodes.LDY_INC(ref cpu, ref opcode); break;
                            case 0x000A: Opcodes.LDY_DEC(ref cpu, ref opcode); break;
                            case 0x000C: Opcodes.LDX(ref cpu, ref opcode); break;
                            case 0x000D: Opcodes.LDX_INC(ref cpu, ref opcode); break;
                            case 0x000E: Opcodes.LDX_DEC(ref cpu, ref opcode); break;
                            case 0x000F: Opcodes.POP(ref cpu, ref opcode); break;
                        }

                        break;

                    case 0x0200:
                    case 0x0300:
                        switch (opcode & 0x000F)
                        {
                            case 0x0000: Opcodes.STS(ref cpu, ref opcode); break;
                            case 0x0001: Opcodes.STZ_INC(ref cpu, ref opcode); break;
                            case 0x0002: Opcodes.STZ_DEC(ref cpu, ref opcode); break;
                            case 0x0004: Opcodes.XCH(ref cpu, ref opcode); break;
                            case 0x0005: Opcodes.LAS(ref cpu, ref opcode); break;
                            case 0x0006: Opcodes.LAC(ref cpu, ref opcode); break;
                            case 0x0007: Opcodes.LAT(ref cpu, ref opcode); break;
                            case 0x0009: Opcodes.STY_INC(ref cpu, ref opcode); break;
                            case 0x000A: Opcodes.STY_DEC(ref cpu, ref opcode); break;
                            case 0x000C: Opcodes.STX(ref cpu, ref opcode); break;
                            case 0x000D: Opcodes.STX_INC(ref cpu, ref opcode); break;
                            case 0x000E: Opcodes.STX_DEC(ref cpu, ref opcode); break;
                            case 0x000F: Opcodes.PUSH(ref cpu, ref opcode); break;
                        }

                        break;

                    case 0x0400:
                    case 0x0500:
                        if ((opcode & 0xFE0E) == 0x940E)
                        {
                            Opcodes.CALL(ref cpu, ref opcode);
                            break;
                        }

                        if ((opcode & 0xFE0E) == 0x940C)
                        {
                            Opcodes.JMP(ref cpu, ref opcode);
                            break;
                        }

                        if ((opcode & 0xFE0F) == 0x9400)
                        {
                            Opcodes.COM(ref cpu, ref opcode);
                            break;
                        }

                        if ((opcode & 0xFE0F) == 0x9401)
                        {
                            Opcodes.NEG(ref cpu, ref opcode);
                            break;
                        }

                        if ((opcode & 0xFE0F) == 0x9402)
                        {
                            Opcodes.SWAP(ref cpu, ref opcode);
                            break;
                        }

                        if ((opcode & 0xFE0F) == 0x9403)
                        {
                            Opcodes.INC(ref cpu, ref opcode);
                            break;
                        }

                        if ((opcode & 0xFE0F) == 0x9405)
                        {
                            Opcodes.ASR(ref cpu, ref opcode);
                            break;
                        }

                        if ((opcode & 0xFE0F) == 0x9406)
                        {
                            Opcodes.LSR(ref cpu, ref opcode);
                            break;
                        }

                        if ((opcode & 0xFE0F) == 0x9407)
                        {
                            Opcodes.ROR(ref cpu, ref opcode);
                            break;
                        }

                        if ((opcode & 0xFE0F) == 0x940A)
                        {
                            Opcodes.DEC(ref cpu, ref opcode);
                            break;
                        }

                        if ((opcode & 0xFF8F) == 0x9488)
                        {
                            Opcodes.BCLR(ref cpu, ref opcode);
                            break;
                        }

                        if ((opcode & 0xFF8F) == 0x9408)
                        {
                            Opcodes.BSET(ref cpu, ref opcode);
                            break;
                        }

                        if (opcode == 0x9519)
                        {
                            Opcodes.EICALL(ref cpu, ref opcode);
                            break;
                        }

                        if (opcode == 0x9419)
                        {
                            Opcodes.EIJMP(ref cpu, ref opcode);
                            break;
                        }

                        if (opcode == 0x95D8)
                        {
                            Opcodes.ELPM(ref cpu, ref opcode);
                            break;
                        }

                        if (opcode == 0x9509)
                        {
                            Opcodes.ICALL(ref cpu, ref opcode);
                            break;
                        }

                        if (opcode == 0x9409)
                        {
                            Opcodes.IJMP(ref cpu, ref opcode);
                            break;
                        }

                        if (opcode == 0x95C8)
                        {
                            Opcodes.LPM(ref cpu, ref opcode);
                            break;
                        }

                        if (opcode == 0x9508)
                        {
                            Opcodes.RET(ref cpu, ref opcode);
                            break;
                        }

                        if (opcode == 0x9518)
                        {
                            Opcodes.RETI(ref cpu, ref opcode);
                            break;
                        }

                        if (opcode == 0x9588)
                        {
                            /* SLEEP — invoke OnSleep callback with SM2:SM1:SM0 bits from SMCR */
                            AvrInterrupt.OnSleep?.Invoke((byte)((cpu.Mmio.Data[0x53] >> 1) & 0x07));
                            break;
                        }

                        if (opcode == 0x9598)
                        {
                            /* BREAK - hardware breakpoint */
                            AvrInterrupt.OnBreakpoint?.Invoke(cpu.Pc);
                            break;
                        }

                        if (opcode == 0x95E8)
                        {
                            /* SPM not implemented */
                            break;
                        }

                        if (opcode == 0x95F8)
                        {
                            /* SPM_INC not implemented */
                            break;
                        }

                        if (opcode == 0x95A8)
                        {
                            Opcodes.WDR(ref cpu, ref opcode);
                            break;
                        }

                        break;

                    case 0x0600:
                    case 0x0700:
                        if ((opcode & 0xFF00) == 0x9600)
                        {
                            Opcodes.ADIW(ref cpu, ref opcode);
                            break;
                        }

                        if ((opcode & 0xFF00) == 0x9700)
                        {
                            Opcodes.SBIW(ref cpu, ref opcode);
                            break;
                        }
                        break;
                    case 0x0800:
                    case 0x0900:
                        if ((opcode & 0xFF00) == 0x9800)
                        {
                            Opcodes.CBI(ref cpu, ref opcode);
                            break;
                        }

                        if ((opcode & 0xFF00) == 0x9900)
                        {
                            Opcodes.SBIC(ref cpu, ref opcode);
                            break;
                        }

                        break;

                    case 0x0A00:
                    case 0x0B00:
                        if ((opcode & 0xFF00) == 0x9A00)
                        {
                            Opcodes.SBI(ref cpu, ref opcode);
                            break;
                        }

                        if ((opcode & 0xFF00) == 0x9B00)
                        {
                            Opcodes.SBIS(ref cpu, ref opcode);
                            break;
                        }

                        break;
                }

                break;

            case 0xB000:
                switch (opcode & 0x0800)
                {
                    case 0x0000: Opcodes.IN(ref cpu, ref opcode); break;
                    case 0x0800: Opcodes.OUT(ref cpu, ref opcode); break;
                }

                break;

            case 0xC000: Opcodes.RJMP(ref cpu, ref opcode); break;
            case 0xD000: Opcodes.RCALL(ref cpu, ref opcode); break;
            case 0xE000: Opcodes.LDI(ref cpu, ref opcode); break;

            case 0xF000:
                switch (opcode & 0x0C00)
                {
                    case 0x0000: Opcodes.BRBS(ref cpu, ref opcode); break;
                    case 0x0400: Opcodes.BRBC(ref cpu, ref opcode); break;
                    case 0x0800:
                    case 0x0C00:
                        if ((opcode & 0xFE08) == 0xF800)
                        {
                            Opcodes.BLD(ref cpu, ref opcode);
                            break;
                        }

                        if ((opcode & 0xFE08) == 0xFA00)
                        {
                            Opcodes.BST(ref cpu, ref opcode);
                            break;
                        }

                        if ((opcode & 0xFE08) == 0xFC00)
                        {
                            Opcodes.SBRC(ref cpu, ref opcode);
                            break;
                        }

                        if ((opcode & 0xFE08) == 0xFE00)
                        {
                            Opcodes.SBRS(ref cpu, ref opcode);
                            break;
                        }

                        break;
                }

                break;
        }

        // Equivalent to (Pc + 1) % Length, but the division only runs on the rare
        // wrap path (end of flash, or a jump/branch that left Pc out of range via
        // uint under/overflow). See NativeLutDecoder for the full rationale.
        var len = (uint)cpu.ProgramMemory.Length;
        var pc = cpu.Pc + 1u;
        if (pc >= len) pc %= len;
        cpu.Pc = pc;
        cpu.Cycles++;
    }
}