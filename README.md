# Avr8Sharp

![Build Status](https://github.com/PyMCU/avr8sharp/actions/workflows/ci.yml/badge.svg)
[![NuGet](https://img.shields.io/nuget/v/Avr8Sharp.svg)](https://www.nuget.org/packages/Avr8Sharp)
![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET Version](https://img.shields.io/badge/.NET-10.0-purple)

**Avr8Sharp** is a high-performance emulator for the **AVR 8-bit microcontroller** family,
written entirely in modern **C# (.NET 10)**. It runs real, unmodified Arduino and AVR
firmware — and is designed as a deterministic **firmware testkit** for validating the output
of the [PyMCU](https://docs.pymcu.org) compiler in CI.

It is a C# port and re-imagination of [avr8js](https://github.com/wokwi/avr8js) by
Uri Shaked, with accuracy fixes and .NET-specific optimizations.

```bash
dotnet add package Avr8Sharp
dotnet add package Avr8Sharp.TestKit   # fluent harness for firmware tests
```

```csharp
using Avr8Sharp.TestKit.Boards;

var uno = new ArduinoUnoSimulation()
    .WithHex(File.ReadAllText("sketch.hex"));

uno.RunMilliseconds(500);

uno.PortB.Should().HavePinHigh(5);    // LED_BUILTIN
uno.Serial.Should().Contain("Hello");
```

---

## Supported chips

| Board preset | Chip | Flash | SRAM | Timers | Serial |
|---|---|---|---|---|---|
| `ArduinoUnoSimulation` | ATmega328P | 32 KB | 2 KB | 0–2 | USART0 |
| `ArduinoMegaSimulation` | ATmega2560 | 256 KB | 8 KB | 0–5 | USART0–3 |
| `ATtiny85Simulation` | ATtiny85 | 8 KB | 512 B | TC0, TC1 | USI |

Any AVR 8-bit device can be modelled via `AvrTestSimulation.Create()` with custom
peripheral configs.

## Features

- **Full AVR Enhanced instruction set** — all ATmega328P / ATmega2560 instructions
- **Datasheet-accurate flags** — half-carry, overflow, sign, and carry verified against
  Microchip datasheets; accuracy bugs from avr8js corrected
- **Board presets** — Uno, Mega 2560, ATtiny85 with all peripherals wired up
- **Peripherals** — GPIO, USART, Timers (8/16-bit), SPI, TWI (I²C), ADC, EEPROM, USI,
  Watchdog
- **Fluent TestKit** — `RunUntilSerial`, `RunToBreak`, `RunUntilMs` with bounded,
  never-hanging runs
- **FluentAssertions extensions** — CPU, GPIO, memory, and serial probe assertions
- **Inline assembly** — load and run short assembly snippets without an external toolchain
- **AOT-friendly** — trimmable and NativeAOT-compatible

## Getting Started

```bash
git clone https://github.com/PyMCU/avr8sharp.git
cd avr8sharp
dotnet restore
dotnet build
```

**Run the tests:**

```bash
dotnet test
```

## TestKit

```csharp
using Avr8Sharp.TestKit.Boards;

var uno = new ArduinoUnoSimulation()
    .WithHex(File.ReadAllText("blink.hex"));

// Blocks until "PASS" appears on Serial, or throws TimeoutException after 5 s
uno.RunUntilSerial(uno.Serial, "PASS", maxMs: 5000);

uno.Serial.Should().Contain("PASS");
uno.PortB.Should().HavePinHigh(5);
```

### Validating firmware in CI

```csharp
[Test]
public void Firmware_output_matches_expected()
{
    var mega = new ArduinoMegaSimulation()
        .WithHex(File.ReadAllText("firmware.hex"));

    mega.RunUntilSerial(mega.Serial0, "done", maxMs: 3000);

    mega.Serial0.Should().Contain("done");
    // Cycle-count regression guard — deterministic across machines
    Assert.That(mega.Cpu.Cycles, Is.LessThanOrEqualTo(1_000_000));
}
```

## Solution structure

| Project | Description |
|---|---|
| `src/Avr8Sharp` | Core library — CPU, instruction decoders, peripherals |
| `src/Avr8Sharp.TestKit` | Fluent test harness, board presets, probes, assertions |
| `tests/Avr8Sharp.Tests` | Unit and integration tests |

## Architecture notes

- **Instruction decoder:** 65 536-entry flat LUT of `delegate*` function pointers —
  O(1) dispatch with no opcode branch (`NativeLutDecoder`)
- **SREG lazy evaluation:** arithmetic flags written to a split field (`_sregArith`);
  I/T flags always accurate in `_ram[95]` — avoids redundant flag computation
- **ProcessClockEvents O(1):** circular deque with `_clockHead` pointer; no array shift
  on timer reprogramming
- **MmioController inline chains:** up to 4 write hooks per address stored inline in a
  value struct — no closure allocations

## Roadmap

### Core / CPU
- [x] Full AVR Enhanced instruction set (ATmega328P / ATmega2560)
- [x] ATtiny85 Reduced Core
- [x] Interrupts, SREG, SP, cycle-accurate timing
- [x] Accuracy fixes: half-carry in ADC, CPI, NEG, SBIW, SUBI
- [ ] `SPM` (self-programming flash)

### Peripherals
- [x] GPIO with pin-change and external interrupts
- [x] USART (multiple channels on Mega)
- [x] Timers 0–5 (8-bit and 16-bit)
- [x] SPI
- [x] TWI / I²C (master mode)
- [x] ADC
- [x] EEPROM (read/write/erase)
- [x] USI
- [x] Watchdog
- [ ] ATtiny85 TC1 CTC and PWM modes
- [ ] TWI slave mode

### Ecosystem
- [x] Board presets: Uno, Mega 2560, ATtiny85
- [x] FluentAssertions extensions
- [x] Inline AVR assembler
- [ ] DocFX API reference

## Contributing

1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/my-feature`).
3. Ensure all tests pass (`dotnet test`).
4. Commit following [Conventional Commits](https://www.conventionalcommits.org/).
5. Open a Pull Request against `main`.

## License

MIT License — see [LICENSE](LICENSE).

Based on the original work from [avr8js](https://github.com/wokwi/avr8js) © 2019–present Uri Shaked.  
C# Port © 2025–present Iván Montiel Cardona.
