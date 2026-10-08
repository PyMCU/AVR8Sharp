# Changelog

All notable changes to this project will be documented in this file.

---

## [Unreleased]

Fidelity fixes from an audit of the CPU, the peripherals and the iCircuit integration.
Several change cycle counts or register values that a test may have pinned; each entry says so.

### Bug Fixes

- **Intel HEX above 64 KB.** `AvrRunner.LoadHex` ignored the extended segment (02) and
  extended linear (04) records, so a Mega sketch larger than 64 KB wrote its upper half over
  the start of flash, vectors included. A shared `IntelHex` parser now handles 00/01/02/04,
  skips 03/05 and verifies checksums. `LoadHex` stays lenient; the new
  `TryLoadHex(hex, out HexInfo)` is strict and leaves flash untouched when the image has
  errors or does not fit.
- **`Cpu.Reset()` is a real reset.** It only reset SP, SREG and PC, so I/O registers and
  peripheral state survived: after a firmware reload `Serial.write` could hang (TXEN was still
  set, so UDRE never came back) and pins stayed outputs. The I/O space from 0x20 to the new
  `Cpu.RamStart` now returns to its reset values (UCSRnA 0x20, UCSRnC 0x06, TWSR 0xF8, the rest
  0), every peripheral resets its internal state, and port listeners see the pins go back to
  input. SRAM and r0-r31 are kept, as on the chip. `RamStart` defaults to 0x200 on 22-bit-PC
  parts and 0x100 otherwise; `AvrBuilder.WithRamStart` and the TestKit boards set it.
- **Interrupt response time.** Entry cost 3 cycles; the datasheet gives 4, and 5 on parts with
  a 22-bit PC. Every ISR timing moves by one cycle (two on the Mega).
- **One instruction after SEI and RETI.** A pending interrupt was served right after `sei`,
  `reti` or a write that set I, so `sei; sleep` raced and an always-pending interrupt starved
  the main loop. The next instruction now always runs first, in every decoder.
- **USART loses a received byte while transmitting.** Any UCSRnB write with RXEN set cleared
  RXC; Arduino's HardwareSerial toggles UDRIE on every byte it sends. RXC and the receive
  buffer are now flushed only when RXEN goes from 1 to 0. Inherited from avr8js.
- **Write-one-to-clear flags.** TIFRn stored the written value, so clearing one flag wiped
  the others; ADIF was stored instead of cleared by a written one; SPIF and WCOL were
  writable, and SPIF did not clear on SPSR-then-SPDR. TWINT, WDIF and EEPE were lost on
  writes that left them at 0. All now follow the datasheet.
- **ATmega2560 configs.** Timer0/1/2 drove the 328P compare-output pins, so `analogWrite` on
  Mega pins 4, 9, 10, 11, 12 and 13 hit the wrong pin or none. Timer1 had no channel C, and
  Timer3/4 had no input capture. INT0-7 did not exist and the pin-change interrupts went to
  328P vectors, so `attachInterrupt` and SoftwareSerial RX were dead on the Mega.
- **Channel C in fast PWM** never set its pin at BOTTOM, so OCnC PWM did not toggle.
- **ATtiny85 EEPROM.** EEARH was mapped to address 0x00 (r0), so `eeprom_write_byte` with
  interrupts enabled threw. It is now 0x3F, and the memory backend wraps addresses beyond the
  EEPROM size, as the chip does.
- **ATtiny85 Timer1** used the generic ATmega timer: TOV1 on the wrong bit, TCCR1 read r0 as
  TCCRA (PB1 froze high), no CTC1, PWM1A/B, OCR1C or prescalers above /64.
  `ATtiny85Simulation.Timer1` is now an `AvrAttinyTimer1`, ported from avr8js `timer-attiny.ts`.
- **EEPROM write time** was fixed in 16 MHz cycles; it now scales with the CPU frequency
  (3.4 ms erase and write, 1.8 ms either alone).

