using static AVR8Sharp.Core.Utils.Encoders.EncoderHelpers;

namespace AVR8Sharp.Core.Utils.Encoders;

internal static class MiscEncoders
{
	public static ushort NOP()    => 0x0000;
	public static ushort RET()    => 0x9508;
	public static ushort RETI()   => 0x9518;
	public static ushort SLEEP()  => 0x9588;
	public static ushort WDR()    => 0x95A8;
	public static ushort BREAK()  => 0x9598;
	public static ushort ICALL()  => 0x9509;
	public static ushort IJMP()   => 0x9409;
	public static ushort EICALL() => 0x9519;
	public static ushort EIJMP()  => 0x9419;

	public static ushort DES(string k)
	{
		int imm = ParseImm(k, 0, 15);
		return (ushort)(0x940B | (imm << 4));
	}

	public static ushort SPM(string? operand = null)
	{
		if (string.IsNullOrEmpty(operand)) return 0x95E8;
		if (operand.Trim().ToUpperInvariant() == "Z+") return 0x95F8;
		throw new Exception("SPM operand must be empty or Z+");
	}
}
