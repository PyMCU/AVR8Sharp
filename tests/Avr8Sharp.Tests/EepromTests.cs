using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

[TestFixture]
public class Eeprom : AvrTestBase
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

	private EepromMemoryBackend _backend;
	private AvrEeprom _eeprom;

	protected override void SetupPeripherals()
	{
		_backend = new EepromMemoryBackend(1024);
		_eeprom = new AvrEeprom(Cpu, _backend);
	}

	[TestFixture]
	public class Read : AvrTestBase
	{
		private EepromMemoryBackend _backend;
		private AvrEeprom _eeprom;

		protected override void SetupPeripherals()
		{
			_backend = new EepromMemoryBackend(1024);
			_eeprom = new AvrEeprom(Cpu, _backend);
		}

		[Test (Description = "Should return 0xff when reading from an empty location")]
		public void Empty ()
		{
			Cpu.WriteData (EEARL, 0);
			Cpu.WriteData (EEARH, 0);
			Cpu.WriteData (EECR, EERE);
			Cpu.Tick ();
            Assert.Multiple(() =>
            {
                Assert.That(Cpu.Cycles, Is.EqualTo(4));
                Assert.That(Cpu.ReadData(EEDR), Is.EqualTo(0xff));
            });
        }
		
		[Test (Description = "Should return the value stored at the given EEPROM address")]
		public void ReadValue ()
		{
			_backend.WriteMemory (0x250, 0x42);

			Cpu.WriteData (EEARL, 0x50);
			Cpu.WriteData (EEARH, 0x2);
			Cpu.WriteData (EEDR, 0x42);
			Cpu.WriteData (EECR, EERE);
			Cpu.Tick ();
			Assert.Multiple(() =>
			{
				Assert.That(Cpu.ReadData(EEDR), Is.EqualTo(0x42));
			});
		}
	}
	
	[TestFixture]
	public class Write : AvrTestBase
	{
		private EepromMemoryBackend _backend;
		private AvrEeprom _eeprom;

		protected override void SetupPeripherals()
		{
			_backend = new EepromMemoryBackend(1024);
			_eeprom = new AvrEeprom(Cpu, _backend);
		}

		[Test (Description = "Should write a byte to the given EEPROM address")]
		public void WriteValue ()
		{
			Cpu.WriteData (EEDR, 0x55);
			Cpu.WriteData (EEARL, 15);
			Cpu.WriteData (EEARH, 0);
			Cpu.WriteData (EECR, EEMPE);

			Cpu.WriteData (EECR, EEPE);
			Cpu.Tick ();

			Assert.Multiple(() =>
			{
				Assert.That(Cpu.Cycles, Is.EqualTo(2), "It should have 2 penalty cycles");
				Assert.That(_backend.ReadMemory (15), Is.EqualTo(0xff), "After 2 cycles, the backend should not have the data yet");
				Assert.That(Cpu.ReadData(EECR) & EEPE, Is.EqualTo(EEPE), "The EEPE bit should be 1 (Occupied)");
			});

			Cpu.Cycles += 60000;
			Cpu.Tick();

			Assert.Multiple(() =>
			{
				Assert.That(_backend.ReadMemory (15), Is.EqualTo(0x55), "After write completion, data should be in backend");
				Assert.That(Cpu.ReadData(EECR) & EEPE, Is.EqualTo(0), "EEPE bit should automatically return to 0");
			});
		}
		
		[Test (Description = "Should not erase the memory when writing if EEPM1 is high")]
		public void NoErase ()
		{
			var program = new AsmProgram ($@"
				  ; register addresses
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
			").Compile();
			
			Cpu.LoadProgram(program.Program);
			
			_backend.WriteMemory (9, 0x0f);
			
			var runner = new TestProgramRunner (Cpu);
			runner.RunInstructions (program.InstructionCount);

			Cpu.Cycles += 30000;
			Cpu.Tick ();
			
			Assert.That (_backend.ReadMemory (9), Is.EqualTo(0x05));
		}
		
		[Test (Description = "Should clear the EEPE bit and fire an interrupt when write has been completed")]
		public void WriteComplete ()
		{
			Cpu.WriteData (EEDR, 0x55);
			Cpu.WriteData (EEARL, 15);
			Cpu.WriteData (EEARH, 0);
			Cpu.WriteData (EECR, EEMPE);
			Cpu.Mmio.Data[SREG] = 0x80; // SREG: I-------
			Cpu.WriteData (EECR, EEPE | EERIE);
			Cpu.Cycles += 1000;
			Cpu.Tick ();
			
			// At this point, write shouldn't be complete yet
			Assert.Multiple(() =>
			{
				Assert.That(Cpu.Mmio.Data[EECR] & EEPE, Is.EqualTo(EEPE));
				Assert.That(Cpu.Pc, Is.EqualTo(0));
			});
			
			Cpu.Cycles += 10_000_000;
			
			// And now, 10 million cycles later, it should.
			Cpu.Tick ();
			
			Assert.Multiple(() =>
			{
				Assert.That(_backend.ReadMemory (15), Is.EqualTo(0x55));
				Assert.That(Cpu.ReadData(EECR) & EEPE, Is.EqualTo(0));
				Assert.That(Cpu.Pc, Is.EqualTo(0x2c)); // EEPROM Ready interrupt
			});
		}
		
		[Test (Description = "Should clear the fire an interrupt when there is a pending interrupt and the interrupt flag is enabled (issue #110)")]
		public void PendingInterrupt ()
		{
			Cpu.WriteData (EEDR, 0x55);
			Cpu.WriteData (EEARL, 15);
			Cpu.WriteData (EEARH, 0);
			Cpu.WriteData (EECR, EEMPE);
			Cpu.Mmio.Data[SREG] = 0x80; // SREG: I-------
			Cpu.WriteData (EECR, EEPE);
			Cpu.Cycles += 1000;
			Cpu.Tick ();
			
			// At this point, write shouldn't be complete yet
			Assert.Multiple(() =>
			{
				Assert.That(Cpu.ReadData(EECR) & EEPE, Is.EqualTo(EEPE));
				Assert.That(Cpu.Pc, Is.EqualTo(0));
			});
			
			Cpu.Cycles += 10_000_000;
			
			// And now, 10 million cycles later, it should.
			Cpu.Tick ();
			
			Assert.Multiple(() =>
			{
				Assert.That(_backend.ReadMemory (15), Is.EqualTo(0x55));
				Assert.That(Cpu.ReadData(EECR) & EEPE, Is.EqualTo(0));
				Cpu.WriteData (EECR, EERIE);
				Cpu.Tick ();
				Assert.That(Cpu.Pc, Is.EqualTo(0x2c)); // EEPROM Ready interrupt
			});
		}
		
		[Test (Description = "Should skip the write if EEMPE is clear")]
		public void NoWrite ()
		{
			Cpu.WriteData (EEDR, 0x55);
			Cpu.WriteData (EEARL, 15);
			Cpu.WriteData (EEARH, 0);
			Cpu.WriteData (EECR, EEPE);
			
			Cpu.Cycles += 8;
			Cpu.Tick ();
			
			Cpu.WriteData (EECR, EEPE);
			
			Cpu.Tick ();
			
			// Ensure that nothing was written, and EEPE bit is clear
			Assert.Multiple(() =>
			{
				Assert.That(_backend.ReadMemory (15), Is.EqualTo(0xff));
				Assert.That(Cpu.ReadData(EECR) & EEPE, Is.EqualTo(0));
			});
		}
		
		[Test (Description = "Should skip the write if another write is already in progress")]
		public void NoWriteInProgress ()
		{
			// Write 0x55 to address 15
			Cpu.WriteData (EEDR, 0x55);
			Cpu.WriteData (EEARL, 15);
			Cpu.WriteData (EEARH, 0);
			Cpu.WriteData (EECR, EEMPE);
			Cpu.WriteData (EECR, EEPE);
			Cpu.Tick ();
			
			Assert.That (Cpu.Cycles, Is.EqualTo(2));
			
			// Write 0x66 to address 16 (first write is still in progress)
			Cpu.WriteData (EEDR, 0x66);
			Cpu.WriteData (EEARL, 16);
			Cpu.WriteData (EEARH, 0);
			Cpu.WriteData (EECR, EEMPE);
			Cpu.WriteData (EECR, EEPE);
			Cpu.Tick ();

			Assert.That (Cpu.Cycles, Is.LessThan(10));

			// Wait long enough time for the first write to finish
			Cpu.Cycles += 6_0000;
			Cpu.Tick ();
			
			// Ensure that second write didn't happen
			Assert.Multiple(() =>
			{
				Assert.That (_backend.ReadMemory (15), Is.EqualTo(0x55));
				Assert.That (_backend.ReadMemory (16), Is.EqualTo(0xff));
				Assert.That (Cpu.ReadData(EECR) & EEPE, Is.EqualTo(0));
			});
		}
		
		[Test (Description = "Should write two bytes sucessfully")]
		public void WriteTwoBytes ()
		{
			// Write 0x55 to address 15
			Cpu.WriteData (EEDR, 0x55);
			Cpu.WriteData (EEARL, 15);
			Cpu.WriteData (EEARH, 0);
			Cpu.WriteData (EECR, EEMPE);
			Cpu.WriteData (EECR, EEPE);
			Cpu.Tick ();
			
			Assert.That (Cpu.Cycles, Is.EqualTo(2));
			
			// Wait long enough time for the first write to finish
			Cpu.Cycles += 10_000_000;
			Cpu.Tick ();
			
			// Write 0x66 to address 16
			Cpu.WriteData (EEDR, 0x66);
			Cpu.WriteData (EEARL, 16);
			Cpu.WriteData (EEARH, 0);
			Cpu.WriteData (EECR, EEMPE);
			Cpu.WriteData (EECR, EEPE);
			Cpu.Tick ();

			// Wait long enough time for the second write to finish
			Cpu.Cycles += 10_000_000;
			Cpu.Tick ();
			
			// Ensure both writes took place
			Assert.Multiple(() =>
			{
				Assert.That (Cpu.Cycles, Is.EqualTo(20_000_004));
				Assert.That (_backend.ReadMemory (15), Is.EqualTo(0x55));
				Assert.That (_backend.ReadMemory (16), Is.EqualTo(0x66));
			});
		}
	}

	[Test (Description = "EEPM=11 (reserved mode) must not write or erase — treated as no-op")]
	public void EepmMode3_DoesNotWrite ()
	{
		_backend.WriteMemory (5, 0xAB); // seed a known value

		// Set EEPM=11 (both EEPM0 and EEPM1), EEMPE, then EEPE
		// EECR bits: EEPM0=bit4, EEPM1=bit5, EEMPE=bit2, EEPE=bit1
		Cpu.WriteData (EEARL, 5);
		Cpu.WriteData (EEDR, 0xFF);
		Cpu.WriteData (EECR, (byte)(EEPM0 | EEPM1 | EEMPE));
		// Trigger EEPE — passes through the write hook on EECR
		Cpu.WriteData ((ushort)(EECR - 0x20), (byte)(EEPM0 | EEPM1 | EEMPE | EEPE));

		Assert.That (_backend.ReadMemory (5), Is.EqualTo (0xAB),
			"EEPM=11 is reserved; memory must remain unchanged");
	}

	[Test(Description = "Should only erase the memory when EEPM0 is high")]
	public void Erase()
	{
		var program = new AsmProgram ($@"
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
			").Compile();

		Cpu.LoadProgram(program.Program);

		_backend.WriteMemory (9, 0x22);

		var runner = new TestProgramRunner (Cpu);
		runner.RunInstructions (program.InstructionCount);

		Cpu.Cycles += 30000;
		Cpu.Tick ();

		Assert.That (_backend.ReadMemory (9), Is.EqualTo(0xff));
	}

	[Test(Description = "Latching: Changing EEAR or EEDR while EEPE is high should not affect the current write")]
	public void WriteLatching_RegistersCanChangeDuringWrite()
	{
		Cpu.WriteData(EEARL, 10);
		Cpu.WriteData(EEDR, 0xAA);
		Cpu.WriteData(EECR, (byte)EEMPE);
		Cpu.WriteData(EECR, (byte)EEPE);

		Cpu.WriteData(EEARL, 20);
		Cpu.WriteData(EEDR, 0xBB);

		Cpu.Cycles += 100_000;
		Cpu.Tick();

		Assert.Multiple(() =>
		{
			Assert.That(_backend.ReadMemory(10), Is.EqualTo(0xAA), "The original value should be preserved");
			Assert.That(_backend.ReadMemory(20), Is.EqualTo(0xFF), "The new address should not be affected");
		});
	}

	[Test(Description = "Safety: Setting EEPE more than 4 cycles after EEMPE should not trigger a write")]
	public void EempeTimeout_ShouldFailAfterFourCycles()
	{
		Cpu.WriteData(EEDR, 0x55);
		Cpu.WriteData(EEARL, 15);

		Cpu.WriteData(EECR, (byte)EEMPE);

		Cpu.Cycles += 5;

		Cpu.WriteData(EECR, (byte)EEPE);
		Cpu.Tick();

		Assert.Multiple(() =>
		{
			Assert.That(_backend.ReadMemory(15), Is.EqualTo(0xFF), "The writing should have failed because of EEMPE timeout");
			Assert.That(Cpu.ReadData(EECR) & EEPE, Is.Zero, "The EEPE bit should be 0");
		});
	}

	[Test(Description = "Timing: Reading EEPROM must halt the CPU for 4 clock cycles")]
	public void ReadHaltCycles_ShouldHaltCpuForFourCycles()
	{
		var cyclesBefore = Cpu.Cycles;

		Cpu.WriteData(EEARL, 0);
		Cpu.WriteData(EECR, (byte)EERE);

		Assert.That(Cpu.Cycles - cyclesBefore, Is.GreaterThanOrEqualTo(4));
	}

	[TestFixture]
	public class Reset : AvrTestBase
	{
		private EepromMemoryBackend _backend;
		private AvrEeprom _eeprom;

		protected override void SetupPeripherals()
		{
			_backend = new EepromMemoryBackend(1024);
			_eeprom = new AvrEeprom(Cpu, _backend);
		}

		[Test(Description = "After CPU reset during a write-in-progress, EEPE must be clear and a new write must succeed")]
		public void WriteInProgress_ResetClearsEepeAndAllowsNewWrite()
		{
			// Start a write to address 5
			Cpu.WriteData(EEARL, 5);
			Cpu.WriteData(EEDR, 0xAA);
			Cpu.WriteData(EECR, (byte)EEMPE);
			Cpu.WriteData(EECR, (byte)EEPE);

			// Sanity: write should be in progress
			Assert.That(Cpu.ReadData(EECR) & EEPE, Is.EqualTo(EEPE), "EEPE must be set before reset");

			// Simulate a CPU reset (e.g. watchdog)
			Cpu.Reset();

			// EEPE must be cleared after reset
			Assert.That(Cpu.ReadData(EECR) & EEPE, Is.Zero, "EEPE must be cleared by reset");

			// A fresh write to a different address must succeed without stale _writeCompleteCycles blocking it
			Cpu.WriteData(EEARL, 10);
			Cpu.WriteData(EEDR, 0x55);
			Cpu.WriteData(EECR, (byte)EEMPE);
			Cpu.WriteData(EECR, (byte)EEPE);

			Cpu.Cycles += 60_000;
			Cpu.Tick();

			Assert.Multiple(() =>
			{
				Assert.That(_backend.ReadMemory(10), Is.EqualTo(0x55), "New write after reset must complete normally");
				Assert.That(Cpu.ReadData(EECR) & EEPE, Is.Zero, "EEPE must be clear after the new write completes");
			});
		}

		[Test(Description = "After CPU reset, the EEMPE write-enable window must be invalidated")]
		public void WriteEnableWindow_ResetInvalidatesWindow()
		{
			// Set EEMPE to open the write window
			Cpu.WriteData(EECR, (byte)EEMPE);

			// Reset fires while the 4-cycle window is still open
			Cpu.Reset();

			// Attempt to write using EEPE only (no EEMPE set after reset)
			Cpu.WriteData(EEARL, 7);
			Cpu.WriteData(EEDR, 0x77);
			Cpu.WriteData(EECR, (byte)EEPE);
			Cpu.Tick();

			Assert.Multiple(() =>
			{
				Assert.That(_backend.ReadMemory(7), Is.EqualTo(0xFF), "Write must be rejected: EEMPE window was invalidated by reset");
				Assert.That(Cpu.ReadData(EECR) & EEPE, Is.Zero, "EEPE must be automatically cleared after rejected write");
			});
		}
	}
}
