using AVR8Sharp.Peripherals;
namespace AVr8SharpTests;

[TestFixture]
public class Eeprom
{
	// EEPROM Registers
	const int EECR = 0x3f;
	const int EEDR = 0x40;
	const int EEARL = 0x41;
	const int EEARH = 0x42;
	const int SREG = 95;

	// Register bit names
	const int EERE = 1;
	const int EEPE = 2;
	const int EEMPE = 4;
	const int EERIE = 8;
	const int EEPM0 = 16;
	const int EEPM1 = 32;
	[TestFixture]
	public class Read
	{
		[Test (Description = "Should return 0xff when reading from an empty location")]
		public void Empty ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[0x1000]);
			var eeprom = new AvrEeprom (cpu, new EepromMemoryBackend (1024));
			
			cpu.WriteData (EEARL, 0);
			cpu.WriteData (EEARH, 0);
			cpu.WriteData (EECR, EERE);
			cpu.Tick ();
            Assert.Multiple(() =>
            {
                Assert.That(cpu.Cycles, Is.EqualTo(4));
                Assert.That(cpu.ReadData(EEDR), Is.EqualTo(0xff));
            });
        }
		
		[Test (Description = "Should return the value stored at the given EEPROM address")]
		public void ReadValue ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[0x1000]);
			var backend = new EepromMemoryBackend (1024);
			var avrEeprom = new AvrEeprom (cpu, backend);
			
			backend.WriteMemory (0x250, 0x42);

			cpu.WriteData (EEARL, 0x50);
			cpu.WriteData (EEARH, 0x2);
			cpu.WriteData (EEDR, 0x42);
			cpu.WriteData (EECR, EERE);
			cpu.Tick ();
			Assert.Multiple(() =>
			{
				Assert.That(cpu.ReadData(EEDR), Is.EqualTo(0x42));
			});
		}
	}
	
	[TestFixture]
	public class Write
	{
		[Test (Description = "Should write a byte to the given EEPROM address")]
		public void WriteValue ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[0x1000]);
			var backend = new EepromMemoryBackend (1024);
			var avrEeprom = new AvrEeprom (cpu, backend);
			
			cpu.WriteData (EEDR, 0x55);
			cpu.WriteData (EEARL, 15);
			cpu.WriteData (EEARH, 0);
			cpu.WriteData (EECR, EEMPE);
			cpu.WriteData (EECR, EEPE);
			cpu.Tick ();
			Assert.Multiple(() =>
			{
				Assert.That(cpu.Cycles, Is.EqualTo(2));
				Assert.That(backend.ReadMemory (15), Is.EqualTo(0x55));
				Assert.That(cpu.ReadData(EECR) & EEPE, Is.EqualTo(EEPE));
			});
		}
		
		[Test (Description = "Should not erase the memory when writing if EEPM1 is high")]
		public void NoErase ()
		{
			var program = Utils.AsmProgram ($@"
				  ; register addresses
		          _REPLACE TWSR, {EECR - 0x20}
        		  _REPLACE EEARL, {EEARL - 0x20}
		          _REPLACE EEDR, {EEDR - 0x20}
		          _REPLACE EECR, {EECR - 0x20}

		          LDI r16, 0x55
		          OUT EEDR, r16
		          LDI r16, 9
        		  OUT EEARL, r16
        		  SBI EECR, 5     ; EECR |= EEPM1
        		  SBI EECR, 2     ; EECR |= EEMPE
 		          SBI EECR, 1     ; EECR |= EEPE
			");
			
			var cpu = new AVR8Sharp.Cpu.Cpu (program.Program);
			var backend = new EepromMemoryBackend (1024);
			var avrEeprom = new AvrEeprom (cpu, backend);
			
			backend.WriteMemory (9, 0x0f);
			
			var runner = new TestProgramRunner (cpu);
			runner.RunInstructions (program.InstructionCount);
			
			Assert.That (backend.ReadMemory (9), Is.EqualTo(0x05));
		}
		
		[Test (Description = "Should clear the EEPE bit and fire an interrupt when write has been completed")]
		public void WriteComplete ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[0x1000]);
			var backend = new EepromMemoryBackend (1024);
			var avrEeprom = new AvrEeprom (cpu, backend);
			
			cpu.WriteData (EEDR, 0x55);
			cpu.WriteData (EEARL, 15);
			cpu.WriteData (EEARH, 0);
			cpu.WriteData (EECR, EEMPE);
			cpu.Data[SREG] = 0x80; // SREG: I-------
			cpu.WriteData (EECR, EEPE | EERIE);
			cpu.Cycles += 1000;
			cpu.Tick ();
			
			// At this point, write shouldn't be complete yet
			Assert.Multiple(() =>
			{
				Assert.That(cpu.Data[EECR] & EEPE, Is.EqualTo(EEPE));
				Assert.That(cpu.PC, Is.EqualTo(0));
			});
			
			cpu.Cycles += 10_000_000;
			
			// And now, 10 million cycles later, it should.
			cpu.Tick ();
			
			Assert.Multiple(() =>
			{
				Assert.That(backend.ReadMemory (15), Is.EqualTo(0x55));
				Assert.That(cpu.ReadData(EECR) & EEPE, Is.EqualTo(0));
				Assert.That(cpu.PC, Is.EqualTo(0x2c)); // EEPROM Ready interrupt
			});
		}
		
		[Test (Description = "Should clear the fire an interrupt when there is a pending interrupt and the interrupt flag is enabled (issue #110)")]
		public void PendingInterrupt ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[0x1000]);
			var backend = new EepromMemoryBackend (1024);
			var avrEeprom = new AvrEeprom (cpu, backend);
			
			cpu.WriteData (EEDR, 0x55);
			cpu.WriteData (EEARL, 15);
			cpu.WriteData (EEARH, 0);
			cpu.WriteData (EECR, EEMPE);
			cpu.Data[SREG] = 0x80; // SREG: I-------
			cpu.WriteData (EECR, EEPE);
			cpu.Cycles += 1000;
			cpu.Tick ();
			
			// At this point, write shouldn't be complete yet
			Assert.Multiple(() =>
			{
				Assert.That(cpu.ReadData(EECR) & EEPE, Is.EqualTo(EEPE));
				Assert.That(cpu.PC, Is.EqualTo(0));
			});
			
			cpu.Cycles += 10_000_000;
			
			// And now, 10 million cycles later, it should.
			cpu.Tick ();
			
			Assert.Multiple(() =>
			{
				Assert.That(backend.ReadMemory (15), Is.EqualTo(0x55));
				Assert.That(cpu.ReadData(EECR) & EEPE, Is.EqualTo(0));
				cpu.WriteData (EECR, EERIE);
				cpu.Tick ();
				Assert.That(cpu.PC, Is.EqualTo(0x2c)); // EEPROM Ready interrupt
			});
		}
		
		[Test (Description = "Should skip the write if EEMPE is clear")]
		public void NoWrite ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[0x1000]);
			var backend = new EepromMemoryBackend (1024);
			var avrEeprom = new AvrEeprom (cpu, backend);
			
			cpu.WriteData (EEDR, 0x55);
			cpu.WriteData (EEARL, 15);
			cpu.WriteData (EEARH, 0);
			cpu.WriteData (EECR, EEPE);
			
			cpu.Cycles += 8;
			cpu.Tick ();
			
			cpu.WriteData (EECR, EEPE);
			
			cpu.Tick ();
			
			// Ensure that nothing was written, and EEPE bit is clear
			Assert.Multiple(() =>
			{
				Assert.That(backend.ReadMemory (15), Is.EqualTo(0xff));
				Assert.That(cpu.ReadData(EECR) & EEPE, Is.EqualTo(0));
			});
		}
		
		[Test (Description = "Should skip the write if another write is already in progress")]
		public void NoWriteInProgress ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[0x1000]);
			var backend = new EepromMemoryBackend (1024);
			var avrEeprom = new AvrEeprom (cpu, backend);
			
			// Write 0x55 to address 15
			cpu.WriteData (EEDR, 0x55);
			cpu.WriteData (EEARL, 15);
			cpu.WriteData (EEARH, 0);
			cpu.WriteData (EECR, EEMPE);
			cpu.WriteData (EECR, EEPE);
			cpu.Tick ();
			
			Assert.That (cpu.Cycles, Is.EqualTo(2));
			
			// Write 0x66 to address 16 (first write is still in progress)
			cpu.WriteData (EEDR, 0x66);
			cpu.WriteData (EEARL, 16);
			cpu.WriteData (EEARH, 0);
			cpu.WriteData (EECR, EEMPE);
			cpu.WriteData (EECR, EEPE);
			cpu.Tick ();
			
			// Ensure that second write didn't happen
			Assert.Multiple(() =>
			{
				Assert.That (cpu.Cycles, Is.EqualTo(2));
				Assert.That (backend.ReadMemory (15), Is.EqualTo(0x55));
				Assert.That (backend.ReadMemory (16), Is.EqualTo(0xff));
			});
		}
		
		[Test (Description = "Should write two bytes sucessfully")]
		public void WriteTwoBytes ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[0x1000]);
			var backend = new EepromMemoryBackend (1024);
			var avrEeprom = new AvrEeprom (cpu, backend);
			
			// Write 0x55 to address 15
			cpu.WriteData (EEDR, 0x55);
			cpu.WriteData (EEARL, 15);
			cpu.WriteData (EEARH, 0);
			cpu.WriteData (EECR, EEMPE);
			cpu.WriteData (EECR, EEPE);
			cpu.Tick ();
			
			Assert.That (cpu.Cycles, Is.EqualTo(2));
			
			// Wait long enough time for the first write to finish
			cpu.Cycles += 10_000_000;
			cpu.Tick ();
			
			// Write 0x66 to address 16
			cpu.WriteData (EEDR, 0x66);
			cpu.WriteData (EEARL, 16);
			cpu.WriteData (EEARH, 0);
			cpu.WriteData (EECR, EEMPE);
			cpu.WriteData (EECR, EEPE);
			cpu.Tick ();
			
			// Ensure both writes took place
			Assert.Multiple(() =>
			{
				Assert.That (cpu.Cycles, Is.EqualTo(10_000_004));
				Assert.That (backend.ReadMemory (15), Is.EqualTo(0x55));
				Assert.That (backend.ReadMemory (16), Is.EqualTo(0x66));
			});
		}
	}

	[Test(Description = "Should only erase the memory when EEPM0 is high")]
	public void Erase()
	{
		var program = Utils.AsmProgram ($@"
				  ; register addresses
		          _REPLACE EEARL, {EEARL - 0x20}
				  _REPLACE EEDR, {EEDR - 0x20}
				  _REPLACE EECR, {EECR - 0x20}

		          LDI r16, 0x55
			      OUT EEDR, r16
				  LDI r16, 9
				  OUT EEARL, r16
				  SBI EECR, 4     ; EECR |= EEPM0
				  SBI EECR, 2     ; EECR |= EEMPE
			      SBI EECR, 1     ; EECR |= EEPE
			");
		
		var cpu = new AVR8Sharp.Cpu.Cpu (program.Program);
		var backend = new EepromMemoryBackend (1024);
		var avrEeprom = new AvrEeprom (cpu, backend);
		
		backend.WriteMemory (9, 0x22);
		
		var runner = new TestProgramRunner (cpu);
		runner.RunInstructions (program.InstructionCount);
		
		Assert.That (backend.ReadMemory (9), Is.EqualTo(0xff));
	}
}
