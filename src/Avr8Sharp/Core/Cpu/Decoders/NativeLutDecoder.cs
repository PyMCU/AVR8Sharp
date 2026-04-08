using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AVR8Sharp.Core.Cpu.Decoders;

public unsafe struct NativeLutDecoder : IInstructionDecoder
{
    private static readonly delegate*<ref Cpu, ref ushort, void>* LookupTable;
    
    static NativeLutDecoder()
    {
        LookupTable = (delegate*<ref Cpu, ref ushort, void>*)NativeMemory.Alloc(0x10000, (nuint)sizeof(void*));
        
        var undefinedPtr = (delegate*<ref Cpu, ref ushort, void>)&Opcodes.NOP;

        new Span<nuint>(LookupTable, 0x10000).Fill((nuint)undefinedPtr);
        
        BuildTable();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Decode(Cpu cpu)
    {
        var opcode = cpu.ProgramMemory[(int)cpu.Pc];

        LookupTable[opcode](ref cpu, ref opcode);

        cpu.Pc = (cpu.Pc + 1u) % (uint)cpu.ProgramMemory.Length;
        cpu.Cycles++;
    }
    
    private static void BuildTable()
    {
        for (var i = 0; i <= 0xFFFF; i++)
        {
            var opcode = (ushort)i;
            
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
                        case 0x0100: LookupTable[i] = &Opcodes.MOVW; break;
                        case 0x0200: LookupTable[i] = &Opcodes.MULS; break;

                        case 0x0300:
                            switch (opcode & 0x0088)
                            {
                                case 0x0000: LookupTable[i] = &Opcodes.MULSU; break;
                                case 0x0008: LookupTable[i] = &Opcodes.FMUL; break;
                                case 0x0080: LookupTable[i] = &Opcodes.FMULS; break;
                                case 0x0088: LookupTable[i] = &Opcodes.FMULSU; break;
                            }

                            break;

                        case 0x0400:
                        case 0x0500:
                        case 0x0600:
                        case 0x0700:
                            LookupTable[i] = &Opcodes.CPC; break;

                        case 0x0800:
                        case 0x0900:
                        case 0x0A00:
                        case 0x0B00:
                            LookupTable[i] = &Opcodes.SBC; break;

                        case 0x0C00:
                        case 0x0D00:
                        case 0x0E00:
                        case 0x0F00:
                            LookupTable[i] = &Opcodes.ADD; break;
                    }

                    break;

                case 0x1000:
                    switch (opcode & 0x0C00)
                    {
                        case 0x0000: LookupTable[i] = &Opcodes.CPSE; break;
                        case 0x0400: LookupTable[i] = &Opcodes.CP; break;
                        case 0x0800: LookupTable[i] = &Opcodes.SUB; break;
                        case 0x0C00: LookupTable[i] = &Opcodes.ADC; break;
                    }

                    break;

                case 0x2000:
                    switch (opcode & 0x0C00)
                    {
                        case 0x0000: LookupTable[i] = &Opcodes.AND; break;
                        case 0x0400: LookupTable[i] = &Opcodes.EOR; break;
                        case 0x0800: LookupTable[i] = &Opcodes.OR; break;
                        case 0x0C00: LookupTable[i] = &Opcodes.MOV; break;
                    }

                    break;

                case 0x3000: LookupTable[i] = &Opcodes.CPI; break;
                case 0x4000: LookupTable[i] = &Opcodes.SBCI; break;
                case 0x5000: LookupTable[i] = &Opcodes.SUBI; break;
                case 0x6000: LookupTable[i] = &Opcodes.SBR; break;
                case 0x7000: LookupTable[i] = &Opcodes.ANDI; break;

                case 0x8000:
                    if ((opcode & 0xFE0F) == 0x8008)
                    {
                        LookupTable[i] = &Opcodes.LDY;
                        break;
                    }

                    if ((opcode & 0xFE0F) == 0x8000)
                    {
                        LookupTable[i] = &Opcodes.LDZ;
                        break;
                    }

                    if ((opcode & 0xFE0F) == 0x8208)
                    {
                        LookupTable[i] = &Opcodes.STY;
                        break;
                    }

                    if ((opcode & 0xFE0F) == 0x8200)
                    {
                        LookupTable[i] = &Opcodes.STZ;
                        break;
                    }

                    goto case 0xA000;

                case 0xA000:
                    if ((opcode & 0xD208) == 0x8008 &&
                        ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)) != 0)
                    {
                        LookupTable[i] = &Opcodes.LDDY;
                        break;
                    }

