using AVR8Sharp.Core;
using AVR8Sharp.Core.Peripherals;
using NUnit.Framework;

namespace Avr8Sharp.Tests.Utils;

public abstract class AvrTestBase
{
    protected const int R0 = 0;
    protected const int R1 = 1;
    protected const int R2 = 2;
    protected const int R3 = 3;
    protected const int R4 = 4;
    protected const int R5 = 5;
    protected const int R6 = 6;
    protected const int R7 = 7;
    protected const int R8 = 8;
    protected const int R9 = 9;
    protected const int R10 = 10;
    protected const int R11 = 11;
    protected const int R12 = 12;
    protected const int R13 = 13;
    protected const int R14 = 14;
    protected const int R15 = 15;
    protected const int R16 = 16;
    protected const int R17 = 17;
    protected const int R18 = 18;
    protected const int R19 = 19;
    protected const int R20 = 20;
    protected const int R21 = 21;
    protected const int R22 = 22;
    protected const int R23 = 23;
    protected const int R24 = 24;
    protected const int R25 = 25;
    protected const int R26 = 26;
    protected const int R27 = 27;
    protected const int R31 = 31;

    protected const int X = 26;
    protected const int Y = 28;
    protected const int Z = 30;

    protected const int RAMPZ = 0x5B;
    protected const int EIND = 0x5C;
    protected const int SP = 93;
    protected const int SPH = 94;
    protected const int SREG = 95;

    // SREG Bits: I-HSVNZC
    protected const int SREG_C = 0b00000001;
    protected const int SREG_Z = 0b00000010;
    protected const int SREG_N = 0b00000100;
    protected const int SREG_V = 0b00001000;
    protected const int SREG_S = 0b00010000;
    protected const int SREG_H = 0b00100000;
    protected const int SREG_I = 0b10000000;

    protected AVR8Sharp.Core.Cpu Cpu;
    protected AvrClock Clock;
    protected const int DefaultFreq = 16_000_000;

    [SetUp]
    public void Setup()
    {
        Cpu = new AVR8Sharp.Core.Cpu(new ushort[FlashByteCount], SramByteCount);
        Clock = new AvrClock(Cpu, DefaultFreq, AvrClock.ClockConfig);

        SetupPeripherals();
    }

    protected virtual ushort SramByteCount => 8192;

    protected virtual int FlashByteCount => 1024;

    protected virtual void SetupPeripherals()
    {
    }

    [TearDown]
    public void TearDown()
    {
        Dispose();
    }

    protected virtual void Dispose() { }
}