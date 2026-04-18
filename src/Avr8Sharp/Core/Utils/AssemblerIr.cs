namespace AVR8Sharp.Core.Utils;

// Sealed discriminated union for instruction encoding
public abstract class InstructionWord
{
	private InstructionWord() { }

	// 2-byte instruction
	public sealed class WordEncoding : InstructionWord
	{
		public ushort Value { get; }
		public WordEncoding(ushort value) => Value = value;
		public int ByteSize => 2;
	}

	// 4-byte instruction (JMP, CALL, LDS, STS)
	public sealed class DwordEncoding : InstructionWord
	{
		public ushort High { get; }
		public ushort Low { get; }
		public DwordEncoding(ushort high, ushort low) { High = high; Low = low; }
		public int ByteSize => 4;
	}

	// Raw byte data (.byte/.ascii/etc.)
	public sealed class DataEncoding : InstructionWord
	{
		public byte[] Data { get; }
		public DataEncoding(byte[] data) => Data = data;
		public int ByteSize => Data.Length;
	}

	// Deferred fixup (forward reference) - estimate size in bytes
	public sealed class Pending : InstructionWord
	{
		public Func<SymbolTable, InstructionWord> Resolve { get; }
		public int EstimatedByteSize { get; }
		public Pending(Func<SymbolTable, InstructionWord> resolve, int estimatedByteSize)
		{
			Resolve = resolve;
			EstimatedByteSize = estimatedByteSize;
		}
	}
}

// Line as produced by pass 1
public class AsmLine
{
	public int Line { get; set; }
	public string Text { get; set; } = string.Empty;
	public int BytesOffset { get; set; }
	public string? SourceFile { get; set; }
	public InstructionWord? Encoding { get; set; }
}

// Fixup entry for forward-reference resolution
public class FixupEntry
{
	public int ByteOffset { get; set; }
	public FixupKind Kind { get; set; }
	public string LabelOrExpr { get; set; } = string.Empty;
	public int InstrLine { get; set; }
	public int OperandA { get; set; }  // extra data needed by the encoder
}

public enum FixupKind
{
	RelBranch7,    // 7-bit signed relative (BRBS/BRBC family)
	RelBranch12,   // 12-bit signed relative (RJMP/RCALL)
	AbsCall22,     // 22-bit absolute (JMP/CALL)
}

// Symbol table (used for .equ / .set / labels / .def)
public class SymbolTable
{
	private readonly Dictionary<string, int> _symbols = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _immutable = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _registerAliases = new(StringComparer.OrdinalIgnoreCase);

	public bool TryGetValue(string name, out int value) =>
		_symbols.TryGetValue(name, out value);

	public bool ContainsKey(string name) => _symbols.ContainsKey(name);

	public bool IsRegisterAlias(string name) => _registerAliases.Contains(name);

	// Define immutable constant (.equ / label)
	public void DefineConst(string name, int value)
	{
		if (_immutable.Contains(name))
		{
			if (_symbols[name] != value)
				throw new Exception($"Symbol '{name}' already defined with a different value");
			return;
		}
		_symbols[name] = value;
		_immutable.Add(name);
	}

	// Define mutable constant (.set)
	public void DefineVar(string name, int value)
	{
		if (_immutable.Contains(name))
			throw new Exception($"Symbol '{name}' is immutable, cannot reassign with .set");
		_symbols[name] = value;
	}

	// Define a register alias (.def)
	public void DefineRegisterAlias(string name, int regNum)
	{
		DefineConst(name, regNum);
		_registerAliases.Add(name);
	}

	// Plain write (for labels, which are always single-definition)
	public void Set(string name, int value) => _symbols[name] = value;

	public IReadOnlyDictionary<string, int> All => _symbols;
}
