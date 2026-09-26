# Changelog

All notable changes to this project will be documented in this file.

---

## [v2.0.0-beta1] — unreleased

### Renamed: the TestKit is now `SiliconTwin.AVR`

The TestKits across the portfolio were named three different ways — `RP2040Sharp.TestKit`,
`RP2350.TestKit`, `Avr8Sharp.TestKit`. They now share one prefix:

| Old | New |
|---|---|
| `Avr8Sharp.TestKit` | `SiliconTwin.AVR` |
| `Avr8Sharp.TestKit.Core` | `SiliconTwin.AVR.Core` |

The old ids are unlisted. Update the `PackageReference`.

`Avr8Sharp` — the emulator core — is *not* renamed. It is what embedders integrate, and it
carries no entitlement check.

For the whole portfolio in one dependency, there is now a `SiliconTwin` metapackage.

### Breaking: the TestKit checks entitlement in CI

Building a simulation now calls `SiliconTwin.Licensing.Entitlement.Require("avr")`.
This is what the BUSL-1.1 licence has said since the relicensing; it is now enforced
rather than only written down.

Who is affected:

- **Local development — nothing changes.** No CI environment detected means free, always.
  An expired or missing licence never stops anyone debugging on their own machine.
- **Public repositories — nothing changes.** Free, and verified against the
  `repository_visibility` claim of a GitHub-signed OIDC token, so nothing has to be
  taken on trust and nothing is sent anywhere.
- **Private CI — needs a licence.** Put the key in the `SILICONTWIN_LICENSE` secret, or
  at `~/.silicontwin/license.key` on a self-hosted runner.
  See <https://silicontwin.co/licensing>.

Every 1.x release stays on NuGet, unchanged and unlisted by nobody. Staying on 1.x is a
supported choice.

Verification is entirely local: an RSA signature check plus GitHub's public keys. Firmware,
test data and results never leave the runner, and air-gapped runners work.

On GitHub Actions the job needs an identity token, since a licence that is bound to no
organisation would work in any of them:

```yaml
permissions:
  contents: read
  id-token: write
```

---

## [v1.1.0-beta10] - 2026-09-26

### Bug Fixes

The first two are timing divergences found by putting the emulator and an Arduino Uno on
the same oscilloscope, 2026-09-14. Both change what a cycle-accurate or waveform test reads, so a
suite that pinned the old numbers will need updating.

