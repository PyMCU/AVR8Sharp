# Custom board configurations

The board presets (`ArduinoUnoSimulation`, etc.) cover the most common cases, but any
AVR 8-bit device can be modelled because all peripheral register addresses are fully
configurable.

## Using AvrTestSimulation directly

```csharp
using Avr8Sharp.TestKit;

var sim = AvrTestSimulation.Create(flashSize: 0x4000, sramBytes: 256)
    .WithFrequency(8_000_000)
    .WithHex(hex)
    .AddGpio(myPortConfig, out var portA)
    .AddTimer(myTimer0Config, out var timer0)
    .AddUsart(myUsartConfig, out var serial);
```

## Peripheral configs

Each peripheral accepts a config struct that maps it to the correct registers for your
target chip.

### GPIO port

```csharp
var portConfig = new AvrPortConfig(
    pin: 0x09, ddr: 0x0A, port: 0x0B,
    pinChange: new AvrPinChangeInterrupt(...),
    externalInterrupts: [...]);
```

### Timer

Timers are configured with `AvrTimerConfig`, which takes the register addresses, interrupt
vectors (as word addresses), comparator pins, and prescaler table:

```csharp
var timerConfig = new AvrTimerConfig(
    bits: 8,
    dividers: AvrTimer.Timer01Dividers,
    overflowInterrupt:    0x12,
    comparatorAInterrupt: 0x14,
    ...
    tccra: 0x44, tccrb: 0x45,
    tcnt:  0x46, ocra:  0x47, ...);
```

For chips that share a single `TCCR` register (e.g. ATtiny85 TC1), map `tccra` to an
unused address (e.g. `R0 = 0x00` which reads as 0 in well-behaved code) and `tccrb` to
the real register — see `ATtiny85Simulation` for a worked example.

### USART

```csharp
var usartConfig = new AvrUsartConfig(
    rxCompleteInterrupt: 0x26,
    dataRegisterEmptyInterrupt: 0x28,
    txCompleteInterrupt: 0x2A,
    udr: 0xC6, ucsra: 0xC0, ucsrb: 0xC1, ucsrc: 0xC2,
    ubrrl: 0xC4, ubrrh: 0xC5);
```

### EEPROM

```csharp
var eepromConfig = new AvrEepromConfig(
    eepromReadyInterrupt: 0x2C,   // word address of EE_RDY vector
    eecr: 0x3F, eedr: 0x40, eearl: 0x41, eearh: 0x42,
    eraseCycles: 28800, writeCycles: 28800);

sim.AddEeprom(eepromConfig, eepromSize: 1024);
```

## Creating a reusable preset

Subclass `AvrTestSimulation` to create a reusable board class:

```csharp
public sealed class ATmega32Simulation : AvrTestSimulation
{
    private const int Flash = 0x8000;
    private const int Sram  = 2048;

    public AvrIoPort PortA { get; }
    public AvrIoPort PortB { get; }
    public SerialProbe Serial { get; }

    public ATmega32Simulation() : base(Flash, Sram)
    {
        WithFrequency(16_000_000);
        AddGpio(PortAConfig, out var a); PortA = a;
        AddGpio(AvrIoPort.PortBConfig, out var b); PortB = b;
        AddUsart(AvrUsart.Usart0Config, out var serial); Serial = serial;
    }

    // ... define PortAConfig from the ATmega32 datasheet
}
```
