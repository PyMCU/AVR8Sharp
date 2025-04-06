using System.Diagnostics;
using System.Text;
using AVR8Sharp;
using AVR8Sharp.Peripherals;
using Newtonsoft.Json;
namespace Avr8SharpDemo;

public static class Program
{
	const string HexiUrl = "https://hexi.wokwi.com";
	const string BlinkCode = @"
// Green LED connected to LED_BUILTIN,
// Red LED connected to pin 12. Enjoy!

void setup() {
  Serial.begin(115200);
  pinMode(LED_BUILTIN, OUTPUT);
}

void loop() {
  Serial.println(""AVR8Sharp is awesome!"");
  digitalWrite(LED_BUILTIN, HIGH);
  delay(500);
  digitalWrite(LED_BUILTIN, LOW);
  delay(500);
}
";
	public static readonly HttpClient Client = new HttpClient ();

	public static void Main ()
	{
		// Compile the code using the Hexi API
		var result = Compile (BlinkCode);
		var hex = result.Hex;
		var watch = new Stopwatch ();
		// Create the AVR runner
		var runner = AvrBuilder.Create ()
			.SetSpeed (16_000_000)
			.SetWorkUnitCycles (1_000)
			.SetHex (hex)
			.AddGpioPort (AvrIoPort.PortBConfig, out var portB)
			.AddGpioPort (AvrIoPort.PortCConfig, out _)
			.AddGpioPort (AvrIoPort.PortDConfig, out _)
			.AddUsart (AvrUsart.Usart0Config, out var usart)
			.AddTimer (AvrTimer.Timer0Config, out _)
			.AddTimer (AvrTimer.Timer1Config, out _)
			.AddTimer (AvrTimer.Timer2Config, out _)
			.Build ();
		// Add a listener to the port B to print the state of pins 4 and 5
		portB.AddListener ((newVal, prevVal) => {
			if (prevVal == newVal)
				return;

			Console.WriteLine ($"Pin 4: {portB.GetPinState (4)}");
			Console.WriteLine ($"Pin 5: {portB.GetPinState (5)}");

			var millis = runner.Cpu.Cycles / 16_000_000.0 * 1000;
			Console.WriteLine ($"CPU Time: {millis} ms");
			Console.WriteLine ($"Time: {watch.Elapsed.TotalMilliseconds} ms");
			Console.WriteLine ();
		});
		// Add a listener when a byte is transmitted
		var builder = new StringBuilder ();
		usart.OnByteTransmit = b => {
			var c = (char)b;
			builder.Append (c);
			if (c != '\n') return;
			Console.WriteLine ($"Serial Output: {builder.ToString ().Trim ()}");
			builder.Clear ();
			var millis = (runner.Cpu.Cycles / 16_000_000.0) * 1000;
			Console.WriteLine ($"CPU Time: {millis} ms");
			Console.WriteLine ($"Time: {watch.Elapsed.TotalMilliseconds} ms");
			Console.WriteLine ();
		};
		Console.WriteLine ("Running...");
		const int fiveSecs = (int)(5.5 * 16_000_000);
		watch.Start ();
		// Run the program for 5.5 seconds
		while (runner.Cpu.Cycles < fiveSecs) {
			runner.Execute ();
			var millis = (runner.Cpu.Cycles / 16_000_000.0) * 1000;
			// Sync the real time with the CPU time
			while (watch.Elapsed.TotalMilliseconds < millis) {
				// Wait for the real time to catch up
			}
		}
		watch.Stop ();
		Console.WriteLine (builder.ToString ());
		Console.WriteLine ($"Execution time: {watch.ElapsedMilliseconds} ms");
	}

	public static HexiResult Compile (string source)
	{
		var content = new StringContent (JsonConvert.SerializeObject (new {
			sketch = source,
		}), Encoding.UTF8, "application/json");
		var response = Client.PostAsync ($"{HexiUrl}/build", content).Result;
		var result = response.Content.ReadAsStringAsync ().Result;
		return JsonConvert.DeserializeObject<HexiResult> (result) ?? new HexiResult ();
	}

	public class HexiResult
	{
		public string Stdout { get; set; }
		public string Stderr { get; set; }
		public string Hex { get; set; }
	}
}