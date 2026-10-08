using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

/// <summary>SetInputs, the no-change early returns, and OutputEnableMask / OutputLevels.</summary>
[TestFixture]
public class GpioBatchInput : AvrTestBase
{
	const int PINB = 0x23;
	const int DDRB = 0x24;
	const int PORTB = 0x25;
	const int DDRD = 0x2a;
	const int PCICR = 0x68;
	const int PCIFR = 0x3b;
	const int PCMSK0 = 0x6b;
	const int TCCR0A = 0x44;
	const int TCCR0B = 0x45;
	const int OCR0A = 0x47;

	AvrIoPort _portB;
	AvrIoPort _portD;
	AvrTimer _timer0;

	protected override void SetupPeripherals ()
	{
		_timer0 = new AvrTimer (Cpu, AvrTimer.Timer0Config);
		_portB = new AvrIoPort (Cpu, AvrIoPort.PortBConfig);
		_portD = new AvrIoPort (Cpu, AvrIoPort.PortDConfig);
	}

	static void Drive (AvrIoPort port, byte mask, byte levels)
	{
		for (byte bit = 0; bit < 8; bit++)
			if ((mask & (1 << bit)) != 0)
				port.SetPinValue (bit, (levels & (1 << bit)) != 0);
	}

	static void Release (AvrIoPort port, byte mask)
	{
		for (byte bit = 0; bit < 8; bit++)
			if ((mask & (1 << bit)) != 0)
				port.ReleasePin (bit);
	}

	static void Apply (AvrIoPort port, byte drivenMask, byte levels)
	{
		for (byte bit = 0; bit < 8; bit++) {
			if ((drivenMask & (1 << bit)) != 0)
				port.SetPinValue (bit, (levels & (1 << bit)) != 0);
			else
				port.ReleasePin (bit);
		}
	}

	[Test (Description = "SetInputs / ReleaseInputs / ApplyInputs leave PIN, PCINT flag and listeners identical to per-bit calls")]
	public void EquivalentToPerBitCalls ()
	{
		// kind: 0 = SetInputs, 1 = ReleaseInputs, 2 = ApplyInputs
		var steps = new (int kind, byte mask, byte levels)[] {
			(0, 0xff, 0xa5), (0, 0xff, 0xa5), (0, 0x0f, 0xff), (1, 0xf0, 0), (0, 0x3c, 0x18), (1, 0x00, 0),
			(2, 0x7e, 0x24), (2, 0x7e, 0x24), (1, 0xff, 0), (0, 0x81, 0x80), (2, 0xff, 0x00),
		};
		foreach (var ddr in new byte[] { 0x00, 0x0f, 0xaa }) {
			foreach (var port in new byte[] { 0x00, 0x55, 0xff }) {
				var a = new AVR8Sharp.Core.Cpu (new ushort[1024], 8192);
				var b = new AVR8Sharp.Core.Cpu (new ushort[1024], 8192);
				var pa = new AvrIoPort (a, AvrIoPort.PortBConfig);
				var pb = new AvrIoPort (b, AvrIoPort.PortBConfig);
				var logA = new List<(byte, byte)> ();
				var logB = new List<(byte, byte)> ();
				pa.AddListener ((v, o) => logA.Add ((v, o)));
				pb.AddListener ((v, o) => logB.Add ((v, o)));
				var clocksA = new List<(int, bool)> ();
				var clocksB = new List<(int, bool)> ();
				for (var i = 0; i < 8; i++) {
					var bit = i;
					pa.ExternalClockListeners[i] = v => clocksA.Add ((bit, v));
					pb.ExternalClockListeners[i] = v => clocksB.Add ((bit, v));
				}
				foreach (var cpu in new[] { a, b }) {
					cpu.WriteData (DDRB, ddr);
					cpu.WriteData (PORTB, port);
					cpu.WriteData (PCMSK0, 0xff);
					cpu.WriteData (PCICR, 1);
				}
				foreach (var step in steps) {
					switch (step.kind) {
					case 0:
						Drive (pa, step.mask, step.levels);
						pb.SetInputs (step.mask, step.levels);
						break;
					case 1:
						Release (pa, step.mask);
						pb.ReleaseInputs (step.mask);
						break;
					default:
						Apply (pa, step.mask, step.levels);
						pb.ApplyInputs (step.mask, step.levels);
						break;
					}
					Assert.Multiple (() => {
						Assert.That (b.Mmio.Data[PINB], Is.EqualTo (a.Mmio.Data[PINB]), $"PIN ddr={ddr:x2} port={port:x2} step={step}");
						Assert.That (b.Mmio.Data[PCIFR], Is.EqualTo (a.Mmio.Data[PCIFR]));
						Assert.That (logB, Is.EqualTo (logA));
						Assert.That (clocksB, Is.EqualTo (clocksA));
					});
					b.Mmio.Data[PCIFR] = 0;
					a.Mmio.Data[PCIFR] = 0;
				}
			}
		}
	}

