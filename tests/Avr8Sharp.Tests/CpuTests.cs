using Avr8Sharp.Tests.Utils;

namespace Avr8Sharp.Tests;

[TestFixture]
public class Cpu : AvrTestBase
{
	protected override ushort SramByteCount => 0x1000;

	[Test(Description = "The initial value of the stack pointer should be the lasr byte of internal SRAM")]
	public void Initial_Stack_Pointer_Value()
	{
		Assert.That(Cpu.Sp, Is.EqualTo(0x10FF));
	}
	
	[TestFixture]
	public class Events : AvrTestBase
	{
		protected override ushort SramByteCount => 0x1000;

		[Test(Description = "The queued events should be executed after a given number of cycles")]
		public void Execute_Queued_Events ()
		{
			var events = new List<KeyValuePair<int, ulong>> ();
			int[] list = [1, 4, 10, ];
			for (int i = 0; i < list.Length; i++) {
				var value = list[i];
				Cpu.AddClockEvent (() => {
					events.Add (new KeyValuePair<int, ulong> (value, Cpu.Cycles));
				}, value);
			}
			for (var i = 0; i < 10; i++) {
				Cpu.Cycles++;
				Cpu.Tick ();
			}
			
			// Events length should be 3
			Assert.That(events, Has.Count.EqualTo(3));
            Assert.Multiple(() =>
            {
	            // events[0] should be (1, 1)
                Assert.That(events[0].Key, Is.EqualTo(1));
                Assert.That(events[0].Value, Is.EqualTo(1));
                // events[1] should be (4, 4)
                Assert.That(events[1].Key, Is.EqualTo(4));
                Assert.That(events[1].Value, Is.EqualTo(4));
                // events[2] should be (10, 10)
                Assert.That(events[2].Key, Is.EqualTo(10));
                Assert.That(events[2].Value, Is.EqualTo(10));
            });
        }

		[Test(Description = "The queued events should be correctly sorted when added in reverse order")]
		public void Order_Reversed_Events ()
		{
			var events = new List<KeyValuePair<int, ulong>> ();
			int[] list = [10, 4, 1, ];
			for (int i = 0; i < list.Length; i++) {
				var value = list[i];
				Cpu.AddClockEvent (() => {
					events.Add (new KeyValuePair<int, ulong> (value, Cpu.Cycles));
				}, value);
			}
			for (var i = 0; i < 10; i++) {
				Cpu.Cycles++;
				Cpu.Tick ();
			}
			
			// Events length should be 3
			Assert.That(events, Has.Count.EqualTo(3));
			Assert.Multiple(() =>
			{
	            // events[0] should be (1, 1)
				Assert.That(events[0].Key, Is.EqualTo(1));
				Assert.That(events[0].Value, Is.EqualTo(1));
				// events[1] should be (4, 4)
				Assert.That(events[1].Key, Is.EqualTo(4));
				Assert.That(events[1].Value, Is.EqualTo(4));
				// events[2] should be (10, 10)
				Assert.That(events[2].Key, Is.EqualTo(10));
				Assert.That(events[2].Value, Is.EqualTo(10));
			});
		}

		[TestFixture]
		class UpdateClockEvent : AvrTestBase
		{
			protected override ushort SramByteCount => 0x1000;

			[Test(Description = "The cycles count should be updated according to the number of cycles of the given clock event")]
			public void Update_Cycles_Count_Based_On_Clock_Event ()
			{
				var events = new List<KeyValuePair<int, ulong>> ();
				var callbacks = new Dictionary<int, Action> ();
				int[] list = [10, 4, 1, ];
				for (int i = 0; i < list.Length; i++) {
					var value = list[i];
					callbacks[value] = Cpu.AddClockEvent (() => {
						events.Add (new KeyValuePair<int, ulong> (value, Cpu.Cycles));
					}, value);
				}
				Cpu.UpdateClockEvent (callbacks[4], 2);
				Cpu.UpdateClockEvent (callbacks[1], 12);
				for (int i = 0; i < 14; i++) {
					Cpu.Cycles++;
					Cpu.Tick ();
				}
				
				// Events length should be 3
				Assert.That(events, Has.Count.EqualTo(3));
				Assert.Multiple(() =>
				{
					// events[0] should be (1, 1)
					Assert.That(events[0].Key, Is.EqualTo(4));
					Assert.That(events[0].Value, Is.EqualTo(2));
					// events[1] should be (4, 4)
					Assert.That(events[1].Key, Is.EqualTo(10));
					Assert.That(events[1].Value, Is.EqualTo(10));
					// events[2] should be (10, 10)
					Assert.That(events[2].Key, Is.EqualTo(1));
					Assert.That(events[2].Value, Is.EqualTo(12));
				});
			}

			[TestFixture]
			class ClearClockEvent : AvrTestBase
			{
				protected override ushort SramByteCount => 0x1000;

				[Test(Description = "The clock event should be removed from the queue")]
				public void Remove_Clock_Event ()
				{
					var events = new List<KeyValuePair<int, ulong>> ();
					var callbacks = new Dictionary<int, Action> ();
					int[] list = [1, 4, 10, ];
					foreach (var value in list) {
						var value1 = value;
						callbacks[value] = Cpu.AddClockEvent (() => {
							events.Add (new KeyValuePair<int, ulong> (value1, Cpu.Cycles));
						}, value);
					}
					Cpu.ClearClockEvent (callbacks[4]);
					for (var i = 0; i < 10; i++) {
						Cpu.Cycles++;
						Cpu.Tick ();
					}
					
					// Events length should be 1
					Assert.That(events, Has.Count.EqualTo(2));
					Assert.Multiple(() =>
					{
						// events[0] should be (1, 1)
						Assert.That(events[0].Key, Is.EqualTo(1));
						Assert.That(events[0].Value, Is.EqualTo(1));
						
						// events[1] should be (10, 10)
						Assert.That(events[1].Key, Is.EqualTo(10));
						Assert.That(events[1].Value, Is.EqualTo(10));
					});
				}

				[Test (Description = "The method should return false if the clock event is not scheduled")]
				public void Not_Scheduled_Event ()
				{
					var event4 = Cpu.AddClockEvent (() => { }, 4);
					var event10 = Cpu.AddClockEvent (() => { }, 10);
					Cpu.AddClockEvent (() => { }, 1);
                    Assert.Multiple(() =>
                    {
                        // Both events should be successfully removed
                        Assert.That(Cpu.ClearClockEvent(event4), Is.True);
                        Assert.That(Cpu.ClearClockEvent(event10), Is.True);
                    });

                    Assert.Multiple(() =>
                    {
                        // And now we should get false, as the events have already been removed
                        Assert.That(Cpu.ClearClockEvent(event4), Is.False);
                        Assert.That(Cpu.ClearClockEvent(event10), Is.False);
                    });
                }
			}
		}
	}
}
