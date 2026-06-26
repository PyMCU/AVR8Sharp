"""End-to-end smoke tests for the avr8sharp bindings.

Run against a freshly published native library via::

    AVR8SHARP_LIBRARY=/path/to/Avr8Sharp.Native.dylib \
      PYTHONPATH=src python -m pytest tests/

or against an installed wheel (the library is bundled, no env var needed)::

    python -m pytest tests/
"""

from __future__ import annotations

from pathlib import Path

import pytest

import avr8sharp as a

FIXTURES = Path(__file__).parent / "fixtures"

# Sets PB5 (Arduino pin 13) as output and drives it high, then BREAK.
SET_PB5_HIGH = "ldi r16, 0x20\nout 0x04, r16\nout 0x05, r16\nbreak\n"


def test_selftest():
    assert a.selftest() == 42


def test_assemble_run_and_gpio():
    uno = a.ArduinoUno()
    uno.with_asm(SET_PB5_HIGH).run_to_break()
    assert uno.port_b.pin_high(5)
    assert not uno.port_b.pin_high(4)
    assert uno.cpu.reg(16) == 0x20
    assert uno.cpu.cycles == 3
    uno.close()


def test_snapshot_restore_round_trips_state():
    uno = a.ArduinoUno()
    uno.snapshot()
    uno.with_asm(SET_PB5_HIGH).run_to_break()
    assert uno.port_b.pin_high(5)
    assert uno.cpu.cycles > 0

    uno.restore()
    assert uno.cpu.cycles == 0
    assert not uno.port_b.pin_high(5)
    uno.close()


def test_invalid_assembly_raises():
    with pytest.raises(a.Avr8SharpError):
        a.ArduinoUno().with_asm("this is not a valid avr program\n")


def test_context_manager_closes():
    with a.ArduinoUno() as uno:
        uno.with_asm(SET_PB5_HIGH).run_to_break()
        assert uno.port_b.pin_high(5)


def test_serial_capture_from_hex_firmware():
    hex_content = (FIXTURES / "serial_hello.hex").read_text()
    uno = a.ArduinoUno().with_hex(hex_content)
    uno.run_ms(100)
    assert "Hello from Uno!" in uno.serial.text
    assert any("Hello from Uno!" in line for line in uno.serial.lines)
    uno.close()


def test_run_until_serial_stops_early():
    hex_content = (FIXTURES / "serial_hello.hex").read_text()
    uno = a.ArduinoUno().with_hex(hex_content)
    uno.run_until_serial(uno.serial, "Hello", max_ms=2000)
    assert "Hello" in uno.serial.text
    uno.close()


def test_custom_session_wiring():
    sim = a.Simulation.create(flash=0x8000, sram=2048)
    port_b = sim.add_gpio(0)
    sim.with_asm(SET_PB5_HIGH).run_to_break()
    assert port_b.pin_high(5)
    sim.close()
