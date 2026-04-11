using System.Text;

namespace AVR8Sharp.Tests.Utils;

public class TwiAsmBuilder
{
    private readonly StringBuilder _asm = new StringBuilder();

    public TwiAsmBuilder AddStandardHeaders()
    {
        _asm.AppendLine("; --- Register and bit definitions ---");
        _asm.AppendLine("_REPLACE TWSR, 0xb9");
        _asm.AppendLine("_REPLACE TWDR, 0xbb");
        _asm.AppendLine("_REPLACE TWCR, 0xbc");
        _asm.AppendLine("_REPLACE TWEN, 4");
        _asm.AppendLine("_REPLACE TWSTO, 0x10");
        _asm.AppendLine("_REPLACE TWSTA, 0x20");
        _asm.AppendLine("_REPLACE TWEA, 0x40");
        _asm.AppendLine("_REPLACE TWINT, 0x80");
        _asm.AppendLine();
        return this;
    }

    public TwiAsmBuilder SendStartCondition()
    {
        _asm.AppendLine("; --- Send START ---");
        _asm.AppendLine("ldi r16, TWEN");
        _asm.AppendLine("sbr r16, TWSTA");
        _asm.AppendLine("sbr r16, TWINT");
        _asm.AppendLine("sts TWCR, r16");
        _asm.AppendLine("call wait_for_twint");
        _asm.AppendLine();
        return this;
    }

    public TwiAsmBuilder SendAddressWrite(byte addressWithWriteBit)
    {
        _asm.AppendLine($"; --- Send SLA+W (0x{addressWithWriteBit:X2}) ---");
        _asm.AppendLine($"ldi r16, 0x{addressWithWriteBit:X2}");
        _asm.AppendLine("sts TWDR, r16");
        _asm.AppendLine("ldi r16, TWINT");
        _asm.AppendLine("sbr r16, TWEN");
        _asm.AppendLine("sts TWCR, r16");
        _asm.AppendLine("call wait_for_twint");
        _asm.AppendLine();
        return this;
    }

    public TwiAsmBuilder SendAddressRead(byte addressWithReadBit)
    {
        _asm.AppendLine($"; --- Send SLA+R (0x{addressWithReadBit:X2}) ---");
        _asm.AppendLine($"ldi r16, 0x{addressWithReadBit:X2}");
        _asm.AppendLine("sts TWDR, r16");
        _asm.AppendLine("ldi r16, TWINT");
        _asm.AppendLine("sbr r16, TWEN");
        _asm.AppendLine("sts TWCR, r16");
        _asm.AppendLine("call wait_for_twint");
        _asm.AppendLine();
        return this;
    }

    public TwiAsmBuilder SendByte(byte data)
    {
        _asm.AppendLine($"; --- Send Data (0x{data:X2}) ---");
        _asm.AppendLine($"ldi r16, 0x{data:X2}");
        _asm.AppendLine("sts TWDR, r16");
        _asm.AppendLine("ldi r16, TWINT");
        _asm.AppendLine("sbr r16, TWEN");
        _asm.AppendLine("sts TWCR, r16");
        _asm.AppendLine("call wait_for_twint");
        _asm.AppendLine();
        return this;
    }

    public TwiAsmBuilder ReceiveByte(bool sendAck)
    {
        _asm.AppendLine($"; --- Receive Byte (ACK: {sendAck}) ---");
        _asm.AppendLine("ldi r16, TWINT");
        if (sendAck)
        {
            _asm.AppendLine("sbr r16, TWEA");
        }
        _asm.AppendLine("sbr r16, TWEN");
        _asm.AppendLine("sts TWCR, r16");
        _asm.AppendLine("call wait_for_twint");
        _asm.AppendLine();
        return this;
    }

    public TwiAsmBuilder SendStopCondition()
    {
        _asm.AppendLine("; --- Send STOP ---");
        _asm.AppendLine("ldi r16, TWINT");
        _asm.AppendLine("sbr r16, TWEN");
        _asm.AppendLine("sbr r16, TWSTO");
        _asm.AppendLine("sts TWCR, r16");
        _asm.AppendLine("call wait_for_twint");
        _asm.AppendLine();
        return this;
    }

    public TwiAsmBuilder CheckStatus(byte expectedStatus)
    {
        _asm.AppendLine($"; Assert Status == 0x{expectedStatus:X2}");
        _asm.AppendLine("lds r16, TWSR");
        _asm.AppendLine("andi r16, 0xf8");
        _asm.AppendLine($"cpi r16, 0x{expectedStatus:X2}");
        _asm.AppendLine("jmp error");
        _asm.AppendLine();
        return this;
    }

    public TwiAsmBuilder CheckData(byte expectedData)
    {
        _asm.AppendLine($"; Assert Data == 0x{expectedData:X2}");
        _asm.AppendLine("lds r16, TWDR");
        _asm.AppendLine($"cpi r16, 0x{expectedData:X2}");
        _asm.AppendLine("jmp error");
        _asm.AppendLine();
        return this;
    }

    public TwiAsmBuilder AddSuccessAndErrorHandlers()
    {
        _asm.AppendLine("; --- Success & Error Routines ---");
        _asm.AppendLine("; Success: load 0x42 into r17");
        _asm.AppendLine("ldi r17, 0x42");
        _asm.AppendLine("loop: jmp loop");
        _asm.AppendLine();
        _asm.AppendLine("; WaitForTwint Routine");
        _asm.AppendLine("wait_for_twint:");
        _asm.AppendLine("lds r16, TWCR");
        _asm.AppendLine("andi r16, TWINT");
        _asm.AppendLine("breq wait_for_twint");
        _asm.AppendLine("ret");
        _asm.AppendLine();
        _asm.AppendLine("; Error Routine");
        _asm.AppendLine("error: break");
        return this;
    }

    public string Build() => _asm.ToString();
}