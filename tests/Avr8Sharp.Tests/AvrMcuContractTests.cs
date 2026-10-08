using System.Text;
using AVR8Sharp.Core;
using AVR8Sharp.Core.Abstractions;
using AVR8Sharp.Core.Peripherals;
using AVR8Sharp.Core.Utils;
using SiliconTwin.Abstractions;

namespace Avr8Sharp.Tests;

/// <summary>Drives an Uno-like machine only through the SiliconTwin interfaces.</summary>
[TestFixture]
public class AvrMcuContract
{
	const int MCUSR = 0x54;
	const int CLKPR = 0x61;
	const int UCSR0B = 0xc1;
	const int UDR0 = 0xc6;
	const int UCSR0A = 0xc0;
	const int SPCR = 0x4c;
	const int SPDR = 0x4e;
	const int TWBR = 0xb8;
	const int TWSR = 0xb9;
	const int TWDR = 0xbb;
	const int TWCR = 0xbc;
	const int ADMUX = 0x7c;
	const int ADCSRA = 0x7a;
	const int ADCL = 0x78;
	const int ADCH = 0x79;
	const int TCCR0A = 0x44;
	const int TCCR0B = 0x45;
	const int OCR0A = 0x47;

	// sbi DDRB,0 ; loop: sbi PORTB,0 ; cbi PORTB,0 ; rjmp loop
	const string Blink = "sbi 0x04, 0\nloop:\nsbi 0x05, 0\ncbi 0x05, 0\nrjmp loop\n";

	AvrMcu _mcu = null!;
	IMcu Mcu => _mcu;
	AVR8Sharp.Core.Cpu Cpu => _mcu.Cpu;
	IPinBank PortB => ((IGpio)_mcu).Banks[0];
	IPinBank PortD => ((IGpio)_mcu).Banks[2];

	[SetUp]
	public void SetUp () => _mcu = BuildUno ();

	[TearDown]
	public void TearDown () => _mcu.Dispose ();

	static AvrMcu BuildUno ()
	{
		var builder = AvrBuilder.Create (0x8000, 2048)
			.AddGpioPort (AvrIoPort.PortBConfig, out _)
			.AddGpioPort (AvrIoPort.PortCConfig, out _)
			.AddGpioPort (AvrIoPort.PortDConfig, out _)
			.AddTimer (AvrTimer.Timer0Config, out _)
			.AddUsart (AvrUsart.Usart0Config, out _)
			.AddSpi (AvrSpi.SpiConfig, out _)
			.AddTwi (AvrTwi.TwiConfig, out _)
			.AddAdc (AvrAdc.AdcConfig, out _)
			.AddClock (AvrClock.ClockConfig, out var clock)
			.AddWatchdog (AvrWatchdog.WatchdogConfig, clock, out _);
		return builder.BuildMcu ("atmega328p");
	}

	static byte[] Assemble (string source)
	{
		var assembler = new AvrAssembler ();
		var bytes = assembler.Assemble (source);
		Assert.That (assembler.Errors, Is.Empty);
		return bytes;
	}

	static string ToHex (byte[] data)
	{
		var sb = new StringBuilder ();
		for (var i = 0; i < data.Length; i += 16) {
			var n = Math.Min (16, data.Length - i);
			var sum = n + (i >> 8) + (i & 0xff);
			sb.Append ($":{n:X2}{i:X4}00");
			for (var j = 0; j < n; j++) { sb.Append (data[i + j].ToString ("X2")); sum += data[i + j]; }
			sb.Append ($"{(byte)-sum:X2}\n");
		}
		sb.Append (":00000001FF\n");
		return sb.ToString ();
	}

	void LoadBlink () => Mcu.Load (FirmwareImage.FromBin (Assemble (Blink)));

	[Test]
	public void Identity ()
	{
		Assert.Multiple (() => {
			Assert.That (Mcu.Name, Is.EqualTo ("atmega328p"));
			Assert.That (Mcu.ClockHz, Is.EqualTo (16_000_000u));
			Assert.That (Mcu.TryGet<IGpio> (out _), Is.True);
			Assert.That (Mcu.TryGet<IPwmSource> (out _), Is.True);
			Assert.That (Mcu.TryGet<IAdcInput> (out _), Is.True);
			Assert.That (Mcu.TryGet<IPeripheralMap> (out _), Is.True);
			Assert.That (((IGpio)_mcu).Banks.Select (b => b.Name), Is.EqualTo (new[] { "PORTB", "PORTC", "PORTD" }));
			Assert.That (PortB.PinCount, Is.EqualTo (8));
		});
	}

