using AVR8Sharp.Peripherals;
namespace AVr8SharpTests;

[TestFixture]
public class Spi
{
	const int FREQ_16MHZ = 16_000_000;

	// CPU registers
	const int R17 = 17;
	const int SREG = 95;

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
	
	[Test (Description = "Should correctly calculate the frequency based on SPCR/SPST values")]
	public void Frequency ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var spi = new AvrSpi (cpu, AvrSpi.SpiConfig, FREQ_16MHZ);
		
		// Standard SPI speed:
		cpu.WriteData(SPSR, 0);
		cpu.WriteData(SPCR, 0);
		Assert.That (spi.SpiFrequency, Is.EqualTo(FREQ_16MHZ / 4));
		
		cpu.WriteData(SPCR, SPR0);
		Assert.That (spi.SpiFrequency, Is.EqualTo(FREQ_16MHZ / 16));
		
		cpu.WriteData(SPCR, SPR1);
		Assert.That (spi.SpiFrequency, Is.EqualTo(FREQ_16MHZ / 64));
		
		cpu.WriteData(SPCR, SPR1 | SPR0);
		Assert.That (spi.SpiFrequency, Is.EqualTo(FREQ_16MHZ / 128));
		
		// Double SPI speed:
		cpu.WriteData(SPSR, SPI2X);
		cpu.WriteData(SPCR, 0);
		Assert.That (spi.SpiFrequency, Is.EqualTo(FREQ_16MHZ / 2));
		
		cpu.WriteData(SPCR, SPR0);
		Assert.That (spi.SpiFrequency, Is.EqualTo(FREQ_16MHZ / 8));
		
		cpu.WriteData(SPCR, SPR1);
		Assert.That (spi.SpiFrequency, Is.EqualTo(FREQ_16MHZ / 32));
		
