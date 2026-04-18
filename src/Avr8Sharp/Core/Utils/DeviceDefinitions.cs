namespace AVR8Sharp.Core.Utils;

/// <summary>
/// Pre-defined hardware constants for an AVR microcontroller.
/// Register addresses are factual hardware data from publicly available datasheets.
/// Low I/O registers (0x00–0x3F) use I/O addresses (for IN/OUT/SBI/CBI).
/// Extended I/O registers (0x60+) use data-space addresses (for LDS/STS).
/// </summary>
public class DeviceDefinition
{
	public string Name { get; init; } = string.Empty;
	public int FlashSize { get; init; }
	public int SramSize { get; init; }
	public int EepromSize { get; init; }
	public Dictionary<string, int> Symbols { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Registry of known AVR device definitions.
/// All register addresses are factual hardware data (not copyrightable)
/// derived from publicly available Microchip/Atmel datasheets.
/// </summary>
public static class DeviceDefinitions
{
	private static readonly Dictionary<string, DeviceDefinition> Devices = new(StringComparer.OrdinalIgnoreCase);

	static DeviceDefinitions()
	{
		Register(ATmega328P());
		Register(ATtiny85());
	}

	/// <summary>
	/// Look up a device definition by name (case-insensitive).
	/// Returns null if the device is not known.
	/// </summary>
	public static DeviceDefinition? Get(string name) =>
		Devices.GetValueOrDefault(name);

	/// <summary>
	/// Returns the names of all registered devices.
	/// </summary>
	public static IEnumerable<string> KnownDevices => Devices.Keys;

	/// <summary>
	/// Register a custom device definition at runtime.
	/// </summary>
	public static void Register(DeviceDefinition def) => Devices[def.Name] = def;

	// -----------------------------------------------------------------------
	// ATmega328P  (Arduino Uno / Nano / Pro Mini)
	// Datasheet: DS40002061B — Microchip ATmega48A/PA/88A/PA/168A/PA/328/P
	// -----------------------------------------------------------------------
	private static DeviceDefinition ATmega328P() => new()
	{
		Name = "ATmega328P",
		FlashSize = 0x8000,   // 32 KB
		SramSize = 2048,
		EepromSize = 1024,
		Symbols = new(StringComparer.OrdinalIgnoreCase)
		{
			// ── Memory limits ───────────────────────────────
			["RAMEND"]   = 0x08FF,
			["FLASHEND"] = 0x7FFF,
			["E2END"]    = 0x03FF,

			// ── I/O port B ──────────────────────────────────
			["PINB"]  = 0x03,
			["DDRB"]  = 0x04,
			["PORTB"] = 0x05,

			// ── I/O port C ──────────────────────────────────
			["PINC"]  = 0x06,
			["DDRC"]  = 0x07,
			["PORTC"] = 0x08,

			// ── I/O port D ──────────────────────────────────
			["PIND"]  = 0x09,
			["DDRD"]  = 0x0A,
			["PORTD"] = 0x0B,

			// ── Timer/Counter 0 (8-bit) ─────────────────────
			["TIFR0"]  = 0x15,
			["TCCR0A"] = 0x24,
			["TCCR0B"] = 0x25,
			["TCNT0"]  = 0x26,
			["OCR0A"]  = 0x27,
			["OCR0B"]  = 0x28,

			// ── Timer/Counter 1 (16-bit) — extended I/O ─────
			["TIFR1"]  = 0x16,
			["TCCR1A"] = 0x80,
			["TCCR1B"] = 0x81,
			["TCCR1C"] = 0x82,
			["TCNT1L"] = 0x84,
			["TCNT1H"] = 0x85,
			["ICR1L"]  = 0x86,
			["ICR1H"]  = 0x87,
			["OCR1AL"] = 0x88,
			["OCR1AH"] = 0x89,
			["OCR1BL"] = 0x8A,
			["OCR1BH"] = 0x8B,

			// ── Timer/Counter 2 (8-bit async) ───────────────
			["TIFR2"]  = 0x17,
			["TCCR2A"] = 0xB0,
			["TCCR2B"] = 0xB1,
			["TCNT2"]  = 0xB2,
			["OCR2A"]  = 0xB3,
			["OCR2B"]  = 0xB4,

			// ── Timer interrupt masks — extended I/O ────────
			["TIMSK0"] = 0x6E,
			["TIMSK1"] = 0x6F,
			["TIMSK2"] = 0x70,

			// ── SPI ─────────────────────────────────────────
			["SPCR"] = 0x2C,
			["SPSR"] = 0x2D,
			["SPDR"] = 0x2E,

			// ── USART0 — extended I/O ───────────────────────
			["UCSR0A"] = 0xC0,
			["UCSR0B"] = 0xC1,
			["UCSR0C"] = 0xC2,
			["UBRR0L"] = 0xC4,
			["UBRR0H"] = 0xC5,
			["UDR0"]   = 0xC6,

			// ── ADC — extended I/O ──────────────────────────
			["ADMUX"]  = 0x7C,
			["ADCSRA"] = 0x7A,
			["ADCSRB"] = 0x7B,
			["ADCL"]   = 0x78,
			["ADCH"]   = 0x79,

			// ── TWI — extended I/O ──────────────────────────
			["TWBR"]  = 0xB8,
			["TWSR"]  = 0xB9,
			["TWAR"]  = 0xBA,
			["TWDR"]  = 0xBB,
			["TWCR"]  = 0xBC,
			["TWAMR"] = 0xBD,

			// ── EEPROM ──────────────────────────────────────
			["EECR"]  = 0x1F,
			["EEDR"]  = 0x20,
			["EEARL"] = 0x21,
			["EEARH"] = 0x22,

			// ── Misc system registers ───────────────────────
			["MCUSR"]  = 0x34,
			["MCUCR"]  = 0x35,
			["SMCR"]   = 0x33,
			["SPL"]    = 0x3D,
			["SPH"]    = 0x3E,
			["SREG"]   = 0x3F,
			["WDTCSR"] = 0x60,

			// ── External interrupts ─────────────────────────
			["EICRA"]  = 0x69,
			["EIMSK"]  = 0x1D,
			["EIFR"]   = 0x1C,
			["PCICR"]  = 0x68,
			["PCIFR"]  = 0x1B,
			["PCMSK0"] = 0x6B,
			["PCMSK1"] = 0x6C,
			["PCMSK2"] = 0x6D,

			// ── SREG bit positions ──────────────────────────
			["SREG_C"] = 0,
			["SREG_Z"] = 1,
			["SREG_N"] = 2,
			["SREG_V"] = 3,
			["SREG_S"] = 4,
			["SREG_H"] = 5,
			["SREG_T"] = 6,
			["SREG_I"] = 7,
		}
	};

	// -----------------------------------------------------------------------
	// ATtiny85  (Digispark / Trinket)
	// Datasheet: DS40002311A — Microchip ATtiny25/45/85
	// -----------------------------------------------------------------------
	private static DeviceDefinition ATtiny85() => new()
	{
		Name = "ATtiny85",
		FlashSize = 0x2000,   // 8 KB
		SramSize = 512,
		EepromSize = 512,
		Symbols = new(StringComparer.OrdinalIgnoreCase)
		{
			// ── Memory limits ───────────────────────────────
			["RAMEND"]   = 0x025F,
			["FLASHEND"] = 0x1FFF,
			["E2END"]    = 0x01FF,

			// ── I/O port B (only port) ──────────────────────
			["PINB"]  = 0x16,
			["DDRB"]  = 0x17,
			["PORTB"] = 0x18,

			// ── Timer/Counter 0 (8-bit) ─────────────────────
			["TCCR0A"] = 0x2A,
			["TCCR0B"] = 0x33,
			["TCNT0"]  = 0x32,
			["OCR0A"]  = 0x29,
			["OCR0B"]  = 0x28,
			["TIMSK"]  = 0x39,
			["TIFR"]   = 0x38,

			// ── Timer/Counter 1 (8-bit high-speed) ──────────
			["TCCR1"]  = 0x30,
			["TCNT1"]  = 0x2F,
			["OCR1A"]  = 0x2E,
			["OCR1B"]  = 0x2B,
			["OCR1C"]  = 0x2D,

			// ── USI ─────────────────────────────────────────
			["USICR"] = 0x0D,
			["USISR"] = 0x0E,
			["USIDR"] = 0x0F,

			// ── ADC ─────────────────────────────────────────
			["ADMUX"]  = 0x07,
			["ADCSRA"] = 0x06,
			["ADCL"]   = 0x04,
			["ADCH"]   = 0x05,

			// ── EEPROM ──────────────────────────────────────
			["EECR"]  = 0x1C,
			["EEDR"]  = 0x1D,
			["EEARL"] = 0x1E,
			["EEARH"] = 0x1F,

			// ── Misc system registers ───────────────────────
			["MCUSR"]  = 0x34,
			["MCUCR"]  = 0x35,
			["SPL"]    = 0x3D,
			["SPH"]    = 0x3E,
			["SREG"]   = 0x3F,
			["WDTCR"]  = 0x21,
			["GIMSK"]  = 0x3B,
			["GIFR"]   = 0x3A,
			["PCMSK"]  = 0x15,

			// ── SREG bit positions ──────────────────────────
			["SREG_C"] = 0,
			["SREG_Z"] = 1,
			["SREG_N"] = 2,
			["SREG_V"] = 3,
			["SREG_S"] = 4,
			["SREG_H"] = 5,
			["SREG_T"] = 6,
			["SREG_I"] = 7,
		}
	};
}
