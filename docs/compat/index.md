# Compatibility

Avr8Sharp runs real, unmodified AVR firmware images.

## Arduino (avr-gcc / Arduino IDE)

Sketches compiled with the standard Arduino toolchain (avr-gcc, arduino-cli, Arduino IDE)
run without modification. Load the `.hex` output directly:

```csharp
var uno = new ArduinoUnoSimulation()
    .WithHex(File.ReadAllText("build/sketch.ino.hex"));
```

The three supported targets and their arduino-cli FQBNs:

| Board | FQBN | Preset |
|---|---|---|
| Arduino Uno | `arduino:avr:uno` | `ArduinoUnoSimulation` |
| Arduino Mega 2560 | `arduino:avr:mega` | `ArduinoMegaSimulation` |
| ATtiny85 | `attiny:avr:ATtinyX5:cpu=attiny85,clock=internal8` | `ATtiny85Simulation` |

## Bare-metal (avr-gcc, PyMCU)

Any firmware built with a standard AVR-GCC toolchain runs directly. This is the primary
use case for [PyMCU](https://docs.pymcu.org), which compiles Python to AVR machine code and
uses Avr8Sharp as its ATmega/ATtiny integration testkit.

## Instruction set coverage

All instructions of the AVR Enhanced Architecture (ATmega328P / ATmega2560) are
emulated. The ATtiny85 uses the Reduced Core (no `MUL`/`MULS`, no `MOVW` on all
registers), which is a strict subset.

Known limitations:

- `SPM` (Store Program Memory) — not emulated; self-programming flash is not modelled.
- ATtiny85 TC1 CTC and PWM modes — not emulated (Normal mode only).
- TWI slave state machine — not emulated (master mode only).

## Accuracy fixes vs. avr8js

The following flag bugs present in the upstream avr8js TypeScript source have been
corrected in Avr8Sharp:

| Instruction | Bug | Fix |
|---|---|---|
| `ADC` | H flag computed from bit 0 instead of bit 3 | `8 &` instead of `1 &` |
| `CPI` | H flag computed from bit 0 instead of bit 3 | `8 &` instead of `1 &` |
| `NEG` | H flag computed from bit 0 instead of bit 3 | `8 &` instead of `1 &` |
| `SBIW` | H flag computed from bit 0 instead of bit 3 | `8 &` instead of `1 &` |
| `SUBI` | H flag computed from bit 0 instead of bit 3 | `8 &` instead of `1 &` |

The half-carry flag (H) is set when there is a carry out of bit 3 (nibble boundary). The
nibble-carry expression `(d&r)|(r&~R)|(~R&d)` computes carry at every bit position
simultaneously; extracting bit 3 with mask `8` gives the correct H flag. The upstream code
used mask `1` (bit 0), which produces the wrong result for any operand pair where the
nibble carry differs from the bit-0 carry.
