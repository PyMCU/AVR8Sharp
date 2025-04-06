using System.Text;
using AVR8Sharp.Peripherals;
namespace AVr8SharpTests;

[TestFixture]
public class Usart
{
	const int FREQ_16MHZ = 16_000_000;
	const int FREQ_11_0529MHZ = 11059200;

	// CPU registers
	const int SREG = 95;

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
	
	[Test(Description = "Should correctly calculate the baudRate from UBRR")]
	public void BaudRateCalculation()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
		var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_11_0529MHZ);
		
		cpu.WriteData (UBRR0H, 0);
		cpu.WriteData (UBRR0L, 5);
		
		Assert.That (usart.BaudRate, Is.EqualTo(115_200));
	}
	
	[Test(Description = "Should correctly calculate the baudRate from UBRR in double-speed mode")]
	public void BaudRateCalculationDoubleSpeed()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
		var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
		
		cpu.WriteData (UBRR0H, 3);
		cpu.WriteData (UBRR0L, 64);
		cpu.WriteData (UCSR0A, U2X0);
		
		Assert.That (usart.BaudRate, Is.EqualTo(2_400));
	}
	
	[Test(Description = "Should call onConfigurationChange when the baudRate changes")]
	public void ConfigurationChange()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
		var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
		var configChangeCount = 0;
		
		usart.OnConfigurationChange += () => configChangeCount++;
		
		cpu.WriteData (UBRR0H, 0);
		Assert.That (configChangeCount, Is.EqualTo(1));
		
		cpu.WriteData (UBRR0L, 5);
		Assert.That (configChangeCount, Is.EqualTo(2));
		
		cpu.WriteData (UCSR0A, U2X0);
		Assert.That (configChangeCount, Is.EqualTo(3));
	}
	
	[Test(Description = "Should invoke onByteTransmit when UDR0 is written to")]
	public void TransmitByte()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
		var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
		var byteTransmitCount = 0;
		
		usart.OnByteTransmit += (b) => byteTransmitCount = b;
		
		cpu.WriteData (UCSR0B, TXEN);
		cpu.WriteData (UDR0, 0x61);
		
		Assert.That (byteTransmitCount, Is.EqualTo(0x61));
	}

	[TestFixture]
	public class BitsPerChar
	{
		[Test(Description = "Should return 5-bits per byte when UCSZ = 0")]
		public void FiveBitsPerChar()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			cpu.WriteData (UCSR0C, 0);
			
			Assert.That (usart.BitsPerChar, Is.EqualTo(5));
		}
		
		[Test(Description = "Should return 6-bits per byte when UCSZ = 1")]
		public void SixBitsPerChar()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			cpu.WriteData (UCSR0C, UCSZ0);
			
			Assert.That (usart.BitsPerChar, Is.EqualTo(6));
		}
		
		[Test(Description = "Should return 7-bits per byte when UCSZ = 2")]
		public void SevenBitsPerChar()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			cpu.WriteData (UCSR0C, UCSZ1);
			
			Assert.That (usart.BitsPerChar, Is.EqualTo(7));
		}
		
		[Test(Description = "Should return 8-bits per byte when UCSZ = 3")]
		public void EightBitsPerChar()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			cpu.WriteData (UCSR0C, UCSZ0 | UCSZ1);
			
			Assert.That (usart.BitsPerChar, Is.EqualTo(8));
		}
		
		[Test(Description = "Should return 9-bits per byte when UCSZ = 7")]
		public void NineBitsPerChar()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			cpu.WriteData (UCSR0C, UCSZ0 | UCSZ1);
			cpu.WriteData (UCSR0B, UCSZ2);
			
			Assert.That (usart.BitsPerChar, Is.EqualTo(9));
		}
		
		[Test(Description = "Should call onConfigurationChange when bitsPerChar change")]
		public void ConfigurationChange()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			var configChangeCount = 0;
			
			usart.OnConfigurationChange += () => configChangeCount++;
			
			cpu.WriteData (UCSR0C, UCSZ0 | UCSZ1);
			Assert.That (configChangeCount, Is.EqualTo(1));
			
			cpu.WriteData (UCSR0B, UCSZ2);
			Assert.That (configChangeCount, Is.EqualTo(2));
			
			cpu.WriteData (UCSR0B, UCSZ2);
			Assert.That (configChangeCount, Is.EqualTo(2));
		}
	}

	[TestFixture]
	public class StopBits
	{
		[Test(Description = "Should return 1 when USBS = 0")]
		public void OneStopBit()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			Assert.That (usart.StopBits, Is.EqualTo(1));
		}
		
		[Test(Description = "Should return 2 when USBS = 1")]
		public void TwoStopBits()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			cpu.WriteData (UCSR0C, USBS);
			
			Assert.That (usart.StopBits, Is.EqualTo(2));
		}
	}

	[TestFixture]
	public class Parity
	{
		[Test(Description = "Should return false when UPM1 = 0")]
		public void ParityDisabled()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			Assert.That (usart.ParityEnabled, Is.False);
		}
		
		[Test(Description = "Should return true when UPM1 = 1")]
		public void ParityEnabled()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			cpu.WriteData (UCSR0C, UPM1);
			
			Assert.That (usart.ParityEnabled, Is.True);
		}
		
		[Test(Description = "Should return false when UPM0 = 0")]
		public void ParityEven()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			Assert.That (usart.ParityOdd, Is.False);
		}
		
		[Test(Description = "Should return true when UPM0 = 1")]
		public void ParityOdd()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			cpu.WriteData (UCSR0C, UPM0);
			
			Assert.That (usart.ParityOdd, Is.True);
		}
	}

	[TestFixture]
	public class TxEnableRxEnable
	{
		[Test(Description = "TxEnable should equal true when the transitter is enabled")]
		public void TxEnable()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			Assert.That (usart.TxEnable, Is.False);
			
			cpu.WriteData (UCSR0B, TXEN);
			
			Assert.That (usart.TxEnable, Is.True);
		}
		
		[Test(Description = "RxEnable should equal true when the transitter is enabled")]
		public void RxEnable()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			Assert.That (usart.RxEnable, Is.False);
			
			cpu.WriteData (UCSR0B, RXEN);
			
			Assert.That (usart.RxEnable, Is.True);
		}
	}
	
	[TestFixture]
	public class Tick
	{
		[Test(Description = "Should trigger data register empty interrupt if UDRE is set")]
		public void DataRegisterEmptyInterrupt()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			cpu.WriteData (UCSR0B, UDRIE | TXEN);
			cpu.Data[SREG] = 0x80; // SREG: I-------
			cpu.Tick();
            Assert.Multiple(() =>
            {
                Assert.That(cpu.PC, Is.EqualTo(PC_INT_UDRE));
                Assert.That(cpu.Cycles, Is.EqualTo(2));
                Assert.That((cpu.Data[UCSR0A] & UDRE), Is.EqualTo(0));
            });
        }

		[Test (Description = "Should trigger data TX Complete interrupt if TXCIE is set")]
		public void TxCompleteInterrupt ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var usart = new AvrUsart (cpu, AvrUsart.Usart0Config, FREQ_16MHZ);
			
			cpu.WriteData (UCSR0B, TXCIE | TXEN);
			cpu.WriteData (UDR0, 0x61);
			cpu.Data[SREG] = 0x80; // SREG: I-------
			cpu.Cycles = 1_000_000;
			cpu.Tick();
            Assert.Multiple(() =>
            {
                Assert.That(cpu.PC, Is.EqualTo(PC_INT_TXC));
                Assert.That(cpu.Cycles, Is.EqualTo(1_000_000 + 2));
                Assert.That((cpu.Data[UCSR0A] & TXC), Is.EqualTo(0));
            });
        }

		[Test (Description = "Should not trigger data TX Complete interrupt if UDR was not written to")]
		public void TxCompleteInterruptNotTriggered ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var usart = new AvrUsart (cpu, AvrUsart.Usart0Config, FREQ_16MHZ);
			
			cpu.WriteData (UCSR0B, TXCIE | TXEN);
			cpu.Data[SREG] = 0x80; // SREG: I-------
			cpu.Tick();
			Assert.Multiple(() =>
			{
				Assert.That(cpu.PC, Is.EqualTo(0));
				Assert.That(cpu.Cycles, Is.EqualTo(0));
			});
		}
		
		[Test(Description = "Should not trigger any interrupt if interrupts are disabled")]
		public void InterruptsDisabled()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			cpu.WriteData (UCSR0B, UDRIE | TXEN);
			cpu.WriteData (UDR0, 0x61);
			cpu.Data[SREG] = 0; // SREG: 0 (disable interrupts)
			cpu.Cycles = 1_000_000;
			cpu.Tick();
			Assert.Multiple(() =>
			{
				Assert.That(cpu.PC, Is.EqualTo(0));
				Assert.That(cpu.Cycles, Is.EqualTo(1_000_000));
				Assert.That(cpu.Data[UCSR0A], Is.EqualTo(TXC | UDRE));
			});
		}
	}

	[TestFixture]
	public class OnLineTransmit
	{
		[Test(Description = "Should call onLineTransmit with the current line buffer after every newline")]
		public void LineTransmit()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			var builder = new StringBuilder();
			
			usart.OnLineTransmit += (line) => builder.Append(line);
			
			cpu.WriteData (UCSR0B, TXEN);
			cpu.WriteData (UDR0, 0x48); // 'H'
			cpu.WriteData (UDR0, 0x65); // 'e'
			cpu.WriteData (UDR0, 0x6c); // 'l'
			cpu.WriteData (UDR0, 0x6c); // 'l'
			cpu.WriteData (UDR0, 0x6f); // 'o'
			cpu.WriteData (UDR0, 0xa); // '\n'
			
			Assert.That (builder.ToString(), Is.EqualTo("Hello"));
		}
		
		[Test(Description = "Should not call onLineTransmit if no newline was received")]
		public void LineTransmitNoNewline()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			var builder = new StringBuilder();
			
			usart.OnLineTransmit += (line) => builder.Append(line);
			
			cpu.WriteData (UCSR0B, TXEN);
			cpu.WriteData (UDR0, 0x48); // 'H'
			cpu.WriteData (UDR0, 0x69); // 'i'
			
			Assert.That (builder.ToString(), Is.EqualTo(""));
		}
		
		[Test(Description = "Should clear the line buffer after each call to onLineTransmit")]
		public void LineBufferClear()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			string lineToTransmit = "";
			
			usart.OnLineTransmit += (line) => lineToTransmit = line;
			
			cpu.WriteData (UCSR0B, TXEN);
			cpu.WriteData (UDR0, 0x48); // 'H'
			cpu.WriteData (UDR0, 0x69); // 'i'
			cpu.WriteData (UDR0, 0xa); // '\n'
			cpu.WriteData (UDR0, 0x74); // 't'
			cpu.WriteData (UDR0, 0x68); // 'h'
			cpu.WriteData (UDR0, 0x65); // 'e'
			cpu.WriteData (UDR0, 0x72); // 'r'
			cpu.WriteData (UDR0, 0x65); // 'e'
			cpu.WriteData (UDR0, 0xa); // '\n'
			
			Assert.That (lineToTransmit, Is.EqualTo("there"));
		}
	}

	[TestFixture]
	public class WriteByte
	{
		[Test(Description = "Should return false if called when RX is busy")]
		public void RxBusy()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			cpu.WriteData (UCSR0B, RXEN);
			cpu.WriteData (UBRR0L, 103); // baud: 9600
			
			Assert.That (usart.WriteByte(10), Is.True);
			Assert.That (usart.WriteByte(10), Is.False);
			
			cpu.Tick();
			
			Assert.That (usart.WriteByte(10), Is.False);
		}
	}

	[TestFixture]
	public class Integration
	{
		[Test(Description = "Should set the TXC bit after ~1.04mS when baud rate set to 9600")]
		public void TxComplete()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			cpu.WriteData (UCSR0B, TXEN);
			cpu.WriteData (UBRR0L, 103); // baud: 9600
			cpu.WriteData (UDR0, 0x48); // 'H'
			cpu.Cycles += 16_000; // 1ms
			cpu.Tick();
			Assert.That ((cpu.Data[UCSR0A] & TXC), Is.EqualTo(0));
			cpu.Cycles += 800; // 0.05ms
			cpu.Tick();
			Assert.That ((cpu.Data[UCSR0A] & TXC), Is.EqualTo(TXC));
		}
		
		[Test(Description = "Should be ready to recieve the next byte after ~1.04ms when baudrate set to 9600")]
		public void ReadyToReceive()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024]);
			var usart = new AvrUsart(cpu,  AvrUsart.Usart0Config, FREQ_16MHZ);
			
			var rxCompleteCallback = 0;
			usart.OnRxComplete += () => rxCompleteCallback++;
			
			cpu.WriteData (UCSR0B, RXEN);
			cpu.WriteData (UBRR0L, 103); // baud: 9600
			Assert.That (usart.WriteByte(0x42), Is.True);
			cpu.Cycles += 16_000; // 1ms
			cpu.Tick();
            Assert.Multiple(() =>
            {
                Assert.That((cpu.Data[UCSR0A] & RXC), Is.EqualTo(0)); // byte not received yet
                Assert.That(usart.RxBusy, Is.True);
                Assert.That(rxCompleteCallback, Is.EqualTo(0));
            });
            cpu.Cycles += 800; // 0.05ms
			cpu.Tick();
            Assert.Multiple(() =>
            {
                Assert.That((cpu.Data[UCSR0A] & RXC), Is.EqualTo(RXC));
                Assert.That(usart.RxBusy, Is.False);
                Assert.That(rxCompleteCallback, Is.EqualTo(1));
                Assert.That(cpu.ReadData(UDR0), Is.EqualTo(0x42));
                Assert.That (cpu.ReadData(UDR0), Is.EqualTo(0));
            });
		}
	}
}
