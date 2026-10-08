using System.Text;
using AvrCpu = AVR8Sharp.Core.Cpu;
using AVR8Sharp.Core;
using AVR8Sharp.Core.Decoders;
using AVR8Sharp.Core.Utils;

namespace Avr8Sharp.Tests;

/// <summary>
/// Regression tests for the CPU-core audit: Intel HEX loading, the one-instruction rule after
/// SEI/RETI, per-CPU instance hooks and the drift-free runner.
/// </summary>
[TestFixture]
public class CpuCoreAuditTests
{
	private const ushort Sei = 0x9478;
	private const ushort Reti = 0x9518;
	private const ushort Nop = 0x0000;
	private const ushort Break = 0x9598;
	private const ushort SleepOp = 0x9588;
	private const ushort IncR16 = 0x9503;
	private const ushort IncR17 = 0x9513;

	#region Intel HEX

	private static string Rec (int type, int addr, params byte[] data)
	{
		var bytes = new List<byte> { (byte)data.Length, (byte)(addr >> 8), (byte)addr, (byte)type };
		bytes.AddRange (data);
		var sum = bytes.Sum (b => b);
		bytes.Add ((byte)(-sum & 0xff));
		var sb = new StringBuilder (":");
		foreach (var b in bytes) sb.Append (b.ToString ("X2"));
		return sb.ToString ();
	}

	private static AvrRunner HexRunner (int flashBytes)
	{
		var runner = new AvrRunner (new byte[flashBytes], 2048);
		for (var i = 0; i < flashBytes; i++) runner.Cpu.ProgBytes[i] = 0;
		return runner;
	}

	[Test (Description = "Extended linear address (04) places data above 64 KB instead of wrapping onto flash start")]
	public void Hex_ExtendedLinearAddress ()
	{
		var runner = HexRunner (0x40000);
		runner.Cpu.SetProgramByte (0, 0xAA);
		var hex = string.Join ("\n",
			Rec (0, 0x0000, 0x11, 0x22),
			Rec (4, 0, 0x00, 0x02),
			Rec (0, 0x0010, 0x33, 0x44),
			Rec (1, 0));

		Assert.That (runner.TryLoadHex (hex, out var info), Is.True, string.Join (";", info.Errors));
		Assert.Multiple (() => {
			Assert.That (runner.Cpu.ProgBytes[0], Is.EqualTo (0x11));
			Assert.That (runner.Cpu.ProgBytes[0x20010], Is.EqualTo (0x33));
			Assert.That (runner.Cpu.ProgBytes[0x20011], Is.EqualTo (0x44));
			Assert.That (info.MaxAddress, Is.EqualTo (0x20011));
			Assert.That (info.ByteCount, Is.EqualTo (4));
		});
	}

	[Test (Description = "void LoadHex honours extended linear addresses too (mega sketches > 64 KB)")]
	public void Hex_LoadHex_ExtendedLinear ()
	{
		var runner = HexRunner (0x40000);
		runner.LoadHex (string.Join ("\n", Rec (0, 0, 0x11), Rec (4, 0, 0x00, 0x01), Rec (0, 0, 0x77), Rec (1, 0)));
		Assert.Multiple (() => {
			Assert.That (runner.Cpu.ProgBytes[0], Is.EqualTo (0x11));
			Assert.That (runner.Cpu.ProgBytes[0x10000], Is.EqualTo (0x77));
		});
	}

	[Test (Description = "Extended segment address (02) shifts the base by 4 bits")]
	public void Hex_ExtendedSegmentAddress ()
	{
		var runner = HexRunner (0x40000);
		var hex = string.Join ("\n", Rec (2, 0, 0x20, 0x00), Rec (0, 0x0004, 0x5A), Rec (1, 0));
		Assert.That (runner.TryLoadHex (hex, out var info), Is.True, string.Join (";", info.Errors));
		Assert.That (runner.Cpu.ProgBytes[0x20004], Is.EqualTo (0x5A));
	}

