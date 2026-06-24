# AVR8Sharp Project Memory

## Project Structure
- Solution: `AVR8Sharp.sln` at repo root
- Core lib: `src/AVR8Sharp.Core/` (multi-target: net6, net8, netstandard1.2, netstandard2.0, net10)
- TestKit: `src/Avr8Sharp.TestKit/` (net10, FluentAssertions 6.12.0)
- Tests: `tests/Avr8Sharp.Tests/` (NUnit, net10)
- Demo: `src/Avr8Sharp.Demo/` (net10)

## TestKit Architecture (created 2026-02-28)
See `memory/testkit.md` for details.

Key entry point:
```csharp
var sim = AvrTestSimulation.Create()
    .WithFrequency(16_000_000)
    .WithHex(hexString)         // or .WithAsm() / .WithProgram()
    .AddGpio(AvrIoPort.PortBConfig, out var portB)
    .AddUsart(AvrUsart.Usart0Config, out var serial)
    .AddTimer(AvrTimer.Timer0Config);

sim.RunMilliseconds(500);

portB.Should().HavePinHigh(5);
serial.Should().Contain("Hello");
sim.Cpu.Should().HaveRegister(24, 42);
sim.Memory.Should().HaveByteAt(0x200, 0xFF);
```

Pre-configured boards (src/Avr8Sharp.TestKit/Boards/):
- ArduinoUnoSimulation  — ATmega328P, 16 MHz, PortB/C/D, Timer0/1/2, Serial
- ArduinoMegaSimulation — ATmega2560, 16 MHz, PortA-L, Timer0-4, Serial0/1/2
- ATtiny85Simulation    — ATtiny85, 8 MHz, PortB, Timer0

CI/CD workflows (.github/workflows/):
- ci.yml           — build + test on every push/PR (GitHub + Gitea compatible)
- publish.yml      — pack + push NuGet on tag/main/dispatch; uses secrets:
                     NUGET_PRIVATE_SOURCE_URL, NUGET_PRIVATE_API_KEY, NUGET_PUBLIC_API_KEY
- build-nugets.yml — deprecated stub (replaced by publish.yml)

NuGet.Config: XML comments must NOT contain '--' (invalid XML).

## Key Namespaces
- `Avr8Sharp.TestKit` — AvrTestSimulation, AvrMemoryView, AssertionExtensions
- `Avr8Sharp.TestKit.Probes` — SerialProbe
- `Avr8Sharp.TestKit.Assertions` — AvrCpuAssertions, AvrGpioAssertions, SerialProbeAssertions, AvrMemoryAssertions

## Core CPU Facts
- `Cpu.Data[0..31]` = general registers R0–R31
- `Cpu.Data[93..94]` = SP (little-endian via DataView)
- `Cpu.Data[95]` = SREG
- `Cpu.PC` = word address (byte address = PC × 2)
- `Cpu.Cycles` = int (wraps ~134 s at 16 MHz)
- BREAK opcode = 0x9598

## Existing Test Utilities
`tests/Avr8Sharp.Tests/Utils.cs` has `TestProgramRunner` and `AsmProgram()` helpers
(these are superseded by TestKit for new tests).
