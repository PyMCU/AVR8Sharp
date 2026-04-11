using System.Text;
using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

[TestFixture]
public class Usart : AvrTestBase
{
	const int FREQ_16MHZ = 16_000_000;
	const int FREQ_11_0529MHZ = 11059200;

	// USART0 Registers
	const int UCSR0A = 0xc0;
	const int UCSR0B = 0xc1;
	const int UCSR0C = 0xc2;
	const int UBRR0L = 0xc4;
	const int UBRR0H = 0xc5;
	const int UDR0 = 0xc6;

	// Register bit names
	const int U2X0 = 2;
	const int TXEN = 8;
	const int RXEN = 16;
	const int UDRIE = 0x20;
	const int TXCIE = 0x40;
	const int RXC = 0x80;
	const int TXC = 0x40;
	const int UDRE = 0x20;
	const int USBS = 0x08;
	const int UPM0 = 0x10;
	const int UPM1 = 0x20;

	// Interrupt address
	const int PC_INT_UDRE = 0x26;
	const int PC_INT_TXC = 0x28;
	const int UCSZ0 = 2;
	const int UCSZ1 = 4;
	const int UCSZ2 = 4;

	private AvrUsart _usart;

	protected override void SetupPeripherals()
	{
		_usart = new AvrUsart(Cpu, AvrUsart.Usart0Config, FREQ_16MHZ);
	}

	[Test(Description = "Should correctly calculate the baudRate from UBRR")]
	public void BaudRateCalculation()
	{
		_usart = new AvrUsart(Cpu,  AvrUsart.Usart0Config, FREQ_11_0529MHZ);

		Cpu.WriteData (UBRR0H, 0);
		Cpu.WriteData (UBRR0L, 5);
		
		Assert.That (_usart.BaudRate, Is.EqualTo(115_200));
	}
	
	[Test(Description = "Should correctly calculate the baudRate from UBRR in double-speed mode")]
	public void BaudRateCalculationDoubleSpeed()
	{
		Cpu.WriteData (UBRR0H, 3);
		Cpu.WriteData (UBRR0L, 64);
		Cpu.WriteData (UCSR0A, U2X0);
		
		Assert.That (_usart.BaudRate, Is.EqualTo(2_400));
	}
	
	[Test(Description = "Should call onConfigurationChange when the baudRate changes")]
	public void ConfigurationChange()
	{
		var configChangeCount = 0;
		
		_usart.OnConfigurationChange += () => configChangeCount++;
		
		Cpu.WriteData (UBRR0H, 0);
		Assert.That (configChangeCount, Is.EqualTo(1));
		
		Cpu.WriteData (UBRR0L, 5);
		Assert.That (configChangeCount, Is.EqualTo(2));
		
		Cpu.WriteData (UCSR0A, U2X0);
		Assert.That (configChangeCount, Is.EqualTo(3));
	}
	
	[Test(Description = "Should invoke onByteTransmit when UDR0 is written to")]
	public void TransmitByte()
	{
		var byteTransmitCount = 0;
		
		_usart.OnByteTransmit += (b) => byteTransmitCount = b;
		
		Cpu.WriteData (UCSR0B, TXEN);
		Cpu.WriteData (UDR0, 0x61);
		
		Assert.That (byteTransmitCount, Is.EqualTo(0x61));
	}

	[TestFixture]
	public class BitsPerChar : AvrTestBase
	{
		private AvrUsart _usart;

		protected override void SetupPeripherals()
		{
			_usart = new AvrUsart(Cpu, AvrUsart.Usart0Config, FREQ_16MHZ);
		}

		[Test(Description = "Should return 5-bits per byte when UCSZ = 0")]
		public void FiveBitsPerChar()
		{
			Cpu.WriteData (UCSR0C, 0);
			
			Assert.That (_usart.BitsPerChar, Is.EqualTo(5));
		}
		
		[Test(Description = "Should return 6-bits per byte when UCSZ = 1")]
		public void SixBitsPerChar()
		{
			Cpu.WriteData (UCSR0C, UCSZ0);
			
			Assert.That (_usart.BitsPerChar, Is.EqualTo(6));
		}
		
		[Test(Description = "Should return 7-bits per byte when UCSZ = 2")]
		public void SevenBitsPerChar()
		{
			Cpu.WriteData (UCSR0C, UCSZ1);
			
			Assert.That (_usart.BitsPerChar, Is.EqualTo(7));
		}
		
