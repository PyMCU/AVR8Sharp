#nullable enable
using LabelTable = System.Collections.Generic.Dictionary<string, int>;
using OpCodeHandler = System.Func<string, string, int, System.Collections.Generic.Dictionary<string, int>, object>;

namespace AVR8Sharp.Core.Utils;

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
			int regNum = Encoders.EncoderHelpers.TryParseRegister(a.AsSpan());
			if (regNum < 0 || (regNum != 24 && regNum != 26 && regNum != 28 && regNum != 30)) {
				throw new Exception("Rd must be 24, 26, 28, or 30");
			}
			var d = (regNum - 24) / 2;
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
			return "94a8";
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
			var k = ConstOrLabel(a, labels, byteLoc + 2);
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
			int regNum = Encoders.EncoderHelpers.TryParseRegister(a.AsSpan());
			if (regNum < 0 || (regNum != 24 && regNum != 26 && regNum != 28 && regNum != 30)) {
				throw new Exception("Rd must be 24, 26, 28, or 30");
			}
			var d = (regNum - 24) / 2;
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
			return SEFlag (1);
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
	
	// Thread-local symbol table used by static helper methods (ConstValue, ConstOrLabel)
	[ThreadStatic]
	private static SymbolTable? _currentSymbolTable;

	// Thread-local byte offset of the instruction currently being encoded.
	// Backs the GNU '.' location-counter operand in ConstOrLabel.
	[ThreadStatic]
	private static int _currentInstrByteOffset;

	private readonly LabelTable _labels = new LabelTable();
	private readonly List<string> _errors = new List<string>();
	private readonly List<LineTablePassOne> _lines = new List<LineTablePassOne>();
	private SymbolTable _symbolTable = new SymbolTable();
	private readonly Func<string, string>? _fileResolver;
	private readonly string? _deviceName;

	// Macro storage: name → (paramNames, bodyLines)
	private Dictionary<string, (List<string> Params, List<string> Body)> _macros
		= new Dictionary<string, (List<string> Params, List<string> Body)>(StringComparer.OrdinalIgnoreCase);

	public AvrAssembler() { }

	/// <summary>
	/// Creates an assembler with optional file resolver and device name.
	/// When a device name is specified (e.g. "ATmega328P"), hardware register
	/// symbols are pre-loaded into the symbol table.
	/// </summary>
	public AvrAssembler(Func<string, string>? fileResolver = null, string? deviceName = null)
	{
		_fileResolver = fileResolver;
		_deviceName = deviceName;
	}

	public LabelTable Labels => _labels;
	public List<string> Errors => _errors;
	public List<LineTablePassOne> Lines => _lines;
	
	public byte[] Assemble (string input)
	{
		PassOne(input);
		return _errors.Count > 0 ? [] : PassTwo();
	}

	/// <summary>
	/// Assemble and report success explicitly. Returns false when any error was
	/// produced (in which case <paramref name="bytes"/> is empty) and exposes the
	/// collected messages, so callers cannot mistake an error for empty output.
	/// </summary>
	public bool TryAssemble (string input, out byte[] bytes, out IReadOnlyList<string> errors)
	{
		bytes = Assemble(input);
		errors = _errors.AsReadOnly();
		return _errors.Count == 0;
	}

	/// <summary>
	/// Assemble, throwing <see cref="AssemblerException"/> if any error was produced
	/// instead of silently returning an empty array.
	/// </summary>
	public byte[] AssembleOrThrow (string input)
	{
		var bytes = Assemble(input);
		if (_errors.Count > 0)
			throw new AssemblerException(_errors.ToList());
		return bytes;
	}

	/// <summary>
	/// Assemble multiple source files together.
	/// Pass 1: Scan all files for .global exports → build combined symbol table.
	/// Pass 2: Assemble each file sequentially with the combined symbol table.
	/// This allows cross-file symbol references without a linker.
	/// </summary>
	public byte[] AssembleMultiFile(string[] sources)
	{
		// ---- Pass 1: collect all .global exported symbols from all files ----
		var globalSymbols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		// First, assemble each file individually to discover symbols
		foreach (var source in sources)
		{
			var scanner = new AvrAssembler(_fileResolver, _deviceName);
			scanner.Assemble(source);
			// Collect any labels/symbols from this file
			foreach (var (name, value) in scanner.Labels)
			{
				globalSymbols[name] = value;
			}
		}

		// ---- Pass 2: assemble all files concatenated, with combined symbol table ----
		var combined = string.Join("\n", sources);
		_labels.Clear();
		_errors.Clear();
		_lines.Clear();

		// Pre-seed labels from the global scan
		foreach (var (name, value) in globalSymbols)
		{
			_labels[name] = value;
		}

		PassOne(combined);
		return _errors.Count > 0 ? [] : PassTwo();
	}

	// -----------------------------------------------------------------------
	// Strip C-style /* ... */ block comments (avr-gcc interleaves them).
	// Newlines inside a block are preserved so reported line numbers stay accurate.
	// String literals are respected so a "/*" inside a quoted string is left intact.
	// -----------------------------------------------------------------------
	private static string StripBlockComments(string input)
	{
		if (!input.Contains("/*")) return input;
		var sb = new System.Text.StringBuilder(input.Length);
		bool inString = false, inBlock = false, escaped = false;
		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];
			if (inBlock)
			{
				if (c == '*' && i + 1 < input.Length && input[i + 1] == '/') { inBlock = false; i++; }
				else if (c == '\n') sb.Append('\n');
				continue;
			}
			if (inString)
			{
				sb.Append(c);
				if (escaped) escaped = false;
				else if (c == '\\') escaped = true;
				else if (c == '"') inString = false;
				continue;
			}
			if (c == '"') { inString = true; sb.Append(c); continue; }
			if (c == '/' && i + 1 < input.Length && input[i + 1] == '*') { inBlock = true; i++; continue; }
			sb.Append(c);
		}
		return sb.ToString();
	}

	// -----------------------------------------------------------------------
	// Include expansion: recursively replaces .include lines with file content
	// -----------------------------------------------------------------------
	private List<string> ExpandIncludes(IEnumerable<string> rawLines, int depth = 0)
	{
		var result = new List<string>();
		foreach (var line in rawLines)
		{
			var trimmed = line.Trim();
			var stripped = LineParser.StripComments(trimmed).Trim();
			if (stripped.StartsWith(".include", StringComparison.OrdinalIgnoreCase))
			{
				var rest = stripped.Substring(".include".Length).Trim();
				var filename = rest.Trim('"', '\'');
				if (_fileResolver == null)
				{
					result.Add(line); // can't expand, will error at parse time
				}
				else if (depth >= 32)
				{
					_errors.Add("Error: .include depth limit exceeded");
				}
				else
				{
					try
					{
						var content = _fileResolver(filename);
						result.AddRange(ExpandIncludes(content.Split('\n'), depth + 1));
					}
					catch (Exception ex)
					{
						_errors.Add($"Error: .include '{filename}': {ex.Message}");
					}
				}
			}
			else
			{
				result.Add(line);
			}
		}
		return result;
	}

	// -----------------------------------------------------------------------
	// PASS ONE
	// -----------------------------------------------------------------------
	private void PassOne (string inputData)
	{
		// Strip /* ... */ block comments (avr-gcc interleaves them) before line splitting.
		inputData = StripBlockComments(inputData);

		// Expand .include directives first
		var rawLines = inputData.Split('\n');
		var allLines = ExpandIncludes(rawLines);

		var replacements = new Dictionary<string, string> ();
		_symbolTable = new SymbolTable();
		_macros = new Dictionary<string, (List<string> Params, List<string> Body)>(StringComparer.OrdinalIgnoreCase);

		// Pre-load device definitions if a device was specified
		if (_deviceName != null)
			LoadDeviceSymbols(_deviceName);

		// Make symbol table available to static helpers
		_currentSymbolTable = _symbolTable;

		int byteOffset = 0;
		_labels.Clear();
		_errors.Clear();
		_lines.Clear();

		// Conditional assembly state
		bool assembling = true;
		var condStack = new Stack<bool>();

		// Macro recording state
		string? recordingMacro = null;
		List<string>? macroParams = null;
		List<string>? macroBody = null;

		// Use a list + index so we can inject macro-expanded lines
		var lineList = allLines;
		int lineCount = lineList.Count;

		for (var idx = 0; idx < lineCount; idx++) {
			var rawLine = lineList[idx];

			// Parse line using recursive-descent scanner (replaces all regex matching)
			var parsed = LineParser.Parse(rawLine);

			// Skip empty / comment-only lines (but not label-only lines)
			if (parsed.IsEmpty && parsed.Label == null)
				continue;

			// Effective keyword for macro/conditional checks
			string? keyword = parsed.IsDirective ? parsed.DirectiveName : parsed.Mnemonic;

			// ----------------------------------------------------------------
			// Macro body recording (must happen before conditional skip)
			// ----------------------------------------------------------------
			if (recordingMacro != null)
			{
				if (keyword is "ENDMACRO" or "ENDM")
				{
					_macros[recordingMacro] = (macroParams!, macroBody!);
					recordingMacro = null;
					macroParams = null;
					macroBody = null;
				}
				else
				{
					macroBody!.Add(LineParser.StripComments(rawLine.Trim()));
				}
				continue;
			}

			// ----------------------------------------------------------------
			// Conditional assembly directives (must process even in false branch)
			// ----------------------------------------------------------------
			if (keyword is "IF" or "IFDEF" or "IFNDEF" or "ELSEIF" or "ELSE" or "ENDIF")
			{
				string condArgs = parsed.IsDirective ? parsed.DirectiveArgs
					: CombineOperands(parsed.Operand1, parsed.Operand2);
				ProcessConditional(keyword, condArgs, byteOffset, ref assembling, condStack);
				continue;
			}

			// Skip lines when in a false conditional branch
			if (!assembling) continue;

			// ----------------------------------------------------------------
			// Label handling
			// ----------------------------------------------------------------
			if (parsed.Label != null)
			{
				_labels[parsed.Label] = byteOffset;
				_symbolTable.Set(parsed.Label, byteOffset);
			}

			// ----------------------------------------------------------------
			// GNU-style symbol assignment: `name = expr` (no .equ/.set keyword).
			// avr-gcc emits these for register aliases and helper symbols
			// (e.g. `__SP_H__ = 0x3e`, `.L__stack_usage = 2`). Treated as a mutable .set.
			// ----------------------------------------------------------------
			if (parsed.Label == null)
			{
				var assignLine = LineParser.StripComments(rawLine).Trim();
				if (LineParser.TryParseAssignment(assignLine, out _, out _))
				{
					ProcessSymbolDef(assignLine, idx, byteOffset, isImmutable: false);
					continue;
				}
			}

			// ----------------------------------------------------------------
			// Dot-directives (.equ, .org, .byte, .macro, etc.)
			// ----------------------------------------------------------------
			if (parsed.IsDirective)
			{
				var dirName = parsed.DirectiveName;
				var dirArgs = parsed.DirectiveArgs;

				// Macro start
				if (dirName == "MACRO")
				{
					var parts = dirArgs.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
					recordingMacro = parts.Length > 0 ? parts[0] : "anon";
					macroParams = parts.Skip(1).Select(p => p.Trim()).ToList();
					macroBody = new List<string>();
					continue;
				}
				if (dirName == "ENDMACRO" || dirName == "ENDM")
				{
					_errors.Add($"Line {idx + 1}: .endm without .macro");
					continue;
				}

				// Include (should have been expanded, but handle gracefully)
				if (dirName == "INCLUDE")
				{
					if (_fileResolver == null)
						_errors.Add($"Line {idx + 1}: .include requires a file resolver");
					continue;
				}

				// Segment directives (simple)
				if (dirName == "CSEG" || dirName == "DSEG" || dirName == "ESEG")
					continue; // segment switching not fully implemented; cseg is default

				// Symbol definitions
				if (dirName == "EQU")
				{
					ProcessSymbolDef(dirArgs, idx, byteOffset, isImmutable: true);
					continue;
				}
				if (dirName == "SET")
				{
					ProcessSymbolDef(dirArgs, idx, byteOffset, isImmutable: false);
					continue;
				}
				if (dirName == "DEF")
				{
					ProcessDefDirective(dirArgs, idx);
					continue;
				}

				// Location / origin
				if (dirName == "ORG")
				{
					int? orgVal = ExpressionEvaluator.TryEvaluate(dirArgs, _symbolTable, byteOffset);
					if (orgVal == null) { _errors.Add($"Line {idx + 1}: Cannot evaluate .org expression"); continue; }
					if ((orgVal.Value & 1) != 0) { _errors.Add($"Line {idx + 1}: .org value must be even"); continue; }
					byteOffset = orgVal.Value;
					continue;
				}

				// Data directives — emit raw bytes into line table
				if (dirName == "BYTE" || dirName == "DB")
				{
					EmitDataBytes(dirArgs, idx, ref byteOffset, rawLine);
					continue;
				}
				if (dirName == "WORD" || dirName == "DW")
				{
					EmitDataWords(dirArgs, idx, ref byteOffset, rawLine);
					continue;
				}
				if (dirName == "DWORD")
				{
					EmitDataDwords(dirArgs, idx, ref byteOffset, rawLine);
					continue;
				}
				if (dirName == "ASCII")
				{
					EmitDataAscii(dirArgs, idx, ref byteOffset, rawLine, nullTerminated: false);
					continue;
				}
				if (dirName == "ASCIZ" || dirName == "STRING")
				{
					EmitDataAscii(dirArgs, idx, ref byteOffset, rawLine, nullTerminated: true);
					continue;
				}

				// External reference directives (informational only — no linker)
				if (dirName == "GLOBAL" || dirName == "EXTERN")
					continue;

				// Device definition loading
				if (dirName == "DEVICE")
				{
					LoadDeviceSymbols(dirArgs.Trim(), idx);
					continue;
				}

				// GNU/avr-gcc metadata directives: accepted as no-ops. AVR8Sharp emits a
				// flat code image, so section/symbol/debug metadata carries no payload here.
				// (Section switching is intentionally ignored — single-section output.)
				if (dirName is "FILE" or "SECTION" or "TEXT" or "DATA" or "TYPE" or "SIZE"
					or "IDENT" or "WEAK" or "GLOBL" or "LOCAL" or "ALIGN" or "P2ALIGN"
					or "BALIGN" or "LOC" or "FUNC" or "ENDFUNC"
					or "CFI_STARTPROC" or "CFI_ENDPROC")
					continue;

				// Unknown dot-directive: fall through to error
				_errors.Add($"Line {idx + 1}: Unknown directive: .{dirName}");
				continue;
			}

			// ----------------------------------------------------------------
			// Label-only line (no instruction after label)
			// ----------------------------------------------------------------
			if (parsed.Mnemonic == null)
				continue;

			// ----------------------------------------------------------------
			// Instruction / macro invocation
			// ----------------------------------------------------------------
			var instruction = parsed.Mnemonic;
			var lt = new LineTablePassOne() {
				Text = rawLine.Trim(),
				Line = idx + 1,
				BytesOffset = 0
			};

			// Expose the current instruction address to ConstOrLabel for the GNU '.' operand.
			_currentInstrByteOffset = byteOffset;

			try {
				switch (instruction) {
					case "_REPLACE":
						if (!string.IsNullOrEmpty(parsed.Operand1)) {
							replacements[parsed.Operand1] = parsed.Operand2;
						}
						continue;
					case "_LOC":
						var num = int.TryParse (parsed.Operand1, out var n) ? n : int.MinValue;
						if (num == int.MinValue) {
							throw new Exception("Invalid location");
						}
						if ((num & 0x1) != 0) {
							throw new Exception("Location must be even");
						}
						byteOffset = num;
						continue;
					case "_IW":
						var num2 = int.TryParse (parsed.Operand1, out var n2) ? n2 : int.MinValue;
						if (num2 == int.MinValue) {
							throw new Exception("Invalid word");
						}
						lt.Bytes = ZeroPad(num2);
						lt.BytesOffset = byteOffset;
						byteOffset += 2;
						_lines.Add(lt);
						continue;
					default:
						break;
				}

				// Try macro expansion
				if (_macros.TryGetValue(instruction, out var macro))
				{
					var macroArgs = CombineOperands(parsed.Operand1, parsed.Operand2);
					var expandedLines = ExpandMacro(instruction, macroArgs, macro);
					// Insert expanded lines immediately after current position
					lineList = lineList.Take(idx + 1).Concat(expandedLines).Concat(lineList.Skip(idx + 1)).ToList();
					lineCount = lineList.Count;
					continue;
				}

				if (!OpTable.ContainsKey (instruction)) {
					throw new Exception("Invalid instruction");
				}
				
				// Apply replacements and symbol substitution on parameters
				var resMatch2 = ApplyReplacements(parsed.Operand1, replacements);
				var resMatch3 = ApplyReplacements(parsed.Operand2, replacements);
				
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
				_errors.Add ($"Line {idx + 1}: {e.Message}");
			}
		}

		_currentSymbolTable = null;
	}

	// -----------------------------------------------------------------------
	// Device definition loading
	// -----------------------------------------------------------------------
	private void LoadDeviceSymbols(string deviceName, int lineIdx = -1)
	{
		var def = DeviceDefinitions.Get(deviceName);
		if (def == null)
		{
			if (lineIdx >= 0)
				_errors.Add($"Line {lineIdx + 1}: Unknown device: {deviceName}");
			return;
		}

		foreach (var (name, value) in def.Symbols)
		{
			_symbolTable.Set(name, value);
		}
	}

	// -----------------------------------------------------------------------
	// Conditional assembly helpers
	// -----------------------------------------------------------------------
	private void ProcessConditional(string directive, string args, int byteOffset, ref bool assembling, Stack<bool> condStack)
	{
		switch (directive)
		{
			case "IF":
				condStack.Push(assembling);
				if (assembling)
				{
					var val = ExpressionEvaluator.TryEvaluate(args, _symbolTable, byteOffset);
					assembling = val.HasValue && val.Value != 0;
				}
				else assembling = false;
				break;
			case "IFDEF":
				condStack.Push(assembling);
				assembling = assembling && _symbolTable.ContainsKey(args.Trim());
				break;
			case "IFNDEF":
				condStack.Push(assembling);
				assembling = assembling && !_symbolTable.ContainsKey(args.Trim());
				break;
			case "ELSEIF":
				if (condStack.Count > 0)
				{
					bool parentActive = condStack.Peek();
					bool wasAssembling = assembling;
					if (assembling)
						assembling = false; // was assembling if-branch, now skip
					else if (parentActive)
					{
						var val = ExpressionEvaluator.TryEvaluate(args, _symbolTable, byteOffset);
						assembling = val.HasValue && val.Value != 0;
					}
				}
				break;
			case "ELSE":
				if (condStack.Count > 0)
				{
					bool parentActive = condStack.Peek();
					assembling = parentActive && !assembling;
				}
				break;
			case "ENDIF":
				if (condStack.Count > 0)
					assembling = condStack.Pop();
				break;
		}
	}

	// -----------------------------------------------------------------------
	// Symbol definition helpers
	// -----------------------------------------------------------------------
	private void ProcessSymbolDef(string args, int lineIdx, int byteOffset, bool isImmutable)
	{
		var parts = args.Split('=', 2);
		if (parts.Length != 2) { _errors.Add($"Line {lineIdx + 1}: .equ/.set requires NAME = VALUE"); return; }
		var name = parts[0].Trim();
		var valStr = parts[1].Trim();
		var val = ExpressionEvaluator.TryEvaluate(valStr, _symbolTable, byteOffset);
		if (val == null) { _errors.Add($"Line {lineIdx + 1}: Cannot evaluate expression for '{name}'"); return; }
		try
		{
			if (isImmutable) _symbolTable.DefineConst(name, val.Value);
			else _symbolTable.DefineVar(name, val.Value);
		}
		catch (Exception ex) { _errors.Add($"Line {lineIdx + 1}: {ex.Message}"); }
	}

	private void ProcessDefDirective(string args, int lineIdx)
	{
		var parts = args.Split('=', 2);
		if (parts.Length != 2) { _errors.Add($"Line {lineIdx + 1}: .def requires ALIAS = rN"); return; }
		var alias = parts[0].Trim();
		var regStr = parts[1].Trim();
		int n = Encoders.EncoderHelpers.TryParseRegister(regStr.AsSpan());
		if (n < 0) { _errors.Add($"Line {lineIdx + 1}: .def: right side must be a register, got '{regStr}'"); return; }
		try { _symbolTable.DefineRegisterAlias(alias, n); }
		catch (Exception ex) { _errors.Add($"Line {lineIdx + 1}: {ex.Message}"); }
	}

	// -----------------------------------------------------------------------
	// Data-emission helpers
	// -----------------------------------------------------------------------
	private void EmitDataBytes(string args, int lineIdx, ref int byteOffset, string rawLine)
	{
		var parts = SplitDirectiveArgs(args);
		var bytes = new List<byte>();
		foreach (var part in parts)
		{
			var p = part.Trim();
			if (p.StartsWith('"') || p.StartsWith('\''))
			{
				var s = p.Trim('"', '\'');
				bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(s));
				continue;
			}
			var val = ExpressionEvaluator.TryEvaluate(p, _symbolTable, byteOffset);
			if (val == null) { _errors.Add($"Line {lineIdx + 1}: Cannot evaluate .byte expression: {p}"); return; }
			bytes.Add((byte)(val.Value & 0xFF));
		}
		if (bytes.Count > 0)
		{
			var lt = new LineTablePassOne { Text = rawLine.Trim(), Line = lineIdx + 1, BytesOffset = byteOffset, Bytes = bytes.ToArray() };
			_lines.Add(lt);
			byteOffset += bytes.Count;
		}
	}

	private void EmitDataWords(string args, int lineIdx, ref int byteOffset, string rawLine)
	{
		var parts = SplitDirectiveArgs(args);
		var wordBytes = new List<byte>();
		foreach (var part in parts)
		{
			var val = ExpressionEvaluator.TryEvaluate(part.Trim(), _symbolTable, byteOffset);
			if (val == null) { _errors.Add($"Line {lineIdx + 1}: Cannot evaluate .word expression: {part.Trim()}"); return; }
			wordBytes.Add((byte)(val.Value & 0xFF));
			wordBytes.Add((byte)((val.Value >> 8) & 0xFF));
		}
		if (wordBytes.Count > 0)
		{
			var lt = new LineTablePassOne { Text = rawLine.Trim(), Line = lineIdx + 1, BytesOffset = byteOffset, Bytes = wordBytes.ToArray() };
			_lines.Add(lt);
			byteOffset += wordBytes.Count;
		}
	}

	private void EmitDataDwords(string args, int lineIdx, ref int byteOffset, string rawLine)
	{
		var parts = SplitDirectiveArgs(args);
		var dwordBytes = new List<byte>();
		foreach (var part in parts)
		{
			var val = ExpressionEvaluator.TryEvaluate(part.Trim(), _symbolTable, byteOffset);
			if (val == null) { _errors.Add($"Line {lineIdx + 1}: Cannot evaluate .dword expression: {part.Trim()}"); return; }
			dwordBytes.Add((byte)(val.Value & 0xFF));
			dwordBytes.Add((byte)((val.Value >> 8) & 0xFF));
			dwordBytes.Add((byte)((val.Value >> 16) & 0xFF));
			dwordBytes.Add((byte)((val.Value >> 24) & 0xFF));
		}
		if (dwordBytes.Count > 0)
		{
			var lt = new LineTablePassOne { Text = rawLine.Trim(), Line = lineIdx + 1, BytesOffset = byteOffset, Bytes = dwordBytes.ToArray() };
			_lines.Add(lt);
			byteOffset += dwordBytes.Count;
		}
	}

	private void EmitDataAscii(string args, int lineIdx, ref int byteOffset, string rawLine, bool nullTerminated)
	{
		var s = args.Trim();
		if (s.StartsWith('"') && s.EndsWith('"') && s.Length >= 2)
			s = s[1..^1];
		var rawBytes = System.Text.Encoding.ASCII.GetBytes(s);
		var bytes = nullTerminated ? rawBytes.Append((byte)0).ToArray() : rawBytes;
		var lt = new LineTablePassOne { Text = rawLine.Trim(), Line = lineIdx + 1, BytesOffset = byteOffset, Bytes = bytes };
		_lines.Add(lt);
		byteOffset += bytes.Length;
	}

	// -----------------------------------------------------------------------
	// Macro expansion
	// -----------------------------------------------------------------------
	private List<string> ExpandMacro(string name, string args, (List<string> Params, List<string> Body) macro, int depth = 0)
	{
		if (depth > 8) throw new Exception($"Macro recursion limit exceeded in '{name}'");
		var argValues = args.Length == 0
			? Array.Empty<string>()
			: args.Split(',').Select(a => a.Trim()).ToArray();
		var expanded = new List<string>();
		foreach (var line in macro.Body)
		{
			var expLine = line;
			for (int i = 0; i < macro.Params.Count; i++)
			{
				if (i < argValues.Length)
				{
					expLine = expLine.Replace(@"\" + macro.Params[i], argValues[i]);
					expLine = expLine.Replace("@" + i, argValues[i]);
				}
			}
			expanded.Add(expLine);
		}
		return expanded;
	}

	// -----------------------------------------------------------------------
	// Small string helpers
	// -----------------------------------------------------------------------
	private static string CombineOperands(string op1, string op2)
	{
		if (string.IsNullOrEmpty(op2)) return op1;
		return op1 + ", " + op2;
	}

	private string ApplyReplacements(string value, Dictionary<string, string> replacements)
	{
		if (replacements.TryGetValue(value, out var r)) return r;
		// Substitute .def register aliases so DestRIndex / SrcRIndex can recognise them
		if (_symbolTable.IsRegisterAlias(value) && _symbolTable.TryGetValue(value, out var regNum))
			return "r" + regNum;
		return value;
	}

	private static List<string> SplitDirectiveArgs(string args)
	{
		var parts = new List<string>();
		bool inStr = false;
		int depth = 0;
		var cur = new System.Text.StringBuilder();
		foreach (char c in args)
		{
			if (c == '"') inStr = !inStr;
			if (!inStr)
			{
				if (c == '(') depth++;
				else if (c == ')') depth--;
				else if (c == ',' && depth == 0) { parts.Add(cur.ToString()); cur.Clear(); continue; }
			}
			cur.Append(c);
		}
		if (cur.Length > 0) parts.Add(cur.ToString());
		return parts;
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
					case byte[] rawBytes:
						Array.Copy(rawBytes, 0, resultTable, lt.BytesOffset, rawBytes.Length);
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
		if (bytes is byte[] ba) {
			return ba.Length;
		}
		return 2;
	}
	
	/// <summary>
	/// Get a destination register index from a string and shift it to
	/// where it is most commonly found. Also, make sure it is within
	/// the valid range.
	/// </summary>
	/// <summary>
	/// Resolve a register operand to its number 0..31. Accepts the rN form and,
	/// as a fallback, a numeric symbol used in register position — e.g. avr-gcc's
	/// <c>__zero_reg__ = 1</c> / <c>__tmp_reg__ = 0</c>. Returns -1 if not a register.
	/// </summary>
	private static int ResolveRegister (string r)
	{
		int n = Encoders.EncoderHelpers.TryParseRegister(r.AsSpan());
		if (n >= 0) return n;
		var st = _currentSymbolTable;
		if (st != null && st.TryGetValue(r, out var v) && v >= 0 && v <= 31)
			return v;
		return -1;
	}

	private static int DestRIndex (string r, int min = 0, int max = 31)
	{
		int dest = ResolveRegister(r);
		if (dest < 0) {
			throw new Exception($"Not a register: {r}");
		}
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
		int dest = ResolveRegister(r);
		if (dest < 0) {
			throw new Exception($"Not a register: {r}");
		}
		if (dest < min || dest > max) {
			throw new Exception($"Rd out of range: {r}");
		}
		var s = dest & 0xf;
		s |= ((dest >> 4) & 1) << 9;
		return s;
	}

	/// <summary>
	/// Get a constant value and check that it is in range.
	/// Falls back to ExpressionEvaluator for complex expressions.
	/// </summary>
	private static int ConstValue (string value, int min = 0, int max = 255)
	{
		int d;
		// Try simple literal parsing first (fast path)
		bool parsed = false;
		if (value.Length > 1 && value[0] == '0' && value[1] == 'x')
		{
			d = (int)uint.Parse(value[2..], System.Globalization.NumberStyles.HexNumber);
			parsed = true;
		}
		else if (value.Length > 1 && value[0] == '0' && value[1] == 'b')
		{
			d = (int)Convert.ToUInt32(value[2..], 2);
			parsed = true;
		}
		else if (int.TryParse(value, out var parsed_d))
		{
			d = parsed_d;
			parsed = true;
		}
		else
		{
			// Try expression evaluator (handles lo8(), hi8(), arithmetic, symbols, etc.)
			var symTable = _currentSymbolTable;
			if (symTable != null)
			{
				var result = ExpressionEvaluator.TryEvaluate(value, symTable);
				if (result.HasValue)
				{
					d = result.Value;
					parsed = true;
				}
				else
				{
					throw new Exception($"Cannot evaluate expression: {value}");
				}
			}
			else
			{
				throw new Exception($"[Ks] out of range: {min} < {value} < {max}");
			}
		}

		if (!parsed)
			throw new Exception($"[Ks] out of range: {min} < {value} < {max}");

		// Accept the signed spelling of an 8-bit immediate (avr-as allows -128..255 for
		// LDI/SUBI/SBCI/ANDI/ORI/CPI). The encoder masks with & 0xFF, so -100 -> 0x9C.
		if (d < 0 && min == 0 && max == 255 && d >= -128)
			d &= 0xFF;

		if (d < min || d > max) {
			throw new Exception($"[Ks] out of range: {min} < {value} < {max}");
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
	/// Determine if input is an address or label and lookup if required.
	/// Also checks the thread-local SymbolTable for .equ/.set/.def symbols.
	/// </summary>
	private static int ConstOrLabel (object value, LabelTable labels, int offset = 0)
	{
		if (value is not string c) return (int)value;
		// GNU '.' location counter. In a relative branch, avr-as resolves '.' to the
		// address of the *following* instruction (so `rjmp .` / `rcall .` encode offset 0,
		// the avr-gcc stack-reserve idiom — not a self-loop). Branches here are one word.
		if (c == ".") return _currentInstrByteOffset + 2 - offset;
		if (labels.TryGetValue(c, out var label)) {
			return label - offset;
		}

		// Check .equ/.set/.def symbols
		var symTable = _currentSymbolTable;
		if (symTable != null && symTable.TryGetValue(c, out var symVal)) {
			return symVal - offset;
		}

		if (c.Length > 1 && c[0] == '0' && c[1] == 'x')
			return (int)uint.Parse(c[2..], System.Globalization.NumberStyles.HexNumber);
		if (c.Length > 1 && c[0] == '0' && c[1] == 'b')
			return (int)Convert.ToUInt32(c[2..], 2);
		if (int.TryParse(c, out var d))
			return d;

		// Try expression evaluator for complex expressions
		if (symTable != null)
		{
			var result = ExpressionEvaluator.TryEvaluate(c, symTable, offset);
			if (result.HasValue) return result.Value - offset;
		}

		return int.MinValue;
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
		var (baseReg, q) = LineParser.ParseYzDisplacement(yzq);
		var r = 0x8000;
		switch (baseReg) {
			case 'Y':
				r |= 0x8;
				break;
			case 'Z':
				break;
			default:
				throw new Exception("Not Y or Z with q");
		}
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

/// <summary>
/// Thrown by <see cref="AvrAssembler.AssembleOrThrow"/> when assembly produced errors.
/// </summary>
public class AssemblerException : Exception
{
	/// <summary>The individual assembler error messages.</summary>
	public IReadOnlyList<string> Errors { get; }

	public AssemblerException(IReadOnlyList<string> errors)
		: base($"Assembly failed with {errors.Count} error(s):\n  " + string.Join("\n  ", errors))
	{
		Errors = errors;
	}
}