                    if ((opcode & 0xD208) == 0x8000 &&
                        ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)) != 0)
                    {
                        LookupTable[i] = &Opcodes.LDDZ;
                        break;
                    }

                    if ((opcode & 0xD208) == 0x8208 &&
                        ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)) != 0)
                    {
                        LookupTable[i] = &Opcodes.STDY;
                        break;
                    }

                    if ((opcode & 0xD208) == 0x8200 &&
                        ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)) != 0)
                    {
                        LookupTable[i] = &Opcodes.STDZ;
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
                            LookupTable[i] = &Opcodes.MUL; break;

                        case 0x0000:
                        case 0x0100:
                            switch (opcode & 0x000F)
                            {
                                case 0x0000: LookupTable[i] = &Opcodes.LDS; break;
                                case 0x0001: LookupTable[i] = &Opcodes.LDZ_INC; break;
                                case 0x0002: LookupTable[i] = &Opcodes.LDZ_DEC; break;
                                case 0x0004: LookupTable[i] = &Opcodes.LPM_REG; break;
                                case 0x0005: LookupTable[i] = &Opcodes.LPM_INC; break;
                                case 0x0006: LookupTable[i] = &Opcodes.ELPM_REG; break;
                                case 0x0007: LookupTable[i] = &Opcodes.ELPM_INC; break;
                                case 0x0009: LookupTable[i] = &Opcodes.LDY_INC; break;
                                case 0x000A: LookupTable[i] = &Opcodes.LDY_DEC; break;
                                case 0x000C: LookupTable[i] = &Opcodes.LDX; break;
                                case 0x000D: LookupTable[i] = &Opcodes.LDX_INC; break;
                                case 0x000E: LookupTable[i] = &Opcodes.LDX_DEC; break;
                                case 0x000F: LookupTable[i] = &Opcodes.POP; break;
                            }

                            break;

                        case 0x0200:
                        case 0x0300:
                            switch (opcode & 0x000F)
                            {
                                case 0x0000: LookupTable[i] = &Opcodes.STS; break;
                                case 0x0001: LookupTable[i] = &Opcodes.STZ_INC; break;
                                case 0x0002: LookupTable[i] = &Opcodes.STZ_DEC; break;
                                case 0x0004: LookupTable[i] = &Opcodes.XCH; break;
                                case 0x0005: LookupTable[i] = &Opcodes.LAS; break;
                                case 0x0006: LookupTable[i] = &Opcodes.LAC; break;
                                case 0x0007: LookupTable[i] = &Opcodes.LAT; break;
                                case 0x0009: LookupTable[i] = &Opcodes.STY_INC; break;
                                case 0x000A: LookupTable[i] = &Opcodes.STY_DEC; break;
                                case 0x000C: LookupTable[i] = &Opcodes.STX; break;
                                case 0x000D: LookupTable[i] = &Opcodes.STX_INC; break;
                                case 0x000E: LookupTable[i] = &Opcodes.STX_DEC; break;
                                case 0x000F: LookupTable[i] = &Opcodes.PUSH; break;
                            }

                            break;

                        case 0x0400:
                        case 0x0500:
                            if ((opcode & 0xFE0E) == 0x940E)
                            {
                                LookupTable[i] = &Opcodes.CALL;
                                break;
                            }

                            if ((opcode & 0xFE0E) == 0x940C)
                            {
                                LookupTable[i] = &Opcodes.JMP;
                                break;
                            }

                            if ((opcode & 0xFE0F) == 0x9400)
                            {
                                LookupTable[i] = &Opcodes.COM;
                                break;
                            }

                            if ((opcode & 0xFE0F) == 0x9401)
                            {
                                LookupTable[i] = &Opcodes.NEG;
                                break;
                            }

                            if ((opcode & 0xFE0F) == 0x9402)
                            {
                                LookupTable[i] = &Opcodes.SWAP;
                                break;
                            }

                            if ((opcode & 0xFE0F) == 0x9403)
                            {
                                LookupTable[i] = &Opcodes.INC;
                                break;
                            }

                            if ((opcode & 0xFE0F) == 0x9405)
                            {
                                LookupTable[i] = &Opcodes.ASR;
                                break;
                            }

                            if ((opcode & 0xFE0F) == 0x9406)
                            {
                                LookupTable[i] = &Opcodes.LSR;
                                break;
                            }

                            if ((opcode & 0xFE0F) == 0x9407)
                            {
                                LookupTable[i] = &Opcodes.ROR;
                                break;
                            }

                            if ((opcode & 0xFE0F) == 0x940A)
                            {
                                LookupTable[i] = &Opcodes.DEC;
                                break;
                            }

                            if ((opcode & 0xFF8F) == 0x9488)
                            {
                                LookupTable[i] = &Opcodes.BCLR;
                                break;
                            }

                            if ((opcode & 0xFF8F) == 0x9408)
                            {
                                LookupTable[i] = &Opcodes.BSET;
                                break;
                            }

                            if (opcode == 0x9519)
                            {
                                LookupTable[i] = &Opcodes.EICALL;
                                break;
                            }

                            if (opcode == 0x9419)
                            {
                                LookupTable[i] = &Opcodes.EIJMP;
                                break;
                            }

                            if (opcode == 0x95D8)
                            { 
                                LookupTable[i] = &Opcodes.ELPM;
                                break;
                            }

                            if (opcode == 0x9509)
                            {
                                LookupTable[i] = &Opcodes.ICALL;
                                break;
                            }

                            if (opcode == 0x9409)
                            {
                                LookupTable[i] = &Opcodes.IJMP;
                                break;
                            }

                            if (opcode == 0x95C8)
                            {
                                LookupTable[i] = &Opcodes.LPM;
                                break;
                            }

                            if (opcode == 0x9508)
                            {
                                LookupTable[i] = &Opcodes.RET;
                                break;
                            }

                            if (opcode == 0x9518)
                            {
                                LookupTable[i] = &Opcodes.RETI;
                                break;
                            }

                            if (opcode == 0x9588)
                            {
                                /* SLEEP — invoke OnSleep callback with SM2:SM1:SM0 bits from SMCR */
                                LookupTable[i] = &Opcodes.SLEEP;
                                break;
                            }

                            if (opcode == 0x9598)
                            {
                                /* BREAK - hardware breakpoint */
                                LookupTable[i] = &Opcodes.BREAK;
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
                                LookupTable[i] = &Opcodes.WDR;
                                break;
                            }

                            break;

                        case 0x0600:
                        case 0x0700:
                            if ((opcode & 0xFF00) == 0x9600)
                            {
                                LookupTable[i] = &Opcodes.ADIW;
                                break;
                            }

                            if ((opcode & 0xFF00) == 0x9700)
                            {
                                LookupTable[i] = &Opcodes.SBIW;
                                break;
                            }

                            break;
                        case 0x0800:
                        case 0x0900:
                            if ((opcode & 0xFF00) == 0x9800)
                            {
                                LookupTable[i] = &Opcodes.CBI;
                                break;
                            }

                            if ((opcode & 0xFF00) == 0x9900)
                            {
                                LookupTable[i] = &Opcodes.SBIC;
                                break;
                            }

                            break;

                        case 0x0A00:
                        case 0x0B00:
                            if ((opcode & 0xFF00) == 0x9A00)
                            {
                                LookupTable[i] = &Opcodes.SBI;
                                break;
                            }

                            if ((opcode & 0xFF00) == 0x9B00)
                            {
                                LookupTable[i] = &Opcodes.SBIS;
                                break;
                            }

                            break;
                    }

                    break;

                case 0xB000:
                    switch (opcode & 0x0800)
                    {
                        case 0x0000: LookupTable[i] = &Opcodes.IN; break;
                        case 0x0800: LookupTable[i] = &Opcodes.OUT; break;
                    }

                    break;

                case 0xC000: LookupTable[i] = &Opcodes.RJMP; break;
                case 0xD000: LookupTable[i] = &Opcodes.RCALL; break;
                case 0xE000: LookupTable[i] = &Opcodes.LDI; break;

                case 0xF000:
                    switch (opcode & 0x0C00)
                    {
                        case 0x0000: LookupTable[i] = &Opcodes.BRBS; break;
                        case 0x0400: LookupTable[i] = &Opcodes.BRBC; break;
                        case 0x0800:
                        case 0x0C00:
                            if ((opcode & 0xFE08) == 0xF800)
                            {
                                LookupTable[i] = &Opcodes.BLD;
                                break;
                            }

                            if ((opcode & 0xFE08) == 0xFA00)
                            {
                                LookupTable[i] = &Opcodes.BST;
                                break;
                            }

                            if ((opcode & 0xFE08) == 0xFC00)
                            {
                                LookupTable[i] = &Opcodes.SBRC;
                                break;
                            }

                            if ((opcode & 0xFE08) == 0xFE00)
                            {
                                LookupTable[i] = &Opcodes.SBRS;
                                break;
                            }

                            break;
                    }

                    break;
            }
        }
    }
}