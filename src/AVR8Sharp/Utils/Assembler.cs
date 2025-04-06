#nullable enable
using System.Text.RegularExpressions;
using LabelTable = System.Collections.Generic.Dictionary<string, int>;
using OpCodeHandler = System.Func<string, string, int, System.Collections.Generic.Dictionary<string, int>, object>;

namespace AVR8Sharp.Utils;

public partial class AvrAssembler
{
	const string Z_OPERAND_ERROR = "First operand must be Z";
	
	// Create an alias for the dictionary type
	static Dictionary<string, OpCodeHandler> OpTable = new Dictionary<string, OpCodeHandler>  {
		{ "ADD", (a, b, _, _) => {
			var r = 0x0c00 | DestRIndex(a) | SrcRIndex(b);
			return ZeroPad (r);
		} },
		{ "ADC", (a, b, _, _) => {
			var r = 0x1c00 | DestRIndex(a) | SrcRIndex(b);
			return ZeroPad (r);
		} },
		{ "ADIW", (a, b, _, _) => {
			var r = 0x9600;
			var dm = RrIndexRegex?.Match(a);
			if (!dm.Success) {
				throw new Exception("Rd must be 24, 26, 28, or 30");
			}
			var d = int.Parse(dm.Groups[1].Value);
			d = (d - 24) / 2;
			r |= (d & 0x3) << 4;
			var k = ConstValue(b, 0, 63);
			r |= ((k & 0x30) << 2) | (k & 0x0f);
			return ZeroPad (r);
		} },
		{ "AND", (a, b, _, _) => {
			var r = 0x2000 | DestRIndex(a) | SrcRIndex(b);
			return ZeroPad (r);
		} },
		{ "ANDI", (a, b, _, _) => {
			var r = 0x7000 | (DestRIndex(a, 16, 31) & 0xf0);
			var k = ConstValue(b);
			r |= ((k & 0xf0) << 4) | (k & 0xf);
			return ZeroPad (r);
		} },
		{ "ASR", (a, _, _, _) => {
			var r = 0x9405 | DestRIndex(a);
			return ZeroPad (r);
		} },
		{ "BCLR", (a, _, _, _) => {
			var r = 0x9488;
			var s = ConstValue(a, 0, 7);
			r |= (s & 0x7) << 4;
			return ZeroPad (r);
		} },
		{ "BLD", (a, b, _, _) => {
			var r = 0xf800 | DestRIndex(a) | (ConstValue(b, 0, 7) & 0x7);
			return ZeroPad (r);
		} },
		{ "BRBC", (a, b, byteLoc, labels) => {
			var k = ConstOrLabel(b, labels, byteLoc + 2);
			if (k == int.MinValue) {
				return new Func<Dictionary<string, int>, string> ((l) => OpTable?["BRBC"](a, b, byteLoc, l) as string ?? string.Empty);
			}
			var r = 0xf400 | ConstValue(a, 0, 7);
			r |= FitTwoC(ConstValue (k >> 1, -64, 63), 7) << 3;
			return ZeroPad (r);
		} },
		{ "BRBS", (a, b, byteLoc, labels) => {
			var k = ConstOrLabel (b, labels, byteLoc + 2);
			if (k == int.MinValue) {
				return new Func<Dictionary<string, int>, string> ((l) => OpTable?["BRBS"](a, b, byteLoc, l) as string ?? string.Empty);
			}
			var r = 0xf000 | ConstValue(a, 0, 7);
			r |= FitTwoC(ConstValue (k >> 1, -64, 63), 7) << 3;
			return ZeroPad (r);
		} },
		{ "BRCC", (a, _, byteLoc, labels) => {
			return OpTable?["BRBC"]("0", a, byteLoc, labels) ?? string.Empty;
		}},
		{ "BRCS", (a, _, byteLoc, labels) => {
			return OpTable?["BRBS"]("0", a, byteLoc, labels) ?? string.Empty;
		}},
		{ "BREAK", (_, _, _, _) => {
			return "9598";
		} },
		{ "BREQ", (a, b, byteLoc, labels) => {
			return OpTable?["BRBS"]("1", a, byteLoc, labels) ?? string.Empty;
		} },
		{ "BRGE", (a, b, byteLoc, labels) => {
			return OpTable?["BRBC"]("4", a, byteLoc, labels) ?? string.Empty;
		} },
		{ "BRHC", (a, b, byteLoc, labels) => {
			return OpTable?["BRBC"]("5", a, byteLoc, labels) ?? string.Empty;
		} },
		{ "BRHS", (a, b, byteLoc, labels) => {
			return OpTable?["BRBS"]("5", a, byteLoc, labels) ?? string.Empty;
		} },
		{ "BRID", (a, b, byteLoc, labels) => {
			return OpTable?["BRBC"]("7", a, byteLoc, labels) ?? string.Empty;
		} },
		{ "BRIE", (a, b, byteLoc, labels) => {
			return OpTable?["BRBS"]("7", a, byteLoc, labels) ?? string.Empty;
		} },
		{ "BRLO", (a, b, byteLoc, labels) => {
			return OpTable?["BRBS"]("0", a, byteLoc, labels) ?? string.Empty;
		} },
		{ "BRLT", (a, b, byteLoc, labels) => {
			return OpTable?["BRBS"]("4", a, byteLoc, labels) ?? string.Empty;
		} },
		{ "BRMI", (a, b, byteLoc, labels) => {
			return OpTable?["BRBS"]("2", a, byteLoc, labels) ?? string.Empty;
		} },
		{ "BRNE", (a, b, byteLoc, labels) => {
			return OpTable?["BRBC"]("1", a, byteLoc, labels) ?? string.Empty;
		} },
		{ "BRPL", (a, b, byteLoc, labels) => {
			return OpTable?["BRBC"]("2", a, byteLoc, labels) ?? string.Empty;
		} },
		{ "BRSH", (a, b, byteLoc, labels) => {
			return OpTable?["BRBC"]("0", a, byteLoc, labels) ?? string.Empty;
		} },
		{ "BRTC", (a, b, byteLoc, labels) => {
			return OpTable?["BRBC"]("6", a, byteLoc, labels) ?? string.Empty;
		} },
		{ "BRTS", (a, b, byteLoc, labels) => {
			return OpTable?["BRBS"]("6", a, byteLoc, labels) ?? string.Empty;
		} },
		{ "BRVC", (a, b, byteLoc, labels) => {
			return OpTable?["BRBC"]("3", a, byteLoc, labels) ?? string.Empty;
		} },
		{ "BRVS", (a, b, byteLoc, labels) => {
			return OpTable?["BRBS"]("3", a, byteLoc, labels) ?? string.Empty;
		} },
		{ "BSET", (a, _, _, _) => {
			var r = 0x9408;
			var s = ConstValue(a, 0, 7);
			r |= (s & 0x7) << 4;
			return ZeroPad (r);
		} },
		{ "BST", (a, b, _, _) => {
			var r = 0xfa00 | DestRIndex(a) | (ConstValue(b, 0, 7) & 0x7);
			return ZeroPad (r);
		} },
		{ "CALL", (a, b, byteLoc, labels) => {
			var k = ConstOrLabel(a, labels);
			if (k == int.MinValue) {
				return new Func<Dictionary<string, int>, object> ((l) => OpTable?["CALL"](a, b, byteLoc, l) ?? new KeyValuePair<string, string>(string.Empty, string.Empty));
			}
			var r = 0x940e;
			k = ConstValue(k, 0, 0x400000) >> 1;
			var lk = k & 0xffff;
			var hk = (k >> 16) & 0x3f;
			r |= ((hk & 0x3e) << 3) | (hk & 1);
			return new KeyValuePair<string, string>(ZeroPad(r), ZeroPad(lk));
		} },
		{ "CBI", (a, b, _, _) => {
			var r = 0x9800 | (ConstValue(a, 0, 31) << 3) | ConstValue(b, 0, 7);
			return ZeroPad (r);
		} },
		{ "CRB", (a, b, byteLoc, l) => {
			var k = ConstValue (b);
			return OpTable?["ANDI"](a, (~k & 0xff).ToString (), byteLoc, l) ?? string.Empty;
		}},
		{ "CLC", (_, _, _, _) => {
			return "9488";
		}},
		{ "CLH", (_, _, _, _) => {
			return "94d8";
		}},
		{ "CLI", (_, _, _, _) => {
			return "94f8";
		}},
		{ "CLN", (_, _, _, _) => {
			return "94f8";
		}},
		{ "CLR", (a, _, byteLoc, l) => {
			return OpTable?["EOR"](a, a, byteLoc, l) ?? string.Empty;
		}},
		{ "CLS", (_, _, _, _) => {
			return "94c8";
		}},
		{ "CLT", (_, _, _, _) => {
			return "94e8";
		}},
		{ "CLV", (_, _, _, _) => {
			return "94b8";
		}},
		{ "CLZ", (_, _, _, _) => {
			return "9498";
		}},
		{ "COM", (a, _, _, _) => {
			var r = 0x9400 | DestRIndex(a);
			return ZeroPad (r);
		}},
		{ "CP", (a, b, _, _) => {
			var r = 0x1400 | DestRIndex(a) | SrcRIndex(b);
			return ZeroPad (r);
		}},
		{ "CPC", (a, b, _, _) => {
			var r = 0x0400 | DestRIndex(a) | SrcRIndex(b);
			return ZeroPad (r);
		}},
		{ "CPI", (a, b, _, _) => {
			var r = 0x3000 | (DestRIndex(a, 16, 31) & 0xf0);
			var k = ConstValue(b);
			r |= ((k & 0xf0) << 4) | (k & 0xf);
			return ZeroPad (r);
		}},
		{ "CPSE", (a, b, byteLoc, labels) => {
			var r = 0x1000 | DestRIndex(a) | SrcRIndex(b);
			return ZeroPad (r);
		}},
		{ "DEC", (a, _, _, _) => {
			var r = 0x940a | DestRIndex(a);
			return ZeroPad (r);
		}},
		{ "DES", (a, _, _, _) => {
			var r = 0x940b | (ConstValue(a, 0, 15) << 4);
			return ZeroPad (r);
		}},
		{ "EICALL", (_, _, _, _) => {
			return "9519";
		}},
		{ "EIJMP", (_, _, _, _) => {
			return "9419";
		}},
		{ "ELPM", (a, b, _, _) => {
			if (string.IsNullOrEmpty(a)) {
				return "95d8";
			}
			var r = 0x9000 | DestRIndex(a);
			switch (b) {
				case "Z":
					r |= 6;
					break;
				case "Z+":
					r |= 7;
					break;
				default:
					throw new Exception("Bad operand");
			}
			return ZeroPad (r);
		}},
		{ "EOR", (a, b, _, _) => {
			var r = 0x2400 | DestRIndex(a) | SrcRIndex(b);
			return ZeroPad (r);
		}},
		{ "FMUL", (a, b, _, _) => {
			var r = 0x0308 | DestRIndex(a) | SrcRIndex(b);
			return ZeroPad (r);
		}},
		{ "FMULS", (a, b, _, _) => {
			var r = 0x0380 | (DestRIndex(a, 16, 23) & 0x70) | (SrcRIndex(b, 16, 23) & 0x7);
			return ZeroPad (r);
		}},
		{ "FMULSU", (a, b, _, _) => {
			var r = 0x0388 | (DestRIndex(a, 16, 23) & 0x70) | (SrcRIndex(b, 16, 23) & 0x7);
			return ZeroPad (r);
		}},
		{ "ICALL", (_, _, _, _) => {
			return "9509";
		}},
		{ "IJMP", (_, _, _, _) => {
			return "9409";
		}},
		{ "IN", (a, b, _, _) => {
			var r = 0xb000 | DestRIndex(a);
			var A = ConstValue(b, 0, 63);
			r |= ((A & 0x30) << 5) | (A & 0x0f);
			return ZeroPad (r);
		}},
		{ "INC", (a, _, _, _) => {
			var r = 0x9403 | DestRIndex(a);
			return ZeroPad (r);
		}},
		{ "JMP", (a, b, byteLoc, labels) => {
			var k = ConstOrLabel(a, labels);
			if (k == int.MinValue) {
				return new Func<Dictionary<string, int>, object> ((l) => OpTable?["JMP"](a, b, byteLoc, l) ?? "xxxx");
			}
			var r = 0x940c;
			k = ConstValue(k, 0, 0x400000) >> 1;
			var lk = k & 0xffff;
			var hk = (k >> 16) & 0x3f;
			r |= ((hk & 0x3e) << 3) | (hk & 1);
			return new KeyValuePair<string, string>(ZeroPad(r), ZeroPad(lk));
		}},
		{ "LAC", (a, b, _, _) => {
			if (a != "Z") {
				throw new Exception(Z_OPERAND_ERROR);
			}
			var r = 0x9206 | DestRIndex(b);
			return ZeroPad (r);
		}},
		{ "LAS", (a, b, _, _) => {
			if (a != "Z") {
				throw new Exception(Z_OPERAND_ERROR);
			}
			var r = 0x9205 | DestRIndex(b);
			return ZeroPad (r);
		}},
		{ "LAT", (a, b, _, _) => {
			if (a != "Z") {
				throw new Exception(Z_OPERAND_ERROR);
			}
			var r = 0x9207 | DestRIndex(b);
			return ZeroPad (r);
		}},
		{ "LD", (a, b, _, _) => {
			var r = DestRIndex(a) | StldXyz(b);
			return ZeroPad (r);
		}},
		{ "LDD", (a, b, _, _) => {
			var r = DestRIndex(a) | StldYzQ(b);
			return ZeroPad (r);
		}},
		{ "LDI", (a, b, _, _) => {
			var r = 0xe000 | (DestRIndex(a, 16, 31) & 0xf0);
			var k = ConstValue(b);
			r |= ((k & 0xf0) << 4) | (k & 0xf);
			return ZeroPad (r);
		}},
		{ "LDS", (a, b, _, _) => {
			var k = ConstValue(b, 0, 65535);
			var r = 0x9000 | DestRIndex(a);
			return new KeyValuePair<string, string>(ZeroPad(r), ZeroPad(k));
		}},
		{ "LPM", (a, b, _, _) => {
			if (string.IsNullOrEmpty(a)) {
				return "95c8";
			}
			var r = 0x9000 | DestRIndex(a);
			switch (b) {
				case "Z":
					r |= 4;
					break;
				case "Z+":
					r |= 5;
					break;
				default:
					throw new Exception("Bad operand");
			}
			return ZeroPad (r);
		}},
		{ "LSL", (a, _, byteLoc, l) => {
			return OpTable?["ADD"](a, a, byteLoc, l) ?? string.Empty;
		}},
		{ "LSR", (a, _, _, _) => {
			var r = 0x9406 | DestRIndex(a);
			return ZeroPad (r);
		}},
		{ "MOV", (a, b, _, _) => {
			var r = 0x2c00 | DestRIndex(a) | SrcRIndex(b);
			return ZeroPad (r);
		}},
		{ "MOVW", (a, b, _, _) => {
			var r = 0x0100 | ((DestRIndex(a) >> 1) & 0xf0) | ((DestRIndex(b) >> 5) & 0xf);
			return ZeroPad (r);
		}},
		{ "MUL", (a, b, _, _) => {
			var r = 0x9c00 | DestRIndex(a) | SrcRIndex(b);
			return ZeroPad (r);
		}},
		{ "MULS", (a, b, _, _) => {
			var r = 0x0200 | (DestRIndex(a, 16, 31) & 0xf0) | (SrcRIndex(b, 16, 31) & 0xf);
			return ZeroPad (r);
		}},
		{ "MULSU", (a, b, _, _) => {
			var r = 0x0300 | (DestRIndex(a, 16, 23) & 0x70) | (SrcRIndex(b, 16, 23) & 0x7);
			return ZeroPad (r);
		}},
		{ "NEG", (a, _, _, _) => {
			var r = 0x9401 | DestRIndex(a);
			return ZeroPad (r);
		}},
		{ "NOP", (_, _, _, _) => {
			return "0000";
		}},
		{ "OR", (a, b, _, _) => {
			var r = 0x2800 | DestRIndex(a) | SrcRIndex(b);
			return ZeroPad (r);
		}},
		{ "ORI", (a, b, _, _) => {
			var r = 0x6000 | (DestRIndex(a, 16, 31) & 0xf0);
			var k = ConstValue(b);
			r |= ((k & 0xf0) << 4) | (k & 0xf);
			return ZeroPad (r);
		}},
		{ "OUT", (a, b, _, _) => {
			var r = 0xb800 | DestRIndex(b);
			var A = ConstValue(a, 0, 63);
			r |= ((A & 0x30) << 5) | (A & 0x0f);
			return ZeroPad (r);
		}},
		{ "POP", (a, _, _, _) => {
			var r = 0x900f | DestRIndex(a);
			return ZeroPad (r);
		}},
		{ "PUSH", (a, _, _, _) => {
			var r = 0x920f | DestRIndex(a);
			return ZeroPad (r);
		}},
		{ "RCALL", (a, b, byteLoc, labels) => {
			var k = ConstOrLabel(a, labels);
			if (k == int.MinValue) {
				return new Func<Dictionary<string, int>, string> ((l) => OpTable?["RCALL"](a, b, byteLoc, l) as string ?? string.Empty);
			}
			var r = 0xd000 | FitTwoC(ConstValue(k >> 1, -2048, 2047), 12);
			return ZeroPad (r);
		}},
		{ "RET", (_, _, _, _) => {
			return "9508";
		}},
		{ "RETI", (_, _, _, _) => {
			return "9518";
		}},
		{ "RJMP", (a, b, byteLoc, labels) => {
			var k = ConstOrLabel(a, labels, byteLoc + 2);
			if (k == int.MinValue) {
				return new Func<Dictionary<string, int>, string> ((l) => OpTable?["RJMP"](a, b, byteLoc, l) as string ?? string.Empty);
			}
			var r = 0xc000 | FitTwoC(ConstValue(k >> 1, -2048, 2047), 12);
			return ZeroPad (r);
		}},
		{ "ROL", (a, _, byteLoc, l) => {
			return OpTable?["ADC"](a, a, byteLoc, l) ?? string.Empty;
		}},
		{ "ROR", (a, _, _, _) => {
			var r = 0x9407 | DestRIndex(a);
			return ZeroPad (r);
		}},
		{ "SBC", (a, b, _, _) => {
			var r = 0x0800 | DestRIndex(a) | SrcRIndex(b);
			return ZeroPad (r);
		}},
		{ "SBCI", (a, b, _, _) => {
			var r = 0x4000 | (DestRIndex(a, 16, 31) & 0xf0);
			var k = ConstValue(b);
			r |= ((k & 0xf0) << 4) | (k & 0xf);
			return ZeroPad (r);
		}},
		{ "SBI", (a, b, _, _) => {
			var r = 0x9a00 | (ConstValue(a, 0, 31) << 3) | ConstValue(b, 0, 7);
			return ZeroPad (r);
		}},
		{ "SBIC", (a, b, _, _) => {
			var r = 0x9900 | (ConstValue(a, 0, 31) << 3) | ConstValue(b, 0, 7);
			return ZeroPad (r);
		}},
		{ "SBIS", (a, b, _, _) => {
			var r = 0x9b00 | (ConstValue(a, 0, 31) << 3) | ConstValue(b, 0, 7);
			return ZeroPad (r);
		}},
		{ "SBIW", (a, b, _, _) => {
			var r = 0x9700;
			var dm = RrIndexRegex.Match(a);
			if (!dm.Success) {
				throw new Exception("Rd must be 24, 26, 28, or 30");
			}
			var d = int.Parse(dm.Groups[1].Value);
			d = (d - 24) / 2;
			r |= (d & 0x3) << 4;
			var k = ConstValue(b, 0, 63);
			r |= ((k & 0x30) << 2) | (k & 0x0f);
			return ZeroPad (r);
		}},
		{ "SBR", (a, b, _, _) => {
			var r = 0x6000 | (DestRIndex(a, 16, 31) & 0xf0);
			var k = ConstValue(b);
			r |= ((k & 0xf0) << 4) | (k & 0xf);
			return ZeroPad (r);
		}},
		{ "SBRC", (a, b, _, _) => {
			var r = 0xfc00 | DestRIndex(a) | ConstValue(b, 0, 7);
			return ZeroPad (r);
		}},
		{ "SBRS", (a, b, _, _) => {
			var r = 0xfe00 | DestRIndex(a) | ConstValue(b, 0, 7);
			return ZeroPad (r);
		}},
		{ "SEC", (_, _, _, _) => {
			return SEFlag (0);
		}},
		{ "SEH", (_, _, _, _) => {
			return SEFlag (5);
		}},
		{ "SEI", (_, _, _, _) => {
			return SEFlag (7);
		}},
		{ "SEN", (_, _, _, _) => {
			return SEFlag (2);
		}},
		{ "SER", (a, _, _, _) => {
			var r = 0xef0f | (DestRIndex(a, 16, 31) & 0xf0);
			return ZeroPad (r);
		}},
		{ "SES", (_, _, _, _) => {
			return SEFlag (4);
		}},
		{ "SET", (_, _, _, _) => {
			return SEFlag (6);
		}},
		{ "SEV", (_, _, _, _) => {
			return SEFlag (3);
		}},
		{ "SEZ", (_, _, _, _) => {
			return SEFlag (6);
		}},
		{ "SLEEP", (_, _, _, _) => {
			return "9588";
		}},
		{ "SPM", (a, _, _, _) => {
			if (string.IsNullOrEmpty(a)) {
				return "95e8";
			}
			if (a != "Z+") {
				throw new Exception("Bad param to SPM");
			}
			return "95f8";
		}},
		{ "ST", (a, b, _, _) => {
			var r = 0x0200 | DestRIndex(b) | StldXyz(a);
			return ZeroPad (r);
		}},
		{ "STD", (a, b, _, _) => {
			var r = 0x0200 | DestRIndex(b) | StldYzQ(a);
			return ZeroPad (r);
		}},
		{ "STS", (a, b, _, _) => {
			var k = ConstValue(a, 0, 65535);
			var r = 0x9200 | DestRIndex(b);
			return new KeyValuePair<string, string>(ZeroPad(r), ZeroPad(k));
		}},
		{ "SUB", (a, b, _, _) => {
			var r = 0x1800 | DestRIndex(a) | SrcRIndex(b);
			return ZeroPad (r);
		}},
		{ "SUBI", (a, b, _, _) => {
			var r = 0x5000 | (DestRIndex(a, 16, 31) & 0xf0);
			var k = ConstValue(b);
			r |= ((k & 0xf0) << 4) | (k & 0xf);
			return ZeroPad (r);
		}},
		{ "SWAP", (a, _, _, _) => {
			var r = 0x9402 | DestRIndex(a);
			return ZeroPad (r);
		}},
		{ "TST", (a, _, byteLoc, l) => {
			return OpTable?["AND"](a, a, byteLoc, l) ?? string.Empty;
		}},
		{ "WDR", (_, _, _, _) => {
			return "95a8";
		}},
		{ "XCH", (a, b, _, _) => {
			if (a != "Z") {
				throw new Exception("First operand must be Z");
			}
			var r = 0x9204 | DestRIndex(b);
			return ZeroPad (r);
		}}
	};
	