	[Test (Description = "SetInputs leaves pins outside the mask alone; ReleaseInputs and ApplyInputs release")]
	public void MaskSemantics ()
	{
		Cpu.WriteData (PORTB, 0x03); // pull-ups on PB0, PB1
		_portB.SetInputs (0xff, 0x00);
		Assert.That (Cpu.Mmio.Data[PINB], Is.EqualTo (0x00));
		_portB.SetInputs (0x80, 0x80);
		Assert.That (Cpu.Mmio.Data[PINB], Is.EqualTo (0x80), "other pins stay driven low");
		_portB.ReleaseInputs (0x03);
		Assert.That (Cpu.Mmio.Data[PINB], Is.EqualTo (0x83), "released pins read their pull-up");
		_portB.ApplyInputs (0x80, 0x00);
		Assert.That (Cpu.Mmio.Data[PINB], Is.EqualTo (0x03));
	}

	[Test (Description = "PullUpMask matches GetPinState")]
	public void PullUpMaskMatchesGetPinState ()
	{
		foreach (var oc in new byte[] { 0x00, 0x0f }) {
			_portB.OpenCollector = oc;
			foreach (var ddr in new byte[] { 0x00, 0x3c, 0xff }) {
				foreach (var port in new byte[] { 0x00, 0x55, 0xff }) {
					Cpu.WriteData (DDRB, ddr);
					Cpu.WriteData (PORTB, port);
					byte expected = 0;
					for (byte bit = 0; bit < 8; bit++)
						if (_portB.GetPinState (bit) == PinState.InputPullup) expected |= (byte)(1 << bit);
					Assert.That (_portB.PullUpMask, Is.EqualTo (expected), $"oc={oc:x2} ddr={ddr:x2} port={port:x2}");
				}
			}
		}
	}

	[Test (Description = "PinChanged reports chip-side changes with the cycle, honours WatchMask, costs nothing unsubscribed")]
	public void PinChangedEvent ()
	{
		var events = new List<(byte changed, byte en, byte lev, ulong cycle)> ();
		AvrPinChangeHandler handler = (c, e, l, cy) => events.Add ((c, e, l, cy));
		Cpu.WriteData (DDRB, 0x03);
		Cpu.WriteData (PORTB, 0x01); // nobody subscribed yet
		_portB.PinChanged += handler;

		Cpu.Cycles = 100;
		Cpu.WriteData (PORTB, 0x02);
		Assert.That (events, Is.EqualTo (new[] { ((byte)0x03, (byte)0x03, (byte)0x02, 100UL) }));

		events.Clear ();
		_portB.WatchMask = 0x02;
		Cpu.Cycles = 200;
		Cpu.WriteData (PORTB, 0x01);
		Assert.That (events, Is.EqualTo (new[] { ((byte)0x02, (byte)0x03, (byte)0x01, 200UL) }));

		events.Clear ();
		Cpu.WriteData (PORTB, 0x01); // unchanged
		Cpu.WriteData (DDRB, 0x01);  // PB1 released: output enable changes
		Assert.That (events, Is.EqualTo (new[] { ((byte)0x02, (byte)0x01, (byte)0x01, 200UL) }));

		events.Clear ();
		_portB.PinChanged -= handler;
		Cpu.WriteData (PORTB, 0x00);
		Assert.That (events, Is.Empty);
	}

