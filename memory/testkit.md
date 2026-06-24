# Avr8Sharp.TestKit — Design Notes

## Files Created
```
src/Avr8Sharp.TestKit/
├── Avr8Sharp.TestKit.csproj          # FluentAssertions 6.12.0 + Core ref
├── AvrTestSimulation.cs              # Fluent builder + execution engine
├── AvrMemoryView.cs                  # Read-only SRAM wrapper for assertions
├── Probes/
│   └── SerialProbe.cs               # Captures USART TX bytes
├── Assertions/
│   ├── AvrCpuAssertions.cs          # HaveRegister, HavePC, HaveSP, SREG flags
│   ├── SerialProbeAssertions.cs     # Contain, StartWith, ContainLine, etc.
│   ├── AvrGpioAssertions.cs         # HavePinHigh, HavePinLow, HaveOutputValue
│   └── AvrMemoryAssertions.cs       # HaveByteAt, HaveWordAt, HaveBytesAt
└── Extensions/
    └── AssertionExtensions.cs       # .Should() for AvrCpu, SerialProbe, AvrIoPort, AvrMemoryView
```

## AvrTestSimulation API

### Configuration (builder methods, all return `this`)
- `.Create(flashSize, sramBytes)` — static factory (default: 0x8000, 8192)
- `.WithFrequency(uint hz)` — default 16 MHz
- `.LoadHex(string)` — Intel HEX (gcc/avra output)
- `.LoadAsm(string)` — inline assembly via AvrAssembler
- `.LoadBytes(byte[])` — raw binary
- `.AddGpio(AvrPortConfig, out AvrIoPort)`
- `.AddUsart(AvrUsartConfig, out SerialProbe)` — auto-captures TX
- `.AddUsart(AvrUsartConfig)` — no capture
- `.AddTimer(AvrTimerConfig)` / `.AddTimer(config, out AvrTimer)`
- `.AddSpi(AvrSpiConfig, out AvrSpi)`
- `.AddTwi(AvrTwiConfig, out AvrTwi)`
- `.AddAdc(AvrAdcConfig, out AvrAdc)`

### Execution (all return `this`)
- `.RunCycles(long)` — precise cycle count
- `.RunMilliseconds(double)` — simulated time
- `.RunInstructions(int)` — instruction count
- `.RunUntil(Func<AvrTestSimulation,bool>, maxInstructions=100_000)`
- `.RunToBreak(maxInstructions=100_000)` — stops AT BREAK (0x9598), not executing it
- `.RunToAddress(int byteAddress, maxInstructions=100_000)`

## Assertion Classes

### AvrCpuAssertions (`cpu.Should()`)
- `.HaveRegister(index, byte)` — R0–R31
- `.HavePC(uint)` — word address
- `.HaveSP(ushort)`
- `.HaveCycles(int)`
- `.HaveSreg(byte)` — raw SREG byte
- `.HaveCarryFlag(bool=true)` / `.HaveZeroFlag` / `.HaveNegativeFlag` / `.HaveOverflowFlag` / `.HaveHalfCarryFlag` / `.HaveInterruptsEnabled`

### SerialProbeAssertions (`serial.Should()`)
- `.Contain(string)` / `.NotContain` / `.StartWith` / `.EndWith` / `.Be`
- `.BeEmpty()`
- `.HaveLineCount(int)` / `.ContainLine(string)`

### AvrGpioAssertions (`portB.Should()`)
- `.HavePinHigh(int)` / `.HavePinLow(int)` / `.HavePinInput(int)` / `.HavePinInputPullup(int)`
- `.HavePinState(int, PinState)`
- `.HaveOutputValue(byte)` — bitmask of all 8 output pins

### AvrMemoryAssertions (`sim.Memory.Should()`)
- `.HaveByteAt(address, byte)` / `.HaveWordAt(address, ushort)` / `.HaveWordBEAt` / `.HaveBytesAt(address, byte[])`

## SerialProbe
- `probe.Text` — all captured output as string
- `probe.Lines` — split on '\n', '\r' trimmed
- `probe.Clear()` — reset buffer
- `probe.InjectByte(byte)` — simulate incoming RX byte (calls `AvrUsart.WriteByte`)
