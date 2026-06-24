// Phase 8: Tests for new directives, expression evaluation, and backward compatibility.
using AVR8Sharp.Core.Utils;

namespace Avr8Sharp.Tests;

[TestFixture]
public class AssemblerNewFeatures
{
private static byte[] Bytes(string hex)
{
var result = new byte[hex.Length / 2];
for (var i = 0; i < hex.Length; i += 2)
result[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
return result;
}

// --- Phase 8a: bug regression ---
[Test] public void BugFix_CLN() => Assert.That(new AvrAssembler().Assemble("CLN"), Is.EqualTo(Bytes("a894")));
[Test] public void BugFix_SEZ() => Assert.That(new AvrAssembler().Assemble("SEZ"), Is.EqualTo(Bytes("1894")));

// --- .equ ---
[Test]
public void Equ_DefinesSymbol()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".equ MY_CONST = 42\nldi r16, MY_CONST");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("0ae2"))); // LDI r16,42
}

[Test]
public void Equ_Redefinition_SameValue_Allowed()
{
var asm = new AvrAssembler();
asm.Assemble(".equ MY_VAL = 10\n.equ MY_VAL = 10\nldi r16, MY_VAL");
Assert.That(asm.Errors, Is.Empty);
}

[Test]
public void Equ_Redefinition_DifferentValue_Error()
{
var asm = new AvrAssembler();
asm.Assemble(".equ MY_VAL = 10\n.equ MY_VAL = 20\nldi r16, MY_VAL");
Assert.That(asm.Errors, Has.Count.GreaterThan(0));
}

// --- .set ---
[Test]
public void Set_MutableRedefinition()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".set COUNTER = 5\n.set COUNTER = 15\nldi r17, COUNTER");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("1fe0"))); // LDI r17,15
}

// --- .def ---
[Test]
public void Def_RegisterAlias()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".def temp = r16\nldi temp, 0x42");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("02e4"))); // LDI r16,0x42
}

// --- .org ---
[Test]
public void Org_SetsOffset()
{
var asm = new AvrAssembler();
var result = asm.Assemble("nop\n.org 0x10\nnop");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result.Length, Is.EqualTo(0x12));
Assert.That(result[0x10], Is.EqualTo(0x00));
Assert.That(result[0x11], Is.EqualTo(0x00));
}

// --- .byte / .db ---
[Test]
public void Byte_EmitsRawBytes()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".byte 0x01, 0x02, 0x03");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(new byte[] { 0x01, 0x02, 0x03 }));
}

[Test]
public void Db_AliasForByte()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".db 0xAA, 0xBB");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(new byte[] { 0xAA, 0xBB }));
}

// --- .word / .dw ---
[Test]
public void Word_EmitsLittleEndianWords()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".word 0x1234");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(new byte[] { 0x34, 0x12 }));
}

[Test]
public void Dw_AliasForWord()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".dw 0xABCD, 0x1234");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(new byte[] { 0xCD, 0xAB, 0x34, 0x12 }));
}

// --- .dword ---
[Test]
public void Dword_EmitsFourBytes()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".dword 0x12345678");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(new byte[] { 0x78, 0x56, 0x34, 0x12 }));
}

// --- .ascii / .asciz / .string ---
[Test]
public void Ascii_EmitsBytes()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".ascii \"Hi\"");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(new byte[] { (byte)'H', (byte)'i' }));
}

[Test]
public void Asciz_EmitsNullTerminated()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".asciz \"Hi\"");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(new byte[] { (byte)'H', (byte)'i', 0x00 }));
}

[Test]
public void String_AliasForAsciz()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".string \"AB\"");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(new byte[] { (byte)'A', (byte)'B', 0x00 }));
}

// --- Conditional assembly ---
[Test]
public void If_TrueIncludesBlock()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".if 1\nnop\n.endif");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("0000")));
}

[Test]
public void If_FalseExcludesBlock()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".if 0\nnop\n.endif");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.Empty);
}

[Test]
public void If_Else_TrueBranch()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".if 1\nnop\n.else\nret\n.endif");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("0000")));
}

[Test]
public void If_Else_FalseBranch()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".if 0\nnop\n.else\nret\n.endif");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("0895")));
}