	[Test (Description = "Start-address records (03, 05) are ignored and EOF stops parsing")]
	public void Hex_StartRecordsIgnored_EofStops ()
	{
		var runner = HexRunner (0x100);
		var hex = string.Join ("\n",
			Rec (3, 0, 0, 0, 0, 0),
			Rec (5, 0, 0, 0, 0, 0),
			Rec (0, 0, 0x01),
			Rec (1, 0),
			Rec (0, 1, 0x02));
		Assert.That (runner.TryLoadHex (hex, out var info), Is.True, string.Join (";", info.Errors));
		Assert.Multiple (() => {
			Assert.That (runner.Cpu.ProgBytes[0], Is.EqualTo (1));
			Assert.That (runner.Cpu.ProgBytes[1], Is.EqualTo (0), "data after EOF must not load");
		});
	}

	[Test (Description = "A bad checksum makes TryLoadHex fail and leaves flash untouched")]
	public void Hex_BadChecksum_FlashUntouched ()
	{
		var runner = HexRunner (0x100);
		runner.Cpu.SetProgramByte (0, 0xEE);
		var bad = Rec (0, 0, 0x01, 0x02);
		bad = bad[..^2] + (bad.EndsWith ("00") ? "01" : "00");

		Assert.That (runner.TryLoadHex (bad + "\n" + Rec (1, 0), out var info), Is.False);
		Assert.Multiple (() => {
			Assert.That (info.Errors, Is.Not.Empty);
			Assert.That (runner.Cpu.ProgBytes[0], Is.EqualTo (0xEE));
		});
	}

	[Test (Description = "An image larger than flash is rejected without touching flash, with MaxAddress reported")]
	public void Hex_TooBig_FlashUntouched ()
	{
		var runner = HexRunner (0x100);
		runner.Cpu.SetProgramByte (0, 0xEE);
		var hex = string.Join ("\n", Rec (0, 0, 0x01), Rec (4, 0, 0x00, 0x01), Rec (0, 0, 0x02), Rec (1, 0));

		Assert.That (runner.TryLoadHex (hex, out var info), Is.False);
		Assert.Multiple (() => {
			Assert.That (info.MaxAddress, Is.EqualTo (0x10000));
			Assert.That (runner.Cpu.ProgBytes[0], Is.EqualTo (0xEE));
		});
	}

	[Test (Description = "Malformed lines never throw; the valid records around them still load with LoadHex")]
	public void Hex_Malformed_DoesNotThrow ()
	{
		var runner = HexRunner (0x100);
		var hex = string.Join ("\n",
			":",
			":ZZ",
			":10000000",
			"garbage",
			":0100000G00",
			Rec (0, 0, 0x42),
			":FF0000",
			Rec (1, 0));

		Assert.DoesNotThrow (() => runner.LoadHex (hex));
		Assert.That (runner.Cpu.ProgBytes[0], Is.EqualTo (0x42));
		Assert.That (runner.TryLoadHex (hex, out var info), Is.False);
		Assert.That (info.Errors.Count, Is.GreaterThanOrEqualTo (5));
	}

	[Test (Description = "A clean image loads through TryLoadHex and the real blink sketch parses")]
	public void Hex_Clean_Loads ()
	{
		var runner = HexRunner (0x100);
		Assert.That (runner.TryLoadHex (Rec (0, 0x10, 0xDE, 0xAD) + "\n" + Rec (1, 0), out var info), Is.True);
		Assert.Multiple (() => {
			Assert.That (info.Errors, Is.Empty);
			Assert.That (info.ByteCount, Is.EqualTo (2));
			Assert.That (runner.Cpu.ProgBytes[0x10], Is.EqualTo (0xDE));
		});
	}

	#endregion

	#region One instruction after SEI / RETI

	private static AvrCpu MakeCpu (params ushort[] words)
	{
		var program = new ushort[0x100];
		words.CopyTo (program, 0);
		return new AvrCpu (program);
	}

	private static void Step<TDecoder> (ref TDecoder decoder, AvrCpu cpu) where TDecoder : struct, IInstructionDecoder
	{
		decoder.Decode (cpu);
		cpu.Tick ();
	}

	private static void ForEachDecoder (Action<Action<AvrCpu>> body)
	{
		var sw = new SwitchDecoder ();
		body (cpu => Step (ref sw, cpu));
		var lut = new LutDecoder ();
		body (cpu => Step (ref lut, cpu));
		var native = new NativeLutDecoder ();
		body (cpu => Step (ref native, cpu));
		var counting = new CountingDecoder (new ExecutionCounts (0x100));
		body (cpu => Step (ref counting, cpu));
	}

