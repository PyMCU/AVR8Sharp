namespace AVR8Sharp.Core.Utils;

/// <summary>
/// Segment type for multi-segment support.
/// </summary>
public enum SegmentType { Code, Data, Eeprom }

/// <summary>
/// Processes assembler directives during pass 1.
/// </summary>
internal class DirectiveProcessor
{
	private readonly SymbolTable _symbols;
	private readonly Dictionary<string, (List<string> Params, List<string> Body)> _macros = new(StringComparer.OrdinalIgnoreCase);

	// Segment location counters (byte addresses)
	private int _codeOffset;
	private int _dataOffset;
	private int _eepromOffset;
	private SegmentType _currentSegment = SegmentType.Code;

	// Conditional assembly stack: true = currently assembling
	private readonly Stack<bool> _condStack = new();
	private bool _assembling = true;  // top-level: always assemble

	// Macro recording state
	private string? _recordingMacro;
	private List<string>? _recordingParams;
	private List<string>? _recordingBody;

	// Include file resolver
	private readonly Func<string, string>? _fileResolver;

	public int CurrentOffset => _currentSegment switch
	{
		SegmentType.Code => _codeOffset,
		SegmentType.Data => _dataOffset,
		SegmentType.Eeprom => _eepromOffset,
		_ => _codeOffset
	};

	public SegmentType CurrentSegment => _currentSegment;
	public bool Assembling => _assembling;

	public DirectiveProcessor(SymbolTable symbols, Func<string, string>? fileResolver = null)
	{
		_symbols = symbols;
		_fileResolver = fileResolver;
	}

	public void SetCodeOffset(int offset) => _codeOffset = offset;
	public void AdvanceCodeOffset(int bytes) => _codeOffset += bytes;

