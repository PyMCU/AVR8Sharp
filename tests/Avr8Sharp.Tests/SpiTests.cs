using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

[TestFixture]
public class Spi : AvrTestBase
{
	const int FREQ_16MHZ = 16_000_000;

	// SPI Registers
	const int SPCR = 0x4c;
	const int SPSR = 0x4d;
	const int SPDR = 0x4e;

	// Register bit names
	const int SPR0 = 1;
	const int SPR1 = 2;
	const int CPOL = 4;
	const int CPHA = 8;
	const int MSTR = 0x10;
	const int DORD = 0x20;
	const int SPE = 0x40;
	const int SPIE = 0x80;
	const int WCOL = 0x40;
	const int SPIF = 0x80;
	const int SPI2X = 1;

	private AvrSpi _spi;

	protected override void SetupPeripherals()
	{
		_spi = new AvrSpi (Cpu, AvrSpi.SpiConfig, FREQ_16MHZ);
	}

	[Test (Description = "Should correctly calculate the frequency based on SPCR/SPST values")]
	public void Frequency ()
	{
		// Standard SPI speed:
		Cpu.WriteData(SPSR, 0);
		Cpu.WriteData(SPCR, 0);
		Assert.That (_spi.SpiFrequency, Is.EqualTo(FREQ_16MHZ / 4));
		
		Cpu.WriteData(SPCR, SPR0);
		Assert.That (_spi.SpiFrequency, Is.EqualTo(FREQ_16MHZ / 16));
		
		Cpu.WriteData(SPCR, SPR1);
		Assert.That (_spi.SpiFrequency, Is.EqualTo(FREQ_16MHZ / 64));
		
		Cpu.WriteData(SPCR, SPR1 | SPR0);
		Assert.That (_spi.SpiFrequency, Is.EqualTo(FREQ_16MHZ / 128));
		
		// Double SPI speed:
		Cpu.WriteData(SPSR, SPI2X);
		Cpu.WriteData(SPCR, 0);
		Assert.That (_spi.SpiFrequency, Is.EqualTo(FREQ_16MHZ / 2));
		
		Cpu.WriteData(SPCR, SPR0);
		Assert.That (_spi.SpiFrequency, Is.EqualTo(FREQ_16MHZ / 8));
		
		Cpu.WriteData(SPCR, SPR1);
		Assert.That (_spi.SpiFrequency, Is.EqualTo(FREQ_16MHZ / 32));
		
		Cpu.WriteData(SPCR, SPR1 | SPR0);
		Assert.That (_spi.SpiFrequency, Is.EqualTo(FREQ_16MHZ / 64));
	}
	
	[Test (Description = "Should correctly report the data order (MSB/LSB first), based on SPCR value")]
	public void DataOrder ()
	{
		Cpu.WriteData(SPCR, 0);
		Assert.That (_spi.DataOrder, Is.EqualTo(SpiDataOrder.MsbFirst));
		
		Cpu.WriteData(SPCR, DORD);
		Assert.That (_spi.DataOrder, Is.EqualTo(SpiDataOrder.LsbFirst));
	}
	
	[Test (Description = "Should correctly report the SPI mode, based on SPCR value")]
	public void Mode ()
	{
		// Values in this test are based on Table 2 in the datasheet, page 174.
		Cpu.WriteData(SPCR, 0);
		Assert.That (_spi.SpiMode, Is.EqualTo(0));
		
		Cpu.WriteData(SPCR, CPHA);
		Assert.That (_spi.SpiMode, Is.EqualTo(1));
		
		Cpu.WriteData(SPCR, CPOL);
		Assert.That (_spi.SpiMode, Is.EqualTo(2));
		
		Cpu.WriteData(SPCR, CPOL | CPHA);
		Assert.That (_spi.SpiMode, Is.EqualTo(3));
	}
	
	[Test (Description = "Should indicate slave/master operation, based on SPCR value")]
	public void MasterSlave ()
	{
		Cpu.WriteData(SPCR, 0);
		Assert.That (_spi.IsMaster, Is.False);
		
		Cpu.WriteData(SPCR, MSTR);
		Assert.That (_spi.IsMaster, Is.True);
	}
	
	[Test (Description = "Should call the `onByteTransfer` callback when initiating an SPI trasfer by writing to SPDR")]
	public void Transfer ()
	{
		_spi.OnByte = b => Assert.That(b, Is.EqualTo(0x8f));

		Cpu.WriteData(SPCR, SPE | MSTR);
		Cpu.WriteData(SPDR, 0x8f);
	}
	
	[Test (Description = "Should ignore SPDR writes when the SPE bit in SPCR is clear")]
	public void NoTransfer ()
	{
		_spi.OnByte = b => Assert.Fail("Should not have been called");

		Cpu.WriteData(SPCR, MSTR);
		Cpu.WriteData(SPDR, 0x8f);
	}