[Test]
public void Ifdef_Defined()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".equ DEBUG = 1\n.ifdef DEBUG\nnop\n.endif");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("0000")));
}

[Test]
public void Ifdef_NotDefined()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".ifdef UNDEFINED_SYMBOL\nnop\n.endif");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.Empty);
}

[Test]
public void Ifndef_NotDefined()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".ifndef UNDEFINED_SYMBOL\nnop\n.endif");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("0000")));
}

[Test]
public void Ifndef_Defined()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".equ MY_SYM = 5\n.ifndef MY_SYM\nnop\n.endif");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.Empty);
}

// --- Macros ---
[Test]
public void Macro_NoParams()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".macro do_nop\nnop\n.endm\ndo_nop");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("0000")));
}

[Test]
public void Macro_OneParam()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".macro load_imm reg, val\nldi @0, @1\n.endm\nload_imm r16, 0x42");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("02e4"))); // LDI r16,0x42
}

// --- .include ---
[Test]
public void Include_ViaResolver()
{
var files = new Dictionary<string, string> { ["defs.inc"] = ".equ MY_VAL = 99\n" };
var asm = new AvrAssembler(fn => files[fn]);
var result = asm.Assemble(".include \"defs.inc\"\nldi r16, MY_VAL");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("03e6"))); // LDI r16,99
}

[Test]
public void Include_NoResolver_Error()
{
var asm = new AvrAssembler();
asm.Assemble(".include \"missing.inc\"");
Assert.That(asm.Errors, Has.Count.GreaterThan(0));
}

// --- Expression evaluation ---
[Test]
public void Expr_Arithmetic()
{
var asm = new AvrAssembler();
var result = asm.Assemble("ldi r16, 3+5*2");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("0de0"))); // LDI r16,13
}

[Test]
public void Expr_DollarHexPrefix()
{
var asm = new AvrAssembler();
var result = asm.Assemble("ldi r16, $FF");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("0fef"))); // LDI r16,0xFF
}

[Test]
public void Expr_Lo8()
{
var asm = new AvrAssembler();
var result = asm.Assemble("ldi r16, lo8(0x1234)");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("04e3"))); // LDI r16,0x34=52
}

[Test]
public void Expr_Hi8()
{
var asm = new AvrAssembler();
var result = asm.Assemble("ldi r16, hi8(0x1234)");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("02e1"))); // LDI r16,0x12=18
}

[Test]
public void Expr_BitwiseAnd()
{
var asm = new AvrAssembler();
var result = asm.Assemble("ldi r16, (0xFF & 0x0F)");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("0fe0"))); // LDI r16,15
}

[Test]
public void Expr_BitwiseOr()
{
var asm = new AvrAssembler();
var result = asm.Assemble("ldi r16, (0x10 | 0x05)");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("05e1"))); // LDI r16,21
}

[Test]
public void Expr_SymbolReference()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".equ BASE = 16\nldi r16, BASE + 5");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("05e1"))); // LDI r16,21
}

[Test]
public void Expr_BinaryLiteral()
{
var asm = new AvrAssembler();
var result = asm.Assemble("ldi r16, 0b00001111");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("0fe0"))); // LDI r16,15
}

// --- Backward compatibility ---
[Test]
public void BackwardCompat_Replace()
{
var asm = new AvrAssembler();
var result = asm.Assemble("_REPLACE MY_PORT, 0x25\nout MY_PORT, r16");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("05bd"))); // OUT 0x25,r16
}

[Test]
public void BackwardCompat_Loc()
{
var asm = new AvrAssembler();
var result = asm.Assemble("nop\n_LOC 16\nnop");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result.Length, Is.EqualTo(18));
Assert.That(result[16], Is.EqualTo(0x00));
Assert.That(result[17], Is.EqualTo(0x00));
}

[Test]
public void BackwardCompat_Iw()
{
var asm = new AvrAssembler();
var result = asm.Assemble("_IW 4660"); // decimal 4660 = 0x1234
Assert.That(asm.Errors, Is.Empty);
// ZeroPad(4660) = "1234" → resultTable[0]=0x34, resultTable[1]=0x12
Assert.That(result, Is.EqualTo(new byte[] { 0x34, 0x12 }));
}