### New

- `Mega2560<Peripheral>Config` in the core: `AvrTimer.Mega2560Timer0Config` to `Timer5Config`,
  `AvrUsart.Mega2560Usart0Config` to `Usart3Config`, `AvrSpi`, `AvrTwi`, `AvrEeprom`,
  `AvrWatchdog` and `AvrAdc.Mega2560*Config`, and `AvrIoPort.Mega2560PortAConfig` to
  `PortLConfig` with INT0-7 and PCINT0-2. `ArduinoMegaSimulation` uses them.
- Timers sample their ICPn pin when the config names one (Mega Timer1/3/4/5), on the edge
  selected by ICESn.
- `AvrTimerConfig.CreateNew` takes nullable arguments, so an explicit 0 can be set.
- `AvrAdc.Avcc` and `Aref` are settable (5 V by default).
- `Cpu.OnInterruptDispatch`, `OnBreakpoint` and `OnSleep` are per-CPU events; the static
  `AvrInterrupt` hooks still work but are obsolete, because they fire for every CPU in the
  process.
- `AvrRunner.Run(budget)`, `RunUntil(cycle)` and `RunCycles(budget)` carry the target across
  calls, so stepping in small slices no longer drifts. `Execute()` uses the same target.

### New: the SiliconTwin.Abstractions contract

