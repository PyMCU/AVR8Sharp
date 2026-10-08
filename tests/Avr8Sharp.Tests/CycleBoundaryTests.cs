using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

/// <summary>
/// Regression tests for issue #25 (slowdown past ~130 s at 16 MHz). The suspect was a
/// 32-bit wrap of the cycle counter at 2^31 (~134 s) or 2^32 (~268 s). These tests park
/// <c>Cpu.Cycles</c> just below each boundary and check that clock events and the timer
/// keep firing at the right cycle and rate while crossing it.
/// </summary>
[TestFixture]
public class CycleBoundary : AvrTestBase
{
	const int TCCR0B = 0x45;
	const int TCNT0 = 0x46;
	const int CS01 = 2;
	const int CS00 = 1;

	private AvrTimer _timer0;

	protected override void SetupPeripherals()
	{
		_timer0 = new AvrTimer (Cpu, AvrTimer.Timer0Config);
	}

	private static readonly ulong[] Boundaries = [1UL << 31, 1UL << 32];

	[Test (Description = "A clock event scheduled across a 32-bit boundary fires exactly at its cycle")]
	public void ClockEventFiresAtExactCycleAcrossBoundary ([ValueSource (nameof (Boundaries))] ulong boundary)
	{
		Cpu.Cycles = boundary - 5;
		ulong firedAt = 0;
		var fired = 0;
		Cpu.AddClockEvent (() => { fired++; firedAt = Cpu.Cycles; }, 10);

		for (var i = 0; i < 20; i++) {
			Cpu.Cycles++;
			Cpu.Tick ();
		}

		Assert.That (fired, Is.EqualTo (1));
		Assert.That (firedAt, Is.EqualTo (boundary + 5));
	}

	[Test (Description = "Events keep their order when the queue spans a 32-bit boundary")]
	public void ClockEventsKeepOrderAcrossBoundary ([ValueSource (nameof (Boundaries))] ulong boundary)
	{
		Cpu.Cycles = boundary - 3;
		var order = new List<int> ();
		Cpu.AddClockEvent (() => order.Add (3), 8);
		Cpu.AddClockEvent (() => order.Add (1), 2);
		Cpu.AddClockEvent (() => order.Add (2), 4);

		for (var i = 0; i < 10; i++) {
			Cpu.Cycles++;
			Cpu.Tick ();
		}

		Assert.That (order, Is.EqualTo (new[] { 1, 2, 3 }));
	}

	[Test (Description = "Timer0 at prescaler 64 counts at the same rate across a 32-bit boundary as from zero")]
	public void TimerRateIsStableAcrossBoundary ([ValueSource (nameof (Boundaries))] ulong boundary)
	{
		var baseline = RunTimer (1000);
		var crossing = RunTimer (boundary - 1000);

		Assert.That (baseline.Overflows, Is.EqualTo (3)); // 1000 timer ticks, 256 counts per overflow
		Assert.That (crossing, Is.EqualTo (baseline));
	}

	// Runs 1000 timer ticks (prescaler 64) starting with the cycle counter at startCycles
	private (int Overflows, byte Tcnt) RunTimer (ulong startCycles)
	{
		Setup ();
		Cpu.Cycles = startCycles;
		Cpu.WriteData (TCCR0B, CS01 | CS00);
		Cpu.Cycles++;
		Cpu.Tick ();

		var overflows = 0;
		var lastTcnt = Cpu.ReadData (TCNT0);
		var begin = Cpu.Cycles;
		while (Cpu.Cycles - begin < 64 * 1000) {
			Cpu.Cycles++;
			Cpu.Tick ();
			var tcnt = Cpu.ReadData (TCNT0);
			if (tcnt < lastTcnt) overflows++;
			lastTcnt = tcnt;
		}

		return (overflows, lastTcnt);
	}
}