- **Fast PWM pulse width** (#17). The compare output was held for `OCRnx` counts; the chip
  holds it for `OCRnx + 1`, because the waveform generator updates `OCnx` on the timer clock
  after the match. `OCR0A = 128` at 16 MHz now reads 129 of 256 counts, as the scope does.
  Both extremes the datasheet names come out right too: `OCRnx` at BOTTOM is a one-count
  spike per period rather than a constant high, and `OCRnx` at MAX is constantly high. The
  compare interrupt flag still fires on the match itself, and phase correct PWM, which was
  already correct, is unchanged.
- **CBI cycle cost** (#18). `CBI` was charged one cycle, the reduced-core timing. On the
  classic AVR core it takes two, like `SBI`. Tight bit-banging loops ran fast by the
  difference; an `SBI`/`CBI`/`RJMP` toggle loop now takes the 6 cycles the board takes.
- **A write to UCSRA no longer clears the read-only status bits.** RXC, TXC, UDRE, FE, DOR
  and UPE are read-only to firmware (datasheet 19.10.2) and a write to UCSRA now leaves them
  as they were. TXC still clears when a one is written to it, which is the only way the chip
  clears it. The old behaviour cleared all six, which is the cause of wokwi/avr8js#158: a
  second `Serial.begin()` writes UCSRA to select double speed, UDRE goes to zero, and the
  UCSRB write that follows does not put it back because TXEN is already set, so every later
  `while (!(UCSRA & UDRE))` spins forever. The same write also stranded a received byte by
  clearing RXC while the byte stayed in the receive buffer.
- **XCH, LAC, LAS and LAT are refused on the classic core** (#19). They are XMEGA-only
  instructions and their opcodes are undefined on the ATmega and ATtiny parts, yet the
  emulator executed them for one cycle. They now raise `AvrUnsupportedInstructionException`
  naming the PC, opcode and mnemonic, unless the new `Cpu.Core` is `AvrCore.Xmega`, where
  they cost the manual's two cycles. `Cpu.Core` defaults to `AvrCore.Classic`. All three
  decoders agree.

### New: instruction-level execution counters

`CountingDecoder`, a struct twin of `NativeLutDecoder`, records a full run's counters
with no per-instruction callback: executions and cycle totals per PC (`ExecutionCounts.PcCount`,
`PcCycles`), and taken/not-taken counts for the conditional instructions (the BRBS/BRBC
family, SBRC/SBRS, SBIC/SBIS, CPSE; RJMP/JMP/CALL/RET count as executed only). "Taken" is
decided by comparing the post-instruction PC with the fall-through address, so a branch to
its own fall-through reads as not-taken. Measured on 16M-cycle runs of the bundled
firmwares it costs ~1.3x the plain native decoder, within ~10% of a `ProfilingDecoder`
whose callback only increments `counts[pc]`, while carrying cycle and branch data the
callback does not, and it is reachable from the native library, where a per-instruction
round trip is not viable.

- **TestKit**: `EnableCounting()`/`DisableCounting()` make every `Run*` collect;
  `RunCyclesCounted`/`RunUntilCounted` return the `ExecutionCounts` directly. `Counts`
  stays readable after disabling, and `ExecutionCounts.ToJson()` dumps the non-zero
  counters as JSON.
- **C ABI**: `a8s_counting_enable`/`a8s_counting_disable`, `a8s_counts_len`, and
  `a8s_counts_read` (two-pass, raw little-endian u32/u64 arrays).
- **Python**: `sim.enable_counting()` / `sim.disable_counting()` /
  `sim.counts() -> (pc_count, pc_cycles, branch_taken, branch_not_taken)` as
  `array.array`s indexed by word PC.

### New: external stimulus and bus probes

- **TestKit**: `RunMicroseconds`, the microsecond sibling of `RunMilliseconds`, for driving a
  pin pulse or a sensor's response delay between writes.
- **C ABI**: `a8s_gpio_set_pin` drives a pin the firmware reads as an input (no effect on an
  output, as on the chip), `a8s_run_us` steps in microseconds, and `a8s_serial_inject_bytes`
  injects several USART RX bytes in one call. The I2C slave stub answers a set of addresses
  and journals its transactions, and the SPI and TWI response queues can be shared on demand.
- **Python**: `Port.set`/`set_high`/`set_low`, `Simulation.run_us` and
  `Serial.inject_bytes`; timed-response helpers `wait_for_rise`/`wait_for_fall`,
  `respond_to_rise` and `hc_sr04_echo`; `twi.events` and `twi.reads` probes; and
  `sim.share_bus_responses()`.

---

## [v1.1.0-beta3] — 2026-06-10

### Performance

- **PC advance: drop the per-instruction modulo.** Each decoder advanced the
  program counter with `Pc = (Pc + 1) % ProgramMemory.Length`, paying a division
  on *every* instruction. It is now a branch that only divides on the rare wrap
  path (end of flash, or a jump/branch that left `Pc` out of range via uint
  under/overflow), which is exactly equivalent for any program length — power of
  two or not. Measured **+19%** end-to-end throughput in a host harness
  (~163 → ~193 MIPS on the native decoder).

### New: stack diagnostics

- **Stack underflow** on `RET`/`RETI` now throws `AvrStackUnderflowException`
  instead of surfacing as an opaque `IndexOutOfRangeException`.
- **Stack overflow** on `PUSH`/`CALL`/`RCALL`/`ICALL` and interrupt dispatch now
  throws `AvrStackOverflowException` when the write would drop below
  `Cpu.StackLowLimit` (a chip's RAMSTART). The limit defaults to `0`, which
  disables the check, so raw cores and unit tests that park SP low are
  unaffected; boards set it to the chip's SRAM start to catch the condition.

---

## [v1.1.0-beta1] — 2026-04-11

### Packages

| Package | NuGet |
|---|---|
| `Avr8Sharp` | Core AVR simulator |
| `Avr8Sharp.TestKit` | FluentAssertions-based test harness *(new)* |

---

### Breaking Changes

#### Namespace restructure (from v1.0.2)

All types have been reorganised under `AVR8Sharp.Core.*`. The CPU sub-namespace has been eliminated — `Cpu` and all related types live directly in `AVR8Sharp.Core`:

| Before (v1.0.2) | After (v1.1.0-beta1) |
|---|---|
| `using AVR8Sharp;` | `using AVR8Sharp.Core;` |
| `using AVR8Sharp.Cpu;` | `using AVR8Sharp.Core;` |
| `using AVR8Sharp.Peripherals;` | `using AVR8Sharp.Core.Peripherals;` |
| `using AVR8Sharp.Utils;` | `using AVR8Sharp.Core.Utils;` |
| `new AVR8Sharp.Cpu.Cpu(...)` | `new AVR8Sharp.Core.Cpu(...)` |

Affected types now in `AVR8Sharp.Core`: `Cpu`, `AvrInterruptConfig`, `ClockEventEntry`, `AvrInterrupt`, `Opcodes`.

#### Target framework

Multi-targeting (`netstandard1.2`, `netstandard2.0`, `net6.0`, `net8.0`) is dropped. The library now targets **net10.0** exclusively.

#### CPU API

`Cpu.Data` and `Cpu.DataView` are replaced by `Cpu.Mmio`. Direct SRAM reads and writes go through `Cpu.Mmio.Data[]`; I/O register hooks are registered via `Cpu.Mmio.RegisterWrite()`.

---

### New: Avr8Sharp.TestKit

`Avr8Sharp.TestKit` is a new companion package for integration-testing compiled AVR firmware. It wraps `Avr8Sharp` and provides a fluent, FluentAssertions-compatible API.

```csharp
using Avr8Sharp.TestKit;
using Avr8Sharp.TestKit.Boards;

var sim = new ArduinoUnoSimulation()
    .WithHex(File.ReadAllText("blink.hex"))
    .AddUsart(AvrUsart.Usart0Config, out var serial);

sim.RunMilliseconds(500);

serial.Should().Contain("Hello, world!");
sim.Cpu.Should().HaveRegister(16, 0x01);
```

#### Board presets

| Board | MCU | Clock | Peripherals |
|---|---|---|---|
| `ArduinoUnoSimulation` | ATmega328P | 16 MHz | Timers 0–2, USART0, SPI, TWI, ADC, GPIO B/C/D |
| `ArduinoMegaSimulation` | ATmega2560 | 16 MHz | Timers 0–5, USART0–3, SPI, TWI, ADC, GPIO A–L |
| `ATtiny85Simulation` | ATtiny85 | 8 MHz | Timers 0/1, USI, GPIO B |

#### Assertions

| Subject | Examples |
|---|---|
| `Cpu` | `.HaveRegister(n, v)`, `.HavePC(addr)`, `.HaveSP(addr)`, `.HaveCycles(n)`, `.HaveZeroFlag()`, `.HaveInterruptsEnabled()` |
| `AvrIoPort` | `.HavePinHigh(n)`, `.HavePinLow(n)`, `.HavePinState(n, state)`, `.HaveOutputValue(v)` |
| `AvrMemoryView` | `.HaveByteAt(addr, v)`, `.HaveWordAt(addr, v)`, `.HaveBytesAt(addr, bytes)` |
| `SerialProbe` | `.Contain(str)`, `.StartWith(str)`, `.Be(str)`, `.HaveLineCount(n)`, `.ContainByte(b)` |

#### Execution control

`AvrTestSimulation` exposes: `RunCycles`, `RunMilliseconds`, `RunInstructions`, `RunToBreak`, `RunToAddress`, `RunUntil`, `RunUntilSerial`.

---

### New Features

#### ATmega2560 Timer5 and USART3
`ArduinoMegaSimulation` now includes Timer5 (registers at `0x120`–`0x12C`) and USART3 (registers at `0x130`–`0x136`). Register-address fields in `AvrTimerConfig` and `AvrUsartConfig` were promoted from `byte` to `ushort`, and the internal interrupt-vector table was raised from 128 to 256 entries.

#### Pluggable instruction decoders

Three decoders are available, selectable via `AvrBuilder`:

| Decoder | Builder method | Notes |
|---|---|---|
| `NativeLutDecoder` | `.UseNativeDecoder()` | Unsafe function-pointer LUT — **default** |
| `LutDecoder` | `.UseLutDecoder()` | Delegate-based LUT |
| `SwitchDecoder` | `.UseSwitchDecoder()` | Switch/case |

#### SLEEP instruction
Executing `SLEEP` now invokes `AvrInterrupt.OnSleep` (if set) with the SM2:SM1:SM0 mode bits from SMCR. Previously it was a silent NOP.

#### SPI slave mode
`AvrSpi.SimulateIncomingMasterByte(byte)` lets external code drive a byte as an SPI master when the device is in slave mode (MSTR=0). The `OnSlaveTransfer` callback fires on each received byte.

#### TWI (I²C) slave mode

New methods on `AvrTwi`:

- `SimulateIncomingAddress(byte address, bool isWrite)` — matches TWAR (respecting the TWAMR address mask and general-call flag); sets TWSR to `0x60` (SLA+W), `0xA8` (SLA+R), or `0x70` (general call), and raises TWINT.
- `SimulateIncomingData(byte data)` — delivers a data byte in slave-receive mode; sets TWSR to `0x80`/`0x88` per TWEA.
- `ReadSlaveTransmitByte()` — reads the byte firmware placed in TWDR for slave-transmit.

#### ADC temperature sensor configurability
`AvrAdc.TemperatureVoltage` (default `0.378125 V` ≈ 25 °C) replaces the previously hardcoded value returned by the internal temperature sensor channel (mux 8 on ATmega328P).

#### USI start/stop condition callbacks
`AvrUsi` exposes `OnStartCondition` and `OnStopCondition` callbacks that fire when the two-wire mode port listener detects an I²C start or stop event on the correct SCL/SDA edge transitions.

---

### Bug Fixes

#### Nine datasheet accuracy corrections

Verified against ATmega328P, ATmega2560, and ATtiny85 datasheets:

- **Interrupt latency** — vector-fetch cycle was missing; ISR entry now takes the spec-required 4 cycles.
- **SREG on Reset** — the Global Interrupt Enable bit could survive a mid-execution reset; `SREG` is now cleared in `Reset()`.
- **BREAK instruction** — previously a silent NOP; now invokes `AvrInterrupt.OnBreakpoint`.
- **USART 9-bit RX** — data mask was `0xFF` instead of `0x1FF`, silently discarding the 9th bit.
- **PCMSK write scope** — a PCMSK write was re-evaluating all port groups instead of only the owning one.
- **Timer OC-C** — OCFC flag-clear and OCIE-C enable were not wired in the TIFR/TIMSK write hooks.
- **Timer input capture** — ICF/ICIE fields added to `AvrTimerConfig`; new public `TriggerCapture()` method latches TCNT into ICR and raises the capture interrupt.
- **ADC free-running mode** — `CompleteAdcRead()` now automatically restarts conversion when `ADATE=1` and `ADTS=000`.
- **MmioController hook chaining** — `RegisterWrite()` now chains hooks instead of overwriting, allowing multiple peripherals to share the same I/O register address.

#### EEPROM: realistic timing and register latching
The EEPROM peripheral now models the multi-cycle write timing specified in the datasheet. Register values are latched at the start of a write operation and held until it completes, preventing mid-write corruption from concurrent register access.

#### EEPROM: EEPM=11 (reserved mode)
Writing with `EEPM0=1` and `EEPM1=1` simultaneously is undefined per the datasheet. It is now treated as a no-op.

#### USI: clockSrc/mode bit extraction
`DelegateWriteHookUsicr` had an operator-precedence bug (`value & (MASK >> N)` instead of `(value & MASK) >> N`) that caused the wrong bits to be read for USICS and USIWM, making software-clocked shifts non-functional.

#### USI: pin masking
The output-pin bit mask in `UpdateOutput` was computed incorrectly, causing writes to the wrong DATA port bit on some pin configurations.

#### USART: 9-bit mode data mask
The RX data mask for 9-bit frames was using the wrong bit width after the initial datasheet fix, potentially still producing incorrect values when the 9th bit was set.

#### ATtiny85 Timer1 (TC1)
`ATtiny85Simulation` now includes Timer1: 8-bit, single `TCCR1` register at `0x30`, prescalers /1–/64, sharing TIFR/TIMSK with Timer0 via hook chaining.

#### MmioController: respects write bit-mask
`MmioController.Write()` now honours the register's bit mask before committing the value, preventing reserved bits from being set by peripheral write hooks.

#### Hex loader: variable flash sizes and bounds checks
`AvrRunner.LoadHex()` now correctly handles programs targeting devices with flash sizes other than the default, and skips record data that falls outside the allocated flash buffer.

#### Watchdog: no duplicate timeout events
Reloading the watchdog timer while a timeout event was already queued could schedule a second event. The redundant event is now cancelled before rescheduling.

#### SPI: ignore incoming bytes when disabled or in master mode
`AvrSpi` no longer processes `SimulateIncomingMasterByte` calls when the peripheral is disabled or configured as a master, matching hardware behaviour.

#### TWI: interrupt gating for slave mode simulation
`SimulateIncomingAddress` and `SimulateIncomingData` no longer raise TWINT when the TWI interrupt enable bit (TWIE) is clear, preventing unexpected handler invocations in polling-mode firmware.

#### TWI: NACK when TWEA is cleared in slave-receive mode
Clearing TWEA mid-transfer now correctly causes the slave to NACK the next incoming byte (TWSR → `0x88`), consistent with the datasheet slave-receive state machine.

---

### Performance

- **`MmioController`** replaces dictionary-based hook lookups with a flat array indexed by address, eliminating per-access allocation and hash overhead.
- **Direct register access** — all peripherals now read/write `Cpu.Mmio.Data[]` directly instead of routing through the hook dispatch path for non-hooked registers.
- **`[AggressiveInlining]`** applied to `Cpu.Tick()`, hot-path peripheral methods, and `NativeLutDecoder`.
- **Clock event queue** — `AvrClockEventEntry` linked list replaced with a compact array-based queue.
- **`NativeLutDecoder`** uses unsafe function pointers (`delegate*<ref Cpu, ref ushort, void>`) for maximum dispatch throughput.
- **ADC peripheral** caches reference voltage and sample cycles to avoid recomputing them on every conversion.

---

### Tests

- Added unit tests for `MmioController` write masking and hook-chaining behaviour.
- Added TWI slave NACK test (TWEA cleared during receive).
- Added TWI slave interrupt-gating tests for `SimulateIncomingAddress`/`Data`.
- Restructured test base classes to reduce per-fixture boilerplate.

---

**Full diff:** [`v1.0.2...v1.1.0-beta1`](https://github.com/begeistert/avr8sharp/compare/v1.0.2...v1.1.0-beta1)
