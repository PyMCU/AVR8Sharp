using Avr8Sharp.TestKit;
using Avr8Sharp.TestKit.Boards;
using AVR8Sharp.Core.Peripherals;

namespace Avr8Sharp.TestKit.Samples;

/// <summary>
/// The Uno and Mega presets mount SPI, TWI, ADC and the watchdog, with scripted bus slaves so
/// transfers complete, and MCUSR reports PORF after power-on.
/// </summary>
[TestFixture]
public class BoardPeripheralsTests
{
    private const int McusrAddress = 0x54;
    private const byte Porf = 0x01;
    private const byte Extrf = 0x02;
    private const byte Wdrf = 0x08;

    // SPI master: send 0xAA, wait for SPIF, read the byte clocked back into r18.
    private const string SpiTransfer = @"
        ldi r16, 0x50       ; SPE | MSTR
        out 0x2C, r16       ; SPCR
        ldi r16, 0xAA
        out 0x2E, r16       ; SPDR
    wait:
        in r17, 0x2D        ; SPSR
        sbrs r17, 7         ; SPIF
        rjmp wait
        in r18, 0x2E        ; SPDR
        break
    ";

    // TWI master: START, then SLA+W for address 0x50; r18 = TWSR status after the address phase.
    private const string TwiAddress = @"
        ldi r16, 0xA4       ; TWINT | TWSTA | TWEN
        sts 0x00BC, r16     ; TWCR
    w1:
        lds r17, 0x00BC
        sbrs r17, 7
        rjmp w1
        ldi r16, 0xA0       ; 0x50 << 1 | W
        sts 0x00BB, r16     ; TWDR
        ldi r16, 0x84       ; TWINT | TWEN
        sts 0x00BC, r16
    w2:
        lds r17, 0x00BC
        sbrs r17, 7
        rjmp w2
        lds r18, 0x00B9     ; TWSR
        andi r18, 0xF8
        break
    ";

    // ADC: single conversion on ADC0 (AVCC reference), result in r18 (low) / r19 (high).
    private const string AdcConvert = @"
        ldi r16, 0x40       ; REFS0, MUX = 0
        sts 0x007C, r16     ; ADMUX
        ldi r16, 0xC7       ; ADEN | ADSC | prescaler 128
        sts 0x007A, r16     ; ADCSRA
    wait:
        lds r17, 0x007A
        sbrc r17, 6         ; ADSC clears when done
        rjmp wait
        lds r18, 0x0078     ; ADCL
        lds r19, 0x0079     ; ADCH
        break
    ";

    // Watchdog in reset mode with the shortest (16 ms) timeout, then spin.
    private const string WatchdogArm = @"
        ldi r16, 0x18       ; WDCE | WDE
        sts 0x0060, r16     ; WDTCSR
        ldi r16, 0x08       ; WDE, WDP = 0
        sts 0x0060, r16
    spin:
        rjmp spin
    ";

    // ── Uno ───────────────────────────────────────────────────────────────────

    [Test]
    public void Uno_Spi_TransferCompletesWithIdleMisoByDefault()
    {
        var uno = new ArduinoUnoSimulation();
        uno.WithAsm(SpiTransfer);
        uno.RunToBreak();

        Assert.That(uno.Cpu.ReadData(18), Is.EqualTo(0xFF));
        Assert.That(uno.SpiBus.Mosi, Is.EqualTo(new byte[] { 0xAA }));
    }

    [Test]
    public void Uno_Spi_ReturnsQueuedResponse()
    {
        var uno = new ArduinoUnoSimulation();
        uno.WithAsm(SpiTransfer);
        uno.SpiBus.Responses.Enqueue(0x42);
        uno.RunToBreak();

        Assert.That(uno.Cpu.ReadData(18), Is.EqualTo(0x42));
    }

    [Test]
    public void Uno_Twi_NoSlaveNacksTheAddress()
    {
        var uno = new ArduinoUnoSimulation();
        uno.WithAsm(TwiAddress);
        uno.RunToBreak();

        Assert.That(uno.Cpu.ReadData(18), Is.EqualTo(0x20), "TW_MT_SLA_NACK");
    }

    [Test]
    public void Uno_Twi_ConfiguredSlaveAcks()
    {
        var uno = new ArduinoUnoSimulation();
        uno.WithAsm(TwiAddress);
        uno.TwiBus.Addresses.Add(0x50);
        uno.RunToBreak();

        Assert.That(uno.Cpu.ReadData(18), Is.EqualTo(0x18), "TW_MT_SLA_ACK");
    }

