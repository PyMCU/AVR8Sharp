namespace AVR8Sharp.Core.Utils;

public enum DiagnosticSeverity { Error, Warning }

/// <summary>
/// Structured assembler diagnostic, compatible with avr-as output format.
/// </summary>
public record AssemblerDiagnostic(
	DiagnosticSeverity Severity,
	string Message,
	string? SourceFile,
	int Line,
	int Column = 0)
{
	public override string ToString()
	{
		string loc = SourceFile != null
			? $"{SourceFile}:{Line}:{Column}"
			: $"<input>:{Line}:{Column}";
		string sev = Severity == DiagnosticSeverity.Error ? "error" : "warning";
		return $"{loc}: {sev}: {Message}";
	}
}

/// <summary>
/// Assembler context holding all state that persists through both passes.
/// </summary>
internal class AssemblerContext
{
	public SymbolTable Symbols { get; } = new SymbolTable();
	public List<AsmLine> Lines { get; } = new List<AsmLine>();
	public List<AssemblerDiagnostic> Diagnostics { get; } = new List<AssemblerDiagnostic>();
	public List<FixupEntry> Fixups { get; } = new List<FixupEntry>();

	public bool HasErrors => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

	public void Error(string message, int line, string? file = null, int column = 0) =>
		Diagnostics.Add(new AssemblerDiagnostic(DiagnosticSeverity.Error, message, file, line, column));

	public void Warning(string message, int line, string? file = null, int column = 0) =>
		Diagnostics.Add(new AssemblerDiagnostic(DiagnosticSeverity.Warning, message, file, line, column));

	public void Reset()
	{
		Lines.Clear();
		Diagnostics.Clear();
		Fixups.Clear();
	}
}
