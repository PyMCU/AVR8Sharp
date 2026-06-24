namespace AVR8Sharp.Core.Utils;

/// <summary>
/// Result of parsing a single assembly line using recursive-descent character scanning.
/// Replaces all line-level regex matching (CommentsRegex, LabelRegex, CodeRegex, DotDirectiveRegex).
/// </summary>
public class ParsedLine
{
	/// <summary>Label name if present (without the colon).</summary>
	public string? Label;

	/// <summary>True if the line (after optional label) has no instruction or directive content.</summary>
	public bool IsEmpty;

	/// <summary>True if the line contains a dot-directive (.equ, .org, etc.).</summary>
	public bool IsDirective;

	/// <summary>Directive name without leading dot, uppercase (e.g. "EQU", "ORG").</summary>
	public string DirectiveName = string.Empty;

	/// <summary>Raw arguments string for directives (everything after the directive name, trimmed).</summary>
	public string DirectiveArgs = string.Empty;

	/// <summary>Instruction mnemonic, uppercase (e.g. "ADD", "LDI"). Null for directives or empty lines.</summary>
	public string? Mnemonic;

	/// <summary>First operand string (trimmed). Empty if no operands.</summary>
	public string Operand1 = string.Empty;

	/// <summary>Second operand string (trimmed). Empty if single or no operands.</summary>
	public string Operand2 = string.Empty;
}

/// <summary>
/// Hand-written recursive-descent line parser for AVR assembly.
/// Replaces CommentsRegex, LabelRegex, CodeRegex, DotDirectiveRegex
/// with a single-pass O(n) character scan — no regex, no backtracking.
/// </summary>
public static class LineParser
{
	/// <summary>
	/// Parse a single assembly source line into a structured result.
	/// Handles comment stripping, label detection, directive/instruction identification,
	/// and operand splitting in a single character-scanning pass.
	/// </summary>
	public static ParsedLine Parse(string line)
	{
		var result = new ParsedLine();
		int len = line.Length;
		int pos = 0;

		// Skip leading whitespace
		SkipWhitespace(line, ref pos, len);

		if (pos >= len)
		{
			result.IsEmpty = true;
			return result;
		}

		// Full-line comment
		if (line[pos] == ';' || line[pos] == '#')
		{
			result.IsEmpty = true;
			return result;
		}

		// Try to parse label: [.]identifier followed by ':'
		// A leading '.' is allowed so GNU/avr-gcc local labels (.L0:, .L2:) are
		// recognised as labels instead of being mistaken for dot-directives.
		if (IsIdentStart(line[pos]) || (line[pos] == '.' && pos + 1 < len && IsIdentStart(line[pos + 1])))
		{
			int savedPos = pos;
			int identStart = pos;
			if (line[pos] == '.') pos++; // consume leading dot for local labels
			while (pos < len && IsIdentChar(line[pos])) pos++;

			if (pos < len && line[pos] == ':')
			{
				result.Label = line[identStart..pos];
				pos++; // skip ':'
				SkipWhitespace(line, ref pos, len);
			}
			else
			{
				pos = savedPos; // not a label, reset position
			}
		}

		// Check if rest of line is empty or comment-only
		if (pos >= len || line[pos] == ';' || line[pos] == '#')
		{
			result.IsEmpty = result.Label == null;
			return result;
		}

		// Directive: starts with '.'
		if (line[pos] == '.' && pos + 1 < len && (char.IsLetter(line[pos + 1]) || line[pos + 1] == '_'))
		{
			result.IsDirective = true;
			int dirStart = pos + 1;
			pos++;
			while (pos < len && IsIdentChar(line[pos])) pos++;
			result.DirectiveName = line[dirStart..pos].ToUpperInvariant();

			SkipWhitespace(line, ref pos, len);

			// Raw args = everything until inline comment or end of line
			if (pos < len && line[pos] != ';' && line[pos] != '#')
			{
				int argsEnd = FindCommentOrEnd(line, pos, len);
				result.DirectiveArgs = line[pos..argsEnd].TrimEnd();
			}
			return result;
		}

		// Instruction: identifier followed by optional operands
		if (IsIdentStart(line[pos]))
		{
			int mnemStart = pos;
			while (pos < len && IsIdentChar(line[pos])) pos++;
			result.Mnemonic = line[mnemStart..pos].ToUpperInvariant();

			SkipWhitespace(line, ref pos, len);

			if (pos >= len || line[pos] == ';' || line[pos] == '#')
				return result;

			// Parse operands: everything until comment or EOL, split on first top-level comma
			int operandsEnd = FindCommentOrEnd(line, pos, len);
			string operands = line[pos..operandsEnd].TrimEnd();

			int commaPos = FindFirstTopLevelComma(operands);
			if (commaPos < 0)
			{
				result.Operand1 = operands.Trim();
			}
			else
			{
				result.Operand1 = operands[..commaPos].Trim();
				result.Operand2 = operands[(commaPos + 1)..].Trim();
			}
		}

		return result;
	}