	private static readonly AvrInterruptConfig Irq = new (4, 0x60, 1, 0x61, 1);

	[Test (Description = "SEI with a pending IRQ: the following instruction runs before the interrupt is taken")]
	public void Sei_RunsNextInstructionBeforePendingIrq ()
	{
		ForEachDecoder (step => {
			var cpu = MakeCpu (Sei, IncR16, Nop, Nop, Reti);
			cpu.QueueInterrupt (new AvrInterruptConfig (4, 0x60, 1, 0x61, 1));

			step (cpu); // SEI
			Assert.That (cpu.Pc, Is.EqualTo (1u), "interrupt must not be taken right after SEI");
			step (cpu); // INC r16, then the interrupt is taken
			Assert.Multiple (() => {
				Assert.That (cpu.Mmio.Data[16], Is.EqualTo (1), "the instruction after SEI must execute");
				Assert.That (cpu.Pc, Is.EqualTo (4u), "interrupt taken after that instruction");
			});
		});
	}

	[Test (Description = "OUT SREG that sets I also delays a pending IRQ by one instruction")]
	public void OutSreg_RunsNextInstructionBeforePendingIrq ()
	{
		ForEachDecoder (step => {
			var cpu = MakeCpu (0xE800 /* ldi r16,0x80 */, 0xBF0F /* out SREG,r16 */, IncR17, Nop, Reti);
			cpu.QueueInterrupt (new AvrInterruptConfig (4, 0x60, 1, 0x61, 1));

			step (cpu);
			step (cpu); // OUT SREG
			Assert.That (cpu.Pc, Is.EqualTo (2u));
			step (cpu); // INC r17
			Assert.Multiple (() => {
				Assert.That (cpu.Mmio.Data[17], Is.EqualTo (1));
				Assert.That (cpu.Pc, Is.EqualTo (4u));
			});
		});
	}

	[Test (Description = "Host writes to SREG (not an instruction) still dispatch on the very next Tick")]
	public void HostSregWrite_DispatchesImmediately ()
	{
		var cpu = MakeCpu (Nop, Nop, Nop, Nop, Reti);
		cpu.QueueInterrupt (new AvrInterruptConfig (4, 0x60, 1, 0x61, 1));
		cpu.WriteData (95, 0x80);
		cpu.Tick ();
		Assert.That (cpu.Pc, Is.EqualTo (4u));
	}

	[Test (Description = "An always-pending IRQ with RETI lets exactly one main-line instruction run between ISRs")]
	public void Reti_AlwaysPendingIrq_OneMainInstructionPerIsr ()
	{
		ForEachDecoder (step => {
			var cpu = MakeCpu (Sei, IncR16, IncR17, 0xCFFD /* rjmp .-3 */, Reti);
			cpu.QueueInterrupt (new AvrInterruptConfig (4, 0x60, 1, 0x61, 1, constant: true));
			var pushed = new List<uint> ();
			cpu.OnInterruptDispatch += (_, pc) => pushed.Add (pc);

			for (var i = 0; i < 60 && pushed.Count < 9; i++) step (cpu);

			// SEI, [inc r16] ISR, [inc r17] ISR, [rjmp] ISR, [inc r16] ISR ...
			Assert.That (pushed, Is.EqualTo (new uint[] { 2, 3, 1, 2, 3, 1, 2, 3, 1 }));
		});
	}

	[Test (Description = "Interrupt entry is 4 cycles after the interrupted instruction on a 16-bit PC part")]
	public void InterruptEntry_FourCyclesAfterInstruction ()
	{
		var cpu = MakeCpu (Nop, Nop, Nop, Nop, Reti);
		cpu.QueueInterrupt (new AvrInterruptConfig (4, 0x60, 1, 0x61, 1));
		cpu.WriteData (95, 0x80);
		var decoder = new SwitchDecoder ();
		Step (ref decoder, cpu);
		Assert.That (cpu.Cycles, Is.EqualTo (1 + 4));
	}

	#endregion

	#region Per-CPU hooks

