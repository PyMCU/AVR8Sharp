# Board pin maps

This page maps Arduino pin numbers and peripheral signals to the simulator properties
you use to observe or drive them from your tests.

---

## Arduino Uno (ATmega328P)

**Preset class:** `ArduinoUnoSimulation`  
**Properties:** `PortB`, `PortC`, `PortD`, `Timer0`, `Timer1`, `Timer2`, `Serial`, `Eeprom`

### Digital pins → GPIO port

| Arduino pin | AVR port | Pin index | Simulator property | Notes |
|---|---|---|---|---|
| 0 | PortD | 0 | `uno.PortD` pin 0 | USART0 RX |
| 1 | PortD | 1 | `uno.PortD` pin 1 | USART0 TX |
| 2 | PortD | 2 | `uno.PortD` pin 2 | INT0 |
| 3 | PortD | 3 | `uno.PortD` pin 3 | INT1, OC2B |
| 4 | PortD | 4 | `uno.PortD` pin 4 | |
| 5 | PortD | 5 | `uno.PortD` pin 5 | OC0B |
| 6 | PortD | 6 | `uno.PortD` pin 6 | OC0A |
| 7 | PortD | 7 | `uno.PortD` pin 7 | |
| 8 | PortB | 0 | `uno.PortB` pin 0 | |
| 9 | PortB | 1 | `uno.PortB` pin 1 | OC1A |
| 10 | PortB | 2 | `uno.PortB` pin 2 | OC1B, SS |
| 11 | PortB | 3 | `uno.PortB` pin 3 | MOSI, OC2A |
| 12 | PortB | 4 | `uno.PortB` pin 4 | MISO |
| **13** | **PortB** | **5** | **`uno.PortB` pin 5** | **SCK, LED_BUILTIN** |

### Analog pins → ADC / GPIO

| Arduino pin | AVR port | Pin index | ADC channel | Simulator property |
|---|---|---|---|---|
| A0 | PortC | 0 | 0 | `uno.PortC` pin 0 |
| A1 | PortC | 1 | 1 | `uno.PortC` pin 1 |
| A2 | PortC | 2 | 2 | `uno.PortC` pin 2 |
| A3 | PortC | 3 | 3 | `uno.PortC` pin 3 |
| A4 | PortC | 4 | 4 | `uno.PortC` pin 4 | SDA (TWI) |
| A5 | PortC | 5 | 5 | `uno.PortC` pin 5 | SCL (TWI) |

### Serial → USART

| Arduino `Serial` object | USART | Simulator property |
|---|---|---|
| `Serial` | USART0 | `uno.Serial` |

### Common assertions

```csharp
uno.PortB.Should().HavePinHigh(5);          // LED_BUILTIN on
uno.PortD.Should().HavePinLow(3);           // INT1 pin low
uno.PortC.Should().HavePinInputPullup(4);   // A4 as pulled-up input
uno.Serial.Should().ContainLine("hello");
```

---

## Arduino Mega 2560 (ATmega2560)

**Preset class:** `ArduinoMegaSimulation`  
**Properties:** `PortA`–`PortL`, `Timer0`–`Timer5`, `Serial0`–`Serial3`, `Eeprom`

### Serial channels

| Arduino `Serial` object | USART | TX pin | RX pin | Simulator property |
|---|---|---|---|---|
| `Serial` | USART0 | 1 (PE1) | 0 (PE0) | `mega.Serial0` |
| `Serial1` | USART1 | 18 (PD3) | 19 (PD2) | `mega.Serial1` |
| `Serial2` | USART2 | 16 (PH1) | 17 (PH0) | `mega.Serial2` |
| `Serial3` | USART3 | 14 (PJ1) | 15 (PJ0) | `mega.Serial3` |

### Selected digital pins → GPIO port

| Arduino pin | AVR port | Pin | Simulator property | Notes |
|---|---|---|---|---|
| 0 | PortE | 0 | `mega.PortE` pin 0 | RX0 |
| 1 | PortE | 1 | `mega.PortE` pin 1 | TX0 |
| 13 | PortB | 7 | `mega.PortB` pin 7 | LED_BUILTIN, SCK |
| 14 | PortJ | 1 | `mega.PortJ` pin 1 | TX3 |
| 15 | PortJ | 0 | `mega.PortJ` pin 0 | RX3 |
| 16 | PortH | 1 | `mega.PortH` pin 1 | TX2 |
| 17 | PortH | 0 | `mega.PortH` pin 0 | RX2 |
| 18 | PortD | 3 | `mega.PortD` pin 3 | TX1 |
| 19 | PortD | 2 | `mega.PortD` pin 2 | RX1 |
| 20 | PortD | 1 | `mega.PortD` pin 1 | SDA |
| 21 | PortD | 0 | `mega.PortD` pin 0 | SCL |
| 22–29 | PortA | 0–7 | `mega.PortA` pin 0–7 | |
| 30–37 | PortC | 7–0 | `mega.PortC` pin 7–0 | |
| 42–49 | PortL | 7–0 | `mega.PortL` pin 7–0 | |
| 50 | PortB | 3 | `mega.PortB` pin 3 | MISO |
| 51 | PortB | 2 | `mega.PortB` pin 2 | MOSI |
| 52 | PortB | 1 | `mega.PortB` pin 1 | SCK |
| 53 | PortB | 0 | `mega.PortB` pin 0 | SS |

### Analog pins → ADC / GPIO

| Arduino pin | AVR port | Pin | ADC channel |
|---|---|---|---|
| A0–A7 | PortF | 0–7 | 0–7 |
| A8–A15 | PortK | 0–7 | 8–15 |

### Common assertions

```csharp
mega.PortB.Should().HavePinHigh(7);          // LED_BUILTIN (digital 13)
mega.Serial0.Should().Contain("ready");
mega.Serial1.Should().ContainLine("ping");
mega.PortA.Should().HaveOutputValue(0xFF);   // all Port A pins high
```

---

## ATtiny85

**Preset class:** `ATtiny85Simulation`  
**Properties:** `PortB`, `Timer0`, `Timer1`, `Eeprom`

The ATtiny85 has a single 6-pin I/O port (PB0–PB5; PB3 = RESET, disabled by default).

### Physical pins → port

| DIP8 pin | Signal | Port B pin | Simulator property | Notes |
|---|---|---|---|---|
| 1 | PB5 / RESET | 5 | `tiny.PortB` pin 5 | RESET (fuse-dependent) |
| 2 | PB3 / ADC3 | 3 | `tiny.PortB` pin 3 | ADC3 |
| 3 | PB4 / ADC2 | 4 | `tiny.PortB` pin 4 | ADC2, OC1B |
| 5 | PB0 / MOSI | 0 | `tiny.PortB` pin 0 | USI DO, OC0A |
| 6 | PB1 / MISO | 1 | `tiny.PortB` pin 1 | USI DI, OC0B, OC1A |
| 7 | PB2 / SCK | 2 | `tiny.PortB` pin 2 | USI SCK, INT0 |

### Common assertions

```csharp
tiny.PortB.Should().HavePinHigh(1);   // PB1 (physical pin 6) driven high
tiny.PortB.Should().HavePinLow(0);    // PB0 (physical pin 5) driven low
```

### Injecting a button press on INT0 (PB2)

```csharp
// Drive PB2 low to simulate a button press (active-low)
tiny.PortB.SetPin(2, false);
tiny.RunMilliseconds(10);

// Release
tiny.PortB.SetPin(2, true);
tiny.RunUntilSerial(...);
```