    [Test]
    public void Uno_Adc_ConversionFinishesAtZeroVoltsByDefault()
    {
        var uno = new ArduinoUnoSimulation();
        uno.WithAsm(AdcConvert);
        uno.RunToBreak();

        Assert.That(uno.Cpu.ReadData(18) | (uno.Cpu.ReadData(19) << 8), Is.Zero);
    }

    [Test]
    public void Uno_Adc_ReadsInjectedVoltage()
    {
        var uno = new ArduinoUnoSimulation();
        uno.WithAsm(AdcConvert);
        uno.Adc.ChannelValues[0] = 2.5;   // half of AVCC (5 V) -> 512
        uno.RunToBreak();

        Assert.That(uno.Cpu.ReadData(18) | (uno.Cpu.ReadData(19) << 8), Is.EqualTo(512));
    }

    [Test]
    public void Uno_Watchdog_ResetsTheChipAndSetsWdrf()
    {
        var uno = new ArduinoUnoSimulation();
        uno.WithAsm(WatchdogArm);
        uno.RunMilliseconds(40);

        Assert.That(uno.Cpu.ReadData(McusrAddress) & Wdrf, Is.EqualTo(Wdrf));
    }

    // ── MCUSR ─────────────────────────────────────────────────────────────────

    [Test]
    public void Uno_Mcusr_IsPorfAfterCreation()
    {
        var uno = new ArduinoUnoSimulation();

        Assert.That(uno.Cpu.ReadData(McusrAddress), Is.EqualTo(Porf));
    }

