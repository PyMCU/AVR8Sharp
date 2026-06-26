"""avr8sharp — fast AVR-8 emulator with a pytest-friendly test harness.

Python bindings over the C# Avr8Sharp emulator, compiled to a self-contained Native AOT
shared library and called via ctypes. The per-instruction loop stays native, so the emulator
keeps its full speed; Python only drives coarse-grained ``run_*`` calls.
"""

from __future__ import annotations

from .errors import Avr8SharpError
from .simulation import (
    Adc,
    ArduinoMega,
    ArduinoUno,
    ATtiny13,
    ATtiny85,
    ATtinyX4,
    Cpu,
    PinState,
    Port,
    Serial,
    Simulation,
    Spi,
    Twi,
    board,
    board_for_target,
    selftest,
)

__all__ = [
    "Adc",
    "ArduinoMega",
    "ArduinoUno",
    "ATtiny13",
    "ATtiny85",
    "ATtinyX4",
    "Avr8SharpError",
    "Cpu",
    "PinState",
    "Port",
    "Serial",
    "Simulation",
    "Spi",
    "Twi",
    "board",
    "board_for_target",
    "selftest",
]

__version__ = "1.1.0b1"
