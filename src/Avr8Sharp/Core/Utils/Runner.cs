using System.Globalization;
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
	
	public void LoadHex (string source)
	{
		var flashSize = Cpu.ProgBytes.Length;
		var target = new byte[flashSize];

		foreach (var line in source.AsSpan().EnumerateLines())
		{
			var trimmedLine = line.Trim();

			if (trimmedLine.Length < 11 || trimmedLine[0] != ':') continue;

			if (trimmedLine.Slice(7, 2) is not "00") continue;

			var bytes = int.Parse(trimmedLine.Slice(1, 2), NumberStyles.HexNumber);

			if (trimmedLine.Length < 11 + (bytes * 2)) continue;

			var addr = int.Parse(trimmedLine.Slice(3, 4), NumberStyles.HexNumber);

			for (var i = 0; i < bytes; i++)
			{
				if (addr + i < target.Length)
				{
					target[addr + i] = byte.Parse(trimmedLine.Slice(9 + i * 2, 2), NumberStyles.HexNumber);
				}
			}
		}

		Cpu.LoadProgram (target);
	}

	public void Execute<TDecoder> (ref TDecoder decoder, Action<Cpu> callback) where TDecoder : struct, IInstructionDecoder
	{
		var cyclesToRun = Cpu.Cycles + (ulong)_workUnitCycles;
		while (Cpu.Cycles < cyclesToRun) {
			decoder.Decode (Cpu);
			Cpu.Tick ();
		}
		callback.Invoke (Cpu);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ExecuteInternal<TDecoder> (ref TDecoder decoder) where TDecoder : struct, IInstructionDecoder
	{
		var cpu = Cpu;
		var cyclesToRun = cpu.Cycles + (ulong)_workUnitCycles;
		while (cpu.Cycles < cyclesToRun) {
			decoder.Decode (cpu);
			cpu.Tick ();
		}
	}

	public void ExecuteProfiling(ProfilingDecoder decoder)
	{
		var cpu = Cpu;
		var cyclesToRun = cpu.Cycles + (ulong)_workUnitCycles;
		while (cpu.Cycles < cyclesToRun)
		{
			decoder.Decode(cpu);
			cpu.Tick();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Execute()
	{
		switch(_activeDecoder) {
			case DecoderType.Switch:
				ExecuteInternal (ref _switchDecoder);
				break;
			case DecoderType.Lut:
				ExecuteInternal (ref _lutDecoder);
				break;
			case DecoderType.NativeLut:
				ExecuteInternal (ref _nativeLutDecoder);
				break;
			default:
				throw new NotImplementedException("Decoder type not implemented");
		}
	}
}
