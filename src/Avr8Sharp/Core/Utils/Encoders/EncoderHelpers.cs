namespace AVR8Sharp.Core.Utils.Encoders;

internal static class EncoderHelpers
{
	/// <summary>
	/// Parse register name like "r5", "R5", case-insensitive.
	/// Returns register number 0-31, or -1 if not a register.
	/// </summary>
	public static int TryParseRegister(ReadOnlySpan<char> s)
	{
		if (s.IsEmpty) return -1;
		if ((s[0] != 'r' && s[0] != 'R') || s.Length < 2) return -1;
		if (!int.TryParse(s[1..], out int n)) return -1;
		return (n >= 0 && n <= 31) ? n : -1;
	}

	public static int ParseRegister(string s, int min = 0, int max = 31)
	{
		int n = TryParseRegister(s.AsSpan());
		if (n < 0) throw new Exception($"Not a register: {s}");
		if (n < min || n > max) throw new Exception($"register Rd must be in range r{min}..r{max}, got r{n}");
		return n;
	}

	// Shift destination register to bits 8..4 (most common position)
	public static int DestRd(int reg) => (reg & 0x1F) << 4;

	// Shift source register to bits 9,3..0
	public static int SrcRr(int reg) => ((reg >> 4) & 1) << 9 | (reg & 0xF);

	// Fit a two's-complement number into 'bits' bits
	public static int FitTwoC(int value, int bits)
	{
		int maxVal = 1 << (bits - 1);
		if (value >= maxVal || value < -maxVal)
			throw new Exception($"Value {value} does not fit in {bits} signed bits");
		return value < 0 ? (value + (1 << bits)) & ((1 << bits) - 1) : value;
	}

	// Parse ADIW/SBIW register pair (24, 26, 28, 30)
	public static int ParseWordRegPair(string s)
	{
		int n = TryParseRegister(s.AsSpan());
		if (n < 0 || (n != 24 && n != 26 && n != 28 && n != 30))
			throw new Exception("Word register pair must be r24, r26, r28, or r30");
		return (n - 24) / 2;
	}

	public static int ParseImm(string s, int min, int max)
	{
		int v = ParseValue(s);
		if (v < min || v > max)
			throw new Exception($"Immediate {s} out of range {min}..{max}");
		return v;
	}

	public static int ParseValue(string s)
	{
		s = s.Trim();
		if (s.Length > 1 && s[0] == '0' && (s[1] == 'x' || s[1] == 'X'))
			return (int)uint.Parse(s[2..], System.Globalization.NumberStyles.HexNumber);
		if (s.Length > 1 && s[0] == '$')
			return (int)uint.Parse(s[1..], System.Globalization.NumberStyles.HexNumber);
		if (s.Length > 1 && s[0] == '0' && (s[1] == 'b' || s[1] == 'B'))
			return Convert.ToInt32(s[2..], 2);
		// sign-aware: allow "-4" etc.
		if (s.StartsWith('+')) return int.Parse(s[1..]);
		return int.Parse(s);
	}
}
