# Firmware testing with the TestKit

`Avr8Sharp.TestKit` turns the emulator into a fluent test harness: build a simulation,
attach probes, run it under a bound, and assert on the result.

## Building a simulation

Use a board preset for the fastest path:

```csharp
using Avr8Sharp.TestKit.Boards;

var uno = new ArduinoUnoSimulation()
    .WithHex(File.ReadAllText("sketch.hex"));
```

Or build a custom simulation for chips not covered by a preset:

```csharp
using Avr8Sharp.TestKit;

var sim = AvrTestSimulation.Create(flashSize: 0x8000, sramBytes: 2048)
    .WithFrequency(16_000_000)
    .WithHex(File.ReadAllText("firmware.hex"))
    .AddGpio(AvrIoPort.PortBConfig, out var portB)
    .AddUsart(AvrUsart.Usart0Config, out var serial)
    .AddTimer(AvrTimer.Timer0Config);
```

## Running the simulation

| Method | When to use |
|---|---|
| `RunMilliseconds(ms)` | Fixed simulated time — simple cases where firmware is healthy |
| `RunCycles(n)` | Exact cycle count |
| `RunInstructions(n)` | Exact instruction count |
| `RunUntil(predicate)` | Stop as soon as a condition is true |
| `RunUntilMs(predicate, maxMs)` | Stop on condition, timeout in simulated time |
| `RunUntilSerial(probe, text)` | Stop when serial output contains a string |
| `RunToBreak()` | Stop at the next `BREAK` (0x9598) instruction |
| `RunToAddress(byteAddr)` | Stop when PC reaches a byte address (from objdump) |

## Serial probes

Every `AddUsart` call returns a `SerialProbe` that captures all transmitted bytes:

```csharp
var mega = new ArduinoMegaSimulation()
    .WithHex(hex);

mega.RunUntilSerial(mega.Serial0, "Ready", maxMs: 2000);

mega.Serial0.Text.Should().Contain("Ready");
mega.Serial0.Lines.Should().HaveCount(3);
mega.Serial0.ByteCount.Should().BeGreaterThan(0);
```

## Assertions

### CPU health

```csharp
using Avr8Sharp.TestKit.Assertions;

sim.Cpu.Should().HaveSreg(SREG_Z);         // specific SREG flags set
sim.Cpu.Should().HaveRegister(16, 0xFF);   // r16 == 0xFF
sim.Cpu.Should().HavePc(0x0042);           // PC at word address
```

### GPIO ports

```csharp
uno.PortB.Should().HavePinHigh(5);         // PB5 driven high by firmware
uno.PortB.Should().HavePinLow(0);
```

### Memory

```csharp
sim.Memory.Should().HaveBytesAt(0x0200, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
```

### Serial

```csharp
uno.Serial.Should().Contain("Hello");
uno.Serial.Should().HaveLineCount(5);
```

## Inline assembly

Tests that target individual instructions can assemble code inline without an external
toolchain:

```csharp
var sim = AvrTestSimulation.Create()
    .WithAsm("ldi r16, 0x55\r\nneg r16\r\nbreak");

sim.RunToBreak();

sim.Cpu.Should().HaveRegister(16, 0xAB);
```

## Deterministic instruction count

Because the clock is driven by executed cycles (not wall-clock), the instruction count is
reproducible across machines. Use it as a compiler-size regression guard:

```csharp
var uno = new ArduinoUnoSimulation()
    .WithHex(hex);

uno.RunUntilSerial(uno.Serial, "PASS");

// Fail the test if the firmware grew by more than 10 %
Assert.That(uno.Cpu.Cycles, Is.LessThanOrEqualTo(expectedCycles * 1.10));
```
