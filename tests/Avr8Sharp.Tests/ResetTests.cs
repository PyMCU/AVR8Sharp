using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

[TestFixture]
public class Reset : AvrTestBase
{
	const int FREQ_16MHZ = 16_000_000;

	const int DDRB = 0x24;
	const int PORTB = 0x25;
	const int TIMSK0 = 0x6e;
	const int TCCR0B = 0x45;
	const int TCNT0 = 0x46;
	const int CS00 = 1;
	const int UCSR0A = 0xc0;
	const int UCSR0B = 0xc1;
	const int UCSR0C = 0xc2;
	const int UBRR0L = 0xc4;
	const int UDR0 = 0xc6;
	const int TXEN = 8;
	const int RXEN = 16;
	const int UDRE = 0x20;
	const int MCUSR = 0x54;
	const int WDRF = 1 << 3;
	const int EXTRF = 1 << 1;
	const int WDTCSR = 0x60;
	const int WDCE = 1 << 4;
	const int WDE = 1 << 3;

	private AvrIoPort _portB;
	private AvrTimer _timer0;
	private AvrUsart _usart;
	private AvrWatchdog _watchdog;

	protected override void SetupPeripherals()
	{
		_portB = new AvrIoPort(Cpu, AvrIoPort.PortBConfig);
		_timer0 = new AvrTimer(Cpu, AvrTimer.Timer0Config);
		_usart = new AvrUsart(Cpu, AvrUsart.Usart0Config, FREQ_16MHZ);
		_watchdog = new AvrWatchdog(Cpu, AvrWatchdog.WatchdogConfig, Clock);
	}

	private void DirtyPeripherals()
	{
		Cpu.WriteData(DDRB, 0xff);
		Cpu.WriteData(PORTB, 0x20);
		Cpu.WriteData(UCSR0B, TXEN | RXEN);
		Cpu.WriteData(UBRR0L, 103);
		Cpu.WriteData(TIMSK0, 1);
		Cpu.WriteData(TCCR0B, CS00);
		Cpu.WriteData(UDR0, 0x41);
	}

	[Test(Description = "Reset returns the I/O registers to their reset values")]
	public void RegistersReturnToResetValues()
	{
		DirtyPeripherals();

		Cpu.Reset();

		Assert.Multiple(() =>
		{
			Assert.That(Cpu.ReadData(DDRB), Is.EqualTo(0));
			Assert.That(Cpu.ReadData(PORTB), Is.EqualTo(0));
			Assert.That(Cpu.ReadData(TCCR0B), Is.EqualTo(0));
			Assert.That(Cpu.ReadData(TIMSK0), Is.EqualTo(0));
			Assert.That(Cpu.ReadData(UCSR0A), Is.EqualTo(UDRE));
			Assert.That(Cpu.ReadData(UCSR0B), Is.EqualTo(0));
			Assert.That(Cpu.ReadData(UCSR0C), Is.EqualTo(0x06));
			Assert.That(Cpu.ReadData(UBRR0L), Is.EqualTo(0));
			Assert.That(_portB.GetPinState(5), Is.EqualTo(PinState.Input));
			Assert.That(Cpu.Sp, Is.EqualTo(Cpu.Mmio.Data.Length - 1));
		});
	}

	[Test(Description = "Reset keeps SRAM and the general registers")]
	public void SramAndRegistersSurvive()
	{
		Cpu.WriteData(0x300, 0x5a);
		Cpu.WriteData(R20, 0x77);

		Cpu.Reset();

		Assert.That(Cpu.ReadData(0x300), Is.EqualTo(0x5a));
		Assert.That(Cpu.ReadData(R20), Is.EqualTo(0x77));
	}

	[Test(Description = "Reset notifies port listeners that the pins went back to input")]
	public void PortListenerFiresOnReset()
	{
		Cpu.WriteData(DDRB, 0xff);
		Cpu.WriteData(PORTB, 0x20);
		var calls = new List<(byte Value, byte Old)>();
		_portB.AddListener((value, old) => calls.Add((value, old)));

		Cpu.Reset();

		Assert.That(calls, Has.Count.EqualTo(1));
		Assert.That(calls[0], Is.EqualTo(((byte)0, (byte)0x20)));
	}

