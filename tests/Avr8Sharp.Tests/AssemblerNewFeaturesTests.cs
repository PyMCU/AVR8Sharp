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
}