	[Test (Description = "Repeated small budgets do not drift")]
	public void RunBudgetDoesNotDrift ()
	{
		LoadBlink ();
		long consumed = 0;
		for (var i = 0; i < 1000; i++) consumed += Mcu.Run (15); // odd budget vs 2-cycle instructions
		Assert.Multiple (() => {
			Assert.That (Mcu.Cycles, Is.EqualTo (15000).Within (1));
			Assert.That (consumed, Is.EqualTo (Mcu.Cycles));
			Assert.That (Mcu.Run (0), Is.EqualTo (0));
			Assert.That (Mcu.Run (-5), Is.EqualTo (0));
		});
	}

	[Test]
	public void RunUntilReachesTheAbsoluteCycle ()
	{
		LoadBlink ();
		var used = Mcu.RunUntil (1000);
		Assert.That (Mcu.Cycles, Is.EqualTo (1000).Within (1));
		Assert.That (used, Is.EqualTo (Mcu.Cycles));
		Assert.That (Mcu.RunUntil (500), Is.EqualTo (0));
	}

	[Test]
	public void SoftAndPowerReset ()
	{
		LoadBlink ();
		Mcu.Run (100);
		Cpu.WriteData (0x200, 0x5a);
		Mcu.Reset (ResetKind.Soft);
		Assert.Multiple (() => {
			Assert.That (Cpu.Pc, Is.EqualTo (0));
			Assert.That (Mcu.Cycles, Is.GreaterThanOrEqualTo (100), "soft reset keeps counting");
			Assert.That (Cpu.Mmio.Data[0x200], Is.EqualTo (0x5a), "SRAM is kept");
		});

		Mcu.Run (100);
		Mcu.Reset (ResetKind.Power);
		Assert.Multiple (() => {
			Assert.That (Mcu.Cycles, Is.EqualTo (0));
			Assert.That (Cpu.Pc, Is.EqualTo (0));
			Assert.That (Cpu.Mmio.Data[MCUSR] & 1, Is.EqualTo (1), "PORF");
			Assert.That (Cpu.Mmio.Data[0x200], Is.EqualTo (0));
			Assert.That (Cpu.ProgBytes[0], Is.Not.EqualTo (0), "flash is kept");
		});
		Mcu.Run (50);
		Assert.That (Mcu.Cycles, Is.EqualTo (50).Within (1));
	}

	[Test]
	public void LoadIntelHexAndBin ()
	{
		var program = Assemble (Blink);
		Mcu.Load (FirmwareImage.FromIntelHex (ToHex (program)));
		Assert.That (Cpu.ProgBytes.AsSpan (0, program.Length).ToArray (), Is.EqualTo (program));

		Mcu.Load (FirmwareImage.FromBin (new byte[] { 0xaa, 0xbb }, 0x100));
		Assert.Multiple (() => {
			Assert.That (Cpu.ProgBytes[0x100], Is.EqualTo (0xaa));
			Assert.That (Cpu.ProgBytes[0x101], Is.EqualTo (0xbb));
			Assert.That (Cpu.ProgBytes[0], Is.EqualTo (program[0]), "bytes outside the image are kept");
		});

		Assert.Throws<FormatException> (() => Mcu.Load (FirmwareImage.FromIntelHex (":10000000ZZ\n")));
		Assert.Throws<NotSupportedException> (() => Mcu.Load (FirmwareImage.FromUf2 (new byte[4])));
		Assert.Throws<NotSupportedException> (() => Mcu.Load (FirmwareImage.FromElf (new byte[4])));
		Assert.Throws<ArgumentOutOfRangeException> (() => Mcu.Load (FirmwareImage.FromBin (new byte[4], 0x7ffe)));
	}

