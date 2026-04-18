namespace AVR8Sharp.Core.Utils;

public enum TokenKind
{
	Mnemonic,       // instruction or directive name
	Directive,      // .equ, .org, etc.
	Register,       // r0..r31, X, Y, Z, X+, -X, Y+, -Y, Z+, -Z
	IntLiteral,     // 42, 0xFF, 0b1010, $FF, 'A'
	StringLiteral,  // "hello"
	Identifier,     // label or symbol name
	Comma,
	Colon,
	LParen,
	RParen,
	Plus,
	Minus,
	Star,
	Slash,
	Percent,
	Amp,            // &
	Pipe,           // |
	Caret,          // ^
	Tilde,          // ~
	LShift,         // <<
	RShift,         // >>
	Bang,           // !
	Equal,          // =
	Hash,           // #
	Dollar,         // $ (current PC when not followed by hex)
	EOL,
}

public record Token(TokenKind Kind, string Raw, int Column)
{
	public int IntValue { get; init; }
}

public static class Tokenizer
{
	// Tokenize a single line (already stripped of block comments).
	// Returns tokens up to and including EOL (which includes stripping ; and # inline comments).
	public static List<Token> Tokenize(string line)
	{
		var tokens = new List<Token>();
		int i = 0;
		int len = line.Length;

		while (i < len)
		{
			char c = line[i];

			// Skip whitespace
			if (c == ' ' || c == '\t' || c == '\r')
			{
				i++;
				continue;
			}

			// Inline comments
			if (c == ';' || c == '#')
				break;

			int col = i;

			// String literals
			if (c == '"')
			{
				int start = i + 1;
				i++;
				while (i < len && line[i] != '"')
				{
					if (line[i] == '\\') i++;
					i++;
				}
				string str = i < len ? line[start..i] : line[start..];
				if (i < len) i++; // closing "
				tokens.Add(new Token(TokenKind.StringLiteral, str, col));
				continue;
			}

			// Character literals  'A'
			if (c == '\'' && i + 2 < len && line[i + 2] == '\'')
			{
				char ch = line[i + 1];
				int val = ch == '\\' && i + 3 < len && line[i + 3] == '\''
					? EscapeChar(line[i + 2])
					: ch;
				tokens.Add(new Token(TokenKind.IntLiteral, line[i..(i + 3)], col) { IntValue = val });
				i += 3;
				continue;
			}

			// $ hex prefix  $FF  or bare $ (current PC)
			if (c == '$')
			{
				if (i + 1 < len && IsHexDigit(line[i + 1]))
				{
					i++;
					int start = i;
					while (i < len && IsHexDigit(line[i])) i++;
					string hex = line[start..i];
					int val = Convert.ToInt32(hex, 16);
					tokens.Add(new Token(TokenKind.IntLiteral, "$" + hex, col) { IntValue = val });
				}
				else
				{
					tokens.Add(new Token(TokenKind.Dollar, "$", col));
					i++;
				}
				continue;
			}

			// 0x hex, 0b binary, decimal
			if (char.IsDigit(c))
			{
				if (c == '0' && i + 1 < len && line[i + 1] == 'x')
				{
					i += 2;
					int start = i;
					while (i < len && IsHexDigit(line[i])) i++;
					string hex = line[start..i];
					int val = (int)uint.Parse(hex, System.Globalization.NumberStyles.HexNumber);
					tokens.Add(new Token(TokenKind.IntLiteral, "0x" + hex, col) { IntValue = val });
				}
				else if (c == '0' && i + 1 < len && (line[i + 1] == 'b' || line[i + 1] == 'B'))
				{
					i += 2;
					int start = i;
					while (i < len && (line[i] == '0' || line[i] == '1')) i++;
					string bin = line[start..i];
					int val = Convert.ToInt32(bin, 2);
					tokens.Add(new Token(TokenKind.IntLiteral, "0b" + bin, col) { IntValue = val });
				}
				else
				{
					int start = i;
					while (i < len && (char.IsDigit(line[i]))) i++;
					string num = line[start..i];
					int val = int.Parse(num);
					tokens.Add(new Token(TokenKind.IntLiteral, num, col) { IntValue = val });
				}
				continue;
			}

			// Directives: .something
			if (c == '.' && i + 1 < len && (char.IsLetter(line[i + 1]) || line[i + 1] == '_'))
			{
				int start = i;
				i++;
				while (i < len && (char.IsLetterOrDigit(line[i]) || line[i] == '_')) i++;
				tokens.Add(new Token(TokenKind.Directive, line[start..i].ToLowerInvariant(), col));
				continue;
			}

			// Identifiers, mnemonics, registers
			if (char.IsLetter(c) || c == '_')
			{
				int start = i;
				while (i < len && (char.IsLetterOrDigit(line[i]) || line[i] == '_')) i++;
				string word = line[start..i];

				// Check for X/Y/Z indirect addressing with + (e.g. "X+", "Y+", "Z+")
				// These are handled at parser level, not here; just emit Identifier/Register
				tokens.Add(new Token(TokenKind.Identifier, word, col));
				continue;
			}

			// Two-character operators
			if (c == '<' && i + 1 < len && line[i + 1] == '<') { tokens.Add(new Token(TokenKind.LShift, "<<", col)); i += 2; continue; }
			if (c == '>' && i + 1 < len && line[i + 1] == '>') { tokens.Add(new Token(TokenKind.RShift, ">>", col)); i += 2; continue; }

			// Single-character tokens
			switch (c)
			{
				case ',': tokens.Add(new Token(TokenKind.Comma, ",", col)); i++; break;
				case ':': tokens.Add(new Token(TokenKind.Colon, ":", col)); i++; break;
				case '(': tokens.Add(new Token(TokenKind.LParen, "(", col)); i++; break;
				case ')': tokens.Add(new Token(TokenKind.RParen, ")", col)); i++; break;
				case '+': tokens.Add(new Token(TokenKind.Plus, "+", col)); i++; break;
				case '-': tokens.Add(new Token(TokenKind.Minus, "-", col)); i++; break;
				case '*': tokens.Add(new Token(TokenKind.Star, "*", col)); i++; break;
				case '/': tokens.Add(new Token(TokenKind.Slash, "/", col)); i++; break;
				case '%': tokens.Add(new Token(TokenKind.Percent, "%", col)); i++; break;
				case '&': tokens.Add(new Token(TokenKind.Amp, "&", col)); i++; break;
				case '|': tokens.Add(new Token(TokenKind.Pipe, "|", col)); i++; break;
				case '^': tokens.Add(new Token(TokenKind.Caret, "^", col)); i++; break;
				case '~': tokens.Add(new Token(TokenKind.Tilde, "~", col)); i++; break;
				case '!': tokens.Add(new Token(TokenKind.Bang, "!", col)); i++; break;
				case '=': tokens.Add(new Token(TokenKind.Equal, "=", col)); i++; break;
				case '@': i++; break; // skip
				default: i++; break; // unknown, skip
			}
		}

		tokens.Add(new Token(TokenKind.EOL, "", len));
		return tokens;
	}

	private static bool IsHexDigit(char c) =>
		(c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

	private static int EscapeChar(char c) => c switch
	{
		'n' => '\n', 't' => '\t', 'r' => '\r', '0' => 0, _ => c
	};
}
