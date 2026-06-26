"""Pythonic fluent wrapper over the Avr8Sharp native simulation harness.

Mirrors the C# ``Avr8Sharp.TestKit`` API. A single ``run_*`` call executes many thousands of CPU
cycles entirely on the native side, so the binding adds negligible overhead.

Example::

    from avr8sharp import ArduinoUno

    uno = ArduinoUno().with_hex(open("blink.hex").read())
    uno.run_ms(500)
    assert uno.port_b.pin_high(5)
    assert "Hello" in uno.serial.text
"""

from __future__ import annotations

import ctypes
from enum import IntEnum

from . import _native
from .errors import Avr8SharpError

_ERR_BUF = 1024


class PinState(IntEnum):
    LOW = 0
    HIGH = 1
    INPUT = 2
    INPUT_PULLUP = 3


def _check(lib, handle, rc: int, what: str) -> None:
    if rc >= 0:
        return
    buf = ctypes.create_string_buffer(_ERR_BUF)
    n = lib.a8s_last_error(handle, buf, _ERR_BUF)
    msg = buf.raw[: max(n, 0)].decode("utf-8", "replace") if n > 0 else ""
    raise Avr8SharpError(f"{what} failed (code {rc}){f': {msg}' if msg else ''}")


class Cpu:
    """Read/write accessor for CPU registers and data memory."""

    def __init__(self, sim: "Simulation") -> None:
        self._sim = sim

    @property
    def pc(self) -> int:
        return self._sim._lib.a8s_cpu_pc(self._sim._h)

    @property
    def sp(self) -> int:
        return self._sim._lib.a8s_cpu_sp(self._sim._h)

    @property
    def cycles(self) -> int:
        return self._sim._lib.a8s_cpu_cycles(self._sim._h)

    @property
    def sreg(self) -> int:
        return self._sim._lib.a8s_cpu_sreg(self._sim._h)

    def reg(self, index: int) -> int:
        """Returns general-purpose register R0..R31 (data address == index)."""
        return self.read(index)

    def read(self, address: int) -> int:
        return self._sim._lib.a8s_read_data(self._sim._h, address)

    def write(self, address: int, value: int) -> None:
        lib = self._sim._lib
        _check(lib, self._sim._h, lib.a8s_write_data(self._sim._h, address, value & 0xFF), "write_data")


class Port:
    """GPIO port accessor, identified by a port index registered at creation."""

    def __init__(self, sim: "Simulation", index: int) -> None:
        self._sim = sim
        self._index = index

    def state(self, pin: int) -> PinState:
        rc = self._sim._lib.a8s_gpio_pin(self._sim._h, self._index, pin)
        if rc < 0:
            _check(self._sim._lib, self._sim._h, rc, "gpio_pin")
        return PinState(rc)

    def pin_high(self, pin: int) -> bool:
        return self.state(pin) == PinState.HIGH

    def pin_low(self, pin: int) -> bool:
        return self.state(pin) == PinState.LOW


def _read_buffer(fn, sim: "Simulation", *args) -> bytes:
    """Two-pass read of a native length-prefixed byte buffer (call with cap, then size exactly)."""
    total = fn(sim._h, *args, None, 0)
    if total <= 0:
        return b""
    buf = ctypes.create_string_buffer(total)
    n = fn(sim._h, *args, buf, total)
    return buf.raw[: max(n, 0)]


class Adc:
    """Analog input injection (ATmega328P-family sessions). Set the voltage a channel presents
    so a firmware ADC read on that channel returns the matching value."""

    def __init__(self, sim: "Simulation") -> None:
        self._sim = sim

    def set_channel(self, channel: int, volts: float) -> None:
        lib = self._sim._lib
        _check(lib, self._sim._h, lib.a8s_adc_set_channel(self._sim._h, channel, volts), "adc_set_channel")


class Spi:
    """SPI bus stub: captures what the firmware clocks out (MOSI) and replays a canned response
    queue back (MISO). The standard way to test SPI sensor/display drivers."""

    def __init__(self, sim: "Simulation") -> None:
        self._sim = sim

    def queue_response(self, *values: int) -> None:
        lib = self._sim._lib
        for v in values:
            _check(lib, self._sim._h, lib.a8s_spi_queue_response(self._sim._h, v & 0xFF), "spi_queue_response")

    @property
    def mosi(self) -> bytes:
        return _read_buffer(self._sim._lib.a8s_spi_read_mosi, self._sim)