	[Test (Description = "PinChange.Cycle equals the cycle counter at the write")]
	public void PinChangedCarriesTheCycle ()
	{
		LoadBlink ();
		Mcu.Run (40); // set DDRB, settle
		Mcu.Reset (ResetKind.Power); // cycles restart: the base offset must be applied
		var seen = new List<(PinChange change, long now)> ();
		PortB.PinChanged += (in PinChange e) => seen.Add ((e, Mcu.Cycles));

		Mcu.Run (60);
		Assert.That (seen.Count, Is.GreaterThanOrEqualTo (8));
		Assert.Multiple (() => {
			foreach (var (change, now) in seen) {
				Assert.That (change.Cycle, Is.EqualTo (now));
				Assert.That (change.Changed, Is.EqualTo (1UL));
			}
			Assert.That (seen.Select (s => s.change.IsHigh (0)), Is.EqualTo (Enumerable.Range (0, seen.Count).Select (i => i % 2 == 0)).Or.EqualTo (Enumerable.Range (0, seen.Count).Select (i => i % 2 == 1)));
			Assert.That (seen.Zip (seen.Skip (1), (a, b) => b.change.Cycle - a.change.Cycle), Is.All.AnyOf (2L, 4L), "sbi to cbi is 2 cycles, cbi to sbi is 4 (cbi + rjmp)");
			Assert.That (seen[0].change.IsDriven (0), Is.True);
		});
	}

	[Test]
	public void WatchMaskFiltersPins ()
	{
		var count = 0;
		PinChangeHandler handler = (in PinChange e) => count++;
		PortB.PinChanged += handler;
		Assert.That (PortB.WatchMask, Is.EqualTo (0xffUL));

		Cpu.WriteData (0x24, 0xff);
		count = 0;
		PortB.WatchMask = 1UL << 3;
		Cpu.WriteData (0x25, 0x01);
		Cpu.WriteData (0x25, 0x02);
		Assert.That (count, Is.EqualTo (0), "pins outside the mask are silent");
		Cpu.WriteData (0x25, 0x08);
		Assert.That (count, Is.EqualTo (1));
		PortB.PinChanged -= handler;
		Cpu.WriteData (0x25, 0x00);
		Assert.That (count, Is.EqualTo (1));
	}

	[Test]
	public void SetInputsReleaseInputsAndPullUps ()
	{
		Cpu.WriteData (0x25, 0x06); // PORTB: pull-ups on PB1, PB2
		Assert.That (PortB.PullUpMask, Is.EqualTo (0x06UL));
		Assert.That (PortB.PullDownMask, Is.EqualTo (0UL));
		Assert.That (Cpu.Mmio.Data[0x23], Is.EqualTo (0x06));

		PortB.SetInputs (0x03, 0x01);
		Assert.That (Cpu.Mmio.Data[0x23], Is.EqualTo (0x05), "PB0 high, PB1 driven low, PB2 pulled up");
		PortB.SetInputs (0x80, 0x80); // PB0, PB1 untouched
		Assert.That (Cpu.Mmio.Data[0x23], Is.EqualTo (0x85));
		PortB.ReleaseInputs (0x83);
		Assert.That (Cpu.Mmio.Data[0x23], Is.EqualTo (0x06));

		Cpu.WriteData (0x24, 0x06); // PB1, PB2 become outputs: no pull-up, driven
		Assert.That (PortB.PullUpMask, Is.EqualTo (0UL));
		Assert.That (PortB.OutputEnableMask, Is.EqualTo (0x06UL));
		Assert.That (PortB.OutputLevels & PortB.OutputEnableMask, Is.EqualTo (0x06UL));
	}

	[Test]
	public void AdcPullsThroughTheInterface ()
	{
		var adc = (IAdcInput)_mcu;
		var asked = new List<int> ();
		adc.ReadChannelVolts = ch => { asked.Add (ch); return 2.5; };
		Cpu.WriteData (ADMUX, 0x40 | 2); // AVCC, ADC2
		Assert.That (adc.ReferenceVolts, Is.EqualTo (5.0));
		Cpu.WriteData (ADCSRA, 0x80 | 0x40 | 7);
		Mcu.Run (128 * 26);
		Assert.Multiple (() => {
			Assert.That (asked, Is.EqualTo (new[] { 2 }));
			Assert.That (Cpu.Mmio.Data[ADCL] | (Cpu.Mmio.Data[ADCH] << 8), Is.EqualTo (512));
		});
		Cpu.WriteData (ADMUX, 0xc0 | 2); // internal 1.1 V
		Assert.That (adc.ReferenceVolts, Is.EqualTo (1.1));
	}

