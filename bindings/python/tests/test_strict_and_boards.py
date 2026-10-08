"""Strict mode for unmounted GPIO ports, and the SPI/TWI/ADC/watchdog peripherals of the board presets."""

from __future__ import annotations

import pytest

import avr8sharp as a

MCUSR = 0x54
READ_PIND = "in r16, 0x09\nbreak\n"


def test_unmounted_port_reads_zero_by_default():
    sim = a.Simulation.create()
    sim.with_asm(READ_PIND).run_to_break()
    assert sim.cpu.reg(16) == 0
    sim.close()


def test_strict_raises_naming_register_and_address():
    sim = a.Simulation.create().with_strict()
    sim.with_asm(READ_PIND)
    with pytest.raises(a.Avr8SharpError, match=r"PIND \(0x29\) read but port D is not mounted"):
        sim.run_to_break()
    sim.close()


def test_strict_allows_a_mounted_port():
    sim = a.Simulation.create().with_strict()
    sim.add_gpio(2)
    sim.with_asm(READ_PIND).run_to_break()
    sim.close()


def test_strict_warn_records_without_raising():
    sim = a.Simulation.create().with_strict("warn")
    sim.with_asm(READ_PIND + "").run_to_break()
    assert len(sim.warnings) == 1 and "PIND" in sim.warnings[0]
    sim.close()


def test_uno_mcusr_is_porf_and_adc_spi_twi_exist():
    uno = a.ArduinoUno()
    assert uno.cpu.read(MCUSR) == 0x01
    uno.with_asm(
        "ldi r16, 0x50\nout 0x2C, r16\nldi r16, 0xAA\nout 0x2E, r16\n"
        "w: in r17, 0x2D\nsbrs r17, 7\nrjmp w\nin r18, 0x2E\nbreak\n"
    )
    uno.spi.queue_response(0x42)
    uno.run_to_break()
    assert uno.cpu.reg(18) == 0x42
    uno.close()


def test_mega_has_the_bus_peripherals():
    mega = a.ArduinoMega()
    assert mega.cpu.read(MCUSR) == 0x01
    mega.adc.set_channel(9, 2.5)
    mega.close()