The core implements [`SiliconTwin.Abstractions`](https://github.com/silicon-twin/abstractions),
the shared embedder contract of the Silicon Twin emulators (MIT, no dependencies), so a host
drives an AVR the same way it drives an RP2040.

- `AvrBuilder.BuildMcu(name)` returns an `AvrMcu` (`IMcu`, `IGpio`, `IPwmSource`, `IAdcInput`,
  `IPeripheralMap`) over everything the builder mounted. `runner.AsMcu(...)` covers machines
  built by hand.
- `IMcu`: `Run(budget)`/`RunUntil` without drift, `Reset(Soft | Power)`, `Load` of Intel HEX
  (strict) or a raw binary.
- One `IPinBank` per port: `SetInputs(mask, levels)`, `ReleaseInputs(mask)`,
  `OutputEnableMask`, `OutputLevels`, `PullUpMask`, and `PinChanged` with the cycle of the
  write and a `WatchMask`. It costs one null check when nobody subscribes.
- `IPwmSource.TryGetPwm`: frequency and duty from the timer's clock select, waveform mode, TOP,
  OCR and COM bits (fast, phase-correct and phase-and-frequency-correct, channels A to C).
- `IAdcInput`: `ReadChannelVolts`, a callback the ADC calls only when a conversion samples its
  input, instead of pushing `ChannelValues` every step.
- UART (`TxByte`, `TryInjectRx`), SPI (`Transfer`, idle bus reads 0xFF) and I2C targets attached
  by address.

Underneath, also usable directly:

- `AvrIoPort.SetInputs`, `ReleaseInputs`, `ApplyInputs` (full state in one PIN recompute),
  `OutputEnableMask`, `OutputLevels`, `PullUpMask`, `PinChanged` and `WatchMask`.
  `SetPinValue` and `ReleasePin` no longer recompute PIN when nothing changes.
- `AvrAdc.ReadChannelVolts`, `AvrTimer.TryGetPwm`, `AvrClock.Changed`, `AvrBuilder.AddClock`.
- C ABI `a8s_gpio_set_inputs`, `a8s_gpio_release_inputs`, `a8s_gpio_apply_inputs`; Python
  `Port.set_inputs`, `release_inputs`, `apply_inputs`.
- The core declares `IsTrimmable` and `IsAotCompatible`, and CI publishes and runs a Native AOT
  smoke test.

---

## [v1.1.0] - 2026-10-07

### Bug Fixes

Two of these change what an existing test reads (the pull-up and the TWI timing), so a suite
that pinned the old values will need updating.

- **Backward RCALL on 22-bit-PC parts** (#21). `RCALL` added its signed offset as an unsigned
  16-bit value, so on an ATmega2560 every backward `rcall` landed 0x10000 words too high and
  the firmware never came back. The ATmega328P hid it because its flash length divides
  0x10000. `RET`/`RETI` had a second bug on the same parts: a return address whose low 16
  bits are zero (a call at word 0xFFFF) lost its high byte. Both now do the arithmetic on the
  full address.
- **PINx reads the internal pull-up** (#22). An input pin whose PORT bit is set reads 1 while
  nothing drives it, as the datasheet says, and enabling the pull-up on a floating pin raises
  the same pin-change edge as on the chip. A pin is driven from the first `SetPinValue` until
  the new `AvrIoPort.ReleasePin` lets it float again, so an embedder that injects every input
  on every step sees no change. Tests that read a floating pull-up as 0 now read 1.
- **TWI transfers take their wire time** (#24). START and STOP take one SCL period and an
  address or data byte nine (8 bits plus ACK), at `16 + 2 * TWBR * prescaler` CPU cycles per
  period, before TWINT is raised. The time counts from the moment the event reaches the
  `ITwiEventHandler`, so a handler that completes later than that adds no extra delay.
  `AvrTwi.EmulateBusTiming = false` brings back the old next-cycle completion.
- **ADC reference selection on the ATmega328P.** REFS1:0 mapped to AVCC, AREF, 1.1 V, 2.56 V;
  the datasheet order is AREF, AVCC, reserved, 1.1 V, so `analogReference(INTERNAL)`
  converted against 2.56 V instead of 1.1 V.
- **`RunUntilSerial` no longer slows down as output accumulates** (#25). The predicate ran on
  every instruction against a string rebuilt from all the bytes received so far, so long runs
  that print were quadratic. The text is now cached and the predicate runs only when a byte
  arrives. The core was checked across the 2^31 and 2^32 cycle boundaries and was not the
  cause; tests now cover both.
- **MCUSR reports the reset cause** (#27). `AvrWatchdog` owns MCUSR: it starts with PORF set,
  a CPU reset other than the watchdog's sets EXTRF, a watchdog reset sets WDRF, and firmware
  clears a flag by writing a zero to it (writing a one never sets it).

### New: complete Uno and Mega presets (#27)

- `ArduinoUnoSimulation` and `ArduinoMegaSimulation` mount SPI, TWI, ADC and the watchdog,
  exposed as `Spi`/`SpiBus`, `Twi`/`TwiBus`, `Adc` and `Watchdog`. With no device configured a
  SPI transfer reads 0xFF, an I2C address is NACKed, and every ADC input reads 0 V. The
  `SpiDeviceStub` and `TwiDeviceStub` behind them are now public, in `Avr8Sharp.TestKit.Probes`.
- `AvrAdc.Atmega2560AdcConfig` covers the 16 channels of the ATmega2560 (MUX5 selects ADC8 to
  ADC15) with its reference order. MUX5 is ignored on 8-channel configs, as on the 328P.

### New: strict mode for unmounted ports (#26)

- **TestKit**: `UnmountedAccess` (`Ignore` by default, `Warn`, `Throw`) and the `WithStrict()`
  shortcut. A read or write of PINx, DDRx or PORTx of a port that was never added throws
  `UnmountedIoAccessException` ("PIND (0x29) read but port D is not mounted") or adds one
  entry per register to `Warnings`. The hooks are installed only when the mode leaves
  `Ignore`, so the default run costs nothing.
- **C ABI**: `a8s_set_strict`, `a8s_read_warnings` and `a8s_gpio_release_pin`.
- **Python**: `Simulation.with_strict(True | "warn" | False)`, `Simulation.warnings`,
  `Port.release(pin)`, and `.adc`/`.spi`/`.twi` on `ArduinoMega`.

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