	[Test]
	public void UartTransmitAndReceive ()
	{
		var uart = ((IPeripheralMap)_mcu).Uarts.Single ();
		var tx = new List<byte> ();
		uart.TxByte += tx.Add;

		Cpu.WriteData (0xc4, 103); // UBRR0 = 103: 9600 baud at 16 MHz
		Cpu.WriteData (UCSR0B, 0x18); // RXEN | TXEN
		Assert.That (uart.BaudRate, Is.EqualTo (9615u));
		Cpu.WriteData (UDR0, (byte)'A');
		Mcu.Run (20000);
		Assert.That (tx, Is.EqualTo (new[] { (byte)'A' }));

		Assert.That (uart.TryInjectRx (0x42), Is.True);
		Assert.That (uart.TryInjectRx (0x43), Is.False, "receiver busy with the previous byte");
		Mcu.Run (20000);
		Assert.Multiple (() => {
			Assert.That (Cpu.Mmio.Data[UCSR0A] & 0x80, Is.EqualTo (0x80), "RXC");
			Assert.That (Cpu.ReadData (UDR0), Is.EqualTo (0x42));
		});

		Cpu.WriteData (UCSR0B, 0x08); // RX off
		Assert.That (uart.TryInjectRx (1), Is.False);
	}

	[Test]
	public void SpiTransferMapsToTheCallback ()
	{
		var spi = ((IPeripheralMap)_mcu).Spis.Single ();
		var mosi = new List<uint> ();
		Cpu.WriteData (SPCR, 0x50); // SPE | MSTR

		Cpu.WriteData (SPDR, 0x11);
		Mcu.Run (200);
		Assert.That (Cpu.ReadData (SPDR), Is.EqualTo (0xff), "no callback reads all ones");

		spi.Transfer = b => { mosi.Add (b); return b + 1u; };
		Cpu.WriteData (SPDR, 0x22);
		Mcu.Run (200);
		Assert.Multiple (() => {
			Assert.That (mosi, Is.EqualTo (new[] { 0x22u }));
			Assert.That (Cpu.ReadData (SPDR), Is.EqualTo (0x23));
			Assert.That (spi.FrameBits, Is.EqualTo (8));
		});

		spi.Transfer = null;
		Cpu.WriteData (SPDR, 0x33);
		Mcu.Run (200);
		Assert.That (Cpu.ReadData (SPDR), Is.EqualTo (0xff));
	}

	sealed class Eeprom : II2cTarget
	{
		public readonly List<byte> Written = [];
		public int Stops;
		public bool Probe (byte addr7, bool write) => true;
		public void Write (byte data) => Written.Add (data);
		public byte Read () => 0x5a;
		public void Stop () => Stops++;
	}

	int TwiStep (byte twcr)
	{
		Cpu.WriteData (TWCR, twcr);
		Mcu.Run (3000);
		return Cpu.Mmio.Data[TWSR] & 0xf8;
	}

	[Test]
	public void I2cTargetAcksNacksAndMovesData ()
	{
		var i2c = ((IPeripheralMap)_mcu).I2cs.Single ();
		var target = new Eeprom ();
		i2c.AttachTarget (0x50, target);
		Cpu.WriteData (TWBR, 8);

		Assert.That (TwiStep (0xa4), Is.EqualTo (0x08), "START");
		Cpu.WriteData (TWDR, 0x50 << 1);
		Assert.That (TwiStep (0x84), Is.EqualTo (0x18), "SLA+W acked");
		Cpu.WriteData (TWDR, 0x77);
		Assert.That (TwiStep (0x84), Is.EqualTo (0x28), "data acked");
		Assert.That (TwiStep (0x94), Is.EqualTo (0xf8).Or.EqualTo (0x00), "STOP");
		Assert.Multiple (() => {
			Assert.That (target.Written, Is.EqualTo (new byte[] { 0x77 }));
			Assert.That (target.Stops, Is.EqualTo (1));
		});

		Assert.That (TwiStep (0xa4), Is.EqualTo (0x08));
		Cpu.WriteData (TWDR, 0x51 << 1);
		Assert.That (TwiStep (0x84), Is.EqualTo (0x20), "no device at 0x51: SLA+W NACK");
		TwiStep (0x94);

		Assert.That (TwiStep (0xa4), Is.EqualTo (0x08));
		Cpu.WriteData (TWDR, (0x50 << 1) | 1);
		Assert.That (TwiStep (0x84), Is.EqualTo (0x40), "SLA+R acked");
		Assert.That (TwiStep (0xc4), Is.EqualTo (0x50), "byte read with ACK");
		Assert.That (Cpu.Mmio.Data[TWDR], Is.EqualTo (0x5a));
		TwiStep (0x94);

		i2c.DetachTarget (0x50);
		Assert.That (TwiStep (0xa4), Is.EqualTo (0x08));
		Cpu.WriteData (TWDR, 0x50 << 1);
		Assert.That (TwiStep (0x84), Is.EqualTo (0x20), "detached");
	}

