namespace AVR8Sharp.Core.Utils;

/// <summary>
/// Recursive-descent expression evaluator.
/// Supports: decimal, 0x hex, $hex, 0b binary, 'c' char literals,
/// arithmetic (+,-,*,/,%), bitwise (&amp;,|,^,~,&lt;&lt;,&gt;&gt;), unary (-,~,!),
/// parentheses, symbol references, lo8/hi8/LOW/HIGH/pm/byte2/byte3/byte4/lwrd/hwrd functions.
/// </summary>
public static class ExpressionEvaluator
{
	/// <summary>
	/// Evaluate an expression string against the given symbol table.
	/// Returns null if a symbol is not yet defined (for forward-reference detection).
	/// Throws on syntax or range errors.
	/// </summary>
	public static int? TryEvaluate(string expr, SymbolTable symbols, int currentPc = 0)
	{
		if (string.IsNullOrWhiteSpace(expr)) return null;
		var tokens = Tokenizer.Tokenize(expr.Trim());
		// Remove trailing EOL
		if (tokens.Count > 0 && tokens[^1].Kind == TokenKind.EOL)
			tokens.RemoveAt(tokens.Count - 1);
		if (tokens.Count == 0) return null;

		int pos = 0;
		int? result = ParseOr(tokens, ref pos, symbols, currentPc);
		return result;
	}

	public static int Evaluate(string expr, SymbolTable symbols, int currentPc = 0)
	{
		var result = TryEvaluate(expr, symbols, currentPc);
		if (result == null)
			throw new Exception($"Undefined symbol in expression: {expr}");
		return result.Value;
	}

	private static int? ParseOr(List<Token> t, ref int pos, SymbolTable sym, int pc)
	{
		var left = ParseXor(t, ref pos, sym, pc);
		while (pos < t.Count && t[pos].Kind == TokenKind.Pipe)
		{
			pos++;
			var right = ParseXor(t, ref pos, sym, pc);
			if (left == null || right == null) return null;
			left = left.Value | right.Value;
		}
		return left;
	}

	private static int? ParseXor(List<Token> t, ref int pos, SymbolTable sym, int pc)
	{
		var left = ParseAnd(t, ref pos, sym, pc);
		while (pos < t.Count && t[pos].Kind == TokenKind.Caret)
		{
			pos++;
			var right = ParseAnd(t, ref pos, sym, pc);
			if (left == null || right == null) return null;
			left = left.Value ^ right.Value;
		}
		return left;
	}

	private static int? ParseAnd(List<Token> t, ref int pos, SymbolTable sym, int pc)
	{
		var left = ParseShift(t, ref pos, sym, pc);
		while (pos < t.Count && t[pos].Kind == TokenKind.Amp)
		{
			pos++;
			var right = ParseShift(t, ref pos, sym, pc);
			if (left == null || right == null) return null;
			left = left.Value & right.Value;
		}
		return left;
	}

	private static int? ParseShift(List<Token> t, ref int pos, SymbolTable sym, int pc)
	{
		var left = ParseAddSub(t, ref pos, sym, pc);
		while (pos < t.Count && (t[pos].Kind == TokenKind.LShift || t[pos].Kind == TokenKind.RShift))
		{
			bool isLeft = t[pos].Kind == TokenKind.LShift;
			pos++;
			var right = ParseAddSub(t, ref pos, sym, pc);
			if (left == null || right == null) return null;
			left = isLeft ? left.Value << right.Value : left.Value >> right.Value;
		}
		return left;
	}

	private static int? ParseAddSub(List<Token> t, ref int pos, SymbolTable sym, int pc)
	{
		var left = ParseMulDiv(t, ref pos, sym, pc);
		while (pos < t.Count && (t[pos].Kind == TokenKind.Plus || t[pos].Kind == TokenKind.Minus))
		{
			bool add = t[pos].Kind == TokenKind.Plus;
			pos++;
			var right = ParseMulDiv(t, ref pos, sym, pc);
			if (left == null || right == null) return null;
			left = add ? left.Value + right.Value : left.Value - right.Value;
		}
		return left;
	}

