namespace AVR8Sharp.Cpu;

public static class AvrInterrupt
{
	public static void DoAvrInterrupt (Cpu cpu, int address)
	{
		var sp = cpu.DataView.GetUint16(93, true);
		cpu.Data[sp] = (byte)(cpu.PC & 0xff);
		cpu.Data[sp - 1] = (byte)(cpu.PC >> 8 & 0xff);
		if (cpu.PC22Bits)
		{
			cpu.Data[sp - 2] = (byte)(cpu.PC >> 16 & 0xff);
		}
		cpu.DataView.SetUint16(93, (ushort)(sp - (cpu.PC22Bits ? 3 : 2)), true);
		cpu.Data[95] &= 0x7f;
		cpu.Cycles += 2;
		cpu.PC = (uint)address;
	}
}