		[Test(Description = "Should return 8-bits per byte when UCSZ = 3")]
		public void EightBitsPerChar()
		{
			Cpu.WriteData (UCSR0C, UCSZ0 | UCSZ1);
			
			Assert.That (_usart.BitsPerChar, Is.EqualTo(8));
		}
		
		[Test(Description = "Should return 9-bits per byte when UCSZ = 7")]
		public void NineBitsPerChar()
		{
			Cpu.WriteData (UCSR0C, UCSZ0 | UCSZ1);
			Cpu.WriteData (UCSR0B, UCSZ2);
			
			Assert.That (_usart.BitsPerChar, Is.EqualTo(9));
		}
		
		[Test(Description = "Should call onConfigurationChange when bitsPerChar change")]
		public void ConfigurationChange()
		{
			var configChangeCount = 0;
			
			_usart.OnConfigurationChange += () => configChangeCount++;
			
			Cpu.WriteData (UCSR0C, UCSZ0 | UCSZ1);
			Assert.That (configChangeCount, Is.EqualTo(1));
			
			Cpu.WriteData (UCSR0B, UCSZ2);
			Assert.That (configChangeCount, Is.EqualTo(2));
			
			Cpu.WriteData (UCSR0B, UCSZ2);
			Assert.That (configChangeCount, Is.EqualTo(2));
		}
	}

	[TestFixture]
	public class StopBits : AvrTestBase
	{
		private AvrUsart _usart;

		protected override void SetupPeripherals()
		{
			_usart = new AvrUsart(Cpu, AvrUsart.Usart0Config, FREQ_16MHZ);
		}

		[Test(Description = "Should return 1 when USBS = 0")]
		public void OneStopBit()
		{
			Assert.That (_usart.StopBits, Is.EqualTo(1));
		}
		
		[Test(Description = "Should return 2 when USBS = 1")]
		public void TwoStopBits()
		{
			Cpu.WriteData (UCSR0C, USBS);
			
			Assert.That (_usart.StopBits, Is.EqualTo(2));
		}
	}

	[TestFixture]
	public class Parity : AvrTestBase
	{
		private AvrUsart _usart;

		protected override void SetupPeripherals()
		{
			_usart = new AvrUsart(Cpu, AvrUsart.Usart0Config, FREQ_16MHZ);
		}

		[Test(Description = "Should return false when UPM1 = 0")]
		public void ParityDisabled()
		{
			Assert.That (_usart.ParityEnabled, Is.False);
		}
		
		[Test(Description = "Should return true when UPM1 = 1")]
		public void ParityEnabled()
		{
			Cpu.WriteData (UCSR0C, UPM1);
			
			Assert.That (_usart.ParityEnabled, Is.True);
		}
		
		[Test(Description = "Should return false when UPM0 = 0")]
		public void ParityEven()
		{
			Assert.That (_usart.ParityOdd, Is.False);
		}
		
		[Test(Description = "Should return true when UPM0 = 1")]
		public void ParityOdd()
		{
			Cpu.WriteData (UCSR0C, UPM0);
			
			Assert.That (_usart.ParityOdd, Is.True);
		}
	}

	[TestFixture]
	public class TxEnableRxEnable : AvrTestBase
	{
		private AvrUsart _usart;

		protected override void SetupPeripherals()
		{
			_usart = new AvrUsart(Cpu, AvrUsart.Usart0Config, FREQ_16MHZ);
		}

		[Test(Description = "TxEnable should equal true when the transitter is enabled")]
		public void TxEnable()
		{
			Assert.That (_usart.TxEnable, Is.False);
			
			Cpu.WriteData (UCSR0B, TXEN);
			
			Assert.That (_usart.TxEnable, Is.True);
		}
		
		[Test(Description = "RxEnable should equal true when the transitter is enabled")]
		public void RxEnable()
		{
			Assert.That (_usart.RxEnable, Is.False);
			
			Cpu.WriteData (UCSR0B, RXEN);
			
			Assert.That (_usart.RxEnable, Is.True);
		}
	}
	
	[TestFixture]
	public class Tick : AvrTestBase
	{
		private AvrUsart _usart;

		protected override void SetupPeripherals()
		{
			_usart = new AvrUsart(Cpu, AvrUsart.Usart0Config, FREQ_16MHZ);
		}

