"""ctypes loader and C-ABI signature declarations for the Avr8Sharp native library.

The native library is a Native AOT shared library built from ``src/Avr8Sharp.Native``. It is
self-contained (no .NET runtime needed at install time) and exposes a flat C ABI whose entry
points are all prefixed ``a8s_``. Every call is coarse-grained: the per-instruction loop stays
on the native side, so the emulator keeps its full speed.
"""

from __future__ import annotations

import ctypes
import sys
from ctypes import (
    POINTER,
    c_byte,
    c_char_p,
    c_double,
    c_int,
    c_long,
    c_longlong,
    c_uint,
    c_ulonglong,
    c_void_p,
)
from pathlib import Path

# Signature table: name -> (argtypes, restype). Applied to the loaded CDLL.
_SIGNATURES: dict[str, tuple[list, object]] = {
    # lifecycle
    "a8s_create": ([c_int, c_int], c_void_p),
    "a8s_create_uno": ([], c_void_p),
    "a8s_create_mega": ([], c_void_p),
    "a8s_create_attiny85": ([], c_void_p),
    "a8s_create_attinyx4": ([c_int, c_int], c_void_p),
    "a8s_create_attiny13": ([], c_void_p),
    "a8s_destroy": ([c_void_p], None),
    # program loading
    "a8s_with_frequency": ([c_void_p, c_uint], c_int),
    "a8s_with_hex": ([c_void_p, c_char_p, c_int], c_int),
    "a8s_with_asm": ([c_void_p, c_char_p, c_int], c_int),
    "a8s_with_program": ([c_void_p, c_char_p, c_int], c_int),
    # generic peripheral wiring
    "a8s_add_gpio": ([c_void_p, c_int], c_int),
    "a8s_add_usart0": ([c_void_p], c_int),
    "a8s_add_timer": ([c_void_p, c_int], c_int),
    # execution
    "a8s_run_cycles": ([c_void_p, c_long], c_int),
    "a8s_run_ms": ([c_void_p, c_double], c_int),
    "a8s_run_instructions": ([c_void_p, c_int], c_int),
    "a8s_run_to_break": ([c_void_p, c_int], c_int),
    "a8s_run_until_serial": ([c_void_p, c_int, c_char_p, c_int, c_double], c_int),
    "a8s_run_until_serial_bytes": ([c_void_p, c_int, c_int, c_double], c_int),
    # serial observation
    "a8s_serial_read": ([c_void_p, c_int, c_char_p, c_int], c_int),
    "a8s_serial_byte_count": ([c_void_p, c_int], c_int),
    "a8s_serial_clear": ([c_void_p, c_int], c_int),
    "a8s_serial_inject": ([c_void_p, c_int, c_byte], c_int),
    # gpio observation
    "a8s_gpio_pin": ([c_void_p, c_int, c_int], c_int),
    # analog / bus peripherals (ATmega328P-family sessions)
    "a8s_adc_set_channel": ([c_void_p, c_int, c_double], c_int),
    "a8s_spi_queue_response": ([c_void_p, c_byte], c_int),
    "a8s_spi_read_mosi": ([c_void_p, c_char_p, c_int], c_int),
    "a8s_twi_set_slave": ([c_void_p, c_int, c_int], c_int),
    "a8s_twi_queue_response": ([c_void_p, c_byte], c_int),
    "a8s_twi_read_writes": ([c_void_p, c_char_p, c_int], c_int),
    # cpu / memory
    "a8s_cpu_pc": ([c_void_p], c_uint),
    "a8s_cpu_sp": ([c_void_p], c_uint),
    "a8s_cpu_cycles": ([c_void_p], c_ulonglong),
    "a8s_cpu_sreg": ([c_void_p], c_int),
    "a8s_read_data": ([c_void_p, c_int], c_int),
    "a8s_write_data": ([c_void_p, c_int, c_byte], c_int),
    # reset / snapshot
    "a8s_reset": ([c_void_p], c_int),
    "a8s_snapshot": ([c_void_p], c_int),
    "a8s_restore": ([c_void_p], c_int),
    # errors / smoke
    "a8s_last_error": ([c_void_p, c_char_p, c_int], c_int),
    "a8s_selftest": ([], c_longlong),
}


def _candidate_names() -> list[str]:
    """Platform-specific shared-library filenames to look for, in priority order."""
    if sys.platform == "darwin":
        return ["libavr8sharp.dylib", "Avr8Sharp.Native.dylib"]
    if sys.platform == "win32":
        return ["avr8sharp.dll", "Avr8Sharp.Native.dll"]
    return ["libavr8sharp.so", "Avr8Sharp.Native.so"]


def _find_library() -> Path:
    """Locate the bundled native library next to this package.

    Honours ``AVR8SHARP_LIBRARY`` for development against a freshly published build.
    """
    import os

    override = os.environ.get("AVR8SHARP_LIBRARY")
    if override:
        p = Path(override)
        if p.is_file():
            return p
        raise FileNotFoundError(f"AVR8SHARP_LIBRARY points to a missing file: {p}")

    here = Path(__file__).resolve().parent
    for name in _candidate_names():
        candidate = here / name
        if candidate.is_file():
            return candidate

    searched = ", ".join(_candidate_names())
    raise FileNotFoundError(
        f"Could not find the Avr8Sharp native library next to {here}. "
        f"Looked for: {searched}. Set AVR8SHARP_LIBRARY to override."
    )


_lib: ctypes.CDLL | None = None


def lib() -> ctypes.CDLL:
    """Loads (once) and returns the native library with all signatures applied."""
    global _lib
    if _lib is not None:
        return _lib
    loaded = ctypes.CDLL(str(_find_library()))
    for name, (argtypes, restype) in _SIGNATURES.items():
        fn = getattr(loaded, name)
        fn.argtypes = argtypes
        fn.restype = restype
    _lib = loaded
    return _lib