	static readonly System.Text.RegularExpressions.Regex RIndexRegex = new System.Text.RegularExpressions.Regex(@"[Rr](\d{1,2})", RegexOptions.CultureInvariant, TimeSpan.FromSeconds (1));
	static readonly System.Text.RegularExpressions.Regex RrIndexRegex = new System.Text.RegularExpressions.Regex(@"[Rr](24|26|28|30)", RegexOptions.CultureInvariant, TimeSpan.FromSeconds (1));
	static readonly System.Text.RegularExpressions.Regex YzQRegex = new System.Text.RegularExpressions.Regex(@"([YZ])\+(\d+)", RegexOptions.CultureInvariant, TimeSpan.FromSeconds (1));
	static readonly System.Text.RegularExpressions.Regex CommentsRegex = new System.Text.RegularExpressions.Regex("[#;].*$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds (1));
	static readonly System.Text.RegularExpressions.Regex LabelRegex = new System.Text.RegularExpressions.Regex(@"^(\w+):", RegexOptions.CultureInvariant, TimeSpan.FromSeconds (1));
	static readonly System.Text.RegularExpressions.Regex CodeRegex = new System.Text.RegularExpressions.Regex(@"^\s*(\w+)(?:\s+([^,]+)(?:,\s*(\S+))?)?\s*$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds (1));
	
	private readonly LabelTable _labels = new LabelTable();
	private readonly List<string> _errors = new List<string>();
	private readonly List<LineTablePassOne> _lines = new List<LineTablePassOne>();
	
	public LabelTable Labels => _labels;
	public List<string> Errors => _errors;
	public List<LineTablePassOne> Lines => _lines;
	
	public byte[] Assemble (string input)
	{
		PassOne(input);
		return _errors.Count > 0 ? [] : PassTwo();
	}

	private void PassOne (string inputData)
	{
		var lines = inputData.Split('\n');
		LineTablePassOne lt;
		string res;
		string instruction;

		var replacements = new Dictionary<string, string> ();
		
		int byteOffset = 0;
		_labels.Clear();
		_errors.Clear();
		_lines.Clear();

		for (var idx = 0; idx < lines.Length; idx++) {
			res = lines[idx].Trim();
			if (string.IsNullOrEmpty(res)) {
				continue;
			}
			lt = new LineTablePassOne() {
				Text = res,
				Line = idx + 1,
				BytesOffset = 0
			};
			// Replace the comments with the comments regex
			res = CommentsRegex.Replace(res, string.Empty);
			if (string.IsNullOrEmpty (res)) {
				continue;
			}
			// Check for a label
			var match = LabelRegex.Match(res);
			if (match.Success) {
				_labels[match.Groups[1].Value] = byteOffset;
				// Remove the label from the line
				res = res.Substring(match.Length).Trim();
			}
			if (string.IsNullOrEmpty(res)) {
				continue;
			}
			// Check for a mnemonic line
			match = CodeRegex.Match(res);
			try {
				if (!match.Success) {
					throw new Exception("Invalid instruction");
				}

				if (!match.Groups[1].Success) {
					throw new Exception("No instruction found");
				}
				
				// Do Opcode
				instruction = match.Groups[1].Value.ToUpper();
				/* This switch is ok for just these three.
				* If ever to add more, then need to figure out how to merge all of the
				* mnemonics into the OPTABLE. (or build a seperate internal op table)
				*/
				switch (instruction) {
					case "_REPLACE":
						// Replace the instruction with the replacement
						if (match.Groups[2].Success) {
							replacements[match.Groups[2].Value.Trim ()] = match.Groups[3].Value.Trim ();
						}
						continue;
					case "_LOC":
						var num = int.TryParse (match.Groups[2].Value.Trim (), out var n) ? n : int.MinValue;
						if (num == int.MinValue) {
							throw new Exception("Invalid location");
						}
						if ((num & 0x1) != 0) {
							throw new Exception("Location must be even");
						}
						byteOffset = num;
						continue;
					case "_IW":
						var num2 = int.TryParse (match.Groups[2].Value.Trim (), out var n2) ? n2 : int.MinValue;
						if (num2 == int.MinValue) {
							throw new Exception("Invalid word");
						}
						lt.Bytes = ZeroPad(num2);
						lt.BytesOffset = byteOffset;
						byteOffset += 2;
						continue;
					default:
						break;
				}

				if (!OpTable.ContainsKey (instruction)) {
					throw new Exception("Invalid instruction");
				}
				
				// Do replacements on parameters
				var resMatch2 = match.Groups[2].Value.Trim ();
				var resMatch3 = match.Groups[3].Value.Trim ();
				if (replacements.TryGetValue (resMatch2, out var value)) {
					resMatch2 = value;
				}
				if (replacements.TryGetValue (resMatch3, out var value2)) {
					resMatch3 = value2;
				}
				
				var bytes = OpTable[instruction](resMatch2, resMatch3, byteOffset, _labels);
				lt.BytesOffset = byteOffset;
				switch (bytes) {
					case string:
					case Func<LabelTable, string>:
						byteOffset += 2;
						break;
					case Func<LabelTable, object>:
					case KeyValuePair<string, string>:
						byteOffset += 4;
						break;
					default:
						throw new Exception("Invalid return type");
				}
				
				lt.Bytes = bytes;
				_lines.Add(lt);
			}
			catch (Exception e) {
				_errors.Add ($"Line {idx}: {e.Message}");
			}
		}
	}

	private byte [] PassTwo ()
	{
		_errors.Clear();
		
		if (_lines.Count == 0) 
			return [];
		
		var lastElement = _lines[_lines.Count - 1];
		var byteSize = lastElement.BytesOffset + ElementSize(ref lastElement);
		var resultTable = new byte[byteSize];
		
		foreach (var lt in _lines) {
			try {
				// Look for entries that are functions and evaluate them
				if (lt.Bytes is Func<LabelTable, object> f) {
					lt.Bytes = f(_labels);
				}
				// TODO: Port this code
				// if (
				//   ltEntry.bytes instanceof Array &&
				//   ltEntry.bytes.length >= 1 &&
				//   typeof ltEntry.bytes[0] === 'function'
				// ) {
				//   /* a bit gross. FIXME */
				//   ltEntry.bytes = ltEntry.bytes[0](labels);
				// }
				
				// Copy the bytes out of line table into the result table
				switch (lt.Bytes) {
					case string s:
						resultTable[lt.BytesOffset + 1] = Convert.ToByte(s.Substring(0, 2), 16);
						resultTable[lt.BytesOffset] = Convert.ToByte(s.Substring(2, 2), 16);
						break;
					case KeyValuePair<string, string> p:
						var bi = lt.BytesOffset;
						string value;
						for (var j = 0; j < 2; j++, bi += 2) {
							if (j == 0) {
								value = p.Key;
							} else {
								value = p.Value;
							}
							resultTable[bi + 1] = Convert.ToByte(value.Substring(0, 2), 16);
							resultTable[bi] = Convert.ToByte(value.Substring(2, 2), 16);
						}
						break;
					default:
						throw new Exception("Invalid byte type");
				}
			}
			catch (Exception e) {
				_errors.Add ($"Line {lt.Line}: {e.Message}");
			}
		}
		return resultTable;
	}

	private int ElementSize (ref LineTablePassOne lt)
	{
		var bytes = lt.Bytes;
		if (bytes is string s) {
			return s.Length / 2;
		}
		if (bytes is Func<LabelTable, object> f) {
			var res = f(_labels);
			if (res is string s2) {
				lt.Bytes = s2;
				return s2.Length / 2;
			}
			if (res is KeyValuePair<string, string> p) {
				lt.Bytes = p.Key + p.Value;
				return 4;
			}
		}
		if (bytes is KeyValuePair<string, string>) {
			return 4;
		}
		return 2;
	}
	
	/// <summary>
	/// Get a destination register index from a string and shift it to
	/// where it is most commonly found. Also, make sure it is within
	/// the valid range.
	/// </summary>
	private static int DestRIndex (string r, int min = 0, int max = 31)
	{
		var match = RIndexRegex.Match(r);
		if (!match.Success) {
			throw new Exception($"Not a register: {r}");
		}
		
		var dest = int.Parse(match.Groups[1].Value);
		if (dest < min || dest > max) {
			throw new Exception($"Rd out of range: {min}<>{max}");
		}
		return (dest & 0x1f) << 4;
	}

	/// <summary>
	/// Get a source register index from a string and shift it to where
	/// it is most commonly found. Also, make sure it is within the valid
	/// range.
	/// </summary>
	private static int SrcRIndex (string r, int min = 0, int max = 31)
	{
		var match = RIndexRegex.Match(r);
		if (!match.Success) {
			throw new Exception($"Not a register: {r}");
		}
		var dest = int.Parse(match.Groups[1].Value);
		if (dest < min || dest > max) {
			throw new Exception($"Rd out of range: {r}");
		}
		var s = dest & 0xf;
		s |= ((dest >> 4) & 1) << 9;
		return s;
	}

	/// <summary>
	/// Get a constant value and check that it is in range.
	/// </summary>
	private static int ConstValue (string value, int min = 0, int max = 255)
	{
		int d;
		if (value.Length > 1 && value[0] == '0' && value[1] == 'x') {
			d = int.Parse (value.Substring (2), System.Globalization.NumberStyles.HexNumber);
		}
		else if (value.Length > 1 && value[0] == '0' && value[1] == 'b') {
			d = Convert.ToInt32(value.Substring(2), 2);
		}
		else
			d = int.Parse(value);
		
		if (d < min || d > max) {
			throw new Exception($"[Ks] out of range: {min}<{value}<{max}");
		}
		return d;
	}

	/// <summary>
	/// Get a constant value and check that it is in range.
	/// </summary>
	private static int ConstValue (int r, int min = 0, int max = 255)
	{
		if (r < min || r > max) {
			throw new Exception($"[Ks] out of range: {min}<{r}<{max}");
		}
		return r;
	}

	/// <summary>
	/// Fit a twos-complement number into the specific bit count.
	/// </summary>
	private static int FitTwoC (int r, int bits)
	{
		switch (bits) {
			case < 2:
				throw new Exception("Need at least 2 bits to be signed.");
			case > 16:
				throw new Exception("FitTwoC only works on 16bit numbers for now.");
		}
		if (Math.Abs(r) > Math.Pow(2, bits - 1)) {
			throw new Exception($"Not enough bits for number. ({r}, {bits})");
		}
		if (r < 0) {
			r = 0xffff + r + 1;
		}
		var mask = 0xffff >> (16 - bits);
		return r & mask;
	}

	/// <summary>
	/// Determin if input is an address or label and lookup if required.
	/// If label that doesn't exist, return NaN. If offset is not 0,
	/// convert from absolute address to relative.
	/// </summary>
	private static int ConstOrLabel (object value, Dictionary<string, int> labels, int offset = 0)
	{
		if (value is string c) {
			if (labels.ContainsKey(c)) {
				return labels[c] - offset;
			}
			if (c.Length > 1 && c[0] == '0' && c[1] == 'x') {
				return int.Parse (c.Substring (2), System.Globalization.NumberStyles.HexNumber);
			}
			if (c.Length > 1 && c[0] == '0' && c[1] == 'b') {
				return Convert.ToInt32(c.Substring(2), 2);
			}
			if (int.TryParse(c, out var d)) {
				return d;
			}
			return int.MinValue;
		}
		return (int)value;
	}

	/// <summary>
	/// Convert number to hex and left pad it
	/// </summary>
	private static string ZeroPad (object rIn, int len = 4)
	{
		int r;
		if (rIn is string s) {
			r = int.Parse(s);
		} else {
			r = (int)rIn;
		}
		var rStr = r.ToString("x");
		var @base = new string('0', len);
		var t = @base.Substring (0, len - rStr.Length) + rStr;
		return t;
	}

	/// <summary>
	/// Get an Indirect Address Register and shift it to where it is commonly found.
	/// </summary>
	private static int StldXyz (string xyz)
	{
		switch (xyz) {
			case "X":
				return 0x900c;
			case "X+":
				return 0x900d;
			case "-X":
				return 0x900e;
			case "Y":
				return 0x8008;
			case "Y+":
				return 0x9009;
			case "-Y":
				return 0x900a;
			case "Z":
				return 0x8000;
			case "Z+":
				return 0x9001;
			case "-Z":
				return 0x9002;
			default:
				throw new Exception("Not -?[XYZ]\\+?");
		}
	}

	/// <summary>
	/// Get an Indirect Address Register with displacement and shift it to where it is commonly found.
	/// </summary>
	private static int StldYzQ (string yzq)
	{
		var d = YzQRegex.Match(yzq);
		var r = 0x8000;
		if (!d.Success) {
			throw new Exception("Invalid arguments");
		}
		switch (d.Groups[1].Value) {
			case "Y":
				r |= 0x8;
				break;
			case "Z":
				break;
			default:
				throw new Exception("Not Y or Z with q");
		}
		var q = int.Parse(d.Groups[2].Value);
		if (q < 0 || q > 64) {
			throw new Exception("q is out of range");
		}
		r |= ((q & 0x20) << 8) | ((q & 0x18) << 7) | (q & 0x7);
		return r;
	}

	private static string SEFlag (int a)
	{
		return ZeroPad (0x9408 | (ConstValue(a, 0, 7) << 4));
	}
}

public class LineTablePassOne
{
	public int Line { get; set; }
	public object Bytes { get; set; }
	public string Text { get; set; }
	public int BytesOffset { get; set; }
}

public class LineTable : LineTablePassOne
{
	public new string Bytes { get; set; }
}
