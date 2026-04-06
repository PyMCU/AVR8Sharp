using System.Runtime.CompilerServices;

namespace AVR8Sharp.Core.Cpu.Decoders;

public delegate void OpcodeAction(ref Cpu cpu, ref ushort opcode);

public struct LutDecoder : IInstructionDecoder
{
    private static readonly Lazy<OpcodeAction[]> Table = new(BuildTable);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Decode(Cpu cpu)
    {
        var opcode = cpu.ProgramMemory[(int)cpu.Pc];

        Table.Value[opcode](ref cpu, ref opcode);

        cpu.Pc = (cpu.Pc + 1u) % (uint)cpu.ProgramMemory.Length;
        cpu.Cycles++;
    }

    private static OpcodeAction[] BuildTable()
    {
        var table = new OpcodeAction[0x10000];

        for (var i = 0; i < 0x10000; i++)
        {
            table[i] = Opcodes.NOP;
        }

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

                    // Sub-switch basado en los bits 8-11
                    switch (opcode & 0x0F00)
                    {
                        case 0x0100: table[i] = Opcodes.MOVW; break;
                        case 0x0200: table[i] = Opcodes.MULS; break;

                        case 0x0300: // Máscara 0xFF88 (analizamos bit 3 y 7)
                            switch (opcode & 0x0088)
                            {
                                case 0x0000: table[i] = Opcodes.MULSU; break;
                                case 0x0008: table[i] = Opcodes.FMUL; break;
                                case 0x0080: table[i] = Opcodes.FMULS; break;
                                case 0x0088: table[i] = Opcodes.FMULSU; break;
                            }

                            break;

                        // Agrupamos casos para simular la máscara 0xFC00 de forma nativa
                        case 0x0400:
                        case 0x0500:
                        case 0x0600:
                        case 0x0700:
                            table[i] = Opcodes.CPC; break;

                        case 0x0800:
                        case 0x0900:
                        case 0x0A00:
                        case 0x0B00:
                            table[i] = Opcodes.SBC; break;

                        case 0x0C00:
                        case 0x0D00:
                        case 0x0E00:
                        case 0x0F00:
                            table[i] = Opcodes.ADD; break;
                    }

                    break;

                case 0x1000:
                    // Sub-switch sobre los bits 10 y 11 (máscara 0x0C00 interna)
                    switch (opcode & 0x0C00)
                    {
                        case 0x0000: table[i] = Opcodes.CPSE; break; // 1000 a 13FF
                        case 0x0400: table[i] = Opcodes.CP; break; // 1400 a 17FF
                        case 0x0800: table[i] = Opcodes.SUB; break; // 1800 a 1BFF
                        case 0x0C00: table[i] = Opcodes.ADC; break; // 1C00 a 1FFF
                    }

                    break;

                case 0x2000:
                    switch (opcode & 0x0C00)
                    {
                        case 0x0000: table[i] = Opcodes.AND; break;
                        case 0x0400: table[i] = Opcodes.EOR; break;
                        case 0x0800: table[i] = Opcodes.OR; break;
                        case 0x0C00: table[i] = Opcodes.MOV; break;
                    }

                    break;

                case 0x3000: table[i] = Opcodes.CPI; break;
                case 0x4000: table[i] = Opcodes.SBCI; break;
                case 0x5000: table[i] = Opcodes.SUBI; break;
                case 0x6000: table[i] = Opcodes.SBR; break;
                case 0x7000: table[i] = Opcodes.ANDI; break;

                case 0x8000:
                    // Para instrucciones muy granulares o esparcidas, los 'if' siguen siendo 
                    // la mejor opción para no hacer un switch gigante e ilegible.
                    if ((opcode & 0xFE0F) == 0x8008)
                    {
                        table[i] = Opcodes.LDY;
                        break;
                    }

                    if ((opcode & 0xFE0F) == 0x8000)
                    {
                        table[i] = Opcodes.LDZ;
                        break;
                    }

                    if ((opcode & 0xFE0F) == 0x8208)
                    {
                        table[i] =  Opcodes.STY;
                        break;
                    }

                    if ((opcode & 0xFE0F) == 0x8200)
                    {
                        table[i] = Opcodes.STZ;
                        break;
                    }

                    goto case 0xA000;

                case 0xA000:
                    if ((opcode & 0xD208) == 0x8008 &&
                        ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)) != 0)
                    {
                        table[i] = Opcodes.LDDY;
                        break;
                    }

                    if ((opcode & 0xD208) == 0x8000 &&
                        ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)) != 0)
                    {
                        table[i] = Opcodes.LDDZ;
                        break;
                    }

                    if ((opcode & 0xD208) == 0x8208 &&
                        ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)) != 0)
                    {
                        table[i] = Opcodes.STDY;
                        break;
                    }

                    if ((opcode & 0xD208) == 0x8200 &&
                        ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)) != 0)
                    {
                        table[i] =  Opcodes.STDZ;
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
                            table[i] =  Opcodes.MUL; break;

                        case 0x0000:
                        case 0x0100:
                            switch (opcode & 0x000F)
                            {
                                case 0x0000: table[i] = Opcodes.LDS; break;
                                case 0x0001: table[i] = Opcodes.LDZ_INC; break;
                                case 0x0002: table[i] = Opcodes.LDZ_DEC; break;
                                case 0x0004: table[i] = Opcodes.LPM_REG; break;
                                case 0x0005: table[i] = Opcodes.LPM_INC; break;
                                case 0x0006: table[i] = Opcodes.ELPM_REG; break;
                                case 0x0007: table[i] = Opcodes.ELPM_INC; break;
                                case 0x0009: table[i] = Opcodes.LDY_INC; break;
                                case 0x000A: table[i] = Opcodes.LDY_DEC; break;
                                case 0x000C: table[i] = Opcodes.LDX; break;
                                case 0x000D: table[i] = Opcodes.LDX_INC; break;
                                case 0x000E: table[i] = Opcodes.LDX_DEC; break;
                                case 0x000F: table[i] = Opcodes.POP; break;
                            }

                            break;

                        case 0x0200:
                        case 0x0300:
                            switch (opcode & 0x000F)
                            {
                                case 0x0000: table[i] = Opcodes.STS; break;
                                case 0x0001: table[i] = Opcodes.STZ_INC; break;
                                case 0x0002: table[i] = Opcodes.STZ_DEC; break;
                                case 0x0004: table[i] = Opcodes.XCH; break;
                                case 0x0005: table[i] = Opcodes.LAS; break;
                                case 0x0006: table[i] = Opcodes.LAC; break;
                                case 0x0007: table[i] = Opcodes.LAT; break;
                                case 0x0009: table[i] = Opcodes.STY_INC; break;
                                case 0x000A: table[i] = Opcodes.STY_DEC; break;
                                case 0x000C: table[i] = Opcodes.STX; break;
                                case 0x000D: table[i] = Opcodes.STX_INC; break;
                                case 0x000E: table[i] = Opcodes.STX_DEC; break;
                                case 0x000F: table[i] = Opcodes.PUSH; break;
                            }

                            break;

                        case 0x0400:
                        case 0x0500:
                            if ((opcode & 0xFE0E) == 0x940E)
                            {
                                table[i] = Opcodes.CALL;
                                break;
                            }

                            if ((opcode & 0xFE0E) == 0x940C)
                            {
                                table[i] = Opcodes.JMP;
                                break;
                            }

                            if ((opcode & 0xFE0F) == 0x9400)
                            {
                                table[i] = Opcodes.COM;
                                break;
                            }

                            if ((opcode & 0xFE0F) == 0x9401)
                            {
                                table[i] = Opcodes.NEG;
                                break;
                            }

                            if ((opcode & 0xFE0F) == 0x9402)
                            {
                                table[i] = Opcodes.SWAP;
                                break;
                            }

                            if ((opcode & 0xFE0F) == 0x9403)
                            {
                                table[i] =  Opcodes.INC;
                                break;
                            }

                            if ((opcode & 0xFE0F) == 0x9405)
                            {
                                table[i] =  Opcodes.ASR;
                                break;
                            }

                            if ((opcode & 0xFE0F) == 0x9406)
                            {
                                table[i] = Opcodes.LSR;
                                break;
                            }

                            if ((opcode & 0xFE0F) == 0x9407)
                            {
                                table[i] = Opcodes.ROR;
                                break;
                            }

                            if ((opcode & 0xFE0F) == 0x940A)
                            {
                                table[i] = Opcodes.DEC;
                                break;
                            }

                            if ((opcode & 0xFF8F) == 0x9488)
                            {
                                table[i] = Opcodes.BCLR;
                                break;
                            }

                            if ((opcode & 0xFF8F) == 0x9408)
                            {
                                table[i] = Opcodes.BSET;
                                break;
                            }

                            if (opcode == 0x9519)
                            {
                                table[i] = Opcodes.EICALL;
                                break;
                            }

                            if (opcode == 0x9419)
                            {
                                table[i] = Opcodes.EIJMP;
                                break;
                            }

                            if (opcode == 0x95D8)
                            { 
                                table[i] = Opcodes.ELPM;
                                break;
                            }

                            if (opcode == 0x9509)
                            {
                                table[i] = Opcodes.ICALL;
                                break;
                            }

                            if (opcode == 0x9409)
                            {
                                table[i] = Opcodes.IJMP;
                                break;
                            }

                            if (opcode == 0x95C8)
                            {
                                table[i] = Opcodes.LPM;
                                break;
                            }

                            if (opcode == 0x9508)
                            {
                                table[i] = Opcodes.RET;
                                break;
                            }

                            if (opcode == 0x9518)
                            {
                                table[i] = Opcodes.RETI;
                                break;
                            }

                            if (opcode == 0x9588)
                            {
                                /* SLEEP not implemented */
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
                                table[i] = Opcodes.WDR;
                                break;
                            }

                            break;

                        case 0x0600:
                        case 0x0700:
                            if ((opcode & 0xFF00) == 0x9600)
                            {
                                table[i] = Opcodes.ADIW;
                                break;
                            }

                            if ((opcode & 0xFF00) == 0x9700)
                            {
                                table[i] = Opcodes.SBIW;
                                break;
                            }

                            break;
                        case 0x0800:
                        case 0x0900:
                            if ((opcode & 0xFF00) == 0x9800)
                            {
                                table[i] = Opcodes.CBI;
                                break;
                            }

                            if ((opcode & 0xFF00) == 0x9900)
                            {
                                table[i] = Opcodes.SBIC;
                                break;
                            }

                            break;

                        case 0x0A00:
                        case 0x0B00:
                            if ((opcode & 0xFF00) == 0x9A00)
                            {
                                table[i] = Opcodes.SBI;
                                break;
                            }

                            if ((opcode & 0xFF00) == 0x9B00)
                            {
                                table[i] = Opcodes.SBIS;
                                break;
                            }

                            break;
                    }

                    break;

                case 0xB000:
                    // IN (B000-B7FF) y OUT (B800-BFFF)
                    switch (opcode & 0x0800)
                    {
                        case 0x0000: table[i] = Opcodes.IN; break;
                        case 0x0800: table[i] = Opcodes.OUT; break;
                    }

                    break;

                case 0xC000: table[i] = Opcodes.RJMP; break;
                case 0xD000: table[i] = Opcodes.RCALL; break;
                case 0xE000: table[i] = Opcodes.LDI; break;

                case 0xF000:
                    // Sub-switch para BRBS y BRBC
                    switch (opcode & 0x0C00)
                    {
                        case 0x0000: table[i] = Opcodes.BRBS; break;
                        case 0x0400: table[i] = Opcodes.BRBC; break;
                        case 0x0800:
                        case 0x0C00:
                            if ((opcode & 0xFE08) == 0xF800)
                            {
                                table[i] = Opcodes.BLD;
                                break;
                            }

                            if ((opcode & 0xFE08) == 0xFA00)
                            {
                                table[i] = Opcodes.BST;
                                break;
                            }

                            if ((opcode & 0xFE08) == 0xFC00)
                            {
                                table[i] = Opcodes.SBRC;
                                break;
                            }

                            if ((opcode & 0xFE08) == 0xFE00)
                            {
                                table[i] = Opcodes.SBRS;
                                break;
                            }

                            break;
                    }

                    break;
            }
        }

        return table;
    }
}