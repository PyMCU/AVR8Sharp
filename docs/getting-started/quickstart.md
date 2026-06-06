# Quickstart

## Run a firmware image and capture its serial output

The lowest-level entry point is `AvrRunner`. Load an Intel HEX file and run cycles directly:

```csharp
using AVR8Sharp.Core;
using AVR8Sharp.Core.Peripherals;

var runner = new AvrRunner(flashSize: 0x8000, sramBytes: 2048);
runner.SetSpeed(16_000_000);
runner.LoadHex(File.ReadAllText("firmware.hex"));

// Add a USART and capture its output
var usart = new AvrUsart(runner.Cpu, AvrUsart.Usart0Config, runner.Speed);
usart.OnByteTransmit = b => Console.Write((char)b);

// Run 16 000 cycles (1 ms at 16 MHz)
runner.Execute();
```

## Use a board preset (recommended)

Board presets wire up all peripherals automatically:

```csharp
using Avr8Sharp.TestKit.Boards;

var uno = new ArduinoUnoSimulation()
    .WithHex(File.ReadAllText("sketch.hex"));

uno.RunMilliseconds(500);

Console.WriteLine(uno.Serial.Text);     // all bytes sent via USART0
```

Available presets:

| Class | Chip | Flash | SRAM | Ports | Timers | Serial |
|---|---|---|---|---|---|---|
| `ArduinoUnoSimulation` | ATmega328P | 32 KB | 2 KB | B, C, D | 0–2 | 1 (USART0) |
| `ArduinoMegaSimulation` | ATmega2560 | 256 KB | 8 KB | A–L | 0–5 | 4 (USART0–3) |
| `ATtiny85Simulation` | ATtiny85 | 8 KB | 512 B | B | 0, 1 | USI |

## Drive a test with the TestKit

The TestKit wraps the simulation in a fluent harness with probes and assertions:

```csharp
using Avr8Sharp.TestKit.Boards;
using FluentAssertions;

var uno = new ArduinoUnoSimulation()
    .WithHex(File.ReadAllText("blink.hex"));

// Wait up to 2 s of simulated time for "Hello" to appear on Serial
uno.RunUntilSerial(uno.Serial, "Hello");

uno.Serial.Should().Contain("Hello");
uno.PortB.Should().HavePinHigh(5);     // LED_BUILTIN on Arduino Uno
```

For bounded, never-hanging runs and CPU-health checks, see
[Firmware testing with the TestKit](../guides/testkit.md).

## Load raw assembly inline

The TestKit can assemble and load AVR assembly directly in tests:

```csharp
var sim = AvrTestSimulation.Create()
    .WithAsm(@"
        ldi r16, 42
        out 0x05, r16    ; write 42 to PORTB
        break
    ");

sim.RunToBreak();
Assert.That(sim.Data[0x25], Is.EqualTo(42));
```