class Twi:
    """I²C single-address slave stub: ACKs its address, logs the bytes the firmware writes, and
    replays a canned response queue on reads."""

    def __init__(self, sim: "Simulation") -> None:
        self._sim = sim

    def set_slave(self, address: int, present: bool = True) -> None:
        lib = self._sim._lib
        rc = lib.a8s_twi_set_slave(self._sim._h, address, 1 if present else 0)
        _check(lib, self._sim._h, rc, "twi_set_slave")

    def queue_response(self, *values: int) -> None:
        lib = self._sim._lib
        for v in values:
            _check(lib, self._sim._h, lib.a8s_twi_queue_response(self._sim._h, v & 0xFF), "twi_queue_response")

    @property
    def writes(self) -> bytes:
        return _read_buffer(self._sim._lib.a8s_twi_read_writes, self._sim)


class Serial:
    """USART capture probe accessor, identified by a serial index registered at creation."""

    def __init__(self, sim: "Simulation", index: int) -> None:
        self._sim = sim
        self._index = index

    @property
    def bytes(self) -> bytes:
        lib, h = self._sim._lib, self._sim._h
        n = lib.a8s_serial_byte_count(h, self._index)
        if n <= 0:
            return b""
        buf = ctypes.create_string_buffer(n)
        total = lib.a8s_serial_read(h, self._index, buf, n)
        return buf.raw[: max(total, 0)]

    @property
    def text(self) -> str:
        # Latin-1 matches the C# SerialProbe.Text decoding (1 byte == 1 char).
        return self.bytes.decode("latin-1")

    @property
    def byte_count(self) -> int:
        return self._sim._lib.a8s_serial_byte_count(self._sim._h, self._index)

    @property
    def lines(self) -> list[str]:
        return [ln.rstrip("\r") for ln in self.text.split("\n")]

    def clear(self) -> None:
        lib = self._sim._lib
        _check(lib, self._sim._h, lib.a8s_serial_clear(self._sim._h, self._index), "serial_clear")

    def inject(self, value: int) -> None:
        lib = self._sim._lib
        _check(lib, self._sim._h, lib.a8s_serial_inject(self._sim._h, self._index, value & 0xFF), "serial_inject")


