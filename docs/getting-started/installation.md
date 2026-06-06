# Installation

Avr8Sharp targets **.NET 10**. You need the .NET 10 SDK installed.

## From NuGet

```bash
dotnet add package Avr8Sharp              # the emulator core
dotnet add package Avr8Sharp.TestKit      # fluent harness for firmware tests (optional)
```

| Package | Purpose |
|---|---|
| [`Avr8Sharp`](https://www.nuget.org/packages/Avr8Sharp) | CPU, peripherals, instruction decoders. AOT-compatible, trimmable. |
| [`Avr8Sharp.TestKit`](https://www.nuget.org/packages/Avr8Sharp.TestKit) | `AvrTestSimulation`, board presets, probes, and FluentAssertions-based health checks. |

## From source

```bash
git clone https://github.com/PyMCU/avr8sharp.git
cd avr8sharp
dotnet build
dotnet test
```