    [Test]
    public void Mcusr_WritingZeroClearsAndWritingOneNeverSets()
    {
        var uno = new ArduinoUnoSimulation();
        uno.WithAsm(@"
            ldi r16, 0xFF
            out 0x34, r16       ; MCUSR: writing ones leaves PORF as it was
            in r17, 0x34
            clr r16
            out 0x34, r16       ; writing a zero clears the flag
            in r18, 0x34
            ldi r16, 0xFF
            out 0x34, r16       ; writing ones cannot set it again
            in r19, 0x34
            break
        ");
        uno.RunToBreak();

        Assert.That(uno.Cpu.ReadData(17), Is.EqualTo(Porf));
        Assert.That(uno.Cpu.ReadData(18), Is.Zero);
        Assert.That(uno.Cpu.ReadData(19), Is.Zero);
    }

    [Test]
    public void Mcusr_ExternalResetSetsExtrfAndKeepsPorf()
    {
        var uno = new ArduinoUnoSimulation();
        uno.Reset();

        Assert.That(uno.Cpu.ReadData(McusrAddress), Is.EqualTo(Porf | Extrf));
    }

    [Test]
    public void Mcusr_WatchdogResetSetsWdrfNotExtrf()
    {
        var uno = new ArduinoUnoSimulation();
        uno.WithAsm(WatchdogArm);
        uno.RunMilliseconds(40);

        Assert.That(uno.Cpu.ReadData(McusrAddress), Is.EqualTo(Porf | Wdrf));
    }

    [Test]
    public void AvrRunnerMachineWithWatchdogGetsPorfOnConstruction()
    {
        var runner = new AVR8Sharp.Core.Utils.AvrRunner(new byte[0x8000], 2048);
        var clock = new AvrClock(runner.Cpu, 16_000_000, AvrClock.ClockConfig);
        _ = new AvrWatchdog(runner.Cpu, AvrWatchdog.WatchdogConfig, clock);

        Assert.That(runner.Cpu.ReadData(McusrAddress), Is.EqualTo(Porf));
    }

    // ── Mega ──────────────────────────────────────────────────────────────────

    [Test]
    public void Mega_Mcusr_IsPorfAfterCreation()
    {
        Assert.That(new ArduinoMegaSimulation().Cpu.ReadData(McusrAddress), Is.EqualTo(Porf));
    }

    [Test]
    public void Mega_Spi_And_Twi_Complete()
    {
        var spi = new ArduinoMegaSimulation();
        spi.WithAsm(SpiTransfer);
        spi.SpiBus.Responses.Enqueue(0x5A);
        spi.RunToBreak();
        Assert.That(spi.Cpu.ReadData(18), Is.EqualTo(0x5A));

        var twi = new ArduinoMegaSimulation();
        twi.WithAsm(TwiAddress);
        twi.RunToBreak();
        Assert.That(twi.Cpu.ReadData(18), Is.EqualTo(0x20));
    }

    [Test]
    public void Mega_Adc_ReadsHighChannelThroughMux5()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(@"
            ldi r16, 0x08       ; MUX5
            sts 0x007B, r16     ; ADCSRB
            ldi r16, 0x41       ; REFS0, MUX = 1 -> ADC9
            sts 0x007C, r16     ; ADMUX
            ldi r16, 0xC7
            sts 0x007A, r16     ; ADCSRA
        wait:
            lds r17, 0x007A
            sbrc r17, 6
            rjmp wait
            lds r18, 0x0078
            lds r19, 0x0079
            break
        ");
        mega.Adc.ChannelValues[9] = 2.5;
        mega.Adc.ChannelValues[1] = 5.0;   // must not be picked: MUX5 selects ADC9, not ADC1
        mega.RunToBreak();

        Assert.That(mega.Cpu.ReadData(18) | (mega.Cpu.ReadData(19) << 8), Is.EqualTo(512));
    }

    [Test]
    public void Mega_Watchdog_SetsWdrf()
    {
        var mega = new ArduinoMegaSimulation();
        mega.WithAsm(WatchdogArm);
        mega.RunMilliseconds(40);

        Assert.That(mega.Cpu.ReadData(McusrAddress) & Wdrf, Is.EqualTo(Wdrf));
    }

    // ── Strict mode (unmounted ports) ─────────────────────────────────────────

    private const string ReadPind = "in r16, 0x09\nbreak\n";

    [Test]
    public void Strict_Off_ReadingUnmountedPinReadsZero()
    {
        var sim = AvrTestSimulation.Create().WithAsm(ReadPind);

        Assert.DoesNotThrow(() => sim.RunToBreak());
        Assert.That(sim.Cpu.ReadData(16), Is.Zero);
    }

    [Test]
    public void Strict_On_ReadingUnmountedPinThrowsNamingRegisterAndAddress()
    {
        var sim = AvrTestSimulation.Create().WithStrict().WithAsm(ReadPind);

        var ex = Assert.Throws<UnmountedIoAccessException>(() => sim.RunToBreak())!;
        Assert.That(ex.Message, Does.Contain("PIND").And.Contain("0x29").And.Contain("port D"));
        Assert.That(ex.Address, Is.EqualTo(0x29));
        Assert.That(ex.IsWrite, Is.False);
    }

    [Test]
    public void Strict_On_WritingUnmountedDdrThrows()
    {
        var sim = AvrTestSimulation.Create().WithStrict().WithAsm("ldi r16, 1\nout 0x0A, r16\nbreak\n");

        var ex = Assert.Throws<UnmountedIoAccessException>(() => sim.RunToBreak())!;
        Assert.That(ex.Message, Does.Contain("DDRD").And.Contain("written"));
    }

    [Test]
    public void Strict_On_MountedPortDoesNotThrow_WhetherMountedBeforeOrAfter()
    {
        var before = AvrTestSimulation.Create()
            .AddGpio(AvrIoPort.PortDConfig, out _).WithStrict().WithAsm(ReadPind);
        Assert.DoesNotThrow(() => before.RunToBreak());

        var after = AvrTestSimulation.Create().WithStrict();
        after.AddGpio(AvrIoPort.PortDConfig, out var portD).WithAsm(ReadPind);
        Assert.DoesNotThrow(() => after.RunToBreak());
        Assert.That(portD, Is.Not.Null);
    }

    [Test]
    public void Strict_Warn_RecordsOneMessagePerRegisterAndKeepsRunning()
    {
        var sim = AvrTestSimulation.Create();
        sim.UnmountedAccess = UnmountedAccessMode.Warn;
        sim.WithAsm("in r16, 0x09\nin r16, 0x09\nbreak\n");

        Assert.DoesNotThrow(() => sim.RunToBreak());
        Assert.That(sim.Warnings, Has.Count.EqualTo(1));
        Assert.That(sim.Warnings[0], Does.Contain("PIND"));
    }

    [Test]
    public void Strict_OnUno_AllChipPortsAreMounted_NothingThrows()
    {
        var uno = new ArduinoUnoSimulation().WithStrict().WithAsm(
            "in r16, 0x03\nin r16, 0x06\nin r16, 0x09\nldi r16, 1\nout 0x04, r16\nbreak\n");

        Assert.DoesNotThrow(() => uno.RunToBreak());
    }

    [Test]
    public void Strict_OnAttiny85_DoesNotFlagItsOwnLowIoRegisters()
    {
        var tiny = new ATtiny85Simulation().WithStrict().WithAsm("in r16, 0x06\nbreak\n");  // ADCSRA lives at 0x26

        Assert.DoesNotThrow(() => tiny.RunToBreak());
    }
}
