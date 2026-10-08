"""Tests for driving external stimuli into a simulation: input-pin injection, microsecond
stepping, UART RX injection, and the timed-responder helpers built on top of them.
"""

from __future__ import annotations

import pytest

import avr8sharp as a

# ATmega328P I/O register data addresses (see the datasheet, or GpioTests.cs in the C# suite).
PINB = 0x23
PORTB_OUT = 0x25  # data address of PORTB (I/O address 0x05)
DDRB = 0x24  # data address of DDRB (I/O address 0x04)
PIND = 0x29
PORTD = 0x2b
UDR0 = 0xC6
UCSRA0 = 0xC0
UCSRA_RXC = 0x80
UCSRB0 = 0xC1
UCSRB_RXEN = 0x10

# Drives PB0 high as an output: the "firmware" stand-in for a trigger pin in the responder tests.
DRIVE_PB0_HIGH = "ldi r16, 0x01\nout 0x04, r16\nout 0x05, r16\nbreak\n"


def test_run_us_advances_cycles_precisely():
    sim = a.Simulation.create()
    sim.run_us(10)
    assert sim.cpu.cycles == 160  # 10 us * 16 cycles/us @ the default 16 MHz
    sim.close()


def test_set_pin_drives_an_input_pin():
    # PD2 stays at its reset default (input, no pull-up): driving it is uncontested.
    sim = a.Simulation.create()
    port_d = sim.add_gpio(2)

    port_d.set_high(2)
    assert sim.cpu.read(PIND) & 0x04

    sim.run_us(500)
    port_d.set_low(2)
    assert not (sim.cpu.read(PIND) & 0x04)
    assert sim.cpu.cycles == 8000  # 500 us * 16 cycles/us
    sim.close()


def test_set_pin_has_no_effect_on_an_output_pin():
    # The firmware drives PB0 high as an output; an external set() must not contend with it.
    uno = a.ArduinoUno().with_asm(DRIVE_PB0_HIGH).run_to_break()
    assert uno.cpu.read(PINB) & 0x01

    uno.port_b.set_low(0)
    assert uno.cpu.read(PINB) & 0x01, "an output pin ignores external injection"
    uno.close()


def test_release_returns_an_input_pin_to_its_pull_up():
    sim = a.Simulation.create()
    port_d = sim.add_gpio(2)

    port_d.set_low(2)
    sim.cpu.write(PORTD, 0x04)  # pull-up on, but the line is driven low
    assert not (sim.cpu.read(PIND) & 0x04)

    port_d.release(2)
    assert sim.cpu.read(PIND) & 0x04  # floating, the pull-up wins
    sim.close()


def test_serial_inject_arrives_in_udr_after_settling_time():
    sim = a.Simulation.create()
    serial = sim.add_usart0()
    sim.cpu.write(UCSRB0, UCSRB_RXEN)  # enable the receiver, like firmware would at startup

    serial.inject(0x41)
    sim.run_us(1000)  # generous margin past the default (fast, unconfigured-baud) frame time

    # Reading UDR clears RXC (real hardware behaviour), so check the flag first.
    assert sim.cpu.read(UCSRA0) & UCSRA_RXC
    assert sim.cpu.read(UDR0) == 0x41
    sim.close()


def test_serial_inject_bytes_back_to_back_only_the_first_survives():
    # Both calls land at the same simulated instant, so the second byte arrives while the
    # receiver is still mid-frame on the first and is silently dropped -- real UART overrun
    # behaviour, not a partial write. Space bytes out with run_us for reliable multi-byte
    # delivery.
    sim = a.Simulation.create()
    serial = sim.add_usart0()
    sim.cpu.write(UCSRB0, UCSRB_RXEN)

    serial.inject_bytes(b"AB")
    sim.run_us(1000)

    assert sim.cpu.read(UDR0) == ord("A")
    sim.close()


def test_wait_for_rise_times_out_when_the_pin_never_rises():
    sim = a.Simulation.create()
    port_d = sim.add_gpio(2)  # never driven as an output; state() never reports HIGH

    with pytest.raises(TimeoutError):
        a.wait_for_rise(sim, port_d, 2, poll_us=1, timeout_us=50)
    sim.close()


def test_wait_for_rise_measures_a_delayed_firmware_edge():
    # PB0 starts low (output, PORTB=0), then goes high after a fixed delay of NOPs, then breaks.
    delay_cycles = 480  # 30 us @ 16 MHz
    asm = (
        "ldi r16, 0x01\n"
        "out 0x04, r16\n"  # DDRB = 0x01 (PB0 output), PORTB left at 0 (low)
        + "nop\n" * delay_cycles
        + "ldi r16, 0x01\nout 0x05, r16\nbreak\n"  # PORTB = 0x01 (PB0 high)
    )
    uno = a.ArduinoUno().with_asm(asm)

    waited = a.wait_for_rise(uno, uno.port_b, 0, poll_us=1, timeout_us=1000)

    assert 29 <= waited <= 32
    uno.close()


def test_hc_sr04_echo_drives_a_pulse_sized_to_the_distance():
    uno = a.ArduinoUno().with_asm(DRIVE_PB0_HIGH).run_to_break()  # PB0 (trigger) already high
    before = uno.cpu.cycles

    pulse_us = a.hc_sr04_echo(
        uno, uno.port_b, 0,  # trigger pin
        uno.port_b, 1,  # echo pin
        distance_cm=9.7,
        echo_delay_us=450,
    )

    assert pulse_us == pytest.approx(9.7 / a.HCSR04_US_PER_CM)
    # The function leaves the echo pin low again once the pulse ends.
    assert not (uno.cpu.read(PINB) & 0x02)
    # Elapsed time is the echo delay plus the pulse width (no time was spent waiting: the
    # trigger was already high), each run_us call truncated to whole cycles.
    expected_cycles = int(450 / 1_000_000 * 16_000_000) + int(pulse_us / 1_000_000 * 16_000_000)
    assert uno.cpu.cycles - before == expected_cycles
    uno.close()


def test_respond_to_rise_reports_the_wait_before_the_trigger():
    delay_cycles = 160  # 10 us @ 16 MHz
    asm = (
        "ldi r16, 0x01\nout 0x04, r16\n"
        + "nop\n" * delay_cycles
        + "ldi r16, 0x01\nout 0x05, r16\nbreak\n"
    )
    uno = a.ArduinoUno().with_asm(asm)

    waited = a.respond_to_rise(
        uno, uno.port_b, 0, uno.port_b, 1,
        delay_us=20, pulse_us=30, poll_us=1,
    )

    assert 9 <= waited <= 12
    uno.close()
