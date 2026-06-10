namespace AVR8Sharp.Core;

public static class AvrInterrupt
{
	public static Action<int, uint>? OnInterruptDispatch { get; set; }

	/// <summary>
	/// Called when a BREAK instruction (0x9598) is executed.
	/// The parameter is the word address of the BREAK instruction.
	/// </summary>
	public static Action<uint>? OnBreakpoint { get; set; }

	/// <summary>
	/// Called when a SLEEP instruction (0x9588) is executed.
	/// The parameter is the SM2:SM1:SM0 sleep mode bits from SMCR (bits 3:1).
	/// The host simulation can pause the CPU loop in response.
	/// </summary>
	public static Action<byte>? OnSleep { get; set; }

	public static void DoAvrInterrupt (Cpu cpu, int address)
	{
		OnInterruptDispatch?.Invoke(address, cpu.Pc);
		var sp = cpu.Mmio.DataView.GetUint16(93, true);
		if (sp - (cpu.Pc22Bits ? 2 : 1) < cpu.StackLowLimit)
			throw new AvrStackOverflowException(cpu.Pc, sp - (cpu.Pc22Bits ? 2 : 1), cpu.StackLowLimit);
		cpu.Mmio.Data[sp] = (byte)(cpu.Pc & 0xff);
		cpu.Mmio.Data[sp - 1] = (byte)(cpu.Pc >> 8 & 0xff);
		if (cpu.Pc22Bits)
		{
			cpu.Mmio.Data[sp - 2] = (byte)(cpu.Pc >> 16 & 0xff);
		}
		cpu.Mmio.DataView.SetUint16(93, (ushort)(sp - (cpu.Pc22Bits ? 3 : 2)), true);
		cpu.Mmio.Data[95] &= 0x7f;
		cpu.Cycles += 3; // 2 for PC push + 1 for vector fetch (AVR spec: 4 cycles total including current instruction)
		cpu.Pc = (uint)address;
	}
}