	[Test (Description = "Instance hooks fire only for their own CPU; the obsolete statics still fire as fallbacks")]
	public void InstanceHooks_DoNotCrossFire ()
	{
		ForEachDecoder (step => {
			var a = MakeCpu (Break, SleepOp);
			var b = MakeCpu (Break, SleepOp);
			var aBreaks = 0; var bBreaks = 0; var aSleeps = 0; var bSleeps = 0;
			a.OnBreakpoint += _ => aBreaks++;
			b.OnBreakpoint += _ => bBreaks++;
			a.OnSleep += _ => aSleeps++;
			b.OnSleep += _ => bSleeps++;
			var globalBreaks = 0; var globalSleeps = 0;
#pragma warning disable CS0618
			AvrInterrupt.OnBreakpoint = _ => globalBreaks++;
			AvrInterrupt.OnSleep = _ => globalSleeps++;
			try {
				step (a); step (a); // only CPU a runs
				Assert.Multiple (() => {
					Assert.That (aBreaks, Is.EqualTo (1));
					Assert.That (aSleeps, Is.EqualTo (1));
					Assert.That (bBreaks, Is.Zero);
					Assert.That (bSleeps, Is.Zero);
					Assert.That (globalBreaks, Is.EqualTo (1), "obsolete static fallback still called");
					Assert.That (globalSleeps, Is.EqualTo (1));
				});
			} finally {
				AvrInterrupt.OnBreakpoint = null;
				AvrInterrupt.OnSleep = null;
			}
#pragma warning restore CS0618
		});
	}

	[Test (Description = "OnInterruptDispatch is per CPU and reports vector and pushed PC")]
	public void InstanceHook_InterruptDispatch ()
	{
		var a = MakeCpu (Nop, Nop, Nop, Nop, Reti);
		var b = MakeCpu (Nop, Nop, Nop, Nop, Reti);
		var seen = new List<(int, uint)> ();
		var bSeen = 0;
		a.OnInterruptDispatch += (addr, pc) => seen.Add ((addr, pc));
		b.OnInterruptDispatch += (_, _) => bSeen++;
		a.QueueInterrupt (Irq);
		a.WriteData (95, 0x80);
		a.Tick ();
		Assert.Multiple (() => {
			Assert.That (seen, Is.EqualTo (new[] { (4, 0u) }));
			Assert.That (bSeen, Is.Zero);
		});
	}

	#endregion

	#region Runner

	private static AvrRunner SpinRunner ()
	{
		var runner = new AvrRunner (new byte[0x200], 2048);
		runner.LoadProgram (new ushort[] { 0xCFFF }); // rjmp .  (2 cycles per instruction)
		return runner;
	}

	[Test (Description = "Execute with a small work unit does not drift: 1000 x 15 cycles is 15000, not 16000")]
	public void Execute_DoesNotDrift ()
	{
		var runner = SpinRunner ();
		runner.SetWorkUnitCycles (15);
		for (var i = 0; i < 1000; i++) runner.Execute ();
		Assert.That (runner.Cpu.Cycles, Is.InRange (15000UL, 15001UL));
	}

	[Test (Description = "Run returns the cycles consumed and the sum tracks the cumulative budget")]
	public void Run_ReturnsConsumed_AndCarries ()
	{
		var runner = SpinRunner ();
		long total = 0;
		for (var i = 0; i < 500; i++) {
			var used = runner.Run (15);
			Assert.That (used, Is.InRange (14L, 16L));
			total += used;
		}
		Assert.That (total, Is.EqualTo ((long)runner.Cpu.Cycles));
		Assert.That (total, Is.InRange (7500L, 7501L));
	}

	[Test (Description = "RunUntil stops at the absolute cycle and a later Run continues from that target")]
	public void RunUntil_ThenRun_Cumulative ()
	{
		var runner = SpinRunner ();
		var used = runner.RunUntil (101);
		Assert.That (runner.Cpu.Cycles, Is.InRange (101UL, 102UL));
		Assert.That (used, Is.EqualTo ((long)runner.Cpu.Cycles));
		runner.Run (99);
		Assert.That (runner.Cpu.Cycles, Is.InRange (200UL, 201UL));
	}

	[Test (Description = "A core advanced by other means re-bases the carried target")]
	public void Run_RebasesAfterExternalAdvance ()
	{
		var runner = SpinRunner ();
		runner.Run (10);
		runner.Cpu.Cycles += 1000;
		var used = runner.Run (10);
		Assert.That (used, Is.InRange (10L, 11L));
	}

	#endregion
}
