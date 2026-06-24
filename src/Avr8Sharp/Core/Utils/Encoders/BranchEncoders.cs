using static AVR8Sharp.Core.Utils.Encoders.EncoderHelpers;

namespace AVR8Sharp.Core.Utils.Encoders;

internal static class BranchEncoders
{
	public static (ushort, ushort) CALL(int k)
	{
		k = k >> 1;
		int hk = (k >> 16) & 0x3F;
		int lk = k & 0xFFFF;
		ushort high = (ushort)(0x940E | ((hk & 0x3E) << 3) | (hk & 1));
		return (high, (ushort)lk);
	}

	public static (ushort, ushort) JMP(int k)
	{
		k = k >> 1;
		int hk = (k >> 16) & 0x3F;
		int lk = k & 0xFFFF;
		ushort high = (ushort)(0x940C | ((hk & 0x3E) << 3) | (hk & 1));
		return (high, (ushort)lk);
	}

	public static ushort RCALL(int k)
	{
		int offset = k >> 1; // k is byte offset from NEXT instruction
		return (ushort)(0xD000 | FitTwoC(offset, 12));
	}

	public static ushort RJMP(int k)
	{
		int offset = k >> 1;
		return (ushort)(0xC000 | FitTwoC(offset, 12));
	}

	public static ushort BRBC(string s, int k)
	{
		int bit = EncoderHelpers.ParseImm(s, 0, 7);
		int offset = FitTwoC(k >> 1, 7);
		return (ushort)(0xF400 | bit | (offset << 3));
	}

	public static ushort BRBS(string s, int k)
	{
		int bit = EncoderHelpers.ParseImm(s, 0, 7);
		int offset = FitTwoC(k >> 1, 7);
		return (ushort)(0xF000 | bit | (offset << 3));
	}

	// Flag-specific branches (delegate to BRBC/BRBS)
	public static ushort BRCC(int k) => BRBC("0", k);
	public static ushort BRCS(int k) => BRBS("0", k);
	public static ushort BREQ(int k) => BRBS("1", k);
	public static ushort BRGE(int k) => BRBC("4", k);
	public static ushort BRHC(int k) => BRBC("5", k);
	public static ushort BRHS(int k) => BRBS("5", k);
	public static ushort BRID(int k) => BRBC("7", k);
	public static ushort BRIE(int k) => BRBS("7", k);
	public static ushort BRLO(int k) => BRBS("0", k);
	public static ushort BRLT(int k) => BRBS("4", k);
	public static ushort BRMI(int k) => BRBS("2", k);
	public static ushort BRNE(int k) => BRBC("1", k);
	public static ushort BRPL(int k) => BRBC("2", k);
	public static ushort BRSH(int k) => BRBC("0", k);
	public static ushort BRTC(int k) => BRBC("6", k);
	public static ushort BRTS(int k) => BRBS("6", k);
	public static ushort BRVC(int k) => BRBC("3", k);
	public static ushort BRVS(int k) => BRBS("3", k);
}
