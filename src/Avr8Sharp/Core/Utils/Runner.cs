using System.Runtime.CompilerServices;
using AVR8Sharp.Core.Cpu.Decoders;

namespace AVR8Sharp.Core.Utils;

public enum DecoderType { Switch, Lut, NativeLut }

public class AvrRunner(byte[] program, int sramBytes)
{
	public const int FLASH = 0x8000;

	public readonly Cpu.Cpu Cpu = new(program, sramBytes);
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
		var target = new byte[FLASH];
		foreach (var line in source.Split ('\n')) {
			if (!string.IsNullOrEmpty (line) && line[0] == ':' && line.Substring (7, 2) == "00") {
				var bytes = Convert.ToInt32 (line.Substring (1, 2), 16);
				var addr = Convert.ToInt32 (line.Substring (3, 4), 16);
				for (var i = 0; i < bytes; i++) {
					target[addr + i] = Convert.ToByte (line.Substring (9 + i * 2, 2), 16);
				}
			}
		}
		Cpu.LoadProgram (target);
	}

	public void Execute<TDecoder> (ref TDecoder decoder, Action<Cpu.Cpu> callback) where TDecoder : struct, IInstructionDecoder
	{
		var cyclesToRun = Cpu.Cycles + _workUnitCycles;
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
		var cyclesToRun = cpu.Cycles + _workUnitCycles;
		while (cpu.Cycles < cyclesToRun) {
			decoder.Decode (cpu);
			cpu.Tick ();
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