		cpu.WriteData(SPCR, SPR1 | SPR0);
		Assert.That (spi.SpiFrequency, Is.EqualTo(FREQ_16MHZ / 64));
	}
	
	[Test (Description = "hould correctly report the data order (MSB/LSB first), based on SPCR value")]
	public void DataOrder ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var spi = new AvrSpi (cpu, AvrSpi.SpiConfig, FREQ_16MHZ);
		
		cpu.WriteData(SPCR, 0);
		Assert.That (spi.DataOrder, Is.EqualTo(SpiDataOrder.MsbFirst));
		
		cpu.WriteData(SPCR, DORD);
		Assert.That (spi.DataOrder, Is.EqualTo(SpiDataOrder.LsbFirst));
	}
	
	[Test (Description = "Should correctly report the SPI mode, based on SPCR value")]
	public void Mode ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var spi = new AvrSpi (cpu, AvrSpi.SpiConfig, FREQ_16MHZ);
		
		// Values in this test are based on Table 2 in the datasheet, page 174.
		cpu.WriteData(SPCR, 0);
		Assert.That (spi.SpiMode, Is.EqualTo(0));
		
		cpu.WriteData(SPCR, CPHA);
		Assert.That (spi.SpiMode, Is.EqualTo(1));
		
		cpu.WriteData(SPCR, CPOL);
		Assert.That (spi.SpiMode, Is.EqualTo(2));
		
		cpu.WriteData(SPCR, CPOL | CPHA);
		Assert.That (spi.SpiMode, Is.EqualTo(3));
	}
	
	[Test (Description = "Should indicate slave/master operation, based on SPCR value")]
	public void MasterSlave ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var spi = new AvrSpi (cpu, AvrSpi.SpiConfig, FREQ_16MHZ);
		
		cpu.WriteData(SPCR, 0);
		Assert.That (spi.IsMaster, Is.False);
		
		cpu.WriteData(SPCR, MSTR);
		Assert.That (spi.IsMaster, Is.True);
	}
	
	[Test (Description = "Should call the `onByteTransfer` callback when initiating an SPI trasfer by writing to SPDR")]
	public void Transfer ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var spi = new AvrSpi (cpu, AvrSpi.SpiConfig, FREQ_16MHZ) {
			OnByte = b => Assert.That(b, Is.EqualTo(0x8f))
		};

		cpu.WriteData(SPCR, SPE | MSTR);
		cpu.WriteData(SPDR, 0x8f);
	}
	
	[Test (Description = "Should ignore SPDR writes when the SPE bit in SPCR is clear")]
	public void NoTransfer ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var spi = new AvrSpi (cpu, AvrSpi.SpiConfig, FREQ_16MHZ) {
			OnByte = b => Assert.Fail("Should not have been called")
		};

		cpu.WriteData(SPCR, MSTR);
		cpu.WriteData(SPDR, 0x8f);
	}

	[Test (Description = "Should transmit a byte successfully (integration)")]
	public void Transmit ()
	{
		var program = Utils.AsmProgram (@$"
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
");
		
		var cpu = new AVR8Sharp.Cpu.Cpu (program.Program);
		var spi = new AvrSpi (cpu, AvrSpi.SpiConfig, FREQ_16MHZ);
		
		var byteReceivedFromAsmCode = 0;
		
		spi.OnByte = b => {
			byteReceivedFromAsmCode = b;
			cpu.AddClockEvent(() => spi.CompleteTransfer(0x5b), spi.TransferCycles);
		};
		
		var runner = new TestProgramRunner(cpu, (_) => {});
		runner.RunToBreak();
		
        Assert.Multiple(() =>
        {
            // 16 cycles per clock * 8 bits = 128
            Assert.That(cpu.Cycles, Is.GreaterThanOrEqualTo(128));
            
            Assert.That(byteReceivedFromAsmCode, Is.EqualTo(0xb8));
            Assert.That(cpu.Data[R17], Is.EqualTo(0x5b));
        });

    }
	
	[Test (Description = "Should set the WCOL bit in SPSR if writing to SPDR while SPI is already transmitting")]
	public void WriteCollision ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var spi = new AvrSpi (cpu, AvrSpi.SpiConfig, FREQ_16MHZ);
		
		cpu.WriteData(SPCR, SPE | MSTR);
		cpu.WriteData(SPDR, 0x50);
		cpu.Tick();
		Assert.That(cpu.ReadData(SPSR) & WCOL, Is.Zero);
		
		cpu.WriteData(SPDR, 0x51);
		Assert.That(cpu.ReadData(SPSR) & WCOL, Is.EqualTo(WCOL));
	}
	
	[Test (Description = "Should clear the SPIF bit and fire an interrupt when SPI transfer completes")]
	public void TransferComplete ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var spi = new AvrSpi (cpu, AvrSpi.SpiConfig, FREQ_16MHZ);
		
		cpu.WriteData(SPCR, SPE | SPIE | MSTR);
		cpu.WriteData(SPDR, 0x50);
		cpu.Data[SREG] = 0x80; // SREG: I-------
		
		// At this point, write shouldn't be complete yet
		cpu.Cycles += 10;
		cpu.Tick();
		Assert.That (cpu.PC, Is.Zero);
		
		// 100 cycles later, it should (8 bits * 8 cycles per bit = 64).
		cpu.Cycles += 100;
		cpu.Tick();
		Assert.Multiple(() =>
		{
			Assert.That(cpu.ReadData(SPSR) & SPIF, Is.Zero);
			Assert.That(cpu.PC, Is.EqualTo(0x22)); // SPI Ready interrupt
		});
	}
	
	[Test (Description = "Should fire a pending SPI interrupt when SPIE flag is set")]
	public void PendingInterrupt ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var spi = new AvrSpi (cpu, AvrSpi.SpiConfig, FREQ_16MHZ);
		
		cpu.WriteData(SPCR, SPE | MSTR);
		cpu.WriteData(SPDR, 0x50);
		cpu.Data[SREG] = 0x80; // SREG: I-------
		
		// Wait for transfer to complete (8 bits * 8 cycles per bit = 64).
		cpu.Cycles += 64;
		cpu.Tick();
		
		Assert.Multiple(() =>
		{
			Assert.That(cpu.ReadData(SPSR) & SPIF, Is.EqualTo(SPIF));
			Assert.That(cpu.PC, Is.Zero); // Interrupt not taken (yet)
			
			// Enable the interrupt (SPIE)
			cpu.WriteData(SPCR, SPE | MSTR | SPIE);
			cpu.Tick();
			Assert.That(cpu.PC, Is.EqualTo(0x22)); // SPI Ready interrupt
			Assert.That(cpu.ReadData(SPSR) & SPIF, Is.Zero);
		});
	}
	
	[Test (Description = "Should should only update SPDR when tranfer finishes (double buffering)")]
	public void DoubleBuffering ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var spi = new AvrSpi (cpu, AvrSpi.SpiConfig, FREQ_16MHZ);
		
		spi.OnByte = (b) => {
			cpu.AddClockEvent(() => spi.CompleteTransfer(0x88), spi.TransferCycles);
		};
		
		cpu.WriteData(SPCR, SPE | MSTR);
		cpu.WriteData(SPDR, 0x8f);
		
		cpu.Cycles += 10;
		cpu.Tick();
		Assert.That(cpu.ReadData(SPDR), Is.Zero);
		
		cpu.Cycles += 32; // 4 cycles per bit * 8 bits = 32
		cpu.Tick();
		Assert.That(cpu.ReadData(SPDR), Is.EqualTo(0x88));
	}
}
