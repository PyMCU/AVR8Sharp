# Validating firmware in CI

Avr8Sharp is designed to validate compiler/firmware output (for example, the
[PyMCU](https://docs.pymcu.org) compiler) in CI **without flaky or hanging builds**:

- Runs are **bounded** — a wedged program fails with a reason instead of stalling the job.
- The clock is driven by executed cycles, so results are **deterministic** and reproducible
  across machines.

## In a .NET test project (recommended)

Use the TestKit directly from NUnit or xUnit. This is what PyMCU's integration suite does:

```csharp
[Test]
public void Blink_firmware_reports_pass()
{
    var uno = new ArduinoUnoSimulation()
        .WithHex(File.ReadAllText("blink.hex"));

    // Bounded run — throws TimeoutException if "PASS" never arrives
    uno.RunUntilSerial(uno.Serial, "PASS", maxMs: 5000);

    uno.Serial.Should().Contain("PASS");
}
```

### Cycle-count regression guard

Because cycle counts are deterministic you can catch firmware size regressions in CI:

```csharp
[Test]
public void Sort_firmware_stays_within_cycle_budget()
{
    var sim = new ArduinoUnoSimulation()
        .WithHex(hex);

    sim.RunUntilSerial(sim.Serial, "done");

    Assert.That(sim.Cpu.Cycles, Is.LessThanOrEqualTo(500_000),
        "firmware executed more cycles than the approved budget");
}
```

### Multi-board test

The same firmware binary can be validated against multiple board configurations:

```csharp
[TestCase("uno")]
[TestCase("mega")]
public void Firmware_runs_on_all_targets(string target)
{
    AvrTestSimulation sim = target switch
    {
        "uno"  => new ArduinoUnoSimulation().WithHex(unoHex),
        "mega" => new ArduinoMegaSimulation().WithHex(megaHex),
        _      => throw new ArgumentException(target),
    };

    // Both boards expose a Serial / Serial0 SerialProbe
    var serial = target == "mega"
        ? ((ArduinoMegaSimulation)sim).Serial0
        : ((ArduinoUnoSimulation)sim).Serial;

    sim.RunUntilSerial(serial, "PASS", maxMs: 3000);
    serial.Should().Contain("PASS");
}
```

## Build firmware locally with arduino-cli

Compile a sketch for ATmega2560 and test it:

```bash
arduino-cli compile \
    --fqbn arduino:avr:mega \
    --output-dir /tmp/build \
    my_sketch/
```

```csharp
var mega = new ArduinoMegaSimulation()
    .WithHex(File.ReadAllText("/tmp/build/my_sketch.ino.hex"));

mega.RunUntilSerial(mega.Serial0, "OK", maxMs: 5000);
mega.Serial0.Should().Contain("OK");
```

## Full GitHub Actions pipeline

The following workflow compiles a sketch with `arduino-cli` and then runs the .NET tests
that validate its output. Copy it to `.github/workflows/firmware-test.yml`:

```yaml
name: Firmware integration tests

on:
  push:
    branches: ["**"]
  pull_request:

jobs:
  test:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Set up .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.x"

      - name: Install arduino-cli
        run: |
          curl -fsSL https://raw.githubusercontent.com/arduino/arduino-cli/master/install.sh \
            | sh -s -- --bindir /usr/local/bin
          arduino-cli core install arduino:avr

      - name: Compile sketch
        run: |
          arduino-cli compile \
            --fqbn arduino:avr:uno \
            --output-dir build/ \
            sketch/

      - name: Run firmware tests
        run: dotnet test tests/ --logger "console;verbosity=normal"
```

### Loading the `.hex` file in tests

Use a path relative to the test binary output directory so it works both locally and in
CI regardless of the working directory:

```csharp
private static string HexPath => Path.Combine(
    TestContext.CurrentContext.TestDirectory,   // NUnit
    "..", "..", "..", "..", "build", "sketch.ino.hex");
```

For xUnit, use `AppContext.BaseDirectory` in the same pattern.

### Multi-chip CI matrix

Test against several targets in one run:

```yaml
strategy:
  matrix:
    include:
      - fqbn: arduino:avr:uno
        hex_stem: sketch.ino
      - fqbn: arduino:avr:mega
        hex_stem: sketch.ino

steps:
  - name: Compile sketch (${{ matrix.fqbn }})
    run: |
      arduino-cli compile \
        --fqbn ${{ matrix.fqbn }} \
        --output-dir build/${{ matrix.fqbn }}/ \
        sketch/

  - name: Run tests
    run: dotnet test tests/ -e FQBN=${{ matrix.fqbn }}
    env:
      HEX_PATH: build/${{ matrix.fqbn }}/${{ matrix.hex_stem }}.hex
```

---

Supported FQBNs for each board preset:

| Preset | FQBN |
|---|---|
| `ArduinoUnoSimulation` | `arduino:avr:uno` |
| `ArduinoMegaSimulation` | `arduino:avr:mega` |
| `ATtiny85Simulation` | `attiny:avr:ATtinyX5:cpu=attiny85,clock=internal8` |
