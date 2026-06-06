# Assertions reference

All assertion classes live in the `Avr8Sharp.TestKit.Assertions` namespace and are
available via `.Should()` extension methods after `using FluentAssertions`.

---

## CPU assertions — `sim.Cpu.Should()`

Obtained from any `AvrTestSimulation` via `sim.Cpu.Should()`.

### Registers and state

| Assertion | What it checks |
|---|---|
| `HaveRegister(index, value)` | General-purpose register `Rindex == value` (0–31) |
| `HavePC(wordAddr)` | Program counter at `wordAddr` (word index, not byte address) |
| `HaveSP(value)` | Stack pointer equals `value` |
| `HaveCycles(n)` | Cycle counter exactly `n` |

### SREG raw value

| Assertion | What it checks |
|---|---|
| `HaveSreg(byte)` | SREG byte equals the given value exactly |

**SREG bit constants** (defined in test base class and usable as arguments):

| Constant | Bit | Flag |
|---|---|---|
| `SREG_C = 0x01` | 0 | Carry |
| `SREG_Z = 0x02` | 1 | Zero |
| `SREG_N = 0x04` | 2 | Negative |
| `SREG_V = 0x08` | 3 | Overflow |
| `SREG_S = 0x10` | 4 | Sign (N ⊕ V) |
| `SREG_H = 0x20` | 5 | Half-carry |
| `SREG_T = 0x40` | 6 | Bit-copy (T) |
| `SREG_I = 0x80` | 7 | Global interrupt enable |

### SREG individual flags

Each flag assertion accepts an optional `bool expected` argument (default `true`).
Pass `false` to assert the flag is clear.

| Assertion | Flag |
|---|---|
| `HaveCarryFlag()` / `HaveCarryFlag(false)` | C (bit 0) |
| `HaveZeroFlag()` / `HaveZeroFlag(false)` | Z (bit 1) |
| `HaveNegativeFlag()` / `HaveNegativeFlag(false)` | N (bit 2) |
| `HaveOverflowFlag()` / `HaveOverflowFlag(false)` | V (bit 3) |
| `HaveSignFlag()` / `HaveSignFlag(false)` | S (bit 4) |
| `HaveHalfCarryFlag()` / `HaveHalfCarryFlag(false)` | H (bit 5) |
| `HaveTFlag()` / `HaveTFlag(false)` | T (bit 6) |
| `HaveInterruptsEnabled()` / `HaveInterruptsEnabled(false)` | I (bit 7) |

**Examples:**

```csharp
sim.Cpu.Should().HaveRegister(16, 0xAB);
sim.Cpu.Should().HavePC(0x0040);
sim.Cpu.Should().HaveSreg(SREG_Z | SREG_C);

sim.Cpu.Should().HaveZeroFlag();
sim.Cpu.Should().HaveCarryFlag(false);
sim.Cpu.Should().HaveInterruptsEnabled();
```

---

## GPIO assertions — `port.Should()`

Obtained from any `AvrIoPort` via `port.Should()`.

| Assertion | What it checks |
|---|---|
| `HavePinHigh(index)` | Pin `index` is driven high (DDR=output, PORT=1) |
| `HavePinLow(index)` | Pin `index` is driven low (DDR=output, PORT=0) |
| `HavePinInput(index)` | Pin `index` is configured as floating input (DDR=0, PORT=0) |
| `HavePinInputPullup(index)` | Pin `index` is input with pull-up enabled (DDR=0, PORT=1) |
| `HavePinState(index, state)` | Pin `index` matches the given `PinState` enum value |
| `HavePinsHigh(params int[])` | All listed pins are high |
| `HavePinsLow(params int[])` | All listed pins are low |
| `HaveOutputValue(byte)` | The PORT register value equals `byte` (all 8 pins at once) |

**Pin states:**

```csharp
using AVR8Sharp.Core.Peripherals;

uno.PortB.Should().HavePinHigh(5);                    // LED_BUILTIN on
uno.PortD.Should().HavePinLow(2);                     // INT0 driven low
uno.PortC.Should().HavePinInputPullup(4);             // A4/SDA as pulled-up input
uno.PortB.Should().HavePinsHigh(1, 2, 3);             // multiple pins
uno.PortA.Should().HaveOutputValue(0b00001111);       // lower nibble high
```

---

## Memory assertions — `sim.Memory.Should()`

Obtained from `sim.Memory` (an `AvrMemoryView` over the full `_ram` array).

| Assertion | What it checks |
|---|---|
| `HaveByteAt(addr, byte)` | `mem[addr] == byte` |
| `HaveWordAt(addr, ushort)` | 16-bit little-endian word at `addr` equals `value` |
| `HaveWordBEAt(addr, ushort)` | 16-bit big-endian word at `addr` equals `value` |
| `HaveBytesAt(addr, byte[])` | Byte sequence at `addr` matches `expected` exactly |

```{note}
Addresses are in the AVR unified data space: registers at 0x0000–0x001F,
I/O space at 0x0020–0x005F (plus extended I/O on ATmega2560), SRAM at 0x0100+ (ATmega328P)
or 0x0200+ (ATmega2560). The `sim.Data[n]` shortcut gives the same array.
```

**Examples:**

```csharp
sim.Memory.Should().HaveByteAt(0x0100, 0x42);
sim.Memory.Should().HaveWordAt(0x0100, 0x1234);          // little-endian
sim.Memory.Should().HaveBytesAt(0x0200, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });

// Read directly without assertion
byte val = sim.Data[0x0100];
```

---

## Serial probe assertions — `serial.Should()`

Obtained from any `SerialProbe` via `serial.Should()`.

### Text content

| Assertion | What it checks |
|---|---|
| `Be(string)` | Captured text is exactly `string` |
| `Contain(string)` | Captured text contains `string` as a substring |
| `NotContain(string)` | Captured text does not contain `string` |
| `StartWith(string)` | Captured text starts with `string` |
| `EndWith(string)` | Captured text ends with `string` (trailing whitespace ignored) |
| `BeEmpty()` | Nothing transmitted yet |

### Line-level

| Assertion | What it checks |
|---|---|
| `HaveLineCount(n)` | Output split by `'\n'` has exactly `n` lines |
| `ContainLine(string)` | At least one line equals `string` exactly |

### Binary / byte-level

| Assertion | What it checks |
|---|---|
| `ContainByte(byte)` | Raw byte stream contains `value` at least once |
| `HaveBytes(IEnumerable<byte>)` | Raw byte stream is exactly `expected` |
| `HaveBytesAt(index, IEnumerable<byte>)` | Bytes starting at `index` match `expected` |

**Examples:**

```csharp
uno.Serial.Should().ContainLine("count=0");
uno.Serial.Should().HaveLineCount(3);
uno.Serial.Should().StartWith("boot");
uno.Serial.Should().NotContain("ERROR");

// Binary protocol
serial.Should().HaveBytes(new byte[] { 0x02, 0x41, 0x03 });
serial.Should().HaveBytesAt(1, new byte[] { 0x41 });   // 'A' at position 1
```

---

## Chaining assertions

All assertions return `AndConstraint<T>`, so you can chain:

```csharp
sim.Cpu.Should().HaveZeroFlag().And.HaveCarryFlag(false);
uno.Serial.Should().Contain("ready").And.NotContain("error");
```
