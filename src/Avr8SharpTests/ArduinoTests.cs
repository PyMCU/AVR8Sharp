using System.Diagnostics;
using System.Text;
using AVR8Sharp;
using AVR8Sharp.Peripherals;
using Newtonsoft.Json;
namespace AVr8SharpTests;

public class Arduino
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
  Serial.println(""Blink"");
  digitalWrite(LED_BUILTIN, HIGH);
  delay(500);
  digitalWrite(LED_BUILTIN, LOW);
  delay(500);
}
";
	private const string ATtiny85Example = @"
// LEDs connected to pins PB0...PB3

byte leds[] = {PB0, PB1, PB2, PB3};
void setup() {
  for (byte i = 0; i < sizeof(leds); i++) {
    pinMode(leds[i], OUTPUT);
  }
}

int i = 0;
void loop() {
  digitalWrite(leds[i], HIGH);
  delay(250);
  digitalWrite(leds[i], LOW);
  i = (i + 1) % sizeof(leds);
}
";
	private const int FLASH_SIZE = 0x8000;
	private static AvrExternalInterrupt INT0 = new AvrExternalInterrupt (eicr: 0x55, iscOffset: 0, eimsk: 0x5b, eifr: 0x5a, index: 6, interrupt: 1);
	private static AvrPortConfig attinyPortB = new AvrPortConfig (
		pin: 0x36, 
		ddr: 0x37, 
		port: 0x38, 
		pinChange: new AvrPinChangeInterrupt (pcie: 5, pcicr: 0x5b, pcifr: 0x5a, pcmsk: 0x35, pinChangeInterrupt: 2, mask: 0x3f, offset: 0),
		externalInterrupts: new [] { null, null, INT0, }
	);
	private static AvrTimerConfig attinyTimer0 = new AvrTimerConfig (
		bits: 8,
		captureInterrupt: 0,
		tccra: 0x4a,
		tccrb: 0x53,
		tccrc: 0x00,
		tcnt: 0x52,
		ocra: 0x49,
		ocrb: 0x48,
		ocrc: 0,
		icr: 0,
		tifr: 0x58,
		timsk: 0x59,
		overflowInterrupt: 5,
		comparatorAInterrupt: 0xa,
		comparatorBInterrupt: 0xb,
		comparatorCInterrupt: 0,
		comparatorPortA: attinyPortB.PORT,
		comparatorPinA: 0,
		comparatorPortB: attinyPortB.PORT,
		comparatorPinB: 1,
		comparatorPortC: 0,
		comparatorPinC: 0,
		externalClockPort: attinyPortB.PORT,
		externalClockPin: 2,
		dividers: AvrTimer.Timer0Config.Dividers,
			
		// TIFR bits
		tov: 2,
		ocfa: 16,
		ocfb: 8,
		ocfc: 0,
			
		// TIMSK bits
		toie: 2,
		ociea: 16,
		ocieb: 8,
		ociec: 0
	);

	public static HttpClient _Client;

	static Arduino ()
	{
		_Client = new HttpClient ();
	}

	[SetUp]
	public void Setup ()
	{
	}

	#if !DEBUG
	[Test]
    #endif
	public void Run ()
	{
		var hexi = new Arduino ();
		var result = hexi.Compile (BlinkCode);
		var hex = result.Hex;
		var runner = AvrBuilder.Create ()
			.SetSpeed (16_000_000)
			.SetHex (hex)
			.AddGpioPort (AvrIoPort.PortBConfig, out var portB)
			.AddGpioPort (AvrIoPort.PortCConfig, out _)
			.AddGpioPort (AvrIoPort.PortDConfig, out _)
			.AddUsart (AvrUsart.Usart0Config, out var usart)
			.AddTimer (AvrTimer.Timer0Config, out _)
			.AddTimer (AvrTimer.Timer1Config, out _)
			.AddTimer (AvrTimer.Timer2Config, out _)
			.Build ();
		
		var builder = new StringBuilder ();
		usart.OnByteTransmit = b => {
			builder.Append ((char)b);
		};
		const int fiveSecs = 5 * 16_000_000;
		var watch = Stopwatch.StartNew ();
		while (runner.Cpu.Cycles < fiveSecs) {
			runner.Execute ();
		}
		watch.Stop ();
		Assert.That (builder.ToString (), Does.Contain ("Blink"));
		Assert.That (watch.ElapsedMilliseconds, Is.LessThan (5500));
	}

	#if !DEBUG
	[Test]
    #endif
	public void ATtiny85 ()
	{
		var compileResult = Compile (ATtiny85Example);
			
		var runner = AvrBuilder.Create ()
			.SetSpeed (8_000_000)
			.SetHex (compileResult.Hex ?? "")
			.SetWorkUnitCycles (1)
			.AddGpioPort (attinyPortB, out var port)
			.AddTimer (attinyTimer0, out var timer)
			.Build ();
		
		var stringBuilder = new StringBuilder ();
		port.AddListener ((value, oldValue) => {
			stringBuilder.Append (value.ToString ());
		});
		
		const int fiveSecs = 5 * 16_000_000;
		while (runner.Cpu.Cycles < fiveSecs) {
			runner.Execute ();
		}
		
		Assert.True (true);
	}

	public void LoadHex (string source, byte[] target)
	{
		foreach (var line in source.Split ('\n')) {
			if (!string.IsNullOrEmpty (line) && line[0] == ':' && line.Substring (7, 2) == "00") {
				var bytes = Convert.ToInt32 (line.Substring (1, 2), 16);
				var addr = Convert.ToInt32 (line.Substring (3, 4), 16);
				for (var i = 0; i < bytes; i++) {
					target[addr + i] = Convert.ToByte (line.Substring (9 + i * 2, 2), 16);
				}
			}
		}
	}

	public HexiResult Compile (string source)
	{
		var content = new StringContent (JsonConvert.SerializeObject (new {
			sketch = source,
		}), Encoding.UTF8, "application/json");
		var response = _Client.PostAsync ($"{HexiUrl}/build", content).Result;
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
