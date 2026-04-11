using AVR8Sharp.Core.Peripherals;
using Avr8Sharp.Tests.Utils;
using Moq;

namespace Avr8Sharp.Tests;

[TestFixture]
public class Twi : AvrTestBase
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

    private AvrTwi _twi;

    protected override void SetupPeripherals()
    {
        _twi = new AvrTwi(Cpu, AvrTwi.TwiConfig, FREQ_16MHZ);
    }

    [Test]
    public void ShouldCalculateSclFrequencyFromTwbr()
    {
        Cpu.WriteData(TWBR, 0x48);
        Cpu.WriteData(TWSR, 0); // prescaler: 1

        Assert.That(_twi.SclFrequency, Is.EqualTo(100_000));
    }

    [Test]
    public void ShouldCalculateSclFrequencyUsingPrescaler()
    {
        Cpu.WriteData(TWBR, 0x03);
        Cpu.WriteData(TWSR, 0x01); // prescaler: 4

        Assert.That(_twi.SclFrequency, Is.EqualTo(400_000));
    }

    [Test]
    public void ShouldTriggerInterruptWhenTwintIsSet()
    {
        Cpu.WriteData(TWCR, TWIE);
        Cpu.Mmio.Data[SREG] = 0x80; // SREG: I-------

        _twi.CompleteStart(); // This will set the TWINT flag

        Cpu.Tick();

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Pc, Is.EqualTo(0x30)); // 2-wire Serial Interface Vector
            Assert.That(Cpu.Cycles, Is.EqualTo(3)); // 3 cycles from DoAvrInterrupt (4 total incl. instruction)
            Assert.That(Cpu.ReadData(TWCR) & TWINT, Is.EqualTo(0));
        });
    }

    [TestFixture]
    public class MasterMode : AvrTestBase
    {
        private AvrTwi _twi;
        private Mock<ITwiEventHandler> _mockTwiEventHandler;

        protected override void SetupPeripherals()
        {
            _twi = new AvrTwi(Cpu, AvrTwi.TwiConfig, FREQ_16MHZ);

            _mockTwiEventHandler = new Mock<ITwiEventHandler>();

            _mockTwiEventHandler
                .Setup(t => t.Start(It.IsAny<bool>()))
                .Callback<bool>(_ => _twi.CompleteStart());

            _mockTwiEventHandler
                .Setup(m => m.Start(It.IsAny<bool>()))
                .Callback<bool>(_ => _twi.CompleteStart());

            _mockTwiEventHandler
                .Setup(m => m.Stop())
                .Callback(() => _twi.CompleteStop());

            _mockTwiEventHandler
                .Setup(m => m.ConnectToSlave(It.IsAny<byte>(), It.IsAny<bool>()))
                .Callback<byte, bool>((_, _) => _twi.CompleteConnect(false));

            _mockTwiEventHandler
                .Setup(m => m.WriteByte(It.IsAny<byte>()))
                .Callback<byte>(_ => _twi.CompleteWrite(false));

            _mockTwiEventHandler
                .Setup(m => m.ReadByte(It.IsAny<bool>()))
                .Callback<bool>(_ => _twi.CompleteRead(0xff));

            _twi.EventHandler = _mockTwiEventHandler.Object;
        }

        [Test]
        public void ShouldCallStartEventWhenTwstaIsSet()
        {
            // Act
            Cpu.WriteData(TWCR, TWINT | TWSTA | TWEN);
            Cpu.Cycles++;
            Cpu.Tick();

            // Assert
            _mockTwiEventHandler.Verify(t => t.Start(false), Times.Once);
        }

        [Test]
        public void ShouldConnectSuccessfullyInCaseOfRepeatedStart()
        {
            // Start condition
            Cpu.WriteData(TWCR, TWINT | TWSTA | TWEN);
            Cpu.Cycles++;
            Cpu.Tick();

            _mockTwiEventHandler.Verify(t => t.Start(false), Times.Once);

            // Repeated start
            Cpu.WriteData(TWCR, TWINT | TWSTA | TWEN);
            Cpu.Cycles++;
            Cpu.Tick();

            _mockTwiEventHandler.Verify(t => t.Start(true), Times.Once);

            // Now try to connect...
            Cpu.WriteData(TWDR, 0x80); // Address 0x40, write mode
            Cpu.WriteData(TWCR, TWINT | TWEN);
            Cpu.Cycles++;
            Cpu.Tick();

            _mockTwiEventHandler.Verify(t => t.ConnectToSlave(0x40, true), Times.Once);
        }

        [Test]
        public void ShouldSendStopConditionWhenTwstoIsSet()
        {
            // Send Start
            Cpu.WriteData(TWCR, TWINT | TWSTA | TWEN);
            Cpu.Cycles++;
            Cpu.Tick();

            // Send Stop
            Cpu.WriteData(TWCR, TWINT | TWSTO | TWEN);
            Cpu.Cycles++;
            Cpu.Tick();

            // Assert
            _mockTwiEventHandler.Verify(t => t.Stop(), Times.Once);
        }

        [Test]
        public void ShouldSuccessfullyTransmitByteToSlave()
        {
            // based on the example in page 225 of the datasheet:
            // https://ww1.microchip.com/downloads/en/DeviceDoc/ATmega48A-PA-88A-PA-168A-PA-328-P-DS-DS40002061A.pdf
            var program = new AsmProgram(@$"
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
").Compile();
            Cpu.LoadProgram(program.Program);
            var runner = new TestProgramRunner(Cpu, _ => { });
            _mockTwiEventHandler.Reset();

            _mockTwiEventHandler
                .Setup(t => t.Start(false))
                .Callback<bool>(_ => _twi.CompleteStart());

            _mockTwiEventHandler
                .Setup(t => t.ConnectToSlave(0x22, true))
                .Callback<byte, bool>((_, _) => _twi.CompleteConnect(true));

            _mockTwiEventHandler
                .Setup(t => t.WriteByte(0x55))
                .Callback<byte>(_ => _twi.CompleteWrite(true));

            _mockTwiEventHandler
                .Setup(t => t.Stop())
                .Callback(() => _twi.CompleteStop());

            // Step 1: wait for start condition
            runner.RunInstructions(4);
            _mockTwiEventHandler.Verify(t => t.Start(false), Times.Once);

            runner.RunInstructions(16);

            // Step 2: wait for slave connect in write mode
            runner.RunInstructions(16);
            _mockTwiEventHandler.Verify(t => t.ConnectToSlave(0x22, true), Times.Once);

            runner.RunInstructions(16);

            // Step 3: wait for first data byte
            runner.RunInstructions(16);
            _mockTwiEventHandler.Verify(t => t.WriteByte(0x55), Times.Once);

            runner.RunInstructions(16);

            // Step 4: wait for stop condition
            runner.RunInstructions(16);
            _mockTwiEventHandler.Verify(t => t.Stop(), Times.Once);

            runner.RunInstructions(16);

            // Step 5: wait for the assembly code to indicate success by settings r17 to 0x42
            runner.RunInstructions(16);
            Assert.That(Cpu.ReadData(R17), Is.EqualTo(0x42));
        }

        [Test]
        public void ShouldSuccessfullyReceiveByteFromSlave()
        {
            var program = new AsmProgram(@$"
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
").Compile();
            Cpu.LoadProgram(program.Program);
            var runner = new TestProgramRunner(Cpu, _ => { });

            _mockTwiEventHandler
                .Setup(t => t.ConnectToSlave(0x50, false))
                .Callback<byte, bool>((_, _) => _twi.CompleteConnect(true));

            _mockTwiEventHandler
                .Setup(t => t.ReadByte(true))
                .Callback<bool>(_ => _twi.CompleteRead(0x66));

            _mockTwiEventHandler
                .Setup(t => t.ReadByte(false))
                .Callback<bool>(_ => _twi.CompleteRead(0x77));

            // Step 1: wait for start condition
            runner.RunInstructions(4);
            _mockTwiEventHandler.Verify(t => t.Start(false), Times.Once);

            runner.RunInstructions(16);

            // Step 2: wait for slave connect in read mode
            runner.RunInstructions(16);
            _mockTwiEventHandler.Verify(t => t.ConnectToSlave(0x50, false), Times.Once);

            runner.RunInstructions(16);

            // Step 3: send the first byte to the master, expect ack
            runner.RunInstructions(16);
            _mockTwiEventHandler.Verify(t => t.ReadByte(true), Times.Once);

            runner.RunInstructions(16);

            // Step 4: send the second byte to the master, expect nack
            runner.RunInstructions(16);
            _mockTwiEventHandler.Verify(t => t.ReadByte(false), Times.Once);

            runner.RunInstructions(16);

            // Step 5: wait for stop condition
            runner.RunInstructions(24);
            _mockTwiEventHandler.Verify(t => t.Stop(), Times.Once);

            runner.RunInstructions(16);

            // Step 6: wait for the assembly code to indicate success by settings r17 to 0x42
            runner.RunInstructions(16);
            Assert.That(Cpu.Mmio.Data[R17], Is.EqualTo(0x42));
        }
    }

    [TestFixture]
    public class SlaveMode : AvrTestBase
    {
        const int FREQ_16MHZ = 16_000_000;
        const int TWSR = 0xb9;
        const int TWAR = 0xba;
        const int TWDR = 0xbb;
        const int TWCR = 0xbc;
        const int TWAMR = 0xbd;
        const int SREG = 95;

        const int TWIE = 1;
        const int TWEN = 4;
        const int TWEA = 0x40;
        const int TWINT = 0x80;

        const int STATUS_SLAVE_SLAW_ACK = 0x60;
        const int STATUS_SLAVE_GCALL_ACK = 0x70;
        const int STATUS_SLAVE_DATA_RX_ACK = 0x80;
        const int STATUS_SLAVE_DATA_RX_NACK = 0x88;
        const int STATUS_SLAVE_SLAR_ACK = 0xA8;
        const int TWSR_TWS_MASK = 0xf8;

        private AvrTwi _twi;
        private Mock<ITwiEventHandler> _mockTwiEventHandler;

        protected override void SetupPeripherals()
        {
            _twi = new AvrTwi(Cpu, AvrTwi.TwiConfig, FREQ_16MHZ);

            _mockTwiEventHandler = new Mock<ITwiEventHandler>();

            _mockTwiEventHandler
                .Setup(t => t.Start(It.IsAny<bool>()))
                .Callback<bool>(_ => _twi.CompleteStart());

            _mockTwiEventHandler
                .Setup(m => m.Start(It.IsAny<bool>()))
                .Callback<bool>(_ => _twi.CompleteStart());

            _mockTwiEventHandler
                .Setup(m => m.Stop())
                .Callback(() => _twi.CompleteStop());

            _mockTwiEventHandler
                .Setup(m => m.ConnectToSlave(It.IsAny<byte>(), It.IsAny<bool>()))
                .Callback<byte, bool>((_, _) => _twi.CompleteConnect(false));

            _mockTwiEventHandler
                .Setup(m => m.WriteByte(It.IsAny<byte>()))
                .Callback<byte>(_ => _twi.CompleteWrite(false));

            _mockTwiEventHandler
                .Setup(m => m.ReadByte(It.IsAny<bool>()))
                .Callback<bool>(_ => _twi.CompleteRead(0xff));

            _twi.EventHandler = _mockTwiEventHandler.Object;
        }

        [Test(Description = "Address match: TWINT is set and TWSR=0x60 when SLA+W matches TWAR")]
        public void SlaveAddressMatch_SetsInterrupt()
        {
            // Own address = 0x48, written into TWAR bits 7:1
            Cpu.Mmio.Data[TWAR] = 0x48 << 1;

            // Clear TWINT left from construction
            Cpu.WriteData(TWCR, TWEN | TWINT);

            var matched = _twi.SimulateIncomingAddress(0x48, isWrite: true);

            Assert.Multiple(() =>
            {
                Assert.That(matched, Is.True, "SimulateIncomingAddress must return true on match");
                Assert.That(Cpu.Mmio.Data[TWSR] & TWSR_TWS_MASK, Is.EqualTo(STATUS_SLAVE_SLAW_ACK),
                    "TWSR must be 0x60 after SLA+W match");
                Assert.That(Cpu.Mmio.Data[TWCR] & TWINT, Is.EqualTo(TWINT),
                    "TWINT must be set after address match");
            });
        }

        [Test(Description = "Address mismatch: TWINT stays clear when address does not match TWAR")]
        public void SlaveAddressNoMatch_NoInterrupt()
        {
            Cpu.Mmio.Data[TWAR] = (byte)(0x48 << 1);

            // Clear TWINT
            Cpu.WriteData((ushort)TWCR, (byte)(TWEN | TWINT));

            var matched = _twi.SimulateIncomingAddress(0x50, isWrite: true);

            Assert.Multiple(() =>
            {
                Assert.That(matched, Is.False, "SimulateIncomingAddress must return false on mismatch");
                Assert.That(Cpu.Mmio.Data[TWCR] & TWINT, Is.Zero,
                    "TWINT must not be set when address does not match");
            });
        }

        [Test(Description = "Slave receive: SimulateIncomingData stores byte in TWDR and raises TWINT")]
        public void SlaveReceive_DeliversByte()
        {
            Cpu.Mmio.Data[TWAR] = (byte)(0x48 << 1);
            Cpu.WriteData((ushort)TWCR, (byte)(TWEN | TWEA | TWINT));

            _twi.SimulateIncomingAddress(0x48, isWrite: true);
            // Clear TWINT before data phase
            Cpu.WriteData((ushort)TWCR, (byte)(TWEN | TWEA | TWINT));
            _twi.SimulateIncomingData(0x5A);

            Assert.Multiple(() =>
            {
                Assert.That(Cpu.Mmio.Data[TWDR], Is.EqualTo(0x5A), "TWDR must hold the received byte");
                Assert.That(Cpu.Mmio.Data[TWSR] & TWSR_TWS_MASK, Is.EqualTo(STATUS_SLAVE_DATA_RX_ACK),
                    "TWSR must be 0x80 (data received, ACK)");
                Assert.That(Cpu.Mmio.Data[TWCR] & TWINT, Is.EqualTo(TWINT), "TWINT must be set");
            });
        }

        [Test(Description = "General call: TWINT is set and TWSR=0x70 when TWAR bit0=1 and address=0x00")]
        public void GeneralCall_WhenEnabled()
        {
            // Own address 0x48, general call enable (bit 0)
            Cpu.Mmio.Data[TWAR] = (byte)((0x48 << 1) | 0x01);
            Cpu.WriteData((ushort)TWCR, (byte)(TWEN | TWINT));

            var matched = _twi.SimulateIncomingAddress(0x00, isWrite: true);

            Assert.Multiple(() =>
            {
                Assert.That(matched, Is.True, "General call must match when TWGCE=1");
                Assert.That(Cpu.Mmio.Data[TWSR] & TWSR_TWS_MASK, Is.EqualTo(STATUS_SLAVE_GCALL_ACK),
                    "TWSR must be 0x70 for general call");
            });
        }

        [Test(Description = "TWAMR: masked address bits are ignored during address comparison")]
        public void TWAMR_MasksAddressBits()
        {
            // Own address = 0x48 (0b1001000), mask upper bit so 0x48 and 0x08 both match
            Cpu.Mmio.Data[TWAR] = (byte)(0x48 << 1);
            Cpu.Mmio.Data[TWAMR] = (byte)(0x40 << 1); // mask bit 6 of address

            Cpu.WriteData((ushort)TWCR, (byte)(TWEN | TWINT));

            // 0x08 differs from 0x48 only in bit 6, which is masked → should match
            var matched = _twi.SimulateIncomingAddress(0x08, isWrite: true);

            Assert.That(matched, Is.True, "Address differing only in masked bits must still match");
        }

        [Test(Description = "SLA+R: TWSR=0xA8 when SLA+R matches and ReadSlaveTransmitByte returns TWDR")]
        public void SlaveTransmit_AddressMatch()
        {
            Cpu.Mmio.Data[TWAR] = (byte)(0x48 << 1);
            Cpu.Mmio.Data[TWDR] = 0xDE; // firmware loads TX byte
            Cpu.WriteData((ushort)TWCR, (byte)(TWEN | TWINT));

            var matched = _twi.SimulateIncomingAddress(0x48, isWrite: false);

            Assert.Multiple(() =>
            {
                Assert.That(matched, Is.True);
                Assert.That(Cpu.Mmio.Data[TWSR] & TWSR_TWS_MASK, Is.EqualTo(STATUS_SLAVE_SLAR_ACK),
                    "TWSR must be 0xA8 for SLA+R match");
                Assert.That(_twi.ReadSlaveTransmitByte(), Is.EqualTo(0xDE),
                    "ReadSlaveTransmitByte must return the byte firmware placed in TWDR");
            });
        }

        [Test]
        public void ShouldReceiveByte()
        {
            var asmCode = $@"
        ldi r16, 0x44
        sts {TWAR}, r16
        
        ldi r16, {TWINT | TWEA | TWEN}
        sts {TWCR}, r16
        
        call wait_for_twint
        
        lds r16, {TWSR}
        andi r16, 0xf8
        cpi r16, 0x60
        breq skip_err_1
        jmp error
        skip_err_1:
        
        ldi r16, {TWINT | TWEA | TWEN}
        sts {TWCR}, r16
        
        call wait_for_twint
        
        lds r16, {TWSR}
        andi r16, 0xf8
        cpi r16, 0x80
        breq skip_err_2
        jmp error
        skip_err_2:
        
        lds r17, {TWDR}
        
        loop: jmp loop
        
        wait_for_twint:
        lds r16, {TWCR}
        andi r16, {TWINT}
        breq wait_for_twint
        ret
        
        error: break
    ";

            var program = new AsmProgram(asmCode).Compile();
            Cpu.LoadProgram(program.Program);

            var runner = new TestProgramRunner(Cpu,
                _ => { Console.WriteLine($"[AVR CRASH] Breakpoint hit at PC: {Cpu.Pc}"); });


            runner.RunInstructions(32);

            var matched = _twi.SimulateIncomingAddress(0x22, isWrite: true);
            Assert.That(matched, Is.True, "The AVR should recognize its address and wake up");

            runner.RunInstructions(32);

            _twi.SimulateIncomingData(0x55);

            runner.RunInstructions(32);

            Assert.That(Cpu.Mmio.Data[17], Is.EqualTo(0x55), "The assembler did not reach the success instruction");
        }

        [Test(Description = "Integration: AVR acts as a Slave Transmitter and sends a byte to an external Master")]
        public void ShouldTransmitByteActingAsSlave()
        {
            var asmCode = $@"
        ldi r16, 0x44
        sts {TWAR}, r16
        
        ldi r16, {TWINT | TWEA | TWEN}
        sts {TWCR}, r16
        
        call wait_for_twint
        
        lds r16, {TWSR}
        andi r16, 0xf8
        cpi r16, 0xA8
        breq skip_err_1
        jmp error
        skip_err_1:
        
        ldi r16, 0xDE
        sts {TWDR}, r16
        
        ldi r16, {TWINT | TWEA | TWEN}
        sts {TWCR}, r16
        
        ldi r17, 0x42
        loop: jmp loop
        
        wait_for_twint:
        lds r16, {TWCR}
        andi r16, {TWINT}
        breq wait_for_twint
        ret
        
        error: break
    ";

            var program = new AsmProgram(asmCode).Compile();
            Cpu.LoadProgram(program.Program);

            var runner = new TestProgramRunner(Cpu,
                _ => { Console.WriteLine($"[AVR CRASH] Breakpoint hit at PC: {Cpu.Pc}"); });

            runner.RunInstructions(32);

            var matched = _twi.SimulateIncomingAddress(0x22, isWrite: false);
            Assert.That(matched, Is.True, "The AVR should recognize its address and wake up");

            runner.RunInstructions(32);

            var transmittedByte = _twi.ReadSlaveTransmitByte();
            Assert.Multiple(() =>
            {
                Assert.That(transmittedByte, Is.EqualTo(0xDE), "The AVR firmware did not place the byte 0xDE in the TWDR register");
                Assert.That(Cpu.Mmio.Data[17], Is.EqualTo(0x42), "The assembler did not reach the success instruction");
            });
        }

        [Test (Description = "TWEA=0: SimulateIncomingData returns NACK status when TWEA is cleared")]
        public void ShouldReturnNackWhenTweaIsClearedDuringReceive ()
        {
            Cpu.Mmio.Data[TWAR] = 0x48 << 1;

            Cpu.WriteData (TWCR, TWEN | TWINT);

            _twi.SimulateIncomingAddress (0x48, isWrite: true);

            Cpu.WriteData (TWCR, TWEN | TWINT);
            _twi.SimulateIncomingData (0x5A);

            Assert.Multiple (() => {
                Assert.That (Cpu.Mmio.Data[TWDR], Is.EqualTo (0x5A), "TWDR must still hold the received byte");

                Assert.That (Cpu.Mmio.Data[TWSR] & TWSR_TWS_MASK, Is.EqualTo (STATUS_SLAVE_DATA_RX_NACK),
                    "TWSR must be 0x88 (data received, NACK) because TWEA was 0");

                Assert.That (Cpu.Mmio.Data[TWCR] & TWINT, Is.EqualTo (TWINT), "TWINT must be set");
            });
        }
    }
}