		[Test(Description = "Should trigger data register empty interrupt if UDRE is set")]
		public void DataRegisterEmptyInterrupt()
		{
			Cpu.WriteData (UCSR0B, UDRIE | TXEN);
			Cpu.Mmio.Data[SREG] = 0x80; // SREG: I-------
			Cpu.Tick();
            Assert.Multiple(() =>
            {
                Assert.That(Cpu.Pc, Is.EqualTo(PC_INT_UDRE));
                Assert.That(Cpu.Cycles, Is.EqualTo(3)); // 3 cycles from DoAvrInterrupt (4 total incl. instruction)
                Assert.That((Cpu.Mmio.Data[UCSR0A] & UDRE), Is.EqualTo(0));
            });
        }

		[Test (Description = "Should trigger data TX Complete interrupt if TXCIE is set")]
		public void TxCompleteInterrupt ()
		{
			Cpu.WriteData (UCSR0B, TXCIE | TXEN);
			Cpu.WriteData (UDR0, 0x61);
			Cpu.Mmio.Data[SREG] = 0x80; // SREG: I-------
			Cpu.Cycles = 1_000_000;
			Cpu.Tick();
            Assert.Multiple(() =>
            {
                Assert.That(Cpu.Pc, Is.EqualTo(PC_INT_TXC));
                Assert.That(Cpu.Cycles, Is.EqualTo(1_000_000 + 3)); // 3 cycles from DoAvrInterrupt
                Assert.That((Cpu.Mmio.Data[UCSR0A] & TXC), Is.EqualTo(0));
            });
        }

		[Test (Description = "Should not trigger data TX Complete interrupt if UDR was not written to")]
		public void TxCompleteInterruptNotTriggered ()
		{
			Cpu.WriteData (UCSR0B, TXCIE | TXEN);
			Cpu.Mmio.Data[SREG] = 0x80; // SREG: I-------
			Cpu.Tick();
			Assert.Multiple(() =>
			{
				Assert.That(Cpu.Pc, Is.EqualTo(0));
				Assert.That(Cpu.Cycles, Is.EqualTo(0));
			});
		}
		
		[Test(Description = "Should not trigger any interrupt if interrupts are disabled")]
		public void InterruptsDisabled()
		{
			Cpu.WriteData (UCSR0B, UDRIE | TXEN);
			Cpu.WriteData (UDR0, 0x61);
			Cpu.Mmio.Data[SREG] = 0; // SREG: 0 (disable interrupts)
			Cpu.Cycles = 1_000_000;
			Cpu.Tick();
			Assert.Multiple(() =>
			{
				Assert.That(Cpu.Pc, Is.EqualTo(0));
				Assert.That(Cpu.Cycles, Is.EqualTo(1_000_000));
				Assert.That(Cpu.Mmio.Data[UCSR0A], Is.EqualTo(TXC | UDRE));
			});
		}
	}

	[TestFixture]
	public class OnLineTransmit : AvrTestBase
	{
		private AvrUsart _usart;

		protected override void SetupPeripherals()
		{
			_usart = new AvrUsart(Cpu, AvrUsart.Usart0Config, FREQ_16MHZ);
		}

		[Test(Description = "Should call onLineTransmit with the current line buffer after every newline")]
		public void LineTransmit()
		{
			var builder = new StringBuilder();
			
			_usart.OnLineTransmit += (line) => builder.Append(line);
			
			Cpu.WriteData (UCSR0B, TXEN);
			Cpu.WriteData (UDR0, 0x48); // 'H'
			Cpu.WriteData (UDR0, 0x65); // 'e'
			Cpu.WriteData (UDR0, 0x6c); // 'l'
			Cpu.WriteData (UDR0, 0x6c); // 'l'
			Cpu.WriteData (UDR0, 0x6f); // 'o'
			Cpu.WriteData (UDR0, 0xa); // '\n'
			
			Assert.That (builder.ToString(), Is.EqualTo("Hello"));
		}
		
		[Test(Description = "Should not call onLineTransmit if no newline was received")]
		public void LineTransmitNoNewline()
		{
			var builder = new StringBuilder();
			
			_usart.OnLineTransmit += (line) => builder.Append(line);
			
			Cpu.WriteData (UCSR0B, TXEN);
			Cpu.WriteData (UDR0, 0x48); // 'H'
			Cpu.WriteData (UDR0, 0x69); // 'i'
			
			Assert.That (builder.ToString(), Is.EqualTo(""));
		}
		
