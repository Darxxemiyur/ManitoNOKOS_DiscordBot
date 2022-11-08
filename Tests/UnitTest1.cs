using Name.Bayfaderix.Darxxemiyur.Common;

namespace Tests
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void TestMethod1()
		{
		}

		[TestMethod("SHITSHITSHIT")]
		public async Task Method3()
		{
			var locker = new AsyncLocker();

			{
				using var f = await locker.BlockAsyncLock();
			}

			{
				using var f = await locker.BlockAsyncLock();
			}

			{
				using var f = await locker.BlockAsyncLock();
			}

			{
				using var f = await locker.BlockAsyncLock();
			}
		}

		[TestMethod("TaskTest5")]
		public async Task TestMethod3()
		{
			var token1 = new CancellationTokenSource();
			var token2 = new CancellationTokenSource();
			var token3 = new CancellationTokenSource();

			token1.Token.Register(() => token2.Cancel());
			token2.Token.Register(() => token3.Cancel());

			Console.WriteLine("DoTheThing");
			Console.WriteLine($"{DateTime.Now}");
			token1.CancelAfter(2000);

			var theTimer = Task.Delay(-1, token3.Token);

			var token4 = new CancellationTokenSource();
			await theTimer.ContinueWith((x) => token4.Cancel());
			Console.WriteLine("DoTheThingsA");
			Console.WriteLine($"{DateTime.Now}");
			var token5 = new CancellationTokenSource();
			var token6 = new CancellationTokenSource();

			token4.Token.Register(() => token5.Cancel());
			token5.Token.Register(() => token6.CancelAfter(2000));
			try
			{
				await Task.Delay(-1, token6.Token);
			}
			catch (TaskCanceledException e)
			{
				Console.WriteLine($"{e}");
			}
			Console.WriteLine("DoTheThings");
			Console.WriteLine($"{DateTime.Now}");
		}

		[TestMethod("TaskTest")]
		public async Task TestMethod2()
		{
			FIFOFBACollection<int> proxy = new();

			await Task.WhenAll(Gen(proxy), Read(proxy));
		}

		private async Task Gen(FIFOFBACollection<int> proxy)
		{
			await Task.WhenAll(Enumerable.Range(1, 500).Select(x => proxy.Handle(x)));
		}

		private async Task Read(FIFOFBACollection<int> proxy)
		{
			while (await proxy.HasAny())
			{
				Console.WriteLine((await proxy.GetData()));
			}
		}
	}
}