// --- Device definitions (P1) ---
[Test]
public void Device_ATmega328P_ConstructorParam()
{
var asm = new AvrAssembler(deviceName: "ATmega328P");
var result = asm.Assemble("out PORTB, r16"); // PORTB = I/O 0x05
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("05b9"))); // OUT 0x05,r16
}

[Test]
public void Device_ATmega328P_Directive()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".device ATmega328P\nout DDRB, r16"); // DDRB = I/O 0x04
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("04b9"))); // OUT 0x04,r16
}

[Test]
public void Device_ATtiny85_ConstructorParam()
{
var asm = new AvrAssembler(deviceName: "ATtiny85");
var result = asm.Assemble("out PORTB, r16"); // ATtiny85 PORTB = I/O 0x18
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("08bb"))); // OUT 0x18,r16
}

[Test]
public void Device_RAMEND_Symbol()
{
var asm = new AvrAssembler(deviceName: "ATmega328P");
var result = asm.Assemble("ldi r16, lo8(RAMEND)"); // RAMEND=0x08FF, lo8=0xFF
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("0fef"))); // LDI r16,0xFF
}

[Test]
public void Device_UnknownDevice_Error()
{
var asm = new AvrAssembler();
asm.Assemble(".device NoSuchDevice\nnop");
Assert.That(asm.Errors, Has.Count.GreaterThan(0));
Assert.That(asm.Errors[0], Does.Contain("Unknown device"));
}

[Test]
public void Device_CaseInsensitive()
{
var asm = new AvrAssembler(deviceName: "atmega328p");
var result = asm.Assemble("out PORTB, r16");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("05b9")));
}

[Test]
public void Device_ATmega2560_ExtendedIO_Usart3()
{
// UDR3 = data-space 0x136 (extended I/O) — verified against avr-as -mmcu=atmega2560
var asm = new AvrAssembler(deviceName: "ATmega2560");
var result = asm.Assemble("sts UDR3, r16");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("00933601"))); // STS 0x136, r16
}

[Test]
public void Device_ATmega2560_PortL()
{
// PORTL = data-space 0x10B (port H-L block) — verified against avr-as
var asm = new AvrAssembler(deviceName: "ATmega2560");
var result = asm.Assemble("sts PORTL, r16");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("00930b01"))); // STS 0x10B, r16
}

[Test]
public void Device_ATmega2560_Twi_LowIO()
{
// TWBR = data-space 0xB8 (extended I/O) — verified against avr-as
var asm = new AvrAssembler(deviceName: "ATmega2560");
var result = asm.Assemble("lds r17, TWBR");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("1091b800"))); // LDS r17, 0xB8
}

[Test]
public void Device_ATmega2560_RAMEND()
{
// RAMEND = 0x21FF (8 KB SRAM); lo8 = 0xFF
var asm = new AvrAssembler(deviceName: "ATmega2560");
var result = asm.Assemble("ldi r16, lo8(RAMEND)");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("0fef"))); // LDI r16, 0xFF
}

// --- Multi-file assembly (P2) ---
[Test]
public void Global_Extern_DirectivesAccepted()
{
var asm = new AvrAssembler();
var result = asm.Assemble(".global main\n.extern helper\nmain:\nnop");
Assert.That(asm.Errors, Is.Empty);
Assert.That(result, Is.EqualTo(Bytes("0000")));
}

[Test]
public void MultiFile_SharedSymbols()
{
var asm = new AvrAssembler();
var files = new[]
{
".global SHARED_CONST\n.equ SHARED_CONST = 42\nnop",
"ldi r16, SHARED_CONST"
};
var result = asm.AssembleMultiFile(files);
Assert.That(asm.Errors, Is.Empty);
Assert.That(result.Length, Is.GreaterThan(0));
// First file: NOP = 0x0000, second file: LDI r16,42
Assert.That(result[0], Is.EqualTo(0x00)); // NOP low
Assert.That(result[1], Is.EqualTo(0x00)); // NOP high
Assert.That(result[2], Is.EqualTo(0x0A)); // LDI r16,42 low byte
Assert.That(result[3], Is.EqualTo(0xE2)); // LDI r16,42 high byte
}
}
