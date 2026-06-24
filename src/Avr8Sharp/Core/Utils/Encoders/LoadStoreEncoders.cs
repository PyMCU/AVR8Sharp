using static AVR8Sharp.Core.Utils.Encoders.EncoderHelpers;

namespace AVR8Sharp.Core.Utils.Encoders;

internal static class LoadStoreEncoders
{

	public static ushort LD(string rd, string src)
	{
		int d = ParseRegister(rd);
		int mode = StldXyz(src);
		return (ushort)(DestRd(d) | mode);
	}

	public static ushort LDD(string rd, string src)
	{
		int d = ParseRegister(rd);
		int mode = StldYzQ(src);
		return (ushort)(DestRd(d) | mode);
	}

	public static ushort LDI(string rd, string k)
	{
		int d = ParseRegister(rd, 16, 31);
		int imm = ParseImm(k, 0, 255);
		return (ushort)(0xE000 | (DestRd(d) & 0xF0) | ((imm & 0xF0) << 4) | (imm & 0x0F));
	}

	public static (ushort, ushort) LDS(string rd, string k)
	{
		int d = ParseRegister(rd);
		int addr = ParseImm(k, 0, 65535);
		return ((ushort)(0x9000 | DestRd(d)), (ushort)addr);
	}

	public static ushort LPM(string rd, string src)
	{
		if (string.IsNullOrEmpty(rd)) return 0x95C8;
		int d = ParseRegister(rd);
		int op = src switch { "Z" => 4, "Z+" => 5, _ => throw new Exception("LPM: second operand must be Z or Z+") };
		return (ushort)(0x9000 | DestRd(d) | op);
	}

	public static ushort ELPM(string rd, string src)
	{
		if (string.IsNullOrEmpty(rd)) return 0x95D8;
		int d = ParseRegister(rd);
		int op = src switch { "Z" => 6, "Z+" => 7, _ => throw new Exception("ELPM: second operand must be Z or Z+") };
		return (ushort)(0x9000 | DestRd(d) | op);
	}

	public static ushort LSR(string rd) =>
		(ushort)(0x9406 | DestRd(ParseRegister(rd)));

	public static ushort ASR(string rd) =>
		(ushort)(0x9405 | DestRd(ParseRegister(rd)));

	public static ushort ROR(string rd) =>
		(ushort)(0x9407 | DestRd(ParseRegister(rd)));

	public static ushort SWAP(string rd) =>
		(ushort)(0x9402 | DestRd(ParseRegister(rd)));

	public static ushort POP(string rd) =>
		(ushort)(0x900F | DestRd(ParseRegister(rd)));

	public static ushort PUSH(string rd) =>
		(ushort)(0x920F | DestRd(ParseRegister(rd)));

	public static ushort ST(string dst, string rr)
	{
		int r = ParseRegister(rr);
		int mode = StldXyz(dst);
		return (ushort)(0x0200 | DestRd(r) | mode);
	}

	public static ushort STD(string dst, string rr)
	{
		int r = ParseRegister(rr);
		int mode = StldYzQ(dst);
		return (ushort)(0x0200 | DestRd(r) | mode);
	}

	public static (ushort, ushort) STS(string addr, string rr)
	{
		int r = ParseRegister(rr);
		int k = ParseImm(addr, 0, 65535);
		return ((ushort)(0x9200 | DestRd(r)), (ushort)k);
	}

	public static ushort IN(string rd, string A)
	{
		int d = ParseRegister(rd);
		int a = ParseImm(A, 0, 63);
		return (ushort)(0xB000 | DestRd(d) | ((a & 0x30) << 5) | (a & 0x0F));
	}

	public static ushort OUT(string A, string rr)
	{
		int r = ParseRegister(rr);
		int a = ParseImm(A, 0, 63);
		return (ushort)(0xB800 | DestRd(r) | ((a & 0x30) << 5) | (a & 0x0F));
	}

	public static ushort LAC(string z, string rd)
	{
		if (!z.Equals("Z", StringComparison.OrdinalIgnoreCase)) throw new Exception("First operand must be Z");
		return (ushort)(0x9206 | DestRd(ParseRegister(rd)));
	}

	public static ushort LAS(string z, string rd)
	{
		if (!z.Equals("Z", StringComparison.OrdinalIgnoreCase)) throw new Exception("First operand must be Z");
		return (ushort)(0x9205 | DestRd(ParseRegister(rd)));
	}

	public static ushort LAT(string z, string rd)
	{
		if (!z.Equals("Z", StringComparison.OrdinalIgnoreCase)) throw new Exception("First operand must be Z");
		return (ushort)(0x9207 | DestRd(ParseRegister(rd)));
	}

	public static ushort XCH(string z, string rd)
	{
		if (!z.Equals("Z", StringComparison.OrdinalIgnoreCase)) throw new Exception("First operand must be Z");
		return (ushort)(0x9204 | DestRd(ParseRegister(rd)));
	}

	public static ushort SER(string rd)
	{
		int d = ParseRegister(rd, 16, 31);
		return (ushort)(0xEF0F | (DestRd(d) & 0xF0));
	}

	private static int StldXyz(string xyz) => xyz.ToUpperInvariant() switch
	{
		"X"  => 0x900C,
		"X+" => 0x900D,
		"-X" => 0x900E,
		"Y"  => 0x8008,
		"Y+" => 0x9009,
		"-Y" => 0x900A,
		"Z"  => 0x8000,
		"Z+" => 0x9001,
		"-Z" => 0x9002,
		_ => throw new Exception($"Not a valid indirect address mode: {xyz}")
	};

	private static int StldYzQ(string yzq)
	{
		var (baseReg, q) = LineParser.ParseYzDisplacement(yzq);
		if (q < 0 || q > 63) throw new Exception("Displacement q out of range 0..63");
		int r = 0x8000;
		if (baseReg == 'Y') r |= 0x8;
		r |= ((q & 0x20) << 8) | ((q & 0x18) << 7) | (q & 0x7);
		return r;
	}
}
