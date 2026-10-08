namespace AVR8Sharp.Core.Utils;

/// <summary>
/// Result of parsing an Intel HEX image. <see cref="MaxAddress"/> is the highest byte address
/// the image writes (inclusive, 0 when it writes nothing), <see cref="ByteCount"/> the number of
/// data bytes it carries, and <see cref="Errors"/> every problem found (empty when the image is clean).
/// </summary>
public sealed record HexInfo (int MaxAddress, int ByteCount, IReadOnlyList<string> Errors);

/// <summary>
/// Intel HEX parser shared by <see cref="AvrRunner"/> and everything built on it. Handles data
/// (00), EOF (01), extended segment address (02) and extended linear address (04) records,
/// ignores start-address records (03, 05) and verifies every record checksum. It never throws on
/// malformed input; problems are reported in <see cref="HexInfo.Errors"/>.
/// </summary>
public static class IntelHex
{
	/// <summary>
	/// Parses <paramref name="source"/> into <paramref name="target"/>. Bytes that fall outside
	/// the target are dropped and reported as an error. When <paramref name="strict"/> is false a
	/// record with a bad checksum is still applied (it is reported either way).
	/// </summary>
	public static HexInfo Parse (string source, byte[] target, bool strict = true)
	{
		var errors = new List<string> ();
		long maxAddress = -1;
		var byteCount = 0;
		long baseAddress = 0;
		var lineNo = 0;
		var overflowReported = false;
		Span<byte> record = stackalloc byte[5 + 255];

		foreach (var rawLine in source.AsSpan ().EnumerateLines ()) {
			lineNo++;
			var line = rawLine.Trim ();
			if (line.Length == 0) continue;
			if (line[0] != ':') {
				errors.Add ($"line {lineNo}: missing ':' record mark");
				continue;
			}
			var hexChars = line.Length - 1;
			if (hexChars < 10 || (hexChars & 1) != 0 || hexChars / 2 > record.Length) {
				errors.Add ($"line {lineNo}: bad record length");
				continue;
			}
			var total = hexChars / 2;
			var bad = false;
			for (var i = 0; i < total; i++) {
				var hi = Digit (line[1 + i * 2]);
				var lo = Digit (line[2 + i * 2]);
				if (hi < 0 || lo < 0) {
					bad = true;
					break;
				}
				record[i] = (byte)(hi << 4 | lo);
			}
			if (bad) {
				errors.Add ($"line {lineNo}: non-hex character");
				continue;
			}
			var count = record[0];
			if (total != 5 + count) {
				errors.Add ($"line {lineNo}: length field {count} does not match record size");
				continue;
			}
			var sum = 0;
			for (var i = 0; i < total; i++) sum += record[i];
			if ((sum & 0xff) != 0) {
				errors.Add ($"line {lineNo}: checksum mismatch");
				if (strict) continue;
			}
			var address = record[1] << 8 | record[2];
			var type = record[3];
			var data = record.Slice (4, count);

			switch (type) {
				case 0x00:
					for (var i = 0; i < count; i++) {
						var addr = baseAddress + address + i;
						if (addr > maxAddress) maxAddress = addr;
						byteCount++;
						if (addr < target.Length) {
							target[addr] = data[i];
						} else if (!overflowReported) {
							errors.Add ($"line {lineNo}: address 0x{addr:X} is beyond the {target.Length}-byte flash");
							overflowReported = true;
						}
					}
					break;
				case 0x01:
					return Finish ();
				case 0x02:
					if (count != 2) errors.Add ($"line {lineNo}: extended segment record needs 2 data bytes");
					else baseAddress = (long)(data[0] << 8 | data[1]) << 4;
					break;
				case 0x04:
					if (count != 2) errors.Add ($"line {lineNo}: extended linear record needs 2 data bytes");
					else baseAddress = (long)(data[0] << 8 | data[1]) << 16;
					break;
				case 0x03:
				case 0x05:
					break;
				default:
					errors.Add ($"line {lineNo}: unknown record type 0x{type:X2}");
					break;
			}
		}
		return Finish ();

		HexInfo Finish () => new (maxAddress < 0 ? 0 : (int)Math.Min (maxAddress, int.MaxValue), byteCount, errors);
	}

	static int Digit (char c) => c switch {
		>= '0' and <= '9' => c - '0',
		>= 'A' and <= 'F' => c - 'A' + 10,
		>= 'a' and <= 'f' => c - 'a' + 10,
		_ => -1,
	};
}
