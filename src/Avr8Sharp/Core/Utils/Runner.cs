using System.Runtime.CompilerServices;
using AVR8Sharp.Core;
using AVR8Sharp.Core.Decoders;

namespace AVR8Sharp.Core.Utils;

public enum DecoderType { Switch, Lut, NativeLut }

public class AvrRunner(byte[] program, int sramBytes)
{
	public readonly Cpu Cpu = new(program, sramBytes);
	private int _workUnitCycles = 500000;
	
	public uint Speed { get; private set; } = 16_000_000U;

	private DecoderType _activeDecoder = DecoderType.NativeLut;
	private LutDecoder _lutDecoder = new LutDecoder ();
	private SwitchDecoder _switchDecoder = new SwitchDecoder ();
	private NativeLutDecoder _nativeLutDecoder = new NativeLutDecoder ();

	public void SetSpeed (uint speed)
	{
		Speed = speed;
	}

	internal void SetDecoder(DecoderType type)
	{
		_activeDecoder = type;
	}
	
	public void SetWorkUnitCycles (int cycles)
	{
		_workUnitCycles = cycles;
	}
	
	public void LoadProgram (byte[] program)
	{
		Cpu.LoadProgram (program);
	}
	
	public void LoadProgram (ushort[] program)
	{
		Cpu.LoadProgram (program);
	}
	
	/// <summary>
	/// Loads an Intel HEX image into flash (data, extended segment/linear address and EOF
	/// records). Lenient: malformed lines are skipped, bytes beyond the flash are dropped and
	/// checksums are not enforced. Use <see cref="TryLoadHex"/> to find out whether the image was clean.
	/// </summary>
	public void LoadHex (string source)
	{
		var target = new byte[Cpu.ProgBytes.Length];
		IntelHex.Parse (source, target, strict: false);
		Cpu.LoadProgram (target);
	}

	/// <summary>
	/// Loads an Intel HEX image into flash only if it is clean and fits in
	/// <c>Cpu.ProgBytes.Length</c> bytes. Returns false, leaving flash untouched, when a record is
	/// malformed, a checksum fails or the image reaches past the end of flash; the reasons are in
	/// <paramref name="info"/>.
	/// </summary>
	public bool TryLoadHex (string hex, out HexInfo info)
	{
		var target = new byte[Cpu.ProgBytes.Length];
		info = IntelHex.Parse (hex, target);
		if (info.Errors.Count > 0) return false;
		Cpu.LoadProgram (target);
		return true;
	}

	private ulong _runTarget;
	private ulong _runEnd;

	/// <summary>
	/// Runs whole instructions until <paramref name="cycleBudget"/> more cycles of simulated time
	/// have elapsed, carrying the overshoot: the target advances from the previous target, not from
	/// where the core stopped, so repeated small budgets do not drift (a 16-cycle budget spent on
	/// 2-cycle instructions no longer runs ~2 % fast). A core advanced by other means (or reset)
	/// re-bases the target on its cycle counter. Returns the cycles actually consumed.
	/// </summary>
	public long Run (long cycleBudget)
	{
		if (Cpu.Cycles != _runEnd) _runTarget = Cpu.Cycles;
		_runTarget += (ulong)Math.Max (0, cycleBudget);
		return RunToTarget ();
	}

	/// <summary>Same as <see cref="Run"/>; the name used by the SiliconTwin branch.</summary>
	public void RunCycles (long cycles) => Run (cycles);

	/// <summary>
	/// Runs until the cycle counter reaches <paramref name="absoluteCycle"/> (to within one
	/// instruction) and returns the cycles consumed. A later <see cref="Run"/> continues from
	/// that target, so the overshoot is carried.
	/// </summary>
	public long RunUntil (ulong absoluteCycle)
	{
		if (Cpu.Cycles != _runEnd) _runTarget = Cpu.Cycles;
		if (absoluteCycle > _runTarget) _runTarget = absoluteCycle;
		return RunToTarget ();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private long RunToTarget ()
	{
		var start = Cpu.Cycles;
		switch (_activeDecoder) {
			case DecoderType.Switch:
				RunLoop (ref _switchDecoder, _runTarget);
				break;
			case DecoderType.Lut:
				RunLoop (ref _lutDecoder, _runTarget);
				break;
			case DecoderType.NativeLut:
				RunLoop (ref _nativeLutDecoder, _runTarget);
				break;
			default:
				throw new NotImplementedException ("Decoder type not implemented");
		}
		_runEnd = Cpu.Cycles;
		return (long)(Cpu.Cycles - start);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void RunLoop<TDecoder> (ref TDecoder decoder, ulong target) where TDecoder : struct, IInstructionDecoder
	{
		var cpu = Cpu;
		while (cpu.Cycles < target) {
			decoder.Decode (cpu);
			cpu.Tick ();
		}
	}

	public void Execute<TDecoder> (ref TDecoder decoder, Action<Cpu> callback) where TDecoder : struct, IInstructionDecoder
	{
		ExecuteInternal (ref decoder);
		callback.Invoke (Cpu);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ExecuteInternal<TDecoder> (ref TDecoder decoder) where TDecoder : struct, IInstructionDecoder
	{
		if (Cpu.Cycles != _runEnd) _runTarget = Cpu.Cycles;
		_runTarget += (ulong)_workUnitCycles;
		RunLoop (ref decoder, _runTarget);
		_runEnd = Cpu.Cycles;
	}

	public void ExecuteProfiling(ProfilingDecoder decoder)
	{
		var cpu = Cpu;
		if (cpu.Cycles != _runEnd) _runTarget = cpu.Cycles;
		_runTarget += (ulong)_workUnitCycles;
		while (cpu.Cycles < _runTarget)
		{
			decoder.Decode(cpu);
			cpu.Tick();
		}
		_runEnd = cpu.Cycles;
	}

	/// <summary>
	/// Runs one work unit (<see cref="SetWorkUnitCycles"/>). Cumulative like <see cref="Run"/>,
	/// so the per-call overshoot is carried instead of accumulating as clock drift.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Execute()
	{
		Run (_workUnitCycles);
	}
}
