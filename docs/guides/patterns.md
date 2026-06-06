# Patterns and best practices

These patterns come directly from PyMCU's integration test suite, which validates
compiled AVR firmware against hundreds of test cases in CI. Apply them to keep your
tests fast, isolated, and maintainable.

---

## Compile once, simulate many times

Compiling firmware (via `arduino-cli` or any other toolchain) is the slowest part of a
test run. Build the binary once per test fixture using `[OneTimeSetUp]`, then create a
fresh simulation for each test from the cached bytes:

```csharp
[TestFixture]
public class BlinkTests
{
    // Compiled once for the whole fixture class
    private static string _hex = null!;

    [OneTimeSetUp]
    public void BuildFirmware()
    {
        _hex = File.ReadAllText(
            Path.Combine(TestContext.CurrentContext.TestDirectory,
                         "..", "..", "..", "..", "build", "sketch.ino.hex"));
    }

    // Fresh simulation per test — no state leaks between tests
    private ArduinoUnoSimulation Sim() =>
        new ArduinoUnoSimulation().WithHex(_hex);

    [Test]
    public void Led_IsHighAfterSetup()
    {
        var uno = Sim();
        uno.RunMilliseconds(5);
        uno.PortB.Should().HavePinHigh(5);
    }

    [Test]
    public void Serial_PrintsHello()
    {
        var uno = Sim();
        uno.RunUntilSerial(uno.Serial, "Hello", maxMs: 1000);
        uno.Serial.Should().ContainLine("Hello");
    }
}
```

The `Sim()` factory is the key: each test starts from the same clean state. Sharing a
simulation between tests creates hidden ordering dependencies that break in parallel runs.

---

## Cache compiled firmware across fixtures

When multiple test fixtures test the same firmware, an in-process cache ensures the
toolchain runs only once per test session even if fixtures run in parallel:

```csharp
/// <summary>
/// Compiles and caches firmware binaries so each sketch is compiled at most once
/// per test session, regardless of how many test fixtures reference it.
/// </summary>
public static class FirmwareCache
{
    // ConcurrentDictionary<string, Lazy<string>> gives exactly-once semantics:
    // the Lazy ensures only one thread runs the compilation even under parallel access.
    private static readonly ConcurrentDictionary<string, Lazy<string>> Cache = new();

    // Limits concurrent arduino-cli invocations to avoid thrashing disk I/O
    private static readonly SemaphoreSlim Gate =
        new(Math.Clamp(Environment.ProcessorCount, 2, 8));

    /// <summary>Compiles a sketch and returns the path to the produced .hex file.</summary>
    public static string GetHex(string sketchDir, string fqbn = "arduino:avr:uno")
    {
        var key = $"{fqbn}:{sketchDir}";
        return Cache.GetOrAdd(key, _ => new Lazy<string>(() => Compile(sketchDir, fqbn))).Value;
    }

    private static string Compile(string sketchDir, string fqbn)
    {
        Gate.Wait();
        try
        {
            var outDir = Path.Combine(sketchDir, "dist");
            Directory.CreateDirectory(outDir);

            var psi = new ProcessStartInfo("arduino-cli")
            {
                Arguments = $"compile --fqbn {fqbn} --output-dir {outDir} {sketchDir}",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
            };
            using var proc = Process.Start(psi)!;
            var stdout = Task.Run(() => proc.StandardOutput.ReadToEnd());
            var stderr = Task.Run(() => proc.StandardError.ReadToEnd());
            if (!proc.WaitForExit(120_000))
            {
                proc.Kill();
                throw new TimeoutException($"arduino-cli timed out for '{sketchDir}'.");
            }
            if (proc.ExitCode != 0)
                throw new InvalidOperationException(
                    $"arduino-cli failed (exit {proc.ExitCode}):\n{stderr.Result}");

            var hexFile = Directory.GetFiles(outDir, "*.hex").FirstOrDefault()
                ?? throw new FileNotFoundException($"No .hex found in {outDir}");
            return File.ReadAllText(hexFile);
        }
        finally { Gate.Release(); }
    }
}
```

Usage across fixtures:

```csharp
[OneTimeSetUp]
public void BuildFirmware() =>
    _hex = FirmwareCache.GetHex("sketches/blink");
```

---

## Run test fixtures in parallel

