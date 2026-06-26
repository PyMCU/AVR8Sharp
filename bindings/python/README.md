# avr8sharp (Python)

Fast AVR-8 emulator with a pytest-friendly test harness.

`avr8sharp` is a Python binding over the C# [Avr8Sharp](https://github.com/begeistert/avr8sharp)
emulator (itself a port of [avr8js](https://github.com/wokwi/avr8js)), compiled to a
**self-contained Native AOT shared library**. There is **no .NET runtime to install** — the
engine ships inside the wheel.

The per-instruction execution loop stays entirely on the native side. Python only drives
coarse-grained `run_*` calls, so the binding adds negligible overhead and the emulator keeps
its full speed.

## Install

```bash
pip install avr8sharp
```

## Usage

```python
from avr8sharp import ArduinoUno

uno = ArduinoUno().with_hex(open("blink.hex").read())
uno.run_ms(500)

assert uno.port_b.pin_high(5)          # digital pin 13
assert "Hello" in uno.serial.text
assert uno.cpu.cycles <= 8_000_000
```

Inline assembly, custom boards, and fast per-test reset are supported too:

```python
from avr8sharp import ArduinoUno

uno = ArduinoUno()
uno.snapshot()                          # capture power-on state once

uno.with_asm("ldi r16, 0x20\nout 0x04, r16\nout 0x05, r16\nbreak\n").run_to_break()
assert uno.port_b.pin_high(5)

uno.restore()                           # cheap reset to power-on state between tests
```

Boards: `ArduinoUno` (ATmega328P), `ArduinoMega` (ATmega2560), `ATtiny85`, plus a blank
`Simulation.create(flash, sram)` with manual `add_gpio` / `add_usart0` / `add_timer` wiring.

## License

Business Source License 1.1 (see `LICENSE`). The bundled AVR emulation core (derived from
avr8js) and the .NET runtime remain under the MIT License (see `THIRD_PARTY_NOTICES`).
