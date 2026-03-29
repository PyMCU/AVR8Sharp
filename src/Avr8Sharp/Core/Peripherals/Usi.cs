using AVR8Sharp.Core.Cpu;

namespace AVR8Sharp.Core.Peripherals;

public class AvrUsi
{
    const int USICR = 0x2d;
    const int USISR = 0x2e;
    const int USIDR = 0x2f;
    const int USIBR = 0x30;

    // USISR bits
    const int USICNT_MASK = 0x0f;
    const int USIDC = 1 << 4;
    const int USIPF = 1 << 5;
    const int USIOIF = 1 << 6;
    const int USISIF = 1 << 7;

    // USICR bits
    const int USITC = 1 << 0;
    const int USICLK = 1 << 1;
    const int USICS0 = 1 << 2;
    const int USICS1 = 1 << 3;
    const int USIWM0 = 1 << 4;
    const int USIWM1 = 1 << 5;
    const int USIOIE = 1 << 6;
    const int USISIE = 1 << 7;

    static AvrInterruptConfig _start = new AvrInterruptConfig(
        address: 0xd,
        flagRegister: USISR,
        flagMask: USISIF,
        enableRegister: USICR,
        enableMask: USISIE
    );

    private static AvrInterruptConfig _overflow = new AvrInterruptConfig(
        address: 0xe,
        flagRegister: USISR,
        flagMask: USIOIF,
        enableRegister: USICR,
        enableMask: USIOIE
    );

    private readonly Cpu.Cpu _cpu;
    private readonly AvrIoPort _port;
    private readonly int _dataPin;
    private readonly int _clockPin;

    private readonly ushort _PIN;
    private readonly ushort _PORT;

    public AvrUsi(Cpu.Cpu cpu, AvrIoPort port, int portPin, int dataPin, int clockPin)
    {
        _cpu = cpu;
        _port = port;
        _dataPin = dataPin;
        _clockPin = clockPin;

        _PIN = (ushort)portPin;
        _PORT = (ushort)(_PIN + 1);

        port.AddListener(DelegatePortListener);

        _cpu.Mmio.RegisterWrite(USISR, DelegateWriteHookUsisr);

        _cpu.Mmio.RegisterWrite(USICR, DelegateWriteHookUsicr);
    }

    private void DelegatePortListener(byte value, byte oldValue)
    {
        var twoWire = (_cpu.Mmio.Data[USICR] & USIWM1) == USIWM1;
        if (twoWire)
        {
            if ((value & (1 << _clockPin)) != 0 && (value & (1 << _dataPin)) == 0)
            {
                // Start condition detected
                _cpu.SetInterruptFlag(_start);
            }

            if ((value & (1 << _clockPin)) != 0 && (value & (1 << _dataPin)) != 0)
            {
                // Stop condition detected
                _cpu.Mmio.Data[USISR] |= USIPF;
            }
        }
    }

    private bool DelegateWriteHookUsisr(byte value, byte oldValue, ushort address, byte mask)
    {
        var writeClearMask = USISIF | USIOIF | USIPF;
        _cpu.Mmio.Data[USISR] = (byte)((_cpu.Mmio.Data[USISR] & writeClearMask & ~value) | (value & 0xf));
        _cpu.ClearInterruptByFlag(_start, value);
        _cpu.ClearInterruptByFlag(_overflow, value);
        return true;
    }

    private bool DelegateWriteHookUsicr(byte value, byte oldValue, ushort address, byte mask)
    {
        _cpu.Mmio.Data[USICR] = (byte)(value & ~(USICLK | USITC));
        _cpu.UpdateInterruptEnable(_start, value);
        _cpu.UpdateInterruptEnable(_overflow, value);
        var clockSrc = value & ((USICS1 | USICS0) >> 2);
        var mode = value & ((USIWM1 | USIWM0) >> 4);
        var usiClk = value & USICLK;
        _port.OpenCollector = (byte)(mode >= 2 ? (1 << _dataPin) : 0);
        var inputValue = (_cpu.Mmio.Data[_PIN] & (1 << _dataPin)) != 0 ? 1 : 0;
        if (usiClk != 0 && clockSrc == 0)
        {
            Shift(inputValue);
            Count();
        }

        if ((value & USITC) != 0)
        {
            return ProcessUsitcNotZero(ref usiClk, ref clockSrc, ref inputValue);
        }

        return false;
    }

    private bool ProcessUsitcNotZero(ref int usiClk, ref int clockSrc, ref int inputValue)
    {
        _cpu.Mmio.WriteData(_PIN, (byte)(1 << _clockPin));
        var newValue = _cpu.Mmio.Data[_PIN] & (1 << _clockPin);
        if (usiClk != 0 && (clockSrc == 2 || clockSrc == 3))
        {
            if (clockSrc == 2 && newValue != 0)
            {
                Shift(inputValue);
            }

            if (clockSrc == 3 && newValue == 0)
            {
                Shift(inputValue);
            }

            Count();
        }

        return true;
    }

    private void UpdateOutput()
    {
        var oldValue = _cpu.Mmio.Data[_PORT];
        var newValue = (_cpu.Mmio.Data[USIDR] & 0x80) != 0 ? oldValue | (1 << _dataPin) : oldValue & ~(1 << _dataPin);
        _cpu.Mmio.WriteData(_PORT, (byte)newValue);
        if ((newValue & 0x80) != 0 && (_cpu.Mmio.Data[_PIN] & 0x80) == 0)
        {
            _cpu.Mmio.Data[USISR] |= USIDC;
        }
        else
        {
            _cpu.Mmio.Data[USISR] &= ~USIDC & 0xff;
        }
    }

    private void Count()
    {
        var counter = (_cpu.Mmio.Data[USISR] + 1) & USICNT_MASK;
        _cpu.Mmio.Data[USISR] = (byte)((_cpu.Mmio.Data[USISR] & ~USICNT_MASK) | counter);
        if (counter == 0)
        {
            _cpu.Mmio.Data[USIBR] = _cpu.Mmio.Data[USIDR];
            _cpu.SetInterruptFlag(_overflow);
        }
    }

    private void Shift(int inputValue)
    {
        _cpu.Mmio.Data[USIDR] = (byte)((_cpu.Mmio.Data[USIDR] << 1) | inputValue);
        UpdateOutput();
    }
}