NUnit can run fixture classes concurrently. Each fixture gets its own thread, but tests
within a fixture are sequential. Add one file to your test project:

```csharp
// AssemblyParallelism.cs
using NUnit.Framework;

[assembly: Parallelizable(ParallelScope.Fixtures)]
```

This is safe as long as each test creates its own simulation (the `Sim()` factory pattern
above). The `FirmwareCache` is thread-safe by construction (`ConcurrentDictionary` +
`Lazy<T>`), so simultaneous `BuildFirmware()` calls across fixtures are safe.

---

## Sample periodic behavior instead of asserting at a fixed time

For firmware that blinks an LED or toggles a pin periodically, a fixed
`RunMilliseconds(500)` is fragile — it passes only if the timing is exact. Sample the
output repeatedly instead:

```csharp
[Test]
public void Led_TogglesOverTime()
{
    var uno = Sim();
    bool sawHigh = false, sawLow = false;

    // Sample across more than two full blink periods (2 × 500 ms = 1 s)
    for (int i = 0; i < 120 && !(sawHigh && sawLow); i++)
    {
        uno.RunMilliseconds(20);
        if (uno.PortB.GetPinState(5)) sawHigh = true;
        else sawLow = true;
    }

    sawHigh.Should().BeTrue("the LED should be driven high during a blink");
    sawLow.Should().BeTrue("the LED should be driven low during a blink");
}
```

The loop exits as soon as both states have been observed, so fast firmware finishes in
a few iterations; the outer bound prevents a hang.

---

## Test request/response protocols (UART inject → assert echo)

For firmware that reads from serial and responds, inject bytes after the boot banner
arrives and then wait for the response:

```csharp
[Test]
public void Echo_Returns_Same_Byte()
{
    var uno = Sim();

    // Wait for the firmware to print its ready banner
    uno.RunUntilSerial(uno.Serial, "READY", maxMs: 5000);
    var bytesBefore = uno.Serial.ByteCount;

    // Inject 'A' into the RX buffer
    uno.Serial.Usart.WriteByte(0x41);

    // Wait for the echo — firmware must transmit at least one more byte
    uno.RunUntilMs(_ => uno.Serial.ByteCount > bytesBefore, maxMs: 1000);

    // The last byte received should be the echo
    uno.Serial.Bytes[^1].Should().Be(0x41, "firmware should echo the received byte");
}
```

The `RunUntilMs` + `ByteCount` guard is more robust than `RunMilliseconds(100)` because
it waits exactly as long as needed and fails immediately on timeout.

---

## Write descriptive failure messages

When an assertion fails inside a large test suite it should be immediately clear *why*,
not just *what* was wrong. Pass a reason to every `.Should().BeXxx()`:

```csharp
// Bad — failure message just says "Expected true, but found false"
sawHigh.Should().BeTrue();

// Good — failure message includes the reason
sawHigh.Should().BeTrue("the LED should be driven high during the blink-on phase");
uno.Serial.Should().ContainLine("PASS", "firmware must print PASS when all checks succeed");
uno.PortB.Should().HavePinHigh(5, "LED_BUILTIN should turn on 5 ms after setup()");
```

The reason string is printed with the failure details by FluentAssertions and makes triage
faster without needing to read the test source.

---

## Isolate repo root discovery from test output path

`TestContext.CurrentContext.TestDirectory` points into `bin/Debug/net10.0/`, several
levels below the repo root. A reusable helper avoids the fragile `../../../../..` chains:

```csharp
public static class RepoPath
{
    private static readonly string Root = FindRoot();

    /// <summary>Returns an absolute path rooted at the repository root.</summary>
    public static string From(params string[] segments) =>
        Path.Combine([Root, .. segments]);

    private static string FindRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            // Use a reliable marker — the solution file, or a well-known directory
            if (File.Exists(Path.Combine(dir, "Avr8Sharp.sln")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new DirectoryNotFoundException(
            "Cannot locate repo root (no Avr8Sharp.sln found in ancestor directories).");
    }
}
```

Usage:

```csharp
_hex = File.ReadAllText(RepoPath.From("build", "sketch.ino.hex"));
```

This works regardless of whether you run tests from the IDE, `dotnet test`, or CI, and
survives renaming the test project or moving its output directory.