class Simulation:
    """Base fluent simulation handle. Use a board subclass (:class:`ArduinoUno`, etc.) for the
    common cases, or construct a blank one with :meth:`create` and wire peripherals manually."""

    def __init__(self, handle: int) -> None:
        if not handle:
            raise Avr8SharpError("Native simulation could not be created (null handle).")
        self._lib = _native.lib()
        self._h = handle
        self.cpu = Cpu(self)

    # ── factories ─────────────────────────────────────────────────────────────

    @classmethod
    def create(cls, flash: int = 0x8000, sram: int = 8192) -> "Simulation":
        return cls(_native.lib().a8s_create(flash, sram))

    # ── program loading ───────────────────────────────────────────────────────

    def with_frequency(self, hz: int) -> "Simulation":
        _check(self._lib, self._h, self._lib.a8s_with_frequency(self._h, hz), "with_frequency")
        return self

    def with_hex(self, hex_content: str) -> "Simulation":
        data = hex_content.encode("utf-8")
        _check(self._lib, self._h, self._lib.a8s_with_hex(self._h, data, len(data)), "with_hex")
        return self

    def with_asm(self, asm_source: str) -> "Simulation":
        data = asm_source.encode("utf-8")
        _check(self._lib, self._h, self._lib.a8s_with_asm(self._h, data, len(data)), "with_asm")
        return self

    def with_program(self, program: bytes) -> "Simulation":
        _check(self._lib, self._h, self._lib.a8s_with_program(self._h, program, len(program)), "with_program")
        return self

    # ── execution ─────────────────────────────────────────────────────────────

    def run_cycles(self, cycles: int) -> "Simulation":
        _check(self._lib, self._h, self._lib.a8s_run_cycles(self._h, cycles), "run_cycles")
        return self

    def run_ms(self, ms: float) -> "Simulation":
        _check(self._lib, self._h, self._lib.a8s_run_ms(self._h, ms), "run_ms")
        return self

    def run_instructions(self, count: int) -> "Simulation":
        _check(self._lib, self._h, self._lib.a8s_run_instructions(self._h, count), "run_instructions")
        return self

    def run_to_break(self, max_instructions: int = 100_000) -> "Simulation":
        _check(self._lib, self._h, self._lib.a8s_run_to_break(self._h, max_instructions), "run_to_break")
        return self

    def run_until_serial(self, serial: "Serial", text: str, max_ms: float = 2000) -> "Simulation":
        data = text.encode("utf-8")
        rc = self._lib.a8s_run_until_serial(self._h, serial._index, data, len(data), max_ms)
        _check(self._lib, self._h, rc, "run_until_serial")
        return self

    def run_until_serial_bytes(self, serial: "Serial", byte_count: int, max_ms: float = 2000) -> "Simulation":
        rc = self._lib.a8s_run_until_serial_bytes(self._h, serial._index, byte_count, max_ms)
        _check(self._lib, self._h, rc, "run_until_serial_bytes")
        return self

    # ── custom peripheral wiring (blank ATmega328P-style sessions) ─────────────

    def add_gpio(self, which: int) -> "Port":
        """which: 0=PortB, 1=PortC, 2=PortD."""
        idx = self._lib.a8s_add_gpio(self._h, which)
        _check(self._lib, self._h, idx, "add_gpio")
        return Port(self, idx)

    def add_usart0(self) -> "Serial":
        idx = self._lib.a8s_add_usart0(self._h)
        _check(self._lib, self._h, idx, "add_usart0")
        return Serial(self, idx)

    def add_timer(self, which: int) -> int:
        idx = self._lib.a8s_add_timer(self._h, which)
        _check(self._lib, self._h, idx, "add_timer")
        return idx

    # ── reset / snapshot ──────────────────────────────────────────────────────

    def reset(self) -> "Simulation":
        _check(self._lib, self._h, self._lib.a8s_reset(self._h), "reset")
        return self

    def snapshot(self) -> "Simulation":
        """Captures the power-on Data snapshot for :meth:`restore`. Call right after creation,
        before loading firmware, to capture peripheral power-on defaults."""
        _check(self._lib, self._h, self._lib.a8s_snapshot(self._h), "snapshot")
        return self

    def restore(self) -> "Simulation":
        """Full per-test reset: restores the snapshot, resets timers/CPU, zeroes the cycle
        counter, and clears all serial probes (mirrors PyMCU's SimSession)."""
        _check(self._lib, self._h, self._lib.a8s_restore(self._h), "restore")
        return self

    # ── lifecycle ─────────────────────────────────────────────────────────────

    def close(self) -> None:
        if getattr(self, "_h", None):
            self._lib.a8s_destroy(self._h)
            self._h = 0

    def __enter__(self) -> "Simulation":
        return self

    def __exit__(self, *exc) -> None:
        self.close()

    def __del__(self) -> None:
        try:
            self.close()
        except Exception:
            pass


class ArduinoUno(Simulation):
    """ATmega328P: ports B/C/D, timers 0/1/2, USART0, plus ADC and SPI/I²C device stubs.
    16 MHz, 32 KB flash, 2 KB SRAM."""

    def __init__(self) -> None:
        super().__init__(_native.lib().a8s_create_uno())
        self.port_b = Port(self, 0)
        self.port_c = Port(self, 1)
        self.port_d = Port(self, 2)
        self.serial = Serial(self, 0)
        self.adc = Adc(self)
        self.spi = Spi(self)
        self.twi = Twi(self)


class ArduinoMega(Simulation):
    """ATmega2560: ports A..L, timers 0..5, USART0..3."""

    _PORT_LETTERS = "ABCDEFGHJKL"

    def __init__(self) -> None:
        super().__init__(_native.lib().a8s_create_mega())
        for i, letter in enumerate(self._PORT_LETTERS):
            setattr(self, f"port_{letter.lower()}", Port(self, i))
        self.serial = Serial(self, 0)
        self.serial0 = Serial(self, 0)
        self.serial1 = Serial(self, 1)
        self.serial2 = Serial(self, 2)
        self.serial3 = Serial(self, 3)