		[Test(Description = "Should clear the line buffer after each call to onLineTransmit")]
		public void LineBufferClear()
		{
			string lineToTransmit = "";
			
			_usart.OnLineTransmit += (line) => lineToTransmit = line;
			
			Cpu.WriteData (UCSR0B, TXEN);
			Cpu.WriteData (UDR0, 0x48); // 'H'
			Cpu.WriteData (UDR0, 0x69); // 'i'
			Cpu.WriteData (UDR0, 0xa); // '\n'
			Cpu.WriteData (UDR0, 0x74); // 't'
			Cpu.WriteData (UDR0, 0x68); // 'h'
			Cpu.WriteData (UDR0, 0x65); // 'e'
			Cpu.WriteData (UDR0, 0x72); // 'r'
			Cpu.WriteData (UDR0, 0x65); // 'e'
			Cpu.WriteData (UDR0, 0xa); // '\n'
			
			Assert.That (lineToTransmit, Is.EqualTo("there"));
		}
	}

	[TestFixture]
	public class WriteByte : AvrTestBase
	{
		[Test(Description = "Should return false if called when RX is busy")]
		public void RxBusy()
		{
			var usart = new AvrUsart(Cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			Cpu.WriteData (UCSR0B, RXEN);
			Cpu.WriteData (UBRR0L, 103); // baud: 9600
			
			Assert.That (usart.WriteByte(10), Is.True);
			Assert.That (usart.WriteByte(10), Is.False);
			
			Cpu.Tick();
			
			Assert.That (usart.WriteByte(10), Is.False);
		}
	}

	[TestFixture]
	public class Integration : AvrTestBase
	{
		private AvrUsart _usart;

		protected override void SetupPeripherals()
		{
			_usart = new AvrUsart(Cpu, AvrUsart.Usart0Config, FREQ_16MHZ);
		}

		[Test(Description = "Should set the TXC bit after ~1.04mS when baud rate set to 9600")]
		public void TxComplete()
		{
			Cpu.WriteData (UCSR0B, TXEN);
			Cpu.WriteData (UBRR0L, 103); // baud: 9600
			Cpu.WriteData (UDR0, 0x48); // 'H'
			Cpu.Cycles += 16_000; // 1ms
			Cpu.Tick();
			Assert.That ((Cpu.Mmio.Data[UCSR0A] & TXC), Is.EqualTo(0));
			Cpu.Cycles += 800; // 0.05ms
			Cpu.Tick();
			Assert.That ((Cpu.Mmio.Data[UCSR0A] & TXC), Is.EqualTo(TXC));
		}
		
		[Test(Description = "Should be ready to recieve the next byte after ~1.04ms when baudrate set to 9600")]
		public void ReadyToReceive()
		{
			var rxCompleteCallback = 0;
			_usart.OnRxComplete += () => rxCompleteCallback++;
			
			Cpu.WriteData (UCSR0B, RXEN);
			Cpu.WriteData (UBRR0L, 103); // baud: 9600
			Assert.That (_usart.WriteByte(0x42), Is.True);
			Cpu.Cycles += 16_000; // 1ms
			Cpu.Tick();
            Assert.Multiple(() =>
            {
                Assert.That((Cpu.Mmio.Data[UCSR0A] & RXC), Is.EqualTo(0)); // byte not received yet
                Assert.That(_usart.RxBusy, Is.True);
                Assert.That(rxCompleteCallback, Is.EqualTo(0));
            });
            Cpu.Cycles += 800; // 0.05ms
			Cpu.Tick();
            Assert.Multiple(() =>
            {
                Assert.That((Cpu.Mmio.Data[UCSR0A] & RXC), Is.EqualTo(RXC));
                Assert.That(_usart.RxBusy, Is.False);
                Assert.That(rxCompleteCallback, Is.EqualTo(1));
                Assert.That(Cpu.ReadData(UDR0), Is.EqualTo(0x42));
                Assert.That (Cpu.ReadData(UDR0), Is.EqualTo(0));
            });
		}
	}