	[Test]
	public void I2cSlaveSide ()
	{
		var i2c = ((IPeripheralMap)_mcu).I2cs.Single ();
		var addresses = new List<byte> ();
		i2c.SlaveAddressChanged += addresses.Add;
		Cpu.WriteData (0xba, 0x42 << 1); // TWAR
		Assert.That (i2c.SlaveAddress, Is.EqualTo (0x42));
		Assert.That (i2c.BeginMasterTransfer (0x42, write: true), Is.True);
		Assert.That (i2c.BeginMasterTransfer (0x43, write: true), Is.False);
		Cpu.WriteData (TWCR, 0x44); // TWEA | TWEN
		Assert.That (i2c.MasterWrite (0x99), Is.True);
		Assert.That (Cpu.Mmio.Data[TWDR], Is.EqualTo (0x99));
		i2c.MasterStop ();
		Assert.That (Cpu.Mmio.Data[TWSR] & 0xf8, Is.EqualTo (0xa0));
		Assert.Throws<NotSupportedException> (() => i2c.TryMasterRead (out _));
	}

	[Test]
	public void PwmFromTimer0 ()
	{
		var pwm = (IPwmSource)_mcu;
		Assert.That (pwm.TryGetPwm (2, 6, out var info), Is.True, "PD6 is OC0A");
		Assert.That (info.Enabled, Is.False);
		Assert.That (pwm.TryGetPwm (2, 3, out _), Is.False, "PD3 has no mounted timer");
		Assert.That (pwm.TryGetPwm (0, 6, out _), Is.False);

		Cpu.WriteData (0x2a, 0x60); // DDRD: PD5, PD6
		Cpu.WriteData (OCR0A, 128);
		Cpu.WriteData (TCCR0A, 0x83); // COM0A1, fast PWM
		Cpu.WriteData (TCCR0B, 0x01);
		Assert.That (pwm.TryGetPwm (2, 6, out info), Is.True);
		Assert.Multiple (() => {
			Assert.That (info.Enabled, Is.True);
			Assert.That (info.FrequencyHz, Is.EqualTo (16_000_000.0 / 256).Within (1e-6));
			Assert.That (info.Duty, Is.EqualTo(129.0 / 256).Within (1e-9));
		});

		Cpu.WriteData (TCCR0A, 0xc3); // inverting
		pwm.TryGetPwm (2, 6, out info);
		Assert.That (info.Duty, Is.EqualTo (1 - 129.0 / 256).Within (1e-9));

		Cpu.WriteData (TCCR0A, 0x81); // phase correct
		pwm.TryGetPwm (2, 6, out info);
		Assert.Multiple (() => {
			Assert.That (info.FrequencyHz, Is.EqualTo (16_000_000.0 / 510).Within (1e-6));
			Assert.That (info.Duty, Is.EqualTo (128.0 / 255).Within (1e-9));
		});

		Cpu.WriteData (TCCR0B, 0x00); // clock stopped
		pwm.TryGetPwm (2, 6, out info);
		Assert.That (info.Enabled, Is.False);
	}

	[Test]
	public void ClockChangedFollowsClkpr ()
	{
		var raised = 0;
		Mcu.ClockChanged += () => raised++;
		Cpu.WriteData (CLKPR, 0x80);
		Cpu.WriteData (CLKPR, 0x01); // divide by 2
		Assert.Multiple (() => {
			Assert.That (Mcu.ClockHz, Is.EqualTo (8_000_000u));
			Assert.That (raised, Is.EqualTo (1));
		});
	}

	[Test]
	public void CycleBudgetHelperWorksWithRun ()
	{
		LoadBlink ();
		double carry = 0;
		for (var i = 0; i < 100; i++) {
			var budget = CycleBudget.Next (ref carry, Mcu.ClockHz, 0.000001);
			CycleBudget.Settle (ref carry, budget, Mcu.Run (budget));
		}
		Assert.That (Mcu.Cycles, Is.EqualTo (1600).Within (2));
	}
}
