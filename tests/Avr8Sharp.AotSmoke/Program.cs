using AVR8Sharp.Core;
using AVR8Sharp.Core.Abstractions;
using AVR8Sharp.Core.Peripherals;
using AVR8Sharp.Core.Utils;
using SiliconTwin.Abstractions;

// Native AOT smoke check: assemble a blink program, run a few thousand cycles through the
// real decoder, and require PORTB to have toggled. Exit code 0 on success.

const string source = """
    sbi 0x04, 0      ; DDRB.0 output
loop:
    sbi 0x05, 0      ; PORTB.0 high
    cbi 0x05, 0      ; PORTB.0 low
    rjmp loop
""";

var assembler = new AvrAssembler();
var program = assembler.Assemble(source);
if (assembler.Errors.Count > 0) {
    Console.Error.WriteLine("assembly failed: " + string.Join(", ", assembler.Errors));
    return 2;
}

// Everything below goes through the SiliconTwin interfaces
IMcu mcu = AvrBuilder.Create(0x8000, 2048)
    .AddGpioPort(AvrIoPort.PortBConfig, out _)
    .AddTimer(AvrTimer.Timer0Config, out _)
    .AddUsart(AvrUsart.Usart0Config, out _)
    .AddAdc(AvrAdc.AdcConfig, out _)
    .BuildMcu("atmega328p");
mcu.Load(FirmwareImage.FromBin(program));
mcu.Reset(ResetKind.Power);

var portB = ((IGpio)mcu).Banks[0];
var toggles = 0;
var cycleOk = true;
portB.PinChanged += (in PinChange e) => {
    if (e.Affects(1) && e.IsDriven(0)) toggles++;
    if (e.Cycle != mcu.Cycles) cycleOk = false;
};

var consumed = 0L;
for (var i = 0; i < 50; i++) consumed += mcu.Run(100);

((IAdcInput)mcu).ReadChannelVolts = _ => 2.5;
var pwmOk = !((IPwmSource)mcu).TryGetPwm(0, 3, out _);
var uartTx = 0;
((IPeripheralMap)mcu).Uarts[0].TxByte += _ => uartTx++;

Console.WriteLine($"cycles={mcu.Cycles} consumed={consumed} toggles={toggles} outputEnableMask=0x{portB.OutputEnableMask:x2}");
if (toggles < 100 || portB.OutputEnableMask != 0x01 || !cycleOk || !pwmOk || mcu.Cycles != consumed) {
    Console.Error.WriteLine("FAIL: PORTB did not toggle as expected");
    return 1;
}
Console.WriteLine("OK");
return 0;
