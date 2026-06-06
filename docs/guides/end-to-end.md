# End-to-end tutorial

This guide walks through the full path: from zero to a passing integration test that
compiles a real Arduino sketch and validates its output in the simulator. No physical
hardware required.

---

## What you'll build

A .NET test project that:
1. Compiles an Arduino sketch to a `.hex` file using `arduino-cli`
2. Loads the firmware into `ArduinoUnoSimulation`
3. Asserts that the correct output appears on Serial within a bounded run

The sketch prints a counter every 500 ms. The test verifies the first three lines.

---

## Prerequisites

- **.NET 10 SDK** — `dotnet --version` should show `10.x`
- **arduino-cli** — install from [arduino.cc/en/software](https://arduino.cc/en/software) or
  `brew install arduino-cli` on macOS

### Install the AVR core

```bash
arduino-cli core install arduino:avr
```

---

## 1. Create the test project

```bash
mkdir firmware-tests && cd firmware-tests
dotnet new nunit
dotnet add package Avr8Sharp.TestKit --prerelease
```

Your project file should look like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="NUnit" Version="4.*" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.*" />
    <PackageReference Include="Avr8Sharp.TestKit" Version="*-*" />
  </ItemGroup>
</Project>
```

---

## 2. Write the Arduino sketch

Create `sketch/sketch.ino`:

```cpp
void setup() {
    Serial.begin(115200);
}

void loop() {
    static int count = 0;
    Serial.print("count=");
    Serial.println(count++);
    delay(500);
}
```

---

## 3. Compile with arduino-cli

```bash
arduino-cli compile \
    --fqbn arduino:avr:uno \
    --output-dir build/ \
    sketch/
```

This produces `build/sketch.ino.hex`.

---

## 4. Write the test

Replace `Tests.cs` with:

```csharp
using Avr8Sharp.TestKit.Boards;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class FirmwareTests
{
    private static readonly string HexPath =
        Path.Combine(TestContext.CurrentContext.TestDirectory,
                     "..", "..", "..", "..", "build", "sketch.ino.hex");

    [Test]
    public void Counter_increments_and_prints_to_serial()
    {
        var uno = new ArduinoUnoSimulation()
            .WithHex(File.ReadAllText(HexPath));

        // Wait up to 3 s of simulated time for three lines to appear
        uno.RunUntilSerial(uno.Serial, "count=2", maxMs: 3000);

        uno.Serial.Should().ContainLine("count=0");
        uno.Serial.Should().ContainLine("count=1");
        uno.Serial.Should().ContainLine("count=2");
    }

    [Test]
    public void Counter_stays_within_cycle_budget()
    {
        var uno = new ArduinoUnoSimulation()
            .WithHex(File.ReadAllText(HexPath));

        uno.RunUntilSerial(uno.Serial, "count=2", maxMs: 3000);

        // Deterministic across machines — use as a regression guard
        Assert.That(uno.Cpu.Cycles, Is.LessThan(50_000_000UL));
    }
}
```

---

## 5. Run the tests

```bash
dotnet test
```

Expected output:

```
Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2
```

---

## 6. What just happened

- `ArduinoUnoSimulation` wired up an ATmega328P with 16 MHz clock, PortB/C/D, three timers,
  USART0, and EEPROM — all in one line.
- `WithHex(...)` loaded the compiled firmware into flash.
- `RunUntilSerial(uno.Serial, "count=2", maxMs: 3000)` ran the simulation until `"count=2"`
  appeared in the captured serial output, or threw `TimeoutException` after 3 s of simulated
  time. Simulated time is driven by CPU cycles — the run always takes the same number of
  cycles regardless of host machine speed.
- `ContainLine` asserted that specific lines appeared in the output.

---

## 7. Testing GPIO

If the sketch also blinks an LED:

```cpp
void setup() {
    Serial.begin(115200);
    pinMode(LED_BUILTIN, OUTPUT);   // PB5 on Arduino Uno
}

void loop() {
    static int count = 0;
    Serial.println(count++);
    digitalWrite(LED_BUILTIN, HIGH);
    delay(250);
    digitalWrite(LED_BUILTIN, LOW);
    delay(250);
}
```

Add an assertion on pin state after an odd number of blink cycles:

```csharp
// After 3 toggles the LED should be HIGH
uno.RunUntilSerial(uno.Serial, "2", maxMs: 2000);
uno.PortB.Should().HavePinHigh(5);   // PB5 = Arduino digital pin 13 = LED_BUILTIN
```

See [Board pin maps](../peripherals/board-maps.md) to translate Arduino pin numbers
to the port/pin pairs used by the simulator.

---

## 8. Testing with sensors (ADC / UART RX / I²C)

If the firmware reads a sensor, inject the simulated value before running:

```csharp
var uno = new ArduinoUnoSimulation()
    .WithHex(hex)
    .AddAdc(AvrAdc.AdcConfig, out var adc);

// Simulate a temperature sensor reading ~25 °C (500 mV on a TMP36)
adc.ChannelValues[0] = 500;   // millivolts on A0

uno.RunUntilSerial(uno.Serial, "temp=25", maxMs: 2000);
uno.Serial.Should().ContainLine("temp=25");
```

See [Simulating external input](testkit.md#simulating-external-input) for UART RX,
GPIO, and I²C injection.
