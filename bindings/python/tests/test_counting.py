"""Execution-counting tests: enable_counting() + counts() against a known loop.

The program is the same shape the C# suite pins down:

    pc0  ldi r16, 5     — 1 exec, 1 cycle
    pc1  dec r16        — 5 execs (trip count), 1 cycle each
    pc2  brne loop      — 5 execs; taken 4x at 2 cy, falls through once at 1 cy
    pc3  break          — never executed
"""

from __future__ import annotations

import pytest

import avr8sharp as a

LOOP5 = "ldi r16, 5\nloop: dec r16\nbrne loop\nbreak\n"


def test_counts_exact_loop_counts_and_cycles():
    uno = a.ArduinoUno()
    uno.with_asm(LOOP5)
    uno.enable_counting()

    uno.run_instructions(11)  # ldi + 5x(dec, brne)

    pc_count, pc_cycles, taken, not_taken = uno.counts()
    assert uno.cpu.pc == 3
    assert list(pc_count[:4]) == [1, 5, 5, 0]
    assert list(pc_cycles[:4]) == [1, 5, 9, 0]
    assert taken[2] == 4 and not_taken[2] == 1
    # nothing else is a branch
    assert sum(taken) + sum(not_taken) == 5
    assert sum(pc_count) == 11
    uno.close()


def test_counts_require_enable():
    uno = a.ArduinoUno()
    uno.with_asm(LOOP5)
    with pytest.raises(a.Avr8SharpError):
        uno.counts()
    uno.close()


def test_disable_counting_returns_to_plain_path():
    uno = a.ArduinoUno()
    uno.with_asm(LOOP5)
    uno.enable_counting()
    uno.run_instructions(6)
    uno.disable_counting()
    uno.run_instructions(5)  # finishes the loop uncounted

    pc_count, _, taken, not_taken = uno.counts()
    # only the first 6 instructions were counted: ldi + (dec, brne) x2 + dec
    assert pc_count[0] == 1
    assert pc_count[1] == 3
    assert taken[2] == 2 and not_taken[2] == 0
    uno.close()


def test_enable_counting_restarts_counters():
    uno = a.ArduinoUno()
    uno.with_asm(LOOP5)
    uno.enable_counting()
    uno.run_instructions(3)
    uno.enable_counting()  # fresh zeroed arrays
    uno.reset()
    uno.run_instructions(11)

    pc_count, _, taken, _ = uno.counts()
    assert pc_count[0] == 1 and pc_count[1] == 5
    assert taken[2] == 4
    uno.close()
