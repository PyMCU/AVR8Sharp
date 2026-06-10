using System.Runtime.CompilerServices;

namespace AVR8Sharp.Core;

public static class Opcodes
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ADC(ref Cpu cpu, ref ushort opcode)
    {
        var d = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        var r = cpu.Mmio.Data[(opcode & 0xf) | (opcode & 0x200) >> 5];
        var sum = d + r + (cpu._sregArith & 1);
        var R = (byte)(sum & 255);
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = R;
        var sreg = 0;
        sreg |= R == 0 ? 2 : 0;
        sreg |= (128 & R) != 0 ? 4 : 0;
        sreg |= ((R ^ r) & (d ^ R) & 128) != 0 ? 8 : 0;
        sreg |= (sreg >> 2 & 1 ^ sreg >> 3 & 1) != 0 ? 0x10 : 0;
        sreg |= (sum & 256) != 0 ? 1 : 0;
        sreg |= (8 & (d & r | r & ~R | ~R & d)) != 0 ? 0x20 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ADD(ref Cpu cpu, ref ushort opcode)
    {
        var d = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        var r = cpu.Mmio.Data[(opcode & 0xf) | (opcode & 0x200) >> 5];
        var R = (byte)(d + r);
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = R;
        var sreg = 0;
        sreg |= R == 0 ? 2 : 0;
        sreg |= (128 & R) != 0 ? 4 : 0;
        sreg |= ((R ^ r) & (R ^ d) & 128) != 0 ? 8 : 0;
        sreg |= (sreg >> 2 & 1 ^ sreg >> 3 & 1) != 0 ? 0x10 : 0;
        sreg |= (d + r & 256) != 0 ? 1 : 0;
        sreg |= (8 & (d & r | r & ~R | ~R & d)) != 0 ? 0x20 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ADIW(ref Cpu cpu, ref ushort opcode)
    {
        var addr = (ushort)(2 * ((opcode & 0x30) >> 4) + 24);
        var value = cpu.Mmio.DataView.GetUint16(addr, true);
        var R = (ushort)(value + ((opcode & 0xf) | ((opcode & 0xc0) >> 2)) & 0xffff);
        cpu.Mmio.DataView.SetUint16(addr, R, true);
        var sreg = cpu._sregArith & 0x20;
        sreg |= R == 0 ? 2 : 0;
        sreg |= (0x8000 & R) != 0 ? 4 : 0;
        sreg |= (~value & R & 0x8000) != 0 ? 8 : 0;
        sreg |= ((sreg >> 2 & 1) ^ (sreg >> 3 & 1)) != 0 ? 0x10 : 0;
        sreg |= (~R & value & 0x8000) != 0 ? 1 : 0;
        cpu._sregArith = (byte)sreg;
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AND(ref Cpu cpu, ref ushort opcode)
    {
        var R = (byte)(cpu.Mmio.Data[(opcode & 0x1f0) >> 4] & cpu.Mmio.Data[(opcode & 0xf) | (opcode & 0x200) >> 5]);
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = R;
        var sreg = cpu._sregArith & 0x21;
        sreg |= R == 0 ? 2 : 0;
        sreg |= (128 & R) != 0 ? 4 : 0;
        sreg |= ((sreg >> 2 & 1) ^ (sreg >> 3 & 1)) != 0 ? 0x10 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ANDI(ref Cpu cpu, ref ushort opcode)
    {
        var R = (byte)(cpu.Mmio.Data[((opcode & 0xf0) >> 4) + 16] & ((opcode & 0xf) | ((opcode & 0xf00) >> 4)));
        cpu.Mmio.Data[((opcode & 0xf0) >> 4) + 16] = R;
        var sreg = cpu._sregArith & 0x21;
        sreg |= R == 0 ? 2 : 0;
        sreg |= (128 & R) != 0 ? 4 : 0;
        sreg |= ((sreg >> 2 & 1) ^ (sreg >> 3 & 1)) != 0 ? 0x10 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ASR(ref Cpu cpu, ref ushort opcode)
    {
        var value = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        var R = (byte)((value >> 1) | (128 & value));
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = R;
        var sreg = cpu._sregArith & 0x20;
        sreg |= R == 0 ? 2 : 0;
        sreg |= (128 & R) != 0 ? 4 : 0;
        sreg |= value & 1;
        sreg |= (((sreg >> 2) & 1) ^ (sreg & 1)) != 0 ? 8 : 0;
        sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BCLR(ref Cpu cpu, ref ushort opcode)
    {
        var bit = (opcode & 0x70) >> 4;
        if (bit < 6) cpu._sregArith &= (byte)~(1 << bit);
        else cpu.Mmio.Data[95] &= (byte)~(1 << bit);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BLD(ref Cpu cpu, ref ushort opcode)
    {
        var b = opcode & 7;
        var d = (opcode & 0x1f0) >> 4;
        cpu.Mmio.Data[d] = (byte)((~(1 << b) & cpu.Mmio.Data[d]) | (((cpu.Mmio.Data[95] >> 6) & 1) << b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BRBC(ref Cpu cpu, ref ushort opcode)
    {
        if ((((cpu.Mmio.Data[95] & 0xc0) | cpu._sregArith) & (1 << (opcode & 7))) == 0)
        {
            cpu.Pc = (uint)(cpu.Pc + (((opcode & 0x1f8) >> 3) - ((opcode & 0x200) != 0 ? 0x40 : 0)));
            cpu.Cycles++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BRBS(ref Cpu cpu, ref ushort opcode)
    {
        if ((((cpu.Mmio.Data[95] & 0xc0) | cpu._sregArith) & (1 << (opcode & 7))) != 0)
        {
            cpu.Pc = (uint)(cpu.Pc + (((opcode & 0x1f8) >> 3) - ((opcode & 0x200) != 0 ? 0x40 : 0)));
            cpu.Cycles++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BSET(ref Cpu cpu, ref ushort opcode)
    {
        var bit = (opcode & 0x70) >> 4;
        if (bit < 6) cpu._sregArith |= (byte)(1 << bit);
        else cpu.Mmio.Data[95] |= (byte)(1 << bit);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BST(ref Cpu cpu, ref ushort opcode)
    {
        var d = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        var b = opcode & 7;
        cpu.Mmio.Data[95] = (byte)((cpu.Mmio.Data[95] & 0xbf) | (((d >> b) & 1) != 0 ? 0x40 : 0));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CALL(ref Cpu cpu, ref ushort opcode)
    {
        var k = (uint)(cpu.ProgramMemory[(int)(cpu.Pc + 1)] | ((opcode & 1) << 16) | ((opcode & 0x1f0) << 13));
        var ret = cpu.Pc + 2;
        var sp = cpu.Mmio.DataView.GetUint16(93, true);
        if (sp - (cpu.Pc22Bits ? 2 : 1) < cpu.StackLowLimit)
            throw new AvrStackOverflowException(cpu.Pc, sp - (cpu.Pc22Bits ? 2 : 1), cpu.StackLowLimit);
        cpu.Mmio.Data[sp] = (byte)(ret & 255);
        cpu.Mmio.Data[sp - 1] = (byte)((ret >> 8) & 255);
        if (cpu.Pc22Bits)
        {
            cpu.Mmio.Data[sp - 2] = (byte)((ret >> 16) & 255);
        }

        cpu.Mmio.DataView.SetUint16(93, (ushort)(sp - (cpu.Pc22Bits ? 3 : 2)), true);
        cpu.Pc = k - 1;
        cpu.Cycles += cpu.Pc22Bits ? 4UL : 3UL;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CBI(ref Cpu cpu, ref ushort opcode)
    {
        var A = opcode & 0xf8;
        var b = opcode & 7;
        var R = cpu.ReadData((ushort)((A >> 3) + 32));
        var mask = (byte)(1 << b);
        cpu.WriteData((ushort)((A >> 3) + 32), (byte)(R & ~mask), mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void COM(ref Cpu cpu, ref ushort opcode)
    {
        var d = (opcode & 0x1f0) >> 4;
        var R = (byte)(255 - cpu.Mmio.Data[d]);
        cpu.Mmio.Data[d] = R;
        var sreg = (cpu._sregArith & 0x20) | 1;
        sreg |= R == 0 ? 2 : 0;
        sreg |= (128 & R) != 0 ? 4 : 0;
        sreg |= ((sreg >> 2) & 1 ^ (sreg >> 3) & 1) != 0 ? 0x10 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CP(ref Cpu cpu, ref ushort opcode)
    {
        var val1 = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        var val2 = cpu.Mmio.Data[(opcode & 0xf) | ((opcode & 0x200) >> 5)];
        var R = val1 - val2;
        var sreg = 0;
        sreg |= R == 0 ? 2 : 0;
        sreg |= (128 & R) != 0 ? 4 : 0;
        sreg |= ((val1 ^ val2) & (val1 ^ R) & 128) != 0 ? 8 : 0;
        sreg |= ((sreg >> 2 & 1) ^ (sreg >> 3 & 1)) != 0 ? 0x10 : 0;
        sreg |= val2 > val1 ? 1 : 0;
        sreg |= (8 & (~val1 & val2 | val2 & R | R & ~val1)) != 0 ? 0x20 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CPC(ref Cpu cpu, ref ushort opcode)
    {
        var arg1 = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        var arg2 = cpu.Mmio.Data[(opcode & 0xf) | ((opcode & 0x200) >> 5)];
        int oldArith = cpu._sregArith;
        var r = arg1 - arg2 - (oldArith & 1);
        var sreg = ((r == 0 && ((oldArith >> 1) & 1) != 0) ? 2 : 0) | (arg2 + (oldArith & 1) > arg1 ? 1 : 0);
        sreg |= ((128 & r) != 0 ? 4 : 0);
        sreg |= ((arg1 ^ arg2) & (arg1 ^ r) & 128) != 0 ? 8 : 0;
        sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
        sreg |= (8 & ((~arg1 & arg2) | (arg2 & r) | (r & ~arg1))) != 0 ? 0x20 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CPI(ref Cpu cpu, ref ushort opcode)
    {
        var arg1 = cpu.Mmio.Data[((opcode & 0xf0) >> 4) + 16];
        var arg2 = (opcode & 0xf) | ((opcode & 0xf00) >> 4);
        var r = arg1 - arg2;
        var sreg = 0;
        sreg |= r == 0 ? 2 : 0;
        sreg |= (128 & r) != 0 ? 4 : 0;
        sreg |= ((arg1 ^ arg2) & (arg1 ^ r) & 128) != 0 ? 8 : 0;
        sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
        sreg |= arg2 > arg1 ? 1 : 0;
        sreg |= (8 & ((~arg1 & arg2) | (arg2 & r) | (r & ~arg1))) != 0 ? 0x20 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CPSE(ref Cpu cpu, ref ushort opcode)
    {
        if (cpu.Mmio.Data[(opcode & 0x1f0) >> 4] == cpu.Mmio.Data[(opcode & 0xf) | ((opcode & 0x200) >> 5)])
            SkipNextInstruction(cpu);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DEC(ref Cpu cpu, ref ushort opcode)
    {
        var value = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        var r = (byte)(value - 1);
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = r;
        var sreg = cpu._sregArith & 0x21;
        sreg |= r == 0 ? 2 : 0;
        sreg |= (128 & r) != 0 ? 4 : 0;
        sreg |= 128 == value ? 8 : 0;
        sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EICALL(ref Cpu cpu, ref ushort opcode)
    {
        var retAddr = cpu.Pc + 1;
        var sp = cpu.Mmio.DataView.GetUint16(93, true);
        var eind = cpu.Mmio.Data[0x5c];
        cpu.Mmio.Data[sp] = (byte)(retAddr & 255);
        cpu.Mmio.Data[sp - 1] = (byte)((retAddr >> 8) & 255);
        cpu.Mmio.Data[sp - 2] = (byte)((retAddr >> 16) & 255);
        cpu.Mmio.DataView.SetUint16(93, (ushort)(sp - 3), true);
        cpu.Pc = (uint)(((eind << 16) | cpu.Mmio.DataView.GetUint16(30, true)) - 1);
        cpu.Cycles += 3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EIJMP(ref Cpu cpu, ref ushort opcode)
    {
        var eind = cpu.Mmio.Data[0x5c];
        cpu.Pc = (uint)(((eind << 16) | cpu.Mmio.DataView.GetUint16(30, true)) - 1);
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ELPM(ref Cpu cpu, ref ushort opcode)
    {
        var rampz = cpu.Mmio.Data[0x5b];
        cpu.Mmio.Data[0] = cpu.ProgBytes[(rampz << 16) | cpu.Mmio.DataView.GetUint16(30, true)];
        cpu.Cycles += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ELPM_REG(ref Cpu cpu, ref ushort opcode)
    {
        var rampz = cpu.Mmio.Data[0x5b];
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = cpu.ProgBytes[rampz << 16 | cpu.Mmio.DataView.GetUint16(30, true)];
        cpu.Cycles += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ELPM_INC(ref Cpu cpu, ref ushort opcode)
    {
        var rampz = cpu.Mmio.Data[0x5b];
        var i = cpu.Mmio.DataView.GetUint16(30, true);
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = cpu.ProgBytes[rampz << 16 | i];
        cpu.Mmio.DataView.SetUint16(30, (ushort)(i + 1), true);
        if (i == 0xffff)
        {
            cpu.Mmio.Data[0x5b] = (byte)((rampz + 1) % (cpu.ProgBytes.Length >> 16));
        }

        cpu.Cycles += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EOR(ref Cpu cpu, ref ushort opcode)
    {
        var R = (byte)(cpu.Mmio.Data[(opcode & 0x1f0) >> 4] ^ cpu.Mmio.Data[(opcode & 0xf) | ((opcode & 0x200) >> 5)]);
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = R;
        var sreg = cpu._sregArith & 0x21;
        sreg |= R == 0 ? 2 : 0;
        sreg |= (128 & R) != 0 ? 4 : 0;
        sreg |= ((sreg >> 2 & 1) ^ (sreg >> 3 & 1)) != 0 ? 0x10 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FMUL(ref Cpu cpu, ref ushort opcode)
    {
        var v1 = cpu.Mmio.Data[((opcode & 0x70) >> 4) + 16];
        var v2 = cpu.Mmio.Data[(opcode & 7) + 16];
        var R = (v1 * v2) << 1;
        cpu.Mmio.DataView.SetUint16(0, (ushort)R, true);
        cpu._sregArith = (byte)((cpu._sregArith & 0x3c) | ((0xffff & R) != 0 ? 0 : 2) | ((v1 * v2 & 0x8000) != 0 ? 1 : 0));
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FMULS(ref Cpu cpu, ref ushort opcode)
    {
        var v1 = cpu.Mmio.DataView.GetInt8(((opcode & 0x70) >> 4) + 16);
        var v2 = cpu.Mmio.DataView.GetInt8((opcode & 7) + 16);
        var R = (v1 * v2) << 1;
        cpu.Mmio.DataView.SetInt16(0, (short)R, true);
        cpu._sregArith = (byte)((cpu._sregArith & 0x3c) | ((0xffff & R) != 0 ? 0 : 2) | ((v1 * v2 & 0x8000) != 0 ? 1 : 0));
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FMULSU(ref Cpu cpu, ref ushort opcode)
    {
        var v1 = cpu.Mmio.DataView.GetInt8(((opcode & 0x70) >> 4) + 16);
        var v2 = cpu.Mmio.Data[(opcode & 7) + 16];
        var R = (v1 * v2) << 1;
        cpu.Mmio.DataView.SetInt16(0, (short)R, true);
        cpu._sregArith = (byte)((cpu._sregArith & 0x3c) | ((0xffff & R) != 0 ? 0 : 2) | ((v1 * v2 & 0x8000) != 0 ? 1 : 0));
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ICALL(ref Cpu cpu, ref ushort opcode)
    {
        var retAddr = cpu.Pc + 1;
        var sp = cpu.Mmio.DataView.GetUint16(93, true);
        if (sp - (cpu.Pc22Bits ? 2 : 1) < cpu.StackLowLimit)
            throw new AvrStackOverflowException(cpu.Pc, sp - (cpu.Pc22Bits ? 2 : 1), cpu.StackLowLimit);
        cpu.Mmio.Data[sp] = (byte)(retAddr & 255);
        cpu.Mmio.Data[sp - 1] = (byte)((retAddr >> 8) & 255);
        if (cpu.Pc22Bits)
        {
            cpu.Mmio.Data[sp - 2] = (byte)((retAddr >> 16) & 255);
        }

        cpu.Mmio.DataView.SetUint16(93, (ushort)(sp - (cpu.Pc22Bits ? 3 : 2)), true);
        cpu.Pc = (uint)(cpu.Mmio.DataView.GetUint16(30, true) - 1);
        cpu.Cycles += cpu.Pc22Bits ? 3UL : 2UL;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IJMP(ref Cpu cpu, ref ushort opcode)
    {
        cpu.Pc = (uint)(cpu.Mmio.DataView.GetUint16(30, true) - 1);
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IN(ref Cpu cpu, ref ushort opcode)
    {
        var i = cpu.ReadData((ushort)(((opcode & 0xf) | ((opcode & 0x600) >> 5)) + 32));
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = i;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void INC(ref Cpu cpu, ref ushort opcode)
    {
        var d = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        var r = (d + 1) & 255;
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = (byte)r;
        var sreg = cpu._sregArith & 0x21;
        sreg |= r == 0 ? 2 : 0;
        sreg |= (128 & r) != 0 ? 4 : 0;
        sreg |= 127 == d ? 8 : 0;
        sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void JMP(ref Cpu cpu, ref ushort opcode)
    {
        cpu.Pc = (uint)(cpu.ProgramMemory[(int)(cpu.Pc + 1)] | (opcode & 1) << 16 | (opcode & 0x1f0) << 13) - 1;
        cpu.Cycles += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LAC(ref Cpu cpu, ref ushort opcode)
    {
        var r = (opcode & 0x1f0) >> 4;
        var clear = cpu.Mmio.Data[r];
        var value = cpu.ReadData(cpu.Mmio.DataView.GetUint16(30, true));
        cpu.WriteData(cpu.Mmio.DataView.GetUint16(30, true), (byte)(value & (255 - clear)));
        cpu.Mmio.Data[r] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LAS(ref Cpu cpu, ref ushort opcode)
    {
        var r = (opcode & 0x1f0) >> 4;
        var set = cpu.Mmio.Data[r];
        var value = cpu.ReadData(cpu.Mmio.DataView.GetUint16(30, true));
        cpu.WriteData(cpu.Mmio.DataView.GetUint16(30, true), (byte)(value | set));
        cpu.Mmio.Data[r] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LAT(ref Cpu cpu, ref ushort opcode)
    {
        var r = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        var R = cpu.ReadData(cpu.Mmio.DataView.GetUint16(30, true));
        cpu.WriteData(cpu.Mmio.DataView.GetUint16(30, true), (byte)(r ^ R));
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = R;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LDI(ref Cpu cpu, ref ushort opcode)
    {
        cpu.Mmio.Data[((opcode & 0xf0) >> 4) + 16] = (byte)(opcode & 0xf | (opcode & 0xf00) >> 4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LDS(ref Cpu cpu, ref ushort opcode)
    {
        cpu.Cycles++;
        var value = cpu.ReadData(cpu.ProgramMemory[(int)(cpu.Pc + 1)]);
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = value;
        cpu.Pc++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LDX(ref Cpu cpu, ref ushort opcode)
    {
        cpu.Cycles++;
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData(cpu.Mmio.DataView.GetUint16(26, true));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LDX_INC(ref Cpu cpu, ref ushort opcode)
    {
        var x = cpu.Mmio.DataView.GetUint16(26, true);
        cpu.Cycles++;
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData(x);
        cpu.Mmio.DataView.SetUint16(26, (ushort)(x + 1), true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LDX_DEC(ref Cpu cpu, ref ushort opcode)
    {
        var x = cpu.Mmio.DataView.GetUint16(26, true) - 1;
        cpu.Mmio.DataView.SetUint16(26, (ushort)x, true);
        cpu.Cycles++;
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData((ushort)x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LDY(ref Cpu cpu, ref ushort opcode)
    {
        cpu.Cycles++;
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData(cpu.Mmio.DataView.GetUint16(28, true));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LDY_INC(ref Cpu cpu, ref ushort opcode)
    {
        var y = cpu.Mmio.DataView.GetUint16(28, true);
        cpu.Cycles++;
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData(y);
        cpu.Mmio.DataView.SetUint16(28, (ushort)(y + 1), true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LDY_DEC(ref Cpu cpu, ref ushort opcode)
    {
        var y = cpu.Mmio.DataView.GetUint16(28, true) - 1;
        cpu.Mmio.DataView.SetUint16(28, (ushort)y, true);
        cpu.Cycles++;
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData((ushort)y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LDDY(ref Cpu cpu, ref ushort opcode)
    {
        cpu.Cycles++;
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData(
            (ushort)(cpu.Mmio.DataView.GetUint16(28, true) +
                     ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)))
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LDZ(ref Cpu cpu, ref ushort opcode)
    {
        cpu.Cycles++;
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData(cpu.Mmio.DataView.GetUint16(30, true));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LDZ_INC(ref Cpu cpu, ref ushort opcode)
    {
        var z = cpu.Mmio.DataView.GetUint16(30, true);
        cpu.Cycles++;
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData(z);
        cpu.Mmio.DataView.SetUint16(30, (ushort)(z + 1), true);
    }

    public static void LDZ_DEC(ref Cpu cpu, ref ushort opcode)
    {
        var z = cpu.Mmio.DataView.GetUint16(30, true) - 1;
        cpu.Mmio.DataView.SetUint16(30, (ushort)z, true);
        cpu.Cycles++;
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData((ushort)z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LDDZ(ref Cpu cpu, ref ushort opcode)
    {
        cpu.Cycles++;
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = cpu.ReadData(
            (ushort)(cpu.Mmio.DataView.GetUint16(30, true) +
                     ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8)))
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LPM(ref Cpu cpu, ref ushort opcode)
    {
        cpu.Mmio.Data[0] = cpu.ProgBytes[cpu.Mmio.DataView.GetUint16(30, true)];
        cpu.Cycles += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LPM_REG(ref Cpu cpu, ref ushort opcode)
    {
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = cpu.ProgBytes[cpu.Mmio.DataView.GetUint16(30, true)];
        cpu.Cycles += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LPM_INC(ref Cpu cpu, ref ushort opcode)
    {
        var i = cpu.Mmio.DataView.GetUint16(30, true);
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = cpu.ProgBytes[i];
        cpu.Mmio.DataView.SetUint16(30, (ushort)(i + 1), true);
        cpu.Cycles += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LSR(ref Cpu cpu, ref ushort opcode)
    {
        var value = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        var R = (byte)(value >> 1);
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = R;
        var sreg = cpu._sregArith & 0x20;
        sreg |= R == 0 ? 2 : 0;
        sreg |= value & 1;
        sreg |= ((sreg >> 2) & 1 ^ (sreg & 1)) != 0 ? 8 : 0;
        sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MOV(ref Cpu cpu, ref ushort opcode)
    {
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = cpu.Mmio.Data[(opcode & 0xf) | ((opcode & 0x200) >> 5)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MOVW(ref Cpu cpu, ref ushort opcode)
    {
        var r2 = 2 * (opcode & 0xf);
        var d2 = 2 * ((opcode & 0xf0) >> 4);
        cpu.Mmio.Data[d2] = cpu.Mmio.Data[r2];
        cpu.Mmio.Data[d2 + 1] = cpu.Mmio.Data[r2 + 1];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MUL(ref Cpu cpu, ref ushort opcode)
    {
        var R = cpu.Mmio.Data[(opcode & 0x1f0) >> 4] * cpu.Mmio.Data[(opcode & 0xf) | ((opcode & 0x200) >> 5)];
        cpu.Mmio.DataView.SetUint16(0, (ushort)R, true);
        cpu._sregArith = (byte)((cpu._sregArith & 0x3c) | ((0xffff & R) != 0 ? 0 : 2) | ((0x8000 & R) != 0 ? 1 : 0));
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MULS(ref Cpu cpu, ref ushort opcode)
    {
        var R = cpu.Mmio.DataView.GetInt8(((opcode & 0xf0) >> 4) + 16) * cpu.Mmio.DataView.GetInt8((opcode & 0xf) + 16);
        cpu.Mmio.DataView.SetInt16(0, (short)R, true);
        cpu._sregArith = (byte)((cpu._sregArith & 0x3c) | ((0xffff & R) != 0 ? 0 : 2) | ((0x8000 & R) != 0 ? 1 : 0));
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MULSU(ref Cpu cpu, ref ushort opcode)
    {
        var R = cpu.Mmio.DataView.GetInt8(((opcode & 0x70) >> 4) + 16) * cpu.Mmio.Data[(opcode & 7) + 16];
        cpu.Mmio.DataView.SetInt16(0, (short)R, true);
        cpu._sregArith = (byte)((cpu._sregArith & 0x3c) | ((0xffff & R) != 0 ? 0 : 2) | ((0x8000 & R) != 0 ? 1 : 0));
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NEG(ref Cpu cpu, ref ushort opcode)
    {
        var d = (opcode & 0x1f0) >> 4;
        var value = cpu.Mmio.Data[d];
        var R = (byte)(0 - value);
        cpu.Mmio.Data[d] = R;
        var sreg = 0;
        sreg |= R == 0 ? 2 : 0;
        sreg |= (128 & R) != 0 ? 4 : 0;
        sreg |= 128 == R ? 8 : 0;
        sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
        sreg |= R == 0 ? 0 : 1;
        sreg |= (8 & (R | value)) != 0 ? 0x20 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NOP(ref Cpu cpu, ref ushort opcode) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BREAK(ref Cpu cpu, ref ushort opcode)
    {
        AvrInterrupt.OnBreakpoint?.Invoke(cpu.Pc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SLEEP(ref Cpu cpu, ref ushort opcode)
    {
        // Invoke OnSleep with SM2:SM1:SM0 bits from SMCR register (0x53), bits 3:1
        AvrInterrupt.OnSleep?.Invoke((byte)((cpu.Mmio.Data[0x53] >> 1) & 0x07));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OR(ref Cpu cpu, ref ushort opcode)
    {
        var R = cpu.Mmio.Data[(opcode & 0x1f0) >> 4] | cpu.Mmio.Data[(opcode & 0xf) | ((opcode & 0x200) >> 5)];
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = (byte)R;
        var sreg = cpu._sregArith & 0x21;
        sreg |= R == 0 ? 2 : 0;
        sreg |= (128 & R) != 0 ? 4 : 0;
        sreg |= ((sreg >> 2 & 1) ^ (sreg >> 3 & 1)) != 0 ? 0x10 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SBR(ref Cpu cpu, ref ushort opcode)
    {
        var R = cpu.Mmio.Data[((opcode & 0xf0) >> 4) + 16] | ((opcode & 0xf) | ((opcode & 0xf00) >> 4));
        cpu.Mmio.Data[((opcode & 0xf0) >> 4) + 16] = (byte)R;
        var sreg = cpu._sregArith & 0x21;
        sreg |= R == 0 ? 2 : 0;
        sreg |= (128 & R) != 0 ? 4 : 0;
        sreg |= ((sreg >> 2 & 1) ^ (sreg >> 3 & 1)) != 0 ? 0x10 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OUT(ref Cpu cpu, ref ushort opcode)
    {
        cpu.WriteData((ushort)(((opcode & 0xf) | ((opcode & 0x600) >> 5)) + 32), cpu.Mmio.Data[(opcode & 0x1f0) >> 4]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void POP(ref Cpu cpu, ref ushort opcode)
    {
        var value = cpu.Mmio.DataView.GetUint16(93, true) + 1;
        cpu.Mmio.DataView.SetUint16(93, (ushort)value, true);
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = cpu.Mmio.Data[value];
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PUSH(ref Cpu cpu, ref ushort opcode)
    {
        var value = cpu.Mmio.DataView.GetUint16(93, true);
        if (value < cpu.StackLowLimit)
            throw new AvrStackOverflowException(cpu.Pc, value, cpu.StackLowLimit);
        cpu.Mmio.Data[value] = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        cpu.Mmio.DataView.SetUint16(93, (ushort)(value - 1), true);
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RCALL(ref Cpu cpu, ref ushort opcode)
    {
        var k = (opcode & 0x7ff) - ((opcode & 0x800) != 0 ? 0x800 : 0);
        var retAddr = cpu.Pc + 1;
        var sp = cpu.Mmio.DataView.GetUint16(93, true);
        if (sp - (cpu.Pc22Bits ? 2 : 1) < cpu.StackLowLimit)
            throw new AvrStackOverflowException(cpu.Pc, sp - (cpu.Pc22Bits ? 2 : 1), cpu.StackLowLimit);
        cpu.Mmio.Data[sp] = (byte)(retAddr & 255);
        cpu.Mmio.Data[sp - 1] = (byte)((retAddr >> 8) & 255);
        if (cpu.Pc22Bits)
        {
            cpu.Mmio.Data[sp - 2] = (byte)((retAddr >> 16) & 255);
        }

        cpu.Mmio.DataView.SetUint16(93, (ushort)(sp - (cpu.Pc22Bits ? 3 : 2)), true);
        cpu.Pc += (ushort)k;
        cpu.Cycles += cpu.Pc22Bits ? 3UL : 2UL;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RET(ref Cpu cpu, ref ushort opcode)
    {
        var i = cpu.Mmio.DataView.GetUint16(93, true) + (cpu.Pc22Bits ? 3 : 2);
        if (i >= cpu.Mmio.Data.Length)
            throw new AvrStackUnderflowException(cpu.Pc, i - (cpu.Pc22Bits ? 3 : 2));
        cpu.Mmio.DataView.SetUint16(93, (ushort)i, true);
        cpu.Pc = (uint)((cpu.Mmio.Data[i - 1] << 8) + cpu.Mmio.Data[i] - 1);
        if (cpu.Pc22Bits)
        {
            cpu.Pc |= (uint)(cpu.Mmio.Data[i - 2] << 16);
        }

        cpu.Cycles += cpu.Pc22Bits ? 4UL : 3UL;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RETI(ref Cpu cpu, ref ushort opcode)
    {
        var i = cpu.Mmio.DataView.GetUint16(93, true) + (cpu.Pc22Bits ? 3 : 2);
        if (i >= cpu.Mmio.Data.Length)
            throw new AvrStackUnderflowException(cpu.Pc, i - (cpu.Pc22Bits ? 3 : 2));
        cpu.Mmio.DataView.SetUint16(93, (ushort)i, true);
        cpu.Pc = (uint)((cpu.Mmio.Data[i - 1] << 8) + cpu.Mmio.Data[i] - 1);
        if (cpu.Pc22Bits)
        {
            cpu.Pc |= (uint)(cpu.Mmio.Data[i - 2] << 16);
        }

        cpu.Cycles += cpu.Pc22Bits ? 4UL : 3UL;
        cpu.Mmio.Data[95] |= 0x80; // Enable interrupts
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RJMP(ref Cpu cpu, ref ushort opcode)
    {
        cpu.Pc = (uint)(cpu.Pc + ((opcode & 0x7ff) - ((opcode & 0x800) != 0 ? 0x800 : 0)));
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ROR(ref Cpu cpu, ref ushort opcode)
    {
        var d = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        var r = (byte)((d >> 1) | ((cpu._sregArith & 1) << 7));
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = r;
        var sreg = cpu._sregArith & 0x20;
        sreg |= r == 0 ? 2 : 0;
        sreg |= (128 & r) != 0 ? 4 : 0;
        sreg |= d & 1;
        sreg |= ((sreg >> 2) & 1 ^ (sreg & 1)) != 0 ? 8 : 0;
        sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SBC(ref Cpu cpu, ref ushort opcode)
    {
        var val1 = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        var val2 = cpu.Mmio.Data[(opcode & 0xf) | ((opcode & 0x200) >> 5)];
        int oldArith = cpu._sregArith;
        var R = (byte)(val1 - val2 - (oldArith & 1));
        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = R;
        var sreg = ((R == 0 && ((oldArith >> 1) & 1) != 0) ? 2 : 0) | (val2 + (oldArith & 1) > val1 ? 1 : 0);
        sreg |= ((128 & R) != 0 ? 4 : 0);
        sreg |= ((val1 ^ val2) & (val1 ^ R) & 128) != 0 ? 8 : 0;
        sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
        sreg |= (8 & ((~val1 & val2) | (val2 & R) | (R & ~val1))) != 0 ? 0x20 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SBCI(ref Cpu cpu, ref ushort opcode)
    {
        var val1 = cpu.Mmio.Data[((opcode & 0xf0) >> 4) + 16];
        var val2 = (opcode & 0xf) | ((opcode & 0xf00) >> 4);
        int oldArith = cpu._sregArith;
        var R = (byte)(val1 - val2 - (oldArith & 1));
        cpu.Mmio.Data[((opcode & 0xf0) >> 4) + 16] = R;
        var sreg = ((R == 0 && ((oldArith >> 1) & 1) != 0) ? 2 : 0) | (val2 + (oldArith & 1) > val1 ? 1 : 0);
        sreg |= ((128 & R) != 0 ? 4 : 0);
        sreg |= ((val1 ^ val2) & (val1 ^ R) & 128) != 0 ? 8 : 0;
        sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
        sreg |= (8 & ((~val1 & val2) | (val2 & R) | (R & ~val1))) != 0 ? 0x20 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SBI(ref Cpu cpu, ref ushort opcode)
    {
        var target = ((opcode & 0xf8) >> 3) + 32;
        var mask = 1 << (opcode & 7);
        cpu.WriteData((ushort)target, (byte)(cpu.ReadData((ushort)target) | mask), (byte)mask);
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SBIC(ref Cpu cpu, ref ushort opcode)
    {
        var value = cpu.ReadData((ushort)(((opcode & 0xf8) >> 3) + 32));
        if ((value & (1 << (opcode & 7))) == 0) SkipNextInstruction(cpu);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SBIS(ref Cpu cpu, ref ushort opcode)
    {
        var value = cpu.ReadData((ushort)(((opcode & 0xf8) >> 3) + 32));
        if ((value & (1 << (opcode & 7))) != 0) SkipNextInstruction(cpu);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SBIW(ref Cpu cpu, ref ushort opcode)
    {
        var i = 2 * ((opcode & 0x30) >> 4) + 24;
        var a = cpu.Mmio.DataView.GetUint16((ushort)i, true);
        var l = (opcode & 0xf) | ((opcode & 0xc0) >> 2);
        var R = a - l;
        cpu.Mmio.DataView.SetUint16((ushort)i, (ushort)R, true);
        var sreg = 0;
        sreg |= R == 0 ? 2 : 0;
        sreg |= (0x8000 & R) != 0 ? 4 : 0;
        sreg |= (a & ~R & 0x8000) != 0 ? 8 : 0;
        sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
        sreg |= l > a ? 1 : 0;
        sreg |= (8 & ((~a & l) | (l & R) | (R & ~a))) != 0 ? 0x20 : 0;
        cpu._sregArith = (byte)sreg;
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SBRC(ref Cpu cpu, ref ushort opcode)
    {
        if ((cpu.Mmio.Data[(opcode & 0x1f0) >> 4] & (1 << (opcode & 7))) == 0) SkipNextInstruction(cpu);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SBRS(ref Cpu cpu, ref ushort opcode)
    {
        if ((cpu.Mmio.Data[(opcode & 0x1f0) >> 4] & (1 << (opcode & 7))) != 0) SkipNextInstruction(cpu);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipNextInstruction(Cpu cpu)
    {
        var nextOpcode = cpu.ProgramMemory[(int)(cpu.Pc + 1)];
        var skipSize = IsTwoWordInstruction(nextOpcode) ? 2 : 1;
        cpu.Pc += (ushort)skipSize;
        cpu.Cycles += (ulong)skipSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void STS(ref Cpu cpu, ref ushort opcode)
    {
        var value = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        var addr = cpu.ProgramMemory[(int)(cpu.Pc + 1)];
        cpu.WriteData(addr, value);
        cpu.Pc++;
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void STX(ref Cpu cpu, ref ushort opcode)
    {
        cpu.WriteData(cpu.Mmio.DataView.GetUint16(26, true), cpu.Mmio.Data[(opcode & 0x1f0) >> 4]);
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void STX_INC(ref Cpu cpu, ref ushort opcode)
    {
        var x = cpu.Mmio.DataView.GetUint16(26, true);
        cpu.WriteData(x, cpu.Mmio.Data[(opcode & 0x1f0) >> 4]);
        cpu.Mmio.DataView.SetUint16(26, (ushort)(x + 1), true);
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void STX_DEC(ref Cpu cpu, ref ushort opcode)
    {
        var i = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        var x = cpu.Mmio.DataView.GetUint16(26, true) - 1;
        cpu.Mmio.DataView.SetUint16(26, (ushort)x, true);
        cpu.WriteData((ushort)x, i);
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void STY(ref Cpu cpu, ref ushort opcode)
    {
        cpu.WriteData(cpu.Mmio.DataView.GetUint16(28, true), cpu.Mmio.Data[(opcode & 0x1f0) >> 4]);
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void STY_INC(ref Cpu cpu, ref ushort opcode)
    {
        var i = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        var y = cpu.Mmio.DataView.GetUint16(28, true);
        cpu.WriteData(y, i);
        cpu.Mmio.DataView.SetUint16(28, (ushort)(y + 1), true);
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void STY_DEC(ref Cpu cpu, ref ushort opcode)
    {
        var i = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        var y = cpu.Mmio.DataView.GetUint16(28, true) - 1;
        cpu.Mmio.DataView.SetUint16(28, (ushort)y, true);
        cpu.WriteData((ushort)y, i);
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void STDY(ref Cpu cpu, ref ushort opcode)
    {
        cpu.WriteData(
            (ushort)(cpu.Mmio.DataView.GetUint16(28, true) +
                     ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8))),
            cpu.Mmio.Data[(opcode & 0x1f0) >> 4]
        );
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void STZ(ref Cpu cpu, ref ushort opcode)
    {
        cpu.WriteData(cpu.Mmio.DataView.GetUint16(30, true), cpu.Mmio.Data[(opcode & 0x1f0) >> 4]);
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void STZ_INC(ref Cpu cpu, ref ushort opcode)
    {
        var z = cpu.Mmio.DataView.GetUint16(30, true);
        cpu.WriteData(z, cpu.Mmio.Data[(opcode & 0x1f0) >> 4]);
        cpu.Mmio.DataView.SetUint16(30, (ushort)(z + 1), true);
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void STZ_DEC(ref Cpu cpu, ref ushort opcode)
    {
        var i = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        var z = cpu.Mmio.DataView.GetUint16(30, true) - 1;
        cpu.Mmio.DataView.SetUint16(30, (ushort)z, true);
        cpu.WriteData((ushort)z, i);
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void STDZ(ref Cpu cpu, ref ushort opcode)
    {
        cpu.WriteData(
            (ushort)(cpu.Mmio.DataView.GetUint16(30, true) +
                     ((opcode & 7) | ((opcode & 0xc00) >> 7) | ((opcode & 0x2000) >> 8))),
            cpu.Mmio.Data[(opcode & 0x1f0) >> 4]
        );
        cpu.Cycles++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SUB(ref Cpu cpu, ref ushort opcode)
    {
        var val1 = cpu.Mmio.Data[(opcode & 0x1f0) >> 4];
        var val2 = cpu.Mmio.Data[(opcode & 0xf) | ((opcode & 0x200) >> 5)];
        var R = (byte)(val1 - val2);

        cpu.Mmio.Data[(opcode & 0x1f0) >> 4] = R;
        var sreg = 0;
        sreg |= R == 0 ? 2 : 0;
        sreg |= (128 & R) != 0 ? 4 : 0;
        sreg |= ((val1 ^ val2) & (val1 ^ R) & 128) != 0 ? 8 : 0;
        sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
        sreg |= val2 > val1 ? 1 : 0;
        sreg |= (8 & ((~val1 & val2) | (val2 & R) | (R & ~val1))) != 0 ? 0x20 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SUBI(ref Cpu cpu, ref ushort opcode)
    {
        var val1 = cpu.Mmio.Data[((opcode & 0xf0) >> 4) + 16];
        var val2 = (opcode & 0xf) | ((opcode & 0xf00) >> 4);
        var R = (byte)(val1 - val2);
        cpu.Mmio.Data[((opcode & 0xf0) >> 4) + 16] = R;
        var sreg = 0;
        sreg |= R == 0 ? 2 : 0;
        sreg |= (128 & R) != 0 ? 4 : 0;
        sreg |= ((val1 ^ val2) & (val1 ^ R) & 128) != 0 ? 8 : 0;
        sreg |= (((sreg >> 2) & 1) ^ ((sreg >> 3) & 1)) != 0 ? 0x10 : 0;
        sreg |= val2 > val1 ? 1 : 0;
        sreg |= (8 & ((~val1 & val2) | (val2 & R) | (R & ~val1))) != 0 ? 0x20 : 0;
        cpu._sregArith = (byte)sreg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SWAP(ref Cpu cpu, ref ushort opcode)
    {
        var d = (opcode & 0x1f0) >> 4;
        var i = cpu.Mmio.Data[d];
        cpu.Mmio.Data[d] = (byte)(((15 & i) << 4) | ((240 & i) >> 4));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WDR(ref Cpu cpu, ref ushort opcode)
    {
        cpu.OnWatchdogReset();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void XCH(ref Cpu cpu, ref ushort opcode)
    {
        var r = (opcode & 0x1f0) >> 4;
        var val1 = cpu.Mmio.Data[r];
        var val2 = cpu.Mmio.Data[cpu.Mmio.DataView.GetUint16(30, true)];
        cpu.Mmio.Data[cpu.Mmio.DataView.GetUint16(30, true)] = val1;
        cpu.Mmio.Data[r] = val2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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