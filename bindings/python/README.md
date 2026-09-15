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

## Driving external stimuli

A pin the firmware has configured as an input can be driven from the outside world, and the
simulation can be stepped in microseconds between writes -- the two building blocks behind
every silicon test that needs a pulse, a sensor reply, or a byte on UART RX:

```python
from avr8sharp import ArduinoUno

uno = ArduinoUno().with_hex(open("pulsein.hex").read())

uno.port_d.set_high(2)      # drive an input pin high (an interrupt line, a button, ...)
uno.run_us(500)              # step 500 simulated microseconds
uno.port_d.set_low(2)

uno.serial.inject_bytes(b"AT\r\n")   # bytes arriving on UART RX
```

`avr8sharp.hc_sr04_echo` models an HC-SR04 ultrasonic rangefinder on top of `run_us` and
`Port.set`: it waits for the firmware's trigger pulse, then answers with an echo pulse timed to
a distance --

```python
from avr8sharp import ArduinoUno, hc_sr04_echo

uno = ArduinoUno().with_hex(open("hcsr04.hex").read())
uno.run_to_break()
pulse_us = hc_sr04_echo(
    uno, uno.port_d, 5,   # trigger pin: D5
    uno.port_d, 2,        # echo pin: D2
    distance_cm=9.7,
)
```

`avr8sharp.wait_for_rise` / `wait_for_fall` and `respond_to_rise` are the lower-level pieces
`hc_sr04_echo` is built from, for modelling other request/response sensors the same way.

## License

Business Source License 1.1 (see `LICENSE`). The bundled AVR emulation core (derived from
avr8js) and the .NET runtime remain under the MIT License (see `THIRD_PARTY_NOTICES`).