	[Test (Description = "Should transmit a byte successfully (integration)")]
	public void Transmit ()
	{
		var program = new AsmProgram (@$"
		; register addresses
		_REPLACE SPCR, {SPCR - 0x20}
		_REPLACE SPDR, {SPDR - 0x20}
		_REPLACE SPSR, {SPSR - 0x20}
		_REPLACE DDR_SPI, 0x4 ; PORTB

	    SPI_MasterInit:
		    ; Set MOSI and SCK output, all others input
			LDI r17, 0x28
			OUT DDR_SPI, r17
    
	        ; Enable SPI, Master, set clock rate fck/16
		    LDI r17, 0x51   ; (1<<SPE)|(1<<MSTR)|(1<<SPR0)
			OUT SPCR, r17

        SPI_MasterTransmit:
		    LDI r16, 0xb8 ; byte to transmit
			OUT SPDR, r16

		Wait_Transmit:
			IN r16, SPSR
			SBRS r16, 7
			RJMP Wait_Transmit
      
		   ; Now read the result into r17
	        IN r17, SPDR
		    BREAK
").Compile();
		
		Cpu.LoadProgram(program.Program);
		
		var byteReceivedFromAsmCode = 0;
		
		_spi.OnByte = b => {
			byteReceivedFromAsmCode = b;
			Cpu.AddClockEvent(() => _spi.CompleteTransfer(0x5b), _spi.TransferCycles);
		};
		
		var runner = new TestProgramRunner(Cpu, (_) => {});
		runner.RunToBreak();
		
        Assert.Multiple(() =>
        {
            // 16 cycles per clock * 8 bits = 128
            Assert.That(Cpu.Cycles, Is.GreaterThanOrEqualTo(128));
            
            Assert.That(byteReceivedFromAsmCode, Is.EqualTo(0xb8));
            Assert.That(Cpu.Mmio.Data[R17], Is.EqualTo(0x5b));
        });

    }
	
	[Test (Description = "Should set the WCOL bit in SPSR if writing to SPDR while SPI is already transmitting")]
	public void WriteCollision ()
	{
		Cpu.WriteData(SPCR, SPE | MSTR);
		Cpu.WriteData(SPDR, 0x50);
		Cpu.Tick();
		Assert.That(Cpu.ReadData(SPSR) & WCOL, Is.Zero);
		
		Cpu.WriteData(SPDR, 0x51);
		Assert.That(Cpu.ReadData(SPSR) & WCOL, Is.EqualTo(WCOL));
	}
	
	[Test (Description = "Should clear the SPIF bit and fire an interrupt when SPI transfer completes")]
	public void TransferComplete ()
	{
		Cpu.WriteData(SPCR, SPE | SPIE | MSTR);
		Cpu.WriteData(SPDR, 0x50);
		Cpu.Mmio.Data[SREG] = 0x80; // SREG: I-------
		
		// At this point, write shouldn't be complete yet
		Cpu.Cycles += 10;
		Cpu.Tick();
		Assert.That (Cpu.Pc, Is.Zero);
		
		// 100 cycles later, it should (8 bits * 8 cycles per bit = 64).
		Cpu.Cycles += 100;
		Cpu.Tick();
		Assert.Multiple(() =>
		{
			Assert.That(Cpu.ReadData(SPSR) & SPIF, Is.Zero);
			Assert.That(Cpu.Pc, Is.EqualTo(0x22)); // SPI Ready interrupt
		});
	}
	
	[Test (Description = "Should fire a pending SPI interrupt when SPIE flag is set")]
	public void PendingInterrupt ()
	{
		Cpu.WriteData(SPCR, SPE | MSTR);
		Cpu.WriteData(SPDR, 0x50);
		Cpu.Mmio.Data[SREG] = 0x80; // SREG: I-------
		
		// Wait for transfer to complete (8 bits * 8 cycles per bit = 64).
		Cpu.Cycles += 64;
		Cpu.Tick();
		
		Assert.Multiple(() =>
		{
			Assert.That(Cpu.ReadData(SPSR) & SPIF, Is.EqualTo(SPIF));
			Assert.That(Cpu.Pc, Is.Zero); // Interrupt not taken (yet)
			
			// Enable the interrupt (SPIE)
			Cpu.WriteData(SPCR, SPE | MSTR | SPIE);
			Cpu.Tick();
			Assert.That(Cpu.Pc, Is.EqualTo(0x22)); // SPI Ready interrupt
			Assert.That(Cpu.ReadData(SPSR) & SPIF, Is.Zero);
		});
	}
	
	[TestFixture (Description = "SPI slave-mode tests")]
	public class SpiSlave : AvrTestBase
	{
		const int FREQ_16MHZ = 16_000_000;
		const int SPCR = 0x4c;
		const int SPSR = 0x4d;
		const int SPDR = 0x4e;
		const int SREG = 95;
		const int SPE = 0x40;
		const int SPIE = 0x80;
		const int SPIF = 0x80;

		private AvrSpi _spi;

		protected override void SetupPeripherals()
		{
			_spi = new AvrSpi (Cpu, AvrSpi.SpiConfig, FREQ_16MHZ);
		}

		[Test (Description = "Slave mode: SimulateIncomingMasterByte stores byte in SPDR and sets SPIF")]
		public void SlaveMode_ReceivesByte ()
		{
			// Slave mode: MSTR=0, SPE=1
			Cpu.WriteData (SPCR, SPE);

			_spi.SimulateIncomingMasterByte (0xAB);

			Assert.Multiple (() => {
				Assert.That (Cpu.ReadData (SPDR), Is.EqualTo (0xAB), "SPDR must hold the received byte");
				Assert.That (Cpu.ReadData (SPSR) & SPIF, Is.EqualTo (SPIF), "SPIF must be set after receive");
			});
		}

		[Test (Description = "Slave mode: OnSlaveTransfer callback is invoked with the received byte")]
		public void SlaveMode_TransmitsByte ()
		{
			Cpu.WriteData (SPCR, SPE);

			byte? captured = null;
			_spi.OnSlaveTransfer = b => captured = b;

			_spi.SimulateIncomingMasterByte (0xCD);

			Assert.That (captured, Is.EqualTo (0xCD), "OnSlaveTransfer must be invoked with received byte");
		}

		[Test (Description = "Slave mode: SPI interrupt fires when SPIE=1 and a byte is received")]
		public void SlaveMode_Interrupt_Fires ()
		{
			Cpu.WriteData (SPCR, SPE | SPIE);
			Cpu.Mmio.Data[SREG] = 0x80; // global interrupt enable

			_spi.SimulateIncomingMasterByte (0x77);
			Cpu.Tick ();

			Assert.That (Cpu.Pc, Is.EqualTo (0x22), "CPU must jump to SPI interrupt vector");
		}

		[Test (Description = "Guard: SimulateIncomingMasterByte does nothing if SPE is 0 (SPI disabled)")]
		public void SlaveMode_IgnoresByte_WhenSpiDisabled ()
		{
			Cpu.Mmio.Data[SPDR] = 0x11;

			_spi.SimulateIncomingMasterByte (0xAB);

			Assert.Multiple (() => {
				Assert.That (Cpu.ReadData (SPDR), Is.EqualTo (0x11), "SPDR must not change when SPI is disabled");
				Assert.That (Cpu.ReadData (SPSR) & SPIF, Is.Zero, "SPIF must not be set when SPI is disabled");
			});
		}

		[Test (Description = "Guard: SimulateIncomingMasterByte does nothing if MSTR is 1 (Master mode)")]
		public void SlaveMode_IgnoresByte_WhenConfiguredAsMaster ()
		{
			const int SPCR_MSTR = 0x10;
			Cpu.WriteData (SPCR, SPE | SPCR_MSTR);

			Cpu.Mmio.Data[SPDR] = 0x22;

			_spi.SimulateIncomingMasterByte (0xCD);

			Assert.Multiple (() => {
				Assert.That (Cpu.ReadData (SPDR), Is.EqualTo (0x22), "SPDR must not change in Master mode");
				Assert.That (Cpu.ReadData (SPSR) & SPIF, Is.Zero, "SPIF must not be set from external input in Master mode");
			});
		}
	}

	[Test (Description = "Shift register: byte is moved to SPDR only when transfer completes")]
	public void ShiftRegister_ByteMovedToSpdr ()
	{
		_spi.OnByte = b => {
			Cpu.AddClockEvent (() => _spi.CompleteTransfer (0xA5), _spi.TransferCycles);
		};

		Cpu.WriteData (SPCR, SPE | MSTR);
		Cpu.WriteData (SPDR, 0x00); // initiate transfer

		// SPDR must still hold the old value during transfer
		Assert.That (Cpu.ReadData (SPDR), Is.Zero, "SPDR must not update while transfer is in progress");

		// Advance past transfer (8 bits * 4 cycles/bit = 32)
		Cpu.Cycles += 32;
		Cpu.Tick ();

		Assert.That (Cpu.ReadData (SPDR), Is.EqualTo (0xA5),
			"Byte must be moved from shift register to SPDR on transfer completion");
	}

	[Test (Description = "Should should only update SPDR when tranfer finishes (double buffering)")]
	public void DoubleBuffering ()
	{
		_spi.OnByte = (b) => {
			Cpu.AddClockEvent(() => _spi.CompleteTransfer(0x88), _spi.TransferCycles);
		};
		
		Cpu.WriteData(SPCR, SPE | MSTR);
		Cpu.WriteData(SPDR, 0x8f);
		
		Cpu.Cycles += 10;
		Cpu.Tick();
		Assert.That(Cpu.ReadData(SPDR), Is.Zero);
		
		Cpu.Cycles += 32; // 4 cycles per bit * 8 bits = 32
		Cpu.Tick();
		Assert.That(Cpu.ReadData(SPDR), Is.EqualTo(0x88));
	}
}
