namespace AVr8SharpTests;

[TestFixture]
public class Cpu
{
	[Test(Description = "The initial value of the stack pointer should be the lasr byte of internal SRAM")]
	public void Initial_Stack_Pointer_Value()
	{
		var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024], 0x1000);
		Assert.That(cpu.SP, Is.EqualTo(0x10FF));
	}
	
	[TestFixture]
	public class Events
	{
		[Test(Description = "The queued events should be executed after a given number of cycles")]
		public void Execute_Queued_Events ()
		{
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024], 0x1000);
			var events = new List<KeyValuePair<int, int>> ();
			int[] list = [1, 4, 10, ];
			for (int i = 0; i < list.Length; i++) {
				var value = list[i];
				cpu.AddClockEvent (() => {
					events.Add (new KeyValuePair<int, int> (value, cpu.Cycles));
				}, value);
			}
			for (var i = 0; i < 10; i++) {
				cpu.Cycles++;
				cpu.Tick ();
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
			var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024], 0x1000);
			var events = new List<KeyValuePair<int, int>> ();
			int[] list = [10, 4, 1, ];
			for (int i = 0; i < list.Length; i++) {
				var value = list[i];
				cpu.AddClockEvent (() => {
					events.Add (new KeyValuePair<int, int> (value, cpu.Cycles));
				}, value);
			}
			for (var i = 0; i < 10; i++) {
				cpu.Cycles++;
				cpu.Tick ();
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
		class UpdateClockEvent
		{
			[Test(Description = "The cycles count should be updated according to the number of cycles of the given clock event")]
			public void Update_Cycles_Count_Based_On_Clock_Event ()
			{
				var cpu =  new AVR8Sharp.Cpu.Cpu(new ushort[1024], 0x1000);
				var events = new List<KeyValuePair<int, int>> ();
				var callbacks = new Dictionary<int, Action> ();
				int[] list = [10, 4, 1, ];
				for (int i = 0; i < list.Length; i++) {
					var value = list[i];
					callbacks[value] = cpu.AddClockEvent (() => {
						events.Add (new KeyValuePair<int, int> (value, cpu.Cycles));
					}, value);
				}
				cpu.UpdateClockEvent (callbacks[4], 2);
				cpu.UpdateClockEvent (callbacks[1], 12);
				for (int i = 0; i < 14; i++) {
					cpu.Cycles++;
					cpu.Tick ();
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
			class Clear_Clock_Event
			{
				[Test(Description = "The clock event should be removed from the queue")]
				public void Remove_Clock_Event ()
				{
					var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024], 0x1000);
					var events = new List<KeyValuePair<int, int>> ();
					var callbacks = new Dictionary<int, Action> ();
					int[] list = [1, 4, 10, ];
					foreach (var value in list) {
						var value1 = value;
						callbacks[value] = cpu.AddClockEvent (() => {
							events.Add (new KeyValuePair<int, int> (value1, cpu.Cycles));
						}, value);
					}
					cpu.ClearClockEvent (callbacks[4]);
					for (var i = 0; i < 10; i++) {
						cpu.Cycles++;
						cpu.Tick ();
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
					var cpu = new AVR8Sharp.Cpu.Cpu(new ushort[1024], 0x1000);
					var event4 = cpu.AddClockEvent (() => { }, 4);
					var event10 = cpu.AddClockEvent (() => { }, 10);
					cpu.AddClockEvent (() => { }, 1);
                    Assert.Multiple(() =>
                    {
                        // Both events should be successfully removed
                        Assert.That(cpu.ClearClockEvent(event4), Is.True);
                        Assert.That(cpu.ClearClockEvent(event10), Is.True);
                    });

                    Assert.Multiple(() =>
                    {
                        // And now we should get false, as the events have already been removed
                        Assert.That(cpu.ClearClockEvent(event4), Is.False);
                        Assert.That(cpu.ClearClockEvent(event10), Is.False);
                    });
                }
			}
		}
	}
}
