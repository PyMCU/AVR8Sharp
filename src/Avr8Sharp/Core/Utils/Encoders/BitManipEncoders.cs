using static AVR8Sharp.Core.Utils.Encoders.EncoderHelpers;

namespace AVR8Sharp.Core.Utils.Encoders;

internal static class BitManipEncoders
{
	public static ushort BCLR(string s)
	{
		int bit = ParseImm(s, 0, 7);
		return (ushort)(0x9488 | (bit << 4));
	}

	public static ushort BSET(string s)
	{
		int bit = ParseImm(s, 0, 7);
		return (ushort)(0x9408 | (bit << 4));
	}

	public static ushort BLD(string rd, string b)
	{
		int d = ParseRegister(rd);
		int bit = ParseImm(b, 0, 7);
		return (ushort)(0xF800 | DestRd(d) | bit);
	}

	public static ushort BST(string rd, string b)
	{
		int d = ParseRegister(rd);
		int bit = ParseImm(b, 0, 7);
		return (ushort)(0xFA00 | DestRd(d) | bit);
	}

	public static ushort CBI(string A, string b)
	{
		int a = ParseImm(A, 0, 31);
		int bit = ParseImm(b, 0, 7);
		return (ushort)(0x9800 | (a << 3) | bit);
	}

	public static ushort SBI(string A, string b)
	{
		int a = ParseImm(A, 0, 31);
		int bit = ParseImm(b, 0, 7);
		return (ushort)(0x9A00 | (a << 3) | bit);
	}

	public static ushort SBIC(string A, string b)
	{
		int a = ParseImm(A, 0, 31);
		int bit = ParseImm(b, 0, 7);
		return (ushort)(0x9900 | (a << 3) | bit);
	}

	public static ushort SBIS(string A, string b)
	{
		int a = ParseImm(A, 0, 31);
		int bit = ParseImm(b, 0, 7);
		return (ushort)(0x9B00 | (a << 3) | bit);
	}

	public static ushort SBRC(string rd, string b)
	{
		int d = ParseRegister(rd);
		int bit = ParseImm(b, 0, 7);
		return (ushort)(0xFC00 | DestRd(d) | bit);
	}

	public static ushort SBRS(string rd, string b)
	{
		int d = ParseRegister(rd);
		int bit = ParseImm(b, 0, 7);
		return (ushort)(0xFE00 | DestRd(d) | bit);
	}

	// Flag set/clear
	public static ushort CLC() => BCLR("0");
	public static ushort CLH() => BCLR("5");
	public static ushort CLI() => BCLR("7");
	public static ushort CLN() => BCLR("2");
	public static ushort CLS() => BCLR("4");
	public static ushort CLT() => BCLR("6");
	public static ushort CLV() => BCLR("3");
	public static ushort CLZ() => BCLR("1");
	public static ushort SEC() => BSET("0");
	public static ushort SEH() => BSET("5");
	public static ushort SEI() => BSET("7");
	public static ushort SEN() => BSET("2");
	public static ushort SES() => BSET("4");
	public static ushort SET() => BSET("6");
	public static ushort SEV() => BSET("3");
	public static ushort SEZ() => BSET("1");
}