	private static int? ParseMulDiv(List<Token> t, ref int pos, SymbolTable sym, int pc)
	{
		var left = ParseUnary(t, ref pos, sym, pc);
		while (pos < t.Count && (t[pos].Kind == TokenKind.Star || t[pos].Kind == TokenKind.Slash || t[pos].Kind == TokenKind.Percent))
		{
			var op = t[pos].Kind;
			pos++;
			var right = ParseUnary(t, ref pos, sym, pc);
			if (left == null || right == null) return null;
			if (op == TokenKind.Star) left = left.Value * right.Value;
			else if (op == TokenKind.Slash) left = right.Value == 0 ? throw new Exception("Division by zero") : left.Value / right.Value;
			else left = left.Value % right.Value;
		}
		return left;
	}

	private static int? ParseUnary(List<Token> t, ref int pos, SymbolTable sym, int pc)
	{
		if (pos < t.Count)
		{
			if (t[pos].Kind == TokenKind.Minus) { pos++; var v = ParsePrimary(t, ref pos, sym, pc); return v == null ? null : -v.Value; }
			if (t[pos].Kind == TokenKind.Tilde) { pos++; var v = ParsePrimary(t, ref pos, sym, pc); return v == null ? null : ~v.Value; }
			if (t[pos].Kind == TokenKind.Bang)  { pos++; var v = ParsePrimary(t, ref pos, sym, pc); return v == null ? null : (v.Value == 0 ? 1 : 0); }
		}
		return ParsePrimary(t, ref pos, sym, pc);
	}

	private static int? ParsePrimary(List<Token> t, ref int pos, SymbolTable sym, int pc)
	{
		if (pos >= t.Count) throw new Exception("Unexpected end of expression");

		var tok = t[pos];

		// Integer literals
		if (tok.Kind == TokenKind.IntLiteral)
		{
			pos++;
			return tok.IntValue;
		}

		// Current PC
		if (tok.Kind == TokenKind.Dollar)
		{
			pos++;
			return pc;
		}

		// Parenthesised sub-expression
		if (tok.Kind == TokenKind.LParen)
		{
			pos++;
			var v = ParseOr(t, ref pos, sym, pc);
			if (pos < t.Count && t[pos].Kind == TokenKind.RParen) pos++;
			return v;
		}

		// Identifiers: functions or symbols
		if (tok.Kind == TokenKind.Identifier)
		{
			string name = tok.Raw;
			pos++;

			// Built-in functions
			if (pos < t.Count && t[pos].Kind == TokenKind.LParen)
			{
				pos++; // consume (
				var arg = ParseOr(t, ref pos, sym, pc);
				if (pos < t.Count && t[pos].Kind == TokenKind.RParen) pos++;
				if (arg == null) return null;
				return name.ToUpperInvariant() switch
				{
					"LO8" or "LOW"   => arg.Value & 0xFF,
					"HI8" or "HIGH"  => (arg.Value >> 8) & 0xFF,
					"BYTE2"          => (arg.Value >> 8) & 0xFF,
					"BYTE3"          => (arg.Value >> 16) & 0xFF,
					"BYTE4"          => (arg.Value >> 24) & 0xFF,
					"LWRD"           => arg.Value & 0xFFFF,
					"HWRD"           => (arg.Value >> 16) & 0xFFFF,
					"PM"             => arg.Value >> 1,
					_ => throw new Exception($"Unknown function: {name}")
				};
			}

			// Symbol lookup
			if (sym.TryGetValue(name, out int val)) return val;

			// Integer literal that looks like identifier (e.g. hex without prefix — shouldn't reach here normally)
			return null; // unknown symbol — forward reference
		}

		// +expr at start (e.g. "+8" in BRBC 0, +8)
		if (tok.Kind == TokenKind.Plus)
		{
			pos++;
			return ParsePrimary(t, ref pos, sym, pc);
		}

		throw new Exception($"Unexpected token '{tok.Raw}' in expression");
	}
}
