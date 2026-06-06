# Peripherals

Avr8Sharp emulates the ATmega/ATtiny peripheral set with datasheet-accurate register
behaviour. All peripherals are optional and added through the `AvrTestSimulation` builder
API (or wired up automatically by the board presets).

## Coverage

| Peripheral | Status | Notes |
|---|---|---|
| GPIO (I/O ports) | ✅ | Pin-change and external interrupts |
| USART | ✅ | Baud config, TX/RX, interrupts |
| Timer 0/2 (8-bit) | ✅ | Normal, CTC, Fast PWM, Phase-correct PWM |
| Timer 1/3/4/5 (16-bit) | ✅ | Normal, CTC, Fast PWM, Phase-correct PWM, Input Capture |
| ATtiny85 TC1 (8-bit) | 🟡 | Normal mode; CTC and PWM modes not emulated |
| SPI | ✅ | Master mode, SPSR/SPCR/SPDR |
| TWI (I²C) | ✅ | Master mode; slave state machine not emulated |
| ADC | ✅ | 8 channels, single-conversion mode |
| EEPROM | ✅ | Read/write/erase, EE_RDY interrupt |
| USI | ✅ | Three-wire and two-wire mode |
| Watchdog | ✅ | Prescaler, reset, interrupt mode |

```{note}
Per-peripheral reference pages are being filled in. For now, the API surface is documented
via XML-doc comments in the source; see [Reference](../reference/index.md).
```

## GPIO

All I/O ports expose `AvrIoPort`, which mirrors the hardware PIN/DDR/PORT triple:

```csharp
// Drive PB5 high from the test host
uno.PortB.SetPin(5, true);

// Read firmware output
bool isHigh = uno.PortB.GetPinState(5);

// Register a listener for any pin change
uno.PortB.AddListener(pin => Console.WriteLine($"PB{pin} changed"));
```

## USART

```csharp
// Capture all TX bytes in a SerialProbe (done automatically by board presets)
sim.AddUsart(AvrUsart.Usart0Config, out var serial);
serial.OnByte = b => Console.Write((char)b);

// Inject a byte into the RX FIFO (simulates host → firmware)
serial.Usart.WriteByte(0x41);   // send 'A'
```

## Timers

Timer peripherals fire output-compare events that toggle OC pins automatically.
You can also hook the compare match callback:

```csharp
sim.AddTimer(AvrTimer.Timer1Config, out var timer1);
timer1.OnOutputCompareMatch += channel => Console.WriteLine($"OC1{(char)('A' + channel)}");
```

## EEPROM

```csharp
sim.AddEeprom(AvrEeprom.EepromConfig, out var eeprom, eepromSize: 1024);

// Pre-load EEPROM content before running firmware
eeprom.Backend.Data[0] = 0xCA;
eeprom.Backend.Data[1] = 0xFE;
```

## ADC

```csharp
sim.AddAdc(AvrAdc.AdcConfig, out var adc);

// Set the voltage on channel 0 (in millivolts, 0–5000)
adc.ChannelValues[0] = 2500;   // 2.5 V → mid-scale
```