	[Test(Description = "A timer stops counting after a reset")]
	public void TimerStopsCounting()
	{
		Cpu.WriteData(TCCR0B, CS00);
		Cpu.Cycles = 1;
		Cpu.Tick();
		Cpu.Cycles = 10;
		Cpu.Tick();
		Assert.That(Cpu.ReadData(TCNT0), Is.GreaterThan(0));

		Cpu.Reset();
		Cpu.Cycles += 100;
		Cpu.Tick();
		Cpu.Cycles += 100;
		Cpu.Tick();

		Assert.That(Cpu.ReadData(TCNT0), Is.EqualTo(0));
	}

	[Test(Description = "Serial transmission works again after a reset with TXEN left set by the old firmware")]
	public void UsartTransmitsAfterReset()
	{
		DirtyPeripherals();
		Cpu.Reset();

		var sent = new List<byte>();
		_usart.OnByteTransmit = b => sent.Add(b);

		Cpu.WriteData(UCSR0B, TXEN);
		Assert.That(Cpu.ReadData(UCSR0A) & UDRE, Is.EqualTo(UDRE));
		Cpu.WriteData(UDR0, 0x42);

		Assert.That(sent, Is.EqualTo(new byte[] { 0x42 }));

		// The transmit-complete event brings UDRE back
		Cpu.Cycles += (ulong)_usart.CyclesPerChar + 1;
		Cpu.Tick();
		Assert.That(Cpu.ReadData(UCSR0A) & UDRE, Is.EqualTo(UDRE));
	}

	[Test(Description = "A manual reset sets EXTRF and keeps the other MCUSR flags")]
	public void ManualResetSetsExtrf()
	{
		Cpu.Reset();

		Assert.That(Cpu.ReadData(MCUSR) & EXTRF, Is.EqualTo(EXTRF));
		Assert.That(Cpu.ReadData(MCUSR) & 1, Is.EqualTo(1), "PORF survives the reset");
	}

	[Test(Description = "A watchdog reset sets WDRF, not EXTRF, and leaves the watchdog disabled")]
	public void WatchdogResetKeepsMcusrAndClearsWatchdog()
	{
		Cpu.WriteData(MCUSR, 0);
		Cpu.WriteData(WDTCSR, WDCE | WDE);
		Cpu.WriteData(WDTCSR, WDE);
		DirtyPeripherals();

		Cpu.Cycles += 16000 * 20;
		Cpu.Tick();

		Assert.Multiple(() =>
		{
			Assert.That(Cpu.ReadData(MCUSR) & WDRF, Is.EqualTo(WDRF));
			Assert.That(Cpu.ReadData(MCUSR) & EXTRF, Is.EqualTo(0));
			Assert.That(Cpu.ReadData(DDRB), Is.EqualTo(0));
			Assert.That(_watchdog.Enabled, Is.False);
		});
	}

	[Test(Description = "A Mega built like CircuitMirror clears the extended I/O (PORTH) on reset")]
	public void MegaExtendedIoIsCleared()
	{
		var runner = AVR8Sharp.Core.AvrBuilder.Create(0x40000, 0x2100)
			.AddGpioPort(AvrIoPort.PortHConfig, out var portH)
			.Build();
		runner.Cpu.WriteData(AvrIoPort.PortHConfig.DDR, 0xff);
		runner.Cpu.WriteData(AvrIoPort.PortHConfig.PORT, 0x10);

		runner.Cpu.Reset();

		Assert.Multiple(() =>
		{
			Assert.That(runner.Cpu.RamStart, Is.EqualTo(0x200));
			Assert.That(runner.Cpu.ReadData(AvrIoPort.PortHConfig.DDR), Is.EqualTo(0));
			Assert.That(runner.Cpu.ReadData(AvrIoPort.PortHConfig.PORT), Is.EqualTo(0));
			Assert.That(portH.GetPinState(4), Is.EqualTo(PinState.Input));
		});
	}
}
