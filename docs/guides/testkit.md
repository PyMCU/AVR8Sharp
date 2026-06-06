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

## Simulating external input

### GPIO (button, sensor output)

Drive a pin from outside the simulation to simulate a button press or an external signal:

```csharp
// Simulate pressing a button on PD2 (INT0, active-low)
uno.PortD.SetPin(2, false);   // pull low → button pressed
uno.RunMilliseconds(10);
uno.PortD.SetPin(2, true);    // release
uno.RunUntilSerial(uno.Serial, "clicked", maxMs: 500);
```

### UART RX (host → firmware)

Inject bytes into the firmware's receive buffer:

```csharp
// Send the string "hello\n" to the firmware over USART0
foreach (var b in System.Text.Encoding.ASCII.GetBytes("hello\n"))
    uno.Serial.Usart.WriteByte(b);

uno.RunUntilSerial(uno.Serial, "got: hello", maxMs: 1000);
```

### ADC (analog sensor)

Set the voltage on an ADC channel before or during the run. The firmware reads it
on the next ADC conversion:

```csharp
var uno = new ArduinoUnoSimulation()
    .WithHex(hex)
    .AddAdc(AvrAdc.AdcConfig, out var adc);

// 2500 mV on A0 → mid-scale (512 counts at 10-bit / 5 V reference)
adc.ChannelValues[0] = 2500;

uno.RunUntilSerial(uno.Serial, "adc=512", maxMs: 1000);
```

You can change the value mid-run to simulate a changing sensor:

```csharp
uno.RunMilliseconds(100);
adc.ChannelValues[0] = 4000;   // voltage rises
uno.RunUntilSerial(uno.Serial, "adc=819", maxMs: 1000);
```

### TWI / I²C (slave device simulation)

The built-in TWI peripheral emulates master mode. To simulate a slave device responding
to the firmware, hook the TWI events:

```csharp
var uno = new ArduinoUnoSimulation()
    .WithHex(hex)
    .AddTwi(AvrTwi.TwiConfig, out var twi);

// Respond to address 0x48 (a temperature sensor) with two bytes
twi.EventHandler = (eventType, data) =>
{
    if (eventType == TwiEventType.AddressMatch)
        return [0x01, 0x90];   // 25.0 °C in 12-bit format
    return null;
};
```

---

## Debugging a failing test

### Reading the TimeoutException message

When a bounded run times out, the exception message includes the CPU state at the
moment the limit was hit:

```
TimeoutException: RunUntilSerial: "PASS" not found within 2000 ms of simulated time
  Cycles=32000000, PC=0x01A4 (byte 0x0348), SP=0x08F8, SREG=0x02
```

- **PC** (word address): convert to byte address with `× 2` and cross-reference with
  `avr-objdump -d firmware.elf` to find which function the firmware was stuck in.
- **SREG=0x02**: only the Zero flag is set — the firmware may be in a tight busy-wait loop.
- **SP**: compare with RAMEND (`0x08FF` on Uno) — if SP is near RAMEND the stack has not
  grown, suggesting the firmware never reached `main()` or its `setup()`.

### Step-by-step bisection

Run a small number of instructions at a time and print state at each checkpoint:

```csharp
var uno = new ArduinoUnoSimulation().WithHex(hex);

for (int i = 0; i < 100; i++)
{
    uno.RunInstructions(1000);
    Console.WriteLine(
        $"PC=0x{uno.Cpu.Pc:X4}  SP=0x{uno.Cpu.Sp:X4}  " +
        $"SREG=0x{uno.Cpu.Sreg:X2}  Serial=\"{uno.Serial.Text}\"");
}
```

### Checking CPU health mid-run

Assert after each segment to pinpoint when the firmware goes wrong:

```csharp
uno.RunMilliseconds(10);
uno.Cpu.Should().HaveInterruptsEnabled("setup() should have run SEI");
uno.Cpu.Should().HaveSP(0x08F8, "stack should be shallow after setup");

uno.RunUntilSerial(uno.Serial, "ready", maxMs: 500);
uno.PortB.Should().HavePinHigh(5, "LED should be on after init");
```

### Verify the firmware actually loaded

If the PC never moves from 0, the HEX file may not have loaded correctly:

```csharp
uno.RunInstructions(10);
Assert.That(uno.Cpu.Pc, Is.GreaterThan(0u), "PC did not advance — HEX load failed?");
```

---

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
