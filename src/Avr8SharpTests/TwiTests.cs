using AVR8Sharp.Peripherals;
namespace AVr8SharpTests;

[TestFixture]
public class Twi
{
	const int FREQ_16MHZ = 16_000_000;

	// CPU registers
	const int R16 = 16;
	const int R17 = 17;
	const int SREG = 95;

	// TWI Registers
	const int TWBR = 0xb8;
	const int TWSR = 0xb9;
	const int TWDR = 0xbb;
	const int TWCR = 0xbc;

	// Register bit names
	const int TWIE = 1;
	const int TWEN = 4;
	const int TWSTO = 0x10;
	const int TWSTA = 0x20;
	const int TWEA = 0x40;
	const int TWINT = 0x80;

	[Test (Description = "Should correctly calculate the sclFrequency from TWBR")]
	public void SclFrequency ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var twi = new AvrTwi (cpu, AvrTwi.TwiConfig, FREQ_16MHZ);
		
		cpu.WriteData(TWBR, 0x48);
		cpu.WriteData(TWSR, 0); // prescaler: 1
		
		Assert.That (twi.SclFrequency, Is.EqualTo(100_000));
	}
	
	[Test (Description = "Should take the prescaler into consideration when calculating sclFrequenc")]
	public void SclFrequencyPrescaler ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var twi = new AvrTwi (cpu, AvrTwi.TwiConfig, FREQ_16MHZ);
		
		cpu.WriteData(TWBR, 0x03);
		cpu.WriteData(TWSR, 0x01); // prescaler: 4
		
		Assert.That (twi.SclFrequency, Is.EqualTo(400_000));
	}
	
	[Test (Description = "Should trigger data an interrupt if TWINT is set")]
	public void Twint ()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
		var twi = new AvrTwi (cpu, AvrTwi.TwiConfig, FREQ_16MHZ);
		
		cpu.WriteData(TWCR, TWIE);
		cpu.Data [SREG] = 0x80; // SREG: I-------
		
		twi.CompleteStart (); // This will set the TWINT flag
		
		cpu.Tick ();
		
		Assert.Multiple(() =>
		{
			Assert.That (cpu.PC, Is.EqualTo(0x30)); // 2-wire Serial Interface Vector
			Assert.That (cpu.Cycles, Is.EqualTo(2));
			Assert.That ((cpu.ReadData(TWCR) & TWINT), Is.EqualTo(0));
		});
	}

	[TestFixture]
	public class MasterMode
	{
		[Test (Description = "Should call the startEvent handler when TWSTA bit is written 1")]
		public void StartCondition ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var twi = new AvrTwi (cpu, AvrTwi.TwiConfig, FREQ_16MHZ);
			
			// Create a mock event handler
			twi.EventHandler = new MockTwiEventHandler {
				StartPredicate = (repeated) => {
					Assert.That(repeated, Is.False);
				}
			};
			
			cpu.WriteData(TWCR, TWINT | TWSTA | TWEN);
			cpu.Cycles++;
			cpu.Tick();
			
		}
		
		[Test (Description = "Should send a stop condition when TWSTO is set")]
		public void StopCondition ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu (new ushort[1024]);
			var twi = new AvrTwi (cpu, AvrTwi.TwiConfig, FREQ_16MHZ);
			
			// Start condition
			cpu.WriteData(TWCR, TWINT | TWSTA | TWEN);
			cpu.Cycles++;
			cpu.Tick();
			
			// Repeated start
			twi.EventHandler = new MockTwiEventHandler {
				StartPredicate = (repeated) => {
					Assert.That(repeated, Is.True);
				}
			};
			
			cpu.WriteData(TWCR, TWINT | TWSTA | TWEN);
			cpu.Cycles++;
			cpu.Tick();
			
			// Now try to connect...
			twi.EventHandler = new MockTwiEventHandler {
				ConnectToSlavePredicate = (address, read) => {
					Assert.That(address, Is.EqualTo(0x40));
					Assert.That(read, Is.True);
				}
			};
			
			cpu.WriteData(TWDR, 0x80); // Address 0x40, write mode
			cpu.WriteData(TWCR, TWINT | TWEN);
			cpu.Cycles++;
			cpu.Tick();
		}
		
		[Test (Description = "Should successfully transmit a byte to a slave")]
		public void TransmitByte ()
		{
			// based on the example in page 225 of the datasheet:
			// https://ww1.microchip.com/downloads/en/DeviceDoc/ATmega48A-PA-88A-PA-168A-PA-328-P-DS-DS40002061A.pdf
			var program = Utils.AsmProgram (@$"
        ; register addresses
        _REPLACE TWSR, {TWSR}
        _REPLACE TWDR, {TWDR}
        _REPLACE TWCR, {TWCR}

        ; TWCR bits
        _REPLACE TWEN, {TWEN}
        _REPLACE TWSTO, {TWSTO}
        _REPLACE TWSTA, {TWSTA}
        _REPLACE TWINT, {TWINT}

        ; TWSR states
        _REPLACE START, 0x8         ; TWI start
        _REPLACE MT_SLA_ACK, 0x18   ; Slave Adresss ACK has been received
        _REPLACE MT_DATA_ACK, 0x28  ; Data ACK has been received

        ; Send start condition
        ldi r16, TWEN
        sbr r16, TWSTA
        sbr r16, TWINT
        sts TWCR, r16

        ; Wait for TWINT Flag set. This indicates that the START condition has been transmitted
        call wait_for_twint
        
        ; Check value of TWI Status Register. Mask prescaler bits. If status different from START go to ERROR
        lds r16, TWSR
        andi r16, 0xf8
        cpi r16, START
        brne error

        ; Load SLA_W into TWDR Register. Clear TWINT bit in TWCR to start transmission of address
        ; 0x44 = Address 0x22, write mode (R/W bit clear)
        _REPLACE SLA_W, 0x44
        ldi r16, SLA_W
        sts TWDR, r16
        ldi r16, TWINT
        sbr r16, TWEN
        sts TWCR, r16

        ; Wait for TWINT Flag set. This indicates that the SLA+W has been transmitted, and ACK/NACK has been received.
        call wait_for_twint

        ; Check value of TWI Status Register. Mask prescaler bits. If status different from MT_SLA_ACK go to ERROR
        lds r16, TWSR
        andi r16, 0xf8
        cpi r16, MT_SLA_ACK
        brne error

        ; Load DATA into TWDR Register. Clear TWINT bit in TWCR to start transmission of data
        _replace DATA, 0x55
        ldi r16, DATA
        sts TWDR, r16
        ldi r16, TWINT
        sbr r16, TWEN
        sts TWCR, r16

        ; Wait for TWINT Flag set. This indicates that the DATA has been transmitted, and ACK/NACK has been received
        call wait_for_twint

        ; Check value of TWI Status Register. Mask prescaler bits. If status different from MT_DATA_ACK go to ERROR
        lds r16, TWSR
        andi r16, 0xf8
        cpi r16, MT_DATA_ACK
        brne error

        ; Transmit STOP condition
        ldi r16, TWINT
        sbr r16, TWEN
        sbr r16, TWSTO
        sts TWCR, r16

        ; Wait for TWINT Flag set. This indicates that the STOP condition has been sent
        call wait_for_twint

        ; Check value of TWI Status Register. The masked value should be 0xf8 once done
        lds r16, TWSR
        andi r16, 0xf8
        cpi r16, 0xf8
        brne error

        ; Indicate success by loading 0x42 into r17
        ldi r17, 0x42

        loop:
        jmp loop

        ; Busy-waits for the TWINT flag to be set
        wait_for_twint:
        lds r16, TWCR
        andi r16, TWINT
        breq wait_for_twint
        ret

        ; In case of an error, toggle a breakpoint
        error:
        break
");
			
			var cpu = new AVR8Sharp.Cpu.Cpu (program.Program);
			var twi = new AvrTwi (cpu, AvrTwi.TwiConfig, FREQ_16MHZ);
			var runner = new TestProgramRunner (cpu, (cpu) => {
				// Do nothing on break
			});

			var startResult = false;
			var connectToSlaveResult = false;
			var firstByteResult = false;
			var stopCalled = false;

			var eventHandler = new MockTwiEventHandler () {
				StartPredicate = (repeated) => {
					startResult = !repeated;
				},
				ConnectToSlavePredicate = (address, read) => {
					connectToSlaveResult = address == 0x22 && read;
				},
				WriteBytePredicate = (data) => {
					firstByteResult = data == 0x55;
				},
				StopPredicate = () => {
					stopCalled = true;
				}
			};
			
			twi.EventHandler = eventHandler;
			
			// Step 1: wait for start condition
			runner.RunInstructions(4);
			Assert.IsTrue (startResult);
			
			runner.RunInstructions (16);
			twi.CompleteStart();
			
			// Step 2: wait for slave connect in write mode
			runner.RunInstructions(16);
			Assert.IsTrue (connectToSlaveResult);
			
			runner.RunInstructions(16);
			twi.CompleteConnect(0x22, true);
			
			// Step 3: wait for first data byte
			runner.RunInstructions(16);
			Assert.IsTrue (firstByteResult);
			
			runner.RunInstructions(16);
			twi.CompleteWrite(1);
			
			// Step 4: wait for stop condition
			runner.RunInstructions(16);
			Assert.IsTrue (stopCalled);
			
			runner.RunInstructions(16);
			twi.CompleteStop();
			
			// Step 5: wait for the assembly code to indicate success by settings r17 to 0x42
			runner.RunInstructions(16);
			Assert.That(cpu.ReadData(R17), Is.EqualTo(0x42));
		}

		[Test (Description = "Should successfully receive a byte from a slave")]
		public void ReceiveByte ()
		{
			var program = Utils.AsmProgram (@$"
        ; register addresses
        _REPLACE TWSR, {TWSR}
        _REPLACE TWDR, {TWDR}
        _REPLACE TWCR, {TWCR}
        
        ; TWCR bits
        _REPLACE TWEN, {TWEN}
        _REPLACE TWSTO, {TWSTO}
        _REPLACE TWSTA, {TWSTA}
        _REPLACE TWEA, {TWEA}
        _REPLACE TWINT, {TWINT}

        ; TWSR states
        _REPLACE START, 0x8         ; TWI start
        _REPLACE MT_SLAR_ACK, 0x40  ; Slave Adresss ACK has been received
        _REPLACE MT_DATA_RECV, 0x50 ; Data has been received
        _REPLACE MT_DATA_RECV_NACK, 0x58 ; Data has been received, NACK has been returned

        ; Send start condition
        ldi r16, TWEN
        sbr r16, TWSTA
        sbr r16, TWINT
        sts TWCR, r16

        ; Wait for TWINT Flag set. This indicates that the START condition has been transmitted
        call wait_for_twint
        
        ; Check value of TWI Status Register. Mask prescaler bits. If status different from START go to ERROR
        lds r16, TWSR
        andi r16, 0xf8
        ldi r18, START
        cpse r16, r18
        jmp error   ; only jump if r16 != r18 (START)

        ; Load SLA_R into TWDR Register. Clear TWINT bit in TWCR to start transmission of address
        ; 0xa1 = Address 0x50, read mode (R/W bit set)
        _REPLACE SLA_R, 0xa1
        ldi r16, SLA_R
        sts TWDR, r16
        ldi r16, TWINT
        sbr r16, TWEN
        sts TWCR, r16

        ; Wait for TWINT Flag set. This indicates that the SLA+W has been transmitted, and ACK/NACK has been received.
        call wait_for_twint

        ; Check value of TWI Status Register. Mask prescaler bits. If status different from MT_SLA_ACK go to ERROR
        lds r16, TWSR
        andi r16, 0xf8
        cpi r16, MT_SLAR_ACK
        brne error

        ; Clear TWINT bit in TWCR to receive the next byte, set TWEA to send ACK
        ldi r16, TWINT
        sbr r16, TWEA
        sbr r16, TWEN
        sts TWCR, r16

        ; Wait for TWINT Flag set. This indicates that the DATA has been received, and ACK has been transmitted
        call wait_for_twint

        ; Check value of TWI Status Register. Mask prescaler bits. If status different from MT_DATA_RECV go to ERROR
        lds r16, TWSR
        andi r16, 0xf8
        cpi r16, MT_DATA_RECV
        brne error

        ; Validate that we recieved the desired data - first byte should be 0x66
        lds r16, TWDR
        cpi r16, 0x66
        brne error

        ; Clear TWINT bit in TWCR to receive the next byte, this time we don't ACK
        ldi r16, TWINT
        sbr r16, TWEN
        sts TWCR, r16

        ; Wait for TWINT Flag set. This indicates that the DATA has been received, and NACK has been transmitted
        call wait_for_twint

        ; Check value of TWI Status Register. Mask prescaler bits. If status different from MT_DATA_RECV_NACK go to ERROR
        lds r16, TWSR
        andi r16, 0xf8
        cpi r16, MT_DATA_RECV_NACK
        brne error

        ; Validate that we recieved the desired data - second byte should be 0x77
        lds r16, TWDR
        cpi r16, 0x77
        brne error

        ; Transmit STOP condition
        ldi r16, TWINT
        sbr r16, TWEN
        sbr r16, TWSTO
        sts TWCR, r16

        ; Wait for TWINT Flag set. This indicates that the STOP condition has been sent
        call wait_for_twint

        ; Check value of TWI Status Register. The masked value should be 0xf8 once done
        lds r16, TWSR
        andi r16, 0xf8
        cpi r16, 0xf8
        brne error

        ; Indicate success by loading 0x42 into r17
        ldi r17, 0x42

        loop:
        jmp loop

        ; Busy-waits for the TWINT flag to be set
        wait_for_twint:
        lds r16, TWCR
        andi r16, TWINT
        breq wait_for_twint
        ret

        ; In case of an error, toggle a breakpoint
        error:
        break
");
			var cpu = new AVR8Sharp.Cpu.Cpu (program.Program);
			var twi = new AvrTwi (cpu, AvrTwi.TwiConfig, FREQ_16MHZ);
			var runner = new TestProgramRunner (cpu, (cpu) => {
				// Do nothing on break
			});
			var readCount = 0;
			
			var startResult = false;
			var connectToSlaveResult = false;
			var readByteResult = false;
			var stopCalled = false;
			
			var eventHandler = new MockTwiEventHandler () {
				StartPredicate = (repeated) => {
					startResult = !repeated;
				},
				ConnectToSlavePredicate = (address, write) => {
					connectToSlaveResult = address == 0x50 && !write;
				},
				ReadBytePredicate = (ack) => {
					readByteResult = readCount == 0 ? ack : !ack;
					readCount++;
				},
				WriteBytePredicate = (data) => {
					Assert.That(data, Is.EqualTo(0x66));
				},
				StopPredicate = () => {
					stopCalled = true;
				}
			};
			
			twi.EventHandler = eventHandler;
			
			// Step 1: wait for start condition
			runner.RunInstructions(4);
			Assert.That(startResult, Is.True);
			
			runner.RunInstructions (16);
			twi.CompleteStart();
			
			// Step 2: wait for slave connect in read mode
			runner.RunInstructions(16);
			Assert.That(connectToSlaveResult, Is.True);
			
			runner.RunInstructions(16);
			twi.CompleteConnect(0x50, true);
			
			// Step 3: send the first byte to the master, expect ack
			runner.RunInstructions(16);
			Assert.That(readByteResult, Is.True);
			
			runner.RunInstructions(16);
			twi.CompleteRead(0x66);
			
			// Step 4: send the second byte to the master, expect nack
			runner.RunInstructions(16);
			Assert.That(readByteResult, Is.True);
			
			runner.RunInstructions(16);
			twi.CompleteRead(0x77);
			
			// Step 5: wait for stop condition
			runner.RunInstructions(24);
			Assert.That(stopCalled, Is.True);
			
			runner.RunInstructions(16);
			twi.CompleteStop();
			
			// Step 6: wait for the assembly code to indicate success by settings r17 to 0x42
			runner.RunInstructions(16);
			Assert.That(cpu.Data[R17], Is.EqualTo(0x42));
		}

		internal class MockTwiEventHandler : ITwiEventHandler
		{
			public Action<byte, bool> ConnectToSlavePredicate { get; set; }
			public Action<bool> StartPredicate { get; set; }
			public Action StopPredicate { get; set; }
			public Action<byte> WriteBytePredicate { get; set; }
			public Action<bool> ReadBytePredicate { get; set; }
			public void ConnectToSlave (byte address, bool write)
			{
				ConnectToSlavePredicate?.Invoke (address, write);
			}

			public void Start (bool repeated)
			{
				StartPredicate?.Invoke (repeated);
			}

			public void Stop ()
			{
				StopPredicate?.Invoke ();
			}

			public void WriteByte (byte data)
			{
				WriteBytePredicate?.Invoke (data);
			}

			public void ReadByte (bool ack)
			{
				ReadBytePredicate?.Invoke (ack);
			}
		}
	}
}