	/// <summary>
	/// Process a directive line. Returns true if the line was a directive (and thus consumed),
	/// false if it is not a directive. Also handles macro expansion by returning lines to insert.
	/// </summary>
	public bool ProcessDirective(string name, string args, out List<string>? extraLines, out List<AsmLine> emittedLines, out string? error)
	{
		extraLines = null;
		emittedLines = new List<AsmLine>();
		error = null;

		// Handle macro recording
		if (_recordingMacro != null)
		{
			string upper = name.ToUpperInvariant();
			if (upper == ".ENDMACRO" || upper == ".ENDM" || upper == "ENDMACRO" || upper == "ENDM")
			{
				_macros[_recordingMacro] = (_recordingParams!, _recordingBody!);
				_recordingMacro = null;
				_recordingParams = null;
				_recordingBody = null;
			}
			else
			{
				_recordingBody!.Add(string.IsNullOrEmpty(args) ? name : $"{name} {args}");
			}
			return true;
		}

		string directive = name.TrimStart('.').ToUpperInvariant();

		switch (directive)
		{
			case "CSEG":
				_currentSegment = SegmentType.Code;
				return true;
			case "DSEG":
				_currentSegment = SegmentType.Data;
				return true;
			case "ESEG":
				_currentSegment = SegmentType.Eeprom;
				return true;

			case "ORG":
			case "LOC":
			{
				string expr = args.Trim();
				int? val = ExpressionEvaluator.TryEvaluate(expr, _symbols, _codeOffset);
				if (val == null) { error = $"Cannot evaluate expression: {expr}"; return true; }
				if ((val.Value & 1) != 0 && _currentSegment == SegmentType.Code) { error = "Code origin must be even"; return true; }
				switch (_currentSegment)
				{
					case SegmentType.Code:    _codeOffset    = val.Value; break;
					case SegmentType.Data:    _dataOffset    = val.Value; break;
					case SegmentType.Eeprom:  _eepromOffset  = val.Value; break;
				}
				return true;
			}

			case "EQU":
			{
				// .equ NAME = VALUE
				var parts = args.Split('=', 2);
				if (parts.Length != 2) { error = ".equ requires NAME = VALUE"; return true; }
				string symName = parts[0].Trim();
				int? val = ExpressionEvaluator.TryEvaluate(parts[1].Trim(), _symbols, _codeOffset);
				if (val == null) { error = $"Cannot evaluate .equ expression for {symName}"; return true; }
				try { _symbols.DefineConst(symName, val.Value); }
				catch (Exception ex) { error = ex.Message; }
				return true;
			}

			case "SET":
			{
				var parts = args.Split('=', 2);
				if (parts.Length != 2) { error = ".set requires NAME = VALUE"; return true; }
				string symName = parts[0].Trim();
				int? val = ExpressionEvaluator.TryEvaluate(parts[1].Trim(), _symbols, _codeOffset);
				if (val == null) { error = $"Cannot evaluate .set expression for {symName}"; return true; }
				try { _symbols.DefineVar(symName, val.Value); }
				catch (Exception ex) { error = ex.Message; }
				return true;
			}

			case "DEF":
			{
				// .def ALIAS = rN
				var parts = args.Split('=', 2);
				if (parts.Length != 2) { error = ".def requires ALIAS = rN"; return true; }
				string alias = parts[0].Trim();
				string regStr = parts[1].Trim();
				int n = Encoders.EncoderHelpers.TryParseRegister(regStr.AsSpan());
				if (n < 0) { error = $".def: right side must be a register, got '{regStr}'"; return true; }
				try { _symbols.DefineConst(alias, n); }
				catch (Exception ex) { error = ex.Message; }
				return true;
			}

			case "REPLACE":
			{
				// backward-compat: _REPLACE NAME, VALUE (no '=')
				var parts = args.Split(',', 2);
				if (parts.Length != 2) { error = "_REPLACE requires NAME, VALUE"; return true; }
				string symName = parts[0].Trim();
				string valStr = parts[1].Trim();
				int? val = ExpressionEvaluator.TryEvaluate(valStr, _symbols, _codeOffset);
				if (val == null)
				{
					// store as raw text for later text-substitution (backward-compat mode)
					_symbols.Set(symName, 0); // placeholder
					return true;
				}
				try { _symbols.Set(symName, val.Value); }
				catch { /* ignore redefinition in backward-compat mode */ }
				return true;
			}

			case "BYTE":
			case "DB":
			{
				if (_currentSegment != SegmentType.Code) return true; // DSEG .byte just reserves space
				var bytes = EmitCommaSeparatedBytes(args, out error);
				if (bytes != null && bytes.Length > 0)
				{
					emittedLines.Add(new AsmLine
					{
						BytesOffset = _codeOffset,
						Encoding = new InstructionWord.DataEncoding(bytes),
						Text = $".{directive.ToLowerInvariant()} {args}",
						Line = 0
					});
					_codeOffset += bytes.Length;
				}
				return true;
			}

			case "WORD":
			case "DW":
			case "IW": // backward-compat _IW
			{
				var words = EmitCommaSeparatedWords(args, out error);
				if (words != null && words.Length > 0)
				{
					var wordBytes = new byte[words.Length * 2];
					for (int i = 0; i < words.Length; i++)
					{
						wordBytes[i * 2]     = (byte)(words[i] & 0xFF);
						wordBytes[i * 2 + 1] = (byte)((words[i] >> 8) & 0xFF);
					}
					emittedLines.Add(new AsmLine
					{
						BytesOffset = _codeOffset,
						Encoding = new InstructionWord.DataEncoding(wordBytes),
						Text = $".{directive.ToLowerInvariant()} {args}",
						Line = 0
					});
					_codeOffset += wordBytes.Length;
				}
				return true;
			}

			case "DWORD":
			{
				var words = EmitCommaSeparatedWords(args, out error);
				if (words != null && words.Length > 0)
				{
					var dwordBytes = new byte[words.Length * 4];
					for (int i = 0; i < words.Length; i++)
					{
						dwordBytes[i * 4]     = (byte)(words[i] & 0xFF);
						dwordBytes[i * 4 + 1] = (byte)((words[i] >> 8) & 0xFF);
						dwordBytes[i * 4 + 2] = (byte)((words[i] >> 16) & 0xFF);
						dwordBytes[i * 4 + 3] = (byte)((words[i] >> 24) & 0xFF);
					}
					emittedLines.Add(new AsmLine
					{
						BytesOffset = _codeOffset,
						Encoding = new InstructionWord.DataEncoding(dwordBytes),
						Text = ".dword " + args,
						Line = 0
					});
					_codeOffset += dwordBytes.Length;
				}
				return true;
			}

			case "ASCII":
			{
				string str = UnquoteString(args.Trim());
				var bytes = System.Text.Encoding.ASCII.GetBytes(str);
				emittedLines.Add(new AsmLine
				{
					BytesOffset = _codeOffset,
					Encoding = new InstructionWord.DataEncoding(bytes),
					Text = ".ascii " + args,
					Line = 0
				});
				_codeOffset += bytes.Length;
				return true;
			}

			case "ASCIZ":
			case "STRING":
			{
				string str = UnquoteString(args.Trim());
				var rawBytes = System.Text.Encoding.ASCII.GetBytes(str);
				var bytes = new byte[rawBytes.Length + 1];
				rawBytes.CopyTo(bytes, 0);
				bytes[^1] = 0;
				emittedLines.Add(new AsmLine
				{
					BytesOffset = _codeOffset,
					Encoding = new InstructionWord.DataEncoding(bytes),
					Text = ".asciz " + args,
					Line = 0
				});
				_codeOffset += bytes.Length;
				return true;
			}

			case "IF":
			{
				int? val = ExpressionEvaluator.TryEvaluate(args.Trim(), _symbols, _codeOffset);
				bool result = val.HasValue && val.Value != 0;
				_condStack.Push(_assembling);
				_assembling = _assembling && result;
				return true;
			}

			case "IFDEF":
			{
				bool defined = _symbols.ContainsKey(args.Trim());
				_condStack.Push(_assembling);
				_assembling = _assembling && defined;
				return true;
			}

			case "IFNDEF":
			{
				bool notDefined = !_symbols.ContainsKey(args.Trim());
				_condStack.Push(_assembling);
				_assembling = _assembling && notDefined;
				return true;
			}

			case "ELSEIF":
			{
				if (_condStack.Count == 0) { error = ".elseif without .if"; return true; }
				bool wasActive = _condStack.Peek();
				if (_assembling)
				{
					// We were assembling the if-branch; now skip
					_assembling = false;
				}
				else if (wasActive)
				{
					// Parent was active; evaluate this branch
					int? val = ExpressionEvaluator.TryEvaluate(args.Trim(), _symbols, _codeOffset);
					_assembling = val.HasValue && val.Value != 0;
				}
				return true;
			}

			case "ELSE":
			{
				if (_condStack.Count == 0) { error = ".else without .if"; return true; }
				bool parentActive = _condStack.Peek();
				_assembling = parentActive && !_assembling;
				return true;
			}

			case "ENDIF":
			{
				if (_condStack.Count == 0) { error = ".endif without .if"; return true; }
				_assembling = _condStack.Pop();
				return true;
			}

			case "MACRO":
			{
				var parts = args.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 0) { error = ".macro requires a name"; return true; }
				_recordingMacro = parts[0].Trim();
				_recordingParams = parts.Skip(1).Select(p => p.Trim()).ToList();
				_recordingBody = new List<string>();
				return true;
			}

			case "ENDMACRO":
			case "ENDM":
			{
				error = ".endm without .macro";
				return true;
			}

			case "INCLUDE":
			{
				string filename = UnquoteString(args.Trim());
				if (_fileResolver == null) { error = ".include requires a file resolver"; return true; }
				try
				{
					string content = _fileResolver(filename);
					extraLines = content.Split('\n').ToList();
				}
				catch (Exception ex)
				{
					error = $".include error: {ex.Message}";
				}
				return true;
			}

			default:
				return false; // Not a directive we handle
		}
	}

	/// <summary>
	/// Try to expand a macro by name. Returns the expanded lines or null if not a macro.
	/// </summary>
	public List<string>? TryExpandMacro(string name, string args, int depth = 0)
	{
		if (depth > 8) throw new Exception($"Macro recursion limit exceeded in '{name}'");
		if (!_macros.TryGetValue(name, out var macro)) return null;

		var argValues = args.Split(',').Select(a => a.Trim()).ToArray();
		var expanded = new List<string>();

		foreach (var line in macro.Body)
		{
			string expandedLine = line;
			for (int i = 0; i < macro.Params.Count; i++)
			{
				if (i < argValues.Length)
				{
					expandedLine = expandedLine.Replace(@"\" + macro.Params[i], argValues[i]);
					expandedLine = expandedLine.Replace("@" + i, argValues[i]);
				}
			}
			expanded.Add(expandedLine);
		}
		return expanded;
	}

	private byte[]? EmitCommaSeparatedBytes(string args, out string? error)
	{
		error = null;
		var parts = SplitArgs(args);
		var bytes = new List<byte>();
		foreach (var p in parts)
		{
			string trimmed = p.Trim();
			// String literals
			if (trimmed.StartsWith('"'))
			{
				string str = UnquoteString(trimmed);
				bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(str));
				continue;
			}
			int? val = ExpressionEvaluator.TryEvaluate(trimmed, _symbols, _codeOffset);
			if (val == null) { error = $"Cannot evaluate: {trimmed}"; return null; }
			bytes.Add((byte)(val.Value & 0xFF));
		}
		return bytes.ToArray();
	}

	private int[]? EmitCommaSeparatedWords(string args, out string? error)
	{
		error = null;
		var parts = SplitArgs(args);
		var words = new List<int>();
		foreach (var p in parts)
		{
			int? val = ExpressionEvaluator.TryEvaluate(p.Trim(), _symbols, _codeOffset);
			if (val == null) { error = $"Cannot evaluate: {p.Trim()}"; return null; }
			words.Add(val.Value);
		}
		return words.ToArray();
	}

	private static List<string> SplitArgs(string args)
	{
		// Split by comma, but not inside quotes
		var parts = new List<string>();
		int depth = 0;
		bool inString = false;
		var cur = new System.Text.StringBuilder();
		foreach (char c in args)
		{
			if (c == '"') inString = !inString;
			if (!inString)
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

	private static string UnquoteString(string s)
	{
		if (s.StartsWith('"') && s.EndsWith('"') && s.Length >= 2)
			return s[1..^1];
		return s;
	}
}