	[TestFixture]
	public class NineBitFrame : AvrTestBase
	{
		const int FREQ_16MHZ = 16_000_000;
		const int UCSR0B = 0xc1;
		const int UCSR0C = 0xc2;
		const int UCSZ0  = 2;
		const int UCSZ1  = 4;
		const int UCSZ2  = 4; // in UCSR0B
		const int RXEN   = 16;

		[Test (Description = "RxMasks[9] must be 0x1ff so the 9th data bit is not silently discarded")]
		public void RxMask_NineBit_Is_0x1ff ()
		{
			Assert.That(AvrUsart.RxMasks[9], Is.EqualTo(0x1ff),
				"9-bit RX mask must be 0x1ff so bit 8 survives; was incorrectly 0xff before fix");
		}

		[Test (Description = "BitsPerChar returns 9 when UCSZ2:0 = 7")]
		public void BitsPerChar_Nine ()
		{
			var usart = new AvrUsart (Cpu, AvrUsart.Usart0Config, FREQ_16MHZ);

			Cpu.WriteData (UCSR0C, UCSZ0 | UCSZ1);
			Cpu.WriteData (UCSR0B, UCSZ2);

			Assert.That (usart.BitsPerChar, Is.EqualTo (9));
		}
	}

	[TestFixture]
	public class Usart3 : AvrTestBase
	{
		// ATmega2560 USART3 register addresses (extended I/O, > 0xFF)
		const ushort UCSR3A = 0x130;
		const ushort UCSR3B = 0x131;
		const ushort UCSR3C = 0x132;
		const ushort UBRR3L = 0x134;
		const ushort UBRR3H = 0x135;
		const ushort UDR3   = 0x136;

		const int FREQ_16MHZ = 16_000_000;
		const int TXEN = 8;
		const int RXEN = 16;
		const int RXC  = 0x80;

		private static readonly AvrUsartConfig Usart3Config = new AvrUsartConfig
		{
			RxCompleteInterrupt        = 0xD8,
			DataRegisterEmptyInterrupt = 0xDC,
			TxCompleteInterrupt        = 0xE0,
			UCSRA = UCSR3A, UCSRB = UCSR3B, UCSRC = UCSR3C,
			UBRRL = UBRR3L, UBRRH = UBRR3H, UDR = UDR3,
		};

		private AvrUsart? _usart3;

		protected override void SetupPeripherals()
		{
			try
			{
				_usart3 = new AvrUsart (Cpu, Usart3Config, FREQ_16MHZ);
			}
			catch (Exception e)
			{
				_usart3 = null;
			}
		}

		[Test (Description = "USART3 (ATmega2560) can be instantiated with ushort register addresses > 0xFF")]
		public void Usart3_CanBeCreated ()
		{
			Assert.That(_usart3, Is.Not.Null, "USART3 must be initialized before testing");
		}

		[Test (Description = "USART3 transmits a byte and invokes OnByteTransmit callback")]
		public void Usart3_Transmit ()
		{
			if (_usart3 == null)
			{
				Assert.Fail ("USART3 is not initialized");
				return;
			}

			byte? received = null;
			_usart3.OnByteTransmit = b => received = b;

			// Enable TX, then write a byte — OnByteTransmit fires immediately
			Cpu.WriteData (UCSR3B, TXEN);
			Cpu.WriteData (UDR3, 0x55);

			Assert.That (received, Is.EqualTo (0x55), "USART3 must invoke OnByteTransmit with the transmitted byte");
		}

		[Test (Description = "USART3 receives a byte and sets RXC flag")]
		public void Usart3_Receive ()
		{
			if (_usart3 == null)
			{
				Assert.Fail ("USART3 is not initialized");
				return;
			}

			// Enable RX, baud 9600 @ 16 MHz
			Cpu.WriteData (UCSR3B, RXEN);
			Cpu.Mmio.Data[UBRR3L] = 103;

			// WriteByte simulates an incoming byte (as if arriving on the RX pin)
			_usart3.WriteByte (0xAB);

			// Advance clock to complete reception (10 bits @ 9600 baud)
			Cpu.Cycles += 16_800;
			Cpu.Tick ();

			Assert.Multiple (() =>
			{
				Assert.That (Cpu.Mmio.Data[UCSR3A] & RXC, Is.EqualTo (RXC), "RXC must be set after receive");
				Assert.That (Cpu.ReadData (UDR3), Is.EqualTo (0xAB), "UDR3 must hold the received byte");
			});
		}
	}
}
