# Reference

## Key public types

| Type | Namespace | Role |
|---|---|---|
| `AvrRunner` | `AVR8Sharp.Core` | Low-level runner: CPU, clock, `Execute()`, `LoadHex`. |
| `AvrTestSimulation` | `Avr8Sharp.TestKit` | Fluent test harness: `RunUntilSerial`, `RunToBreak`, probes. |
| `ArduinoUnoSimulation` | `Avr8Sharp.TestKit.Boards` | Arduino Uno (ATmega328P) board preset. |
| `ArduinoMegaSimulation` | `Avr8Sharp.TestKit.Boards` | Arduino Mega 2560 (ATmega2560) board preset. |
| `ATtiny85Simulation` | `Avr8Sharp.TestKit.Boards` | ATtiny85 board preset. |
| `SerialProbe` | `Avr8Sharp.TestKit.Probes` | Captures all bytes transmitted by a USART. |
| `AvrIoPort` | `AVR8Sharp.Core.Peripherals` | GPIO port — `SetPin`, `GetPinState`, listeners. |
| `AvrUsart` | `AVR8Sharp.Core.Peripherals` | USART peripheral — `OnByteTransmit`, `WriteByte`. |
| `AvrTimer` | `AVR8Sharp.Core.Peripherals` | Timer peripheral — `OnOutputCompareMatch`. |
| `AvrEeprom` | `AVR8Sharp.Core.Peripherals` | EEPROM peripheral — `Backend.Data[]`. |
| `AvrAdc` | `AVR8Sharp.Core.Peripherals` | ADC peripheral — `ChannelValues[]`. |
| `AvrSpi` | `AVR8Sharp.Core.Peripherals` | SPI peripheral — `OnTransfer`. |
| `AvrTwi` | `AVR8Sharp.Core.Peripherals` | TWI (I²C) peripheral — master mode. |
| `LutDecoder` / `NativeLutDecoder` | `AVR8Sharp.Core.Decoders` | Instruction decoders (delegate vs. `delegate*`). |

## Instruction decoders

Three decoders are available; the default is `NativeLutDecoder`:

| Decoder | Speed | Notes |
|---|---|---|
| `NativeLutDecoder` | Fastest | 65 536-entry `delegate*` LUT — requires unsafe code |
| `LutDecoder` | Fast | 65 536-entry `Func<>` delegate LUT — portable |
| `SwitchDecoder` | Baseline | `switch`/`case` — simplest, easiest to read |

The decoder is selected via `AvrRunner.SetDecoder(DecoderType)` or the
`UseNativeLutDecoder` / `UseLutDecoder` / `UseSwitchDecoder` builder methods.

## Assertions

See the [Assertions reference](assertions.md) for the full list of `.Should()` methods
available on `Cpu`, GPIO ports, `Memory`, and `SerialProbe`.

## API documentation

The full public API is documented with XML-doc comments in the source. A generated API
reference is planned; until then, browse the
[source on GitHub](https://github.com/PyMCU/avr8sharp/tree/main/src) or rely on
IntelliSense from the NuGet packages.

```{toctree}
:hidden:

assertions
```
