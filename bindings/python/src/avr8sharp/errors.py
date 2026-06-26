"""Exception types for the avr8sharp bindings."""

from __future__ import annotations


class Avr8SharpError(RuntimeError):
    """Raised when a native call fails (assembly error, timeout, bad index, etc.)."""