class ATtiny85(Simulation):
    """ATtiny85: port B, timers 0/1, no USART."""

    def __init__(self) -> None:
        super().__init__(_native.lib().a8s_create_attiny85())
        self.port_b = Port(self, 0)


class ATtinyX4(Simulation):
    """ATtiny24/44/84 (avr25): ports A and B. GPIO-focused. flash/sram pick the variant."""

    def __init__(self, flash: int = 0x2000, sram: int = 512) -> None:
        super().__init__(_native.lib().a8s_create_attinyx4(flash, sram))
        self.port_a = Port(self, 0)
        self.port_b = Port(self, 1)


class ATtiny13(Simulation):
    """ATtiny13/13A (avr25): port B only. GPIO-focused."""

    def __init__(self) -> None:
        super().__init__(_native.lib().a8s_create_attiny13())
        self.port_b = Port(self, 0)


# PyMCU `target` (chip) -> simulation factory. The ATmegaX8 family shares the ATmega328P
# register map, so smaller siblings run correctly in the 328P session (its 32 KB flash is a
# superset). ATtiny 25/45/85 share the ATtiny85 peripheral layout.
_X8_FAMILY = frozenset({
    "atmega48", "atmega48p", "atmega88", "atmega88p",
    "atmega168", "atmega168p", "atmega328", "atmega328p",
})
_ATTINY_X5 = frozenset({"attiny25", "attiny45", "attiny85"})
# ATtiny24/44/84 share ports A+B; the value is (flash, sram) for the variant.
_ATTINY_X4 = {"attiny84": (0x2000, 512), "attiny44": (0x1000, 256), "attiny24": (0x0800, 128)}
_ATTINY13 = frozenset({"attiny13", "attiny13a"})

# Targets known to PyMCU but without a faithful avr8sharp preset yet (see README/coverage notes).
_UNSUPPORTED_TARGETS = frozenset({
    "atmega32u4",  # Leonardo: distinct port/timer/USART layout, no preset
    "attiny2313", "attiny4313",  # USART parts; preset is a follow-up
})


def board_for_target(target: str) -> Simulation:
    """Returns a simulation matching a PyMCU ``[tool.pymcu] target`` chip name.

    Raises :class:`Avr8SharpError` for targets without a faithful preset yet (e.g. atmega32u4).
    """
    name = target.strip().lower()
    if name in _X8_FAMILY:
        return ArduinoUno()
    if name == "atmega2560":
        return ArduinoMega()
    if name in _ATTINY_X5:
        return ATtiny85()
    if name in _ATTINY_X4:
        flash, sram = _ATTINY_X4[name]
        return ATtinyX4(flash, sram)
    if name in _ATTINY13:
        return ATtiny13()
    if name in _UNSUPPORTED_TARGETS:
        raise Avr8SharpError(
            f"No avr8sharp simulation preset for target '{target}' yet. "
            f"Supported: ATmegaX8 family (48/88/168/328), atmega2560, attiny25/45/85."
        )
    raise Avr8SharpError(f"Unknown PyMCU target '{target}'.")


# Common board aliases -> factory.
_BOARD_ALIASES = {
    "uno": ArduinoUno,
    "arduino_uno": ArduinoUno,
    "mega": ArduinoMega,
    "arduino_mega": ArduinoMega,
    "attiny85": ATtiny85,
}


def board(name: str) -> Simulation:
    """Returns a simulation for a board alias (``uno``/``mega``/``attiny85``) or a PyMCU target
    chip name (``atmega328p``, ``atmega2560``, ...)."""
    key = name.strip().lower()
    if key in _BOARD_ALIASES:
        return _BOARD_ALIASES[key]()
    return board_for_target(key)


def selftest() -> int:
    """Returns the low byte of the native self-test (42 when the engine is healthy)."""
    return _native.lib().a8s_selftest() & 0xFF
