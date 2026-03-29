namespace AVR8Sharp.Core.Cpu;

public static class AvrInterrupt
{
	public static Action<int, uint>? OnInterruptDispatch { get; set; }

	public static void DoAvrInterrupt (Cpu cpu, int address)
	{
		OnInterruptDispatch?.Invoke(address, cpu.Pc);
		var sp = cpu.Mmio.DataView.GetUint16(93, true);
		cpu.Mmio.Data[sp] = (byte)(cpu.Pc & 0xff);
		cpu.Mmio.Data[sp - 1] = (byte)(cpu.Pc >> 8 & 0xff);
		if (cpu.Pc22Bits)
		{
			cpu.Mmio.Data[sp - 2] = (byte)(cpu.Pc >> 16 & 0xff);
		}
		cpu.Mmio.DataView.SetUint16(93, (ushort)(sp - (cpu.Pc22Bits ? 3 : 2)), true);
		cpu.Mmio.Data[95] &= 0x7f;
		cpu.Cycles += 2;
		cpu.Pc = (uint)address;
	}
}
