# Avr8Sharp

**Avr8Sharp** is a high-performance emulator for the **AVR 8-bit microcontroller** family,
written entirely in modern **C# (.NET 10)**. It runs real, unmodified Arduino and AVR firmware
and is designed as a deterministic **firmware testkit** — for example, validating the output
of the [PyMCU](https://docs.pymcu.org) compiler in CI.

It is a C# port and re-imagination of [avr8js](https://github.com/wokwi/avr8js) by
Uri Shaked, with additional accuracy fixes and .NET-specific optimizations.

```bash
dotnet add package Avr8Sharp
dotnet add package Avr8Sharp.TestKit   # fluent harness for firmware tests
```

```csharp
using Avr8Sharp.TestKit.Boards;

var sim = new ArduinoUnoSimulation()
    .WithHex(File.ReadAllText("sketch.hex"));

sim.RunMilliseconds(500);

sim.PortB.Should().HavePinHigh(5);    // digital pin 13 (LED_BUILTIN)
sim.Serial.Should().Contain("Hello");
```

---

## Why Avr8Sharp

::::{grid} 1 2 2 3
:gutter: 3

:::{grid-item-card} Real firmware, unmodified
Executes precompiled AVR firmware — Arduino sketches, avr-gcc output, PyMCU-generated
binaries — without patches or shims.
:::

:::{grid-item-card} Deterministic by design
Time is driven by executed cycles, never by wall-clock. Runs are reproducible across
machines — ideal for CI and compiler regression checks.
:::

:::{grid-item-card} Never hangs in CI
Bounded execution: wedged or crashed firmware fails a test with a diagnostic reason
instead of stalling the build.
:::

:::{grid-item-card} Accurate flags
Half-carry, overflow, sign, and carry flags match the ATmega datasheet for all supported
instructions. Accuracy bugs inherited from the parent avr8js repo have been corrected.
:::

:::{grid-item-card} Board presets
`ArduinoUnoSimulation`, `ArduinoMegaSimulation`, and `ATtiny85Simulation` wire up all
peripherals automatically — ports, timers, serial, EEPROM, ready to use.
:::

:::{grid-item-card} AOT-friendly
The core library is trimmable and NativeAOT-compatible — embed it in mobile apps, CLI
tools, or circuit simulators.
:::
::::

---

## How it compares to avr8js

| | avr8js | **Avr8Sharp** |
|---|---|---|
| Language | TypeScript | **C# (.NET 10)** |
| Half-carry accuracy | Several bugs | **Fixed (ADC, CPI, NEG, SBIW, SUBI)** |
| Test harness | — | **Fluent TestKit + assertions** |
| Board presets | — | **Uno, Mega 2560, ATtiny85** |
| CI guardrails | — | **Bounded runs, health assertions** |
| Deterministic clock | Yes | **Yes** |
| AOT / trimming | n/a | **Yes** |

---

```{toctree}
:maxdepth: 1
:hidden:
:caption: Getting Started

getting-started/index
```

```{toctree}
:maxdepth: 1
:hidden:
:caption: Guides

guides/index
```

```{toctree}
:maxdepth: 1
:hidden:
:caption: Peripherals

peripherals/index
```

```{toctree}
:maxdepth: 1
:hidden:
:caption: Compatibility

compat/index
```

```{toctree}
:maxdepth: 1
:hidden:
:caption: Reference

reference/index
```
