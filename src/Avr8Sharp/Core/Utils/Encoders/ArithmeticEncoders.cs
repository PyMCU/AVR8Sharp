using static AVR8Sharp.Core.Utils.Encoders.EncoderHelpers;

namespace AVR8Sharp.Core.Utils.Encoders;

internal static class ArithmeticEncoders
{
	public static ushort ADD(string rd, string rr) =>
		(ushort)(0x0C00 | DestRd(ParseRegister(rd)) | SrcRr(ParseRegister(rr)));

	public static ushort ADC(string rd, string rr) =>
		(ushort)(0x1C00 | DestRd(ParseRegister(rd)) | SrcRr(ParseRegister(rr)));

	public static ushort ADIW(string rd, string k)
	{
		int d = ParseWordRegPair(rd);
		int imm = ParseImm(k, 0, 63);
		return (ushort)(0x9600 | (d & 0x3) << 4 | ((imm & 0x30) << 2) | (imm & 0x0F));
	}

	public static ushort AND(string rd, string rr) =>
		(ushort)(0x2000 | DestRd(ParseRegister(rd)) | SrcRr(ParseRegister(rr)));

	public static ushort ANDI(string rd, string k)
	{
		int d = ParseRegister(rd, 16, 31);
		int imm = ParseImm(k, 0, 255);
		return (ushort)(0x7000 | (DestRd(d) & 0xF0) | ((imm & 0xF0) << 4) | (imm & 0x0F));
	}

	public static ushort COM(string rd) =>
		(ushort)(0x9400 | DestRd(ParseRegister(rd)));

	public static ushort CP(string rd, string rr) =>
		(ushort)(0x1400 | DestRd(ParseRegister(rd)) | SrcRr(ParseRegister(rr)));

	public static ushort CPC(string rd, string rr) =>
		(ushort)(0x0400 | DestRd(ParseRegister(rd)) | SrcRr(ParseRegister(rr)));

	public static ushort CPI(string rd, string k)
	{
		int d = ParseRegister(rd, 16, 31);
		int imm = ParseImm(k, 0, 255);
		return (ushort)(0x3000 | (DestRd(d) & 0xF0) | ((imm & 0xF0) << 4) | (imm & 0x0F));
	}

	public static ushort CPSE(string rd, string rr) =>
		(ushort)(0x1000 | DestRd(ParseRegister(rd)) | SrcRr(ParseRegister(rr)));

	public static ushort DEC(string rd) =>
		(ushort)(0x940A | DestRd(ParseRegister(rd)));

	public static ushort EOR(string rd, string rr) =>
		(ushort)(0x2400 | DestRd(ParseRegister(rd)) | SrcRr(ParseRegister(rr)));

	public static ushort FMUL(string rd, string rr) =>
		(ushort)(0x0308 | DestRd(ParseRegister(rd, 16, 23)) | (SrcRr(ParseRegister(rr, 16, 23)) & 0x7));

	public static ushort FMULS(string rd, string rr)
	{
		int d = ParseRegister(rd, 16, 23);
		int r = ParseRegister(rr, 16, 23);
		return (ushort)(0x0380 | ((d & 0x7) << 4) | (r & 0x7));
	}

	public static ushort FMULSU(string rd, string rr)
	{
		int d = ParseRegister(rd, 16, 23);
		int r = ParseRegister(rr, 16, 23);
		return (ushort)(0x0388 | ((d & 0x7) << 4) | (r & 0x7));
	}

	public static ushort INC(string rd) =>
		(ushort)(0x9403 | DestRd(ParseRegister(rd)));

	public static ushort MOV(string rd, string rr) =>
		(ushort)(0x2C00 | DestRd(ParseRegister(rd)) | SrcRr(ParseRegister(rr)));

	public static ushort MOVW(string rd, string rr)
	{
		int d = ParseRegister(rd);
		int r = ParseRegister(rr);
		return (ushort)(0x0100 | ((d >> 1) & 0xF) << 4 | ((r >> 1) & 0xF));
	}

	public static ushort MUL(string rd, string rr) =>
		(ushort)(0x9C00 | DestRd(ParseRegister(rd)) | SrcRr(ParseRegister(rr)));

	public static ushort MULS(string rd, string rr)
	{
		int d = ParseRegister(rd, 16, 31);
		int r = ParseRegister(rr, 16, 31);
		return (ushort)(0x0200 | ((d & 0xF) << 4) | (r & 0xF));
	}

	public static ushort MULSU(string rd, string rr)
	{
		int d = ParseRegister(rd, 16, 23);
		int r = ParseRegister(rr, 16, 23);
		return (ushort)(0x0300 | ((d & 0x7) << 4) | (r & 0x7));
	}

	public static ushort NEG(string rd) =>
		(ushort)(0x9401 | DestRd(ParseRegister(rd)));

	public static ushort OR(string rd, string rr) =>
		(ushort)(0x2800 | DestRd(ParseRegister(rd)) | SrcRr(ParseRegister(rr)));

	public static ushort ORI(string rd, string k)
	{
		int d = ParseRegister(rd, 16, 31);
		int imm = ParseImm(k, 0, 255);
		return (ushort)(0x6000 | (DestRd(d) & 0xF0) | ((imm & 0xF0) << 4) | (imm & 0x0F));
	}

	public static ushort SBC(string rd, string rr) =>
		(ushort)(0x0800 | DestRd(ParseRegister(rd)) | SrcRr(ParseRegister(rr)));

	public static ushort SBCI(string rd, string k)
	{
		int d = ParseRegister(rd, 16, 31);
		int imm = ParseImm(k, 0, 255);
		return (ushort)(0x4000 | (DestRd(d) & 0xF0) | ((imm & 0xF0) << 4) | (imm & 0x0F));
	}

	public static ushort SBIW(string rd, string k)
	{
		int d = ParseWordRegPair(rd);
		int imm = ParseImm(k, 0, 63);
		return (ushort)(0x9700 | (d & 0x3) << 4 | ((imm & 0x30) << 2) | (imm & 0x0F));
	}

	public static ushort SUB(string rd, string rr) =>
		(ushort)(0x1800 | DestRd(ParseRegister(rd)) | SrcRr(ParseRegister(rr)));

	public static ushort SUBI(string rd, string k)
	{
		int d = ParseRegister(rd, 16, 31);
		int imm = ParseImm(k, 0, 255);
		return (ushort)(0x5000 | (DestRd(d) & 0xF0) | ((imm & 0xF0) << 4) | (imm & 0x0F));
	}

	// Pseudo-instructions
	public static ushort CLR(string rd) => EOR(rd, rd);
	public static ushort LSL(string rd) => ADD(rd, rd);
	public static ushort ROL(string rd) => ADC(rd, rd);
	public static ushort TST(string rd) => AND(rd, rd);
	public static ushort SBR(string rd, string k) => ORI(rd, k);
	public static ushort CRB(string rd, string k)
	{
		int kv = ParseValue(k);
		return ANDI(rd, (~kv & 0xFF).ToString());
	}
}
