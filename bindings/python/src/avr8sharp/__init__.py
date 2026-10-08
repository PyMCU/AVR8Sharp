"""avr8sharp — fast AVR-8 emulator with a pytest-friendly test harness.

Python bindings over the C# Avr8Sharp emulator, compiled to a self-contained Native AOT
shared library and called via ctypes. The per-instruction loop stays native, so the emulator
keeps its full speed; Python only drives coarse-grained ``run_*`` calls.
"""

from __future__ import annotations

from .errors import Avr8SharpError
from .responder import (
    HCSR04_US_PER_CM,
    hc_sr04_echo,
    respond_to_rise,
    wait_for_fall,
    wait_for_rise,
)
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
    "HCSR04_US_PER_CM",
    "PinState",
    "Port",
    "Serial",
    "Simulation",
    "Spi",
    "Twi",
    "board",
    "board_for_target",
    "hc_sr04_echo",
    "respond_to_rise",
    "selftest",
    "wait_for_fall",
    "wait_for_rise",
]

__version__ = "1.1.0"
