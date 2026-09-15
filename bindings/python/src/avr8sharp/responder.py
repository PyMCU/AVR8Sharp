"""Timed-response helpers for driving external stimuli into a simulation.

Built entirely on the public ``run_us`` / ``Port.set`` / ``Port.state`` API -- no new native
surface. A "responder" watches a pin the firmware drives and, once it sees the stimulus it is
waiting for, drives another pin back after a delay: the pattern behind an echo sensor, a 1-Wire
device, or an interrupt line answering a request. Polling happens in ``poll_us`` steps on the
Python side; each step is one coarse-grained ``run_us`` call, so the per-instruction loop never
crosses the FFI boundary.
"""

from __future__ import annotations

from .simulation import PinState, Port, Simulation


def wait_for_rise(
    sim: Simulation,
    port: Port,
    pin: int,
    poll_us: float = 1.0,
    timeout_us: float = 100_000.0,
) -> float:
    """Runs `sim` in `poll_us` steps until `port` pin `pin` reads HIGH.

    `pin` must be a pin the firmware drives as an output (its ``Port.state`` reports what the
    chip itself is driving, not an externally injected input level) -- this is what a real
    trigger/request line from firmware looks like from the outside.

    Returns the simulated microseconds elapsed before the rise. Raises ``TimeoutError`` if the
    pin has not risen after `timeout_us`.
    """
    elapsed = 0.0
    while elapsed < timeout_us:
        if port.state(pin) == PinState.HIGH:
            return elapsed
        sim.run_us(poll_us)
        elapsed += poll_us
    raise TimeoutError(f"wait_for_rise: pin {pin} did not rise within {timeout_us} us")


def wait_for_fall(
    sim: Simulation,
    port: Port,
    pin: int,
    poll_us: float = 1.0,
    timeout_us: float = 100_000.0,
) -> float:
    """As :func:`wait_for_rise`, but waits for `pin` to read LOW."""
    elapsed = 0.0
    while elapsed < timeout_us:
        if port.state(pin) == PinState.LOW:
            return elapsed
        sim.run_us(poll_us)
        elapsed += poll_us
    raise TimeoutError(f"wait_for_fall: pin {pin} did not fall within {timeout_us} us")


def respond_to_rise(
    sim: Simulation,
    trigger_port: Port,
    trigger_pin: int,
    echo_port: Port,
    echo_pin: int,
    delay_us: float,
    pulse_us: float,
    poll_us: float = 1.0,
    timeout_us: float = 100_000.0,
) -> float:
    """Generic edge-triggered responder.

    Waits for `trigger_pin` to rise, then after `delay_us` drives `echo_pin` high for
    `pulse_us`, then low. Returns the simulated microseconds spent waiting for the trigger to
    rise. Models any sensor that answers a request pulse with a timed pulse of its own (an
    HC-SR04 ultrasonic rangefinder, a 1-Wire device, ...); see :func:`hc_sr04_echo` for that
    specific model.
    """
    waited = wait_for_rise(sim, trigger_port, trigger_pin, poll_us, timeout_us)
    sim.run_us(delay_us)
    echo_port.set_high(echo_pin)
    sim.run_us(pulse_us)
    echo_port.set_low(echo_pin)
    return waited


# Round-trip microseconds per centimetre at the speed of sound the HC-SR04 datasheet assumes --
# the constant its formula uses to turn a measured echo pulse width back into a distance
# (`distance_cm = pulse_us * HCSR04_US_PER_CM`).
HCSR04_US_PER_CM = 0.017


def hc_sr04_echo(
    sim: Simulation,
    trigger_port: Port,
    trigger_pin: int,
    echo_port: Port,
    echo_pin: int,
    distance_cm: float,
    echo_delay_us: float = 450.0,
    poll_us: float = 1.0,
    timeout_us: float = 100_000.0,
) -> float:
    """Models an HC-SR04 ultrasonic rangefinder.

    Waits for the firmware's trigger pulse on `trigger_pin`, then after `echo_delay_us` (the
    sensor's internal turnaround before it starts chirping and listening) drives `echo_pin`
    high for ``distance_cm / HCSR04_US_PER_CM`` microseconds, then low.

    Returns the echo pulse width actually driven, in microseconds, so a caller can compare it
    against what the firmware reports back.
    """
    pulse_us = distance_cm / HCSR04_US_PER_CM
    respond_to_rise(
        sim,
        trigger_port,
        trigger_pin,
        echo_port,
        echo_pin,
        delay_us=echo_delay_us,
        pulse_us=pulse_us,
        poll_us=poll_us,
        timeout_us=timeout_us,
    )
    return pulse_us