	/// <summary>
	/// Strip inline comments from a line. Respects string and character literals.
	/// Replaces CommentsRegex usage.
	/// </summary>
	public static string StripComments(string line)
	{
		int commentPos = FindCommentOrEnd(line, 0, line.Length);
		return commentPos < line.Length ? line[..commentPos].TrimEnd() : line;
	}

	private static void SkipWhitespace(string s, ref int pos, int len)
	{
		while (pos < len && (s[pos] == ' ' || s[pos] == '\t' || s[pos] == '\r'))
			pos++;
	}

	private static bool IsIdentStart(char c) =>
		char.IsLetter(c) || c == '_';

	private static bool IsIdentChar(char c) =>
		char.IsLetterOrDigit(c) || c == '_';

	/// <summary>
	/// Find the position of the first inline comment (';' or '#') outside string/char literals.
	/// Returns line length if no comment found.
	/// </summary>
	private static int FindCommentOrEnd(string line, int start, int len)
	{
		bool inString = false;
		for (int i = start; i < len; i++)
		{
			char c = line[i];
			if (inString)
			{
				if (c == '"') inString = false;
				else if (c == '\\' && i + 1 < len) i++; // skip escape
				continue;
			}
			if (c == '"') { inString = true; continue; }
			// Simple char literal: 'X' — skip 3 chars
			if (c == '\'' && i + 2 < len && line[i + 2] == '\'')
			{
				i += 2;
				continue;
			}
			if (c == ';' || c == '#') return i;
		}
		return len;
	}

	/// <summary>
	/// Find the first comma that is not inside parentheses or string literals.
	/// Returns -1 if no top-level comma found.
	/// </summary>
	private static int FindFirstTopLevelComma(string s)
	{
		int depth = 0;
		bool inString = false;
		for (int i = 0; i < s.Length; i++)
		{
			char c = s[i];
			if (inString)
			{
				if (c == '"') inString = false;
				else if (c == '\\' && i + 1 < s.Length) i++;
				continue;
			}
			if (c == '"') { inString = true; continue; }
			if (c == '(') depth++;
			else if (c == ')') depth--;
			else if (c == ',' && depth == 0) return i;
		}
		return -1;
	}

	/// <summary>
	/// Detect a GNU-style symbol assignment line: <c>name = expression</c>
	/// (e.g. <c>__SP_H__ = 0x3e</c> or <c>.L__stack_usage = 2</c>, as emitted by avr-gcc).
	/// The name may carry a leading '.' and embedded '.' characters. Returns false for
	/// instructions, directives and comparisons (a leading <c>==</c> is rejected).
	/// </summary>
	public static bool TryParseAssignment(string line, out string name, out string expr)
	{
		name = string.Empty;
		expr = string.Empty;
		int len = line.Length;
		int pos = 0;
		while (pos < len && (line[pos] == ' ' || line[pos] == '\t')) pos++;
		if (pos >= len) return false;

		// LHS must be a single identifier token (letters/digits/_/.).
		char first = line[pos];
		if (!(char.IsLetter(first) || first == '_' || first == '.')) return false;
		int nameStart = pos;
		pos++;
		while (pos < len && (char.IsLetterOrDigit(line[pos]) || line[pos] == '_' || line[pos] == '.')) pos++;
		int nameEnd = pos;

		while (pos < len && (line[pos] == ' ' || line[pos] == '\t')) pos++;
		if (pos >= len || line[pos] != '=') return false;
		if (pos + 1 < len && line[pos + 1] == '=') return false; // '==' is a comparison, not an assignment

		name = line[nameStart..nameEnd];
		expr = line[(pos + 1)..].Trim();
		return expr.Length > 0;
	}

	/// <summary>
	/// Parse a Y+q or Z+q displacement string without regex.
	/// Returns (baseReg: 'Y' or 'Z', displacement: int).
	/// Throws on invalid format.
	/// </summary>
	public static (char BaseReg, int Displacement) ParseYzDisplacement(string yzq)
	{
		if (yzq.Length < 3 || yzq[1] != '+')
			throw new Exception("Invalid Y/Z displacement format");

		char baseReg = char.ToUpperInvariant(yzq[0]);
		if (baseReg != 'Y' && baseReg != 'Z')
			throw new Exception("Not Y or Z with q");

		int pos = 2;
		if (pos >= yzq.Length || !char.IsDigit(yzq[pos]))
			throw new Exception("Invalid displacement value");

		int q = 0;
		while (pos < yzq.Length && char.IsDigit(yzq[pos]))
		{
			q = q * 10 + (yzq[pos] - '0');
			pos++;
		}

		return (baseReg, q);
	}
}