	[Test (Description = "Nothing changed: no PCINT flag, no external clock listener")]
	public void NoChangeFiresNothing ()
	{
		Cpu.WriteData (PCMSK0, 0xff);
		Cpu.WriteData (PCICR, 1);
		var clocks = 0;
		for (var i = 0; i < 8; i++) _portB.ExternalClockListeners[i] = _ => clocks++;

		_portB.SetInputs (0xff, 0x0f);
		Assert.That (clocks, Is.EqualTo (4));
		Assert.That (Cpu.Mmio.Data[PCIFR] & 1, Is.EqualTo (1));
		Cpu.Mmio.Data[PCIFR] = 0;
		clocks = 0;

		_portB.SetInputs (0xff, 0x0f);
		_portB.SetPinValue (0, true);
		_portB.SetPinValue (5, false);
		_portB.ReleasePin (7); // changes: pin 7 was driven low, now floats low
		Assert.That (clocks, Is.EqualTo (0), "no PIN bit changed, so no listener");
		Assert.That (Cpu.Mmio.Data[PCIFR], Is.EqualTo (0));

		_portB.ReleasePin (7);
		_portB.SetInputs (0x7f, 0x0f);
		Assert.Multiple (() => {
			Assert.That (clocks, Is.EqualTo (0));
			Assert.That (Cpu.Mmio.Data[PCIFR], Is.EqualTo (0));
		});
	}

	[Test (Description = "Redundant SetPinValue / ReleasePin / SetInputs do not recompute PIN")]
	public void RedundantCallsAreSilent ()
	{
		_portB.SetPinValue (2, true);
		Cpu.Mmio.Data[PINB] = 0x77; // a recompute would overwrite this
		_portB.SetPinValue (2, true);
		_portB.SetInputs (0x04, 0x04);
		_portB.ReleasePin (3);
		Assert.That (Cpu.Mmio.Data[PINB], Is.EqualTo (0x77));

		_portB.SetPinValue (2, false);
		Assert.That (Cpu.Mmio.Data[PINB], Is.EqualTo (0x00));
	}

	[Test (Description = "OutputEnableMask is DDR, minus open-collector pins that are high")]
	public void OutputEnableMaskFollowsDdr ()
	{
		Cpu.WriteData (DDRB, 0x35);
		Cpu.WriteData (PORTB, 0x15);
		Assert.Multiple (() => {
			Assert.That (_portB.OutputEnableMask, Is.EqualTo (0x35));
			Assert.That (_portB.OutputLevels, Is.EqualTo (0x15));
		});

		_portB.OpenCollector = 0x01;
		Assert.Multiple (() => {
			Assert.That (_portB.OutputEnableMask, Is.EqualTo (0x34));
			Assert.That (_portB.OutputLevels, Is.EqualTo (0x14));
			Assert.That (_portB.GetPinState (0), Is.EqualTo (PinState.InputPullup));
		});
	}

	[Test (Description = "With timer 0 driving OC0A, the masks agree with GetPinState on every count")]
	public void OutputMasksWithTimerOverride ()
	{
		Cpu.WriteData (DDRD, 0x40); // PD6 = OC0A
		Cpu.WriteData (OCR0A, 128);
		Cpu.WriteData (TCCR0A, 0x83);
		Cpu.WriteData (TCCR0B, 1);

		var sawHigh = false;
		var sawLow = false;
		var cycle = Cpu.Cycles;
		for (var i = 0; i < 600; i++) {
			Cpu.Cycles = ++cycle;
			Cpu.Tick ();
			var high = _portD.GetPinState (6) == PinState.High;
			sawHigh |= high;
			sawLow |= !high;
			Assert.Multiple (() => {
				Assert.That (_portD.OutputEnableMask, Is.EqualTo (0x40));
				Assert.That ((_portD.OutputLevels & 0x40) != 0, Is.EqualTo (high));
				Assert.That (_portD.OutputLevels & ~0x40, Is.EqualTo (0));
			});
		}
		Assert.That (sawHigh && sawLow, Is.True);
	}
}
