using Manito.Discord.PatternSystems.Common;
using Manito.Discord.Rules.GUI;

using MongoDB.Driver.Linq;

using Name.Bayfaderix.Darxxemiyur.Common;

using System.Diagnostics;

namespace Tests
{
	[TestClass]
	public class UnitTSSSSS
	{
		[TestMethod("ErrorCheck1")]
		public async Task TestMethod1()
		{
			var source = new MyTaskSource();

			Console.WriteLine(await source.TrySetResultAsync());

			await source.MyTask;
			Console.WriteLine(1000);
		}
		[TestMethod("LineFillCheck")]
		public async Task Method3F()
		{
			var items = Enumerable.Range(1, 54).Select(x => (ItemFrameBase)new ItemFrame<string>($"name1{x}", EditorType.String, null));
			var redactor = new ItemRedactor(null, items.ToList());

		}
		[TestMethod("LineCheck")]
		public async Task Method3N()
		{
			var items = Enumerable.Range(1, 54).Select(x => (ItemFrameBase)new ItemFrame<string>($"name1{x}", EditorType.String, null));
			var redactor = new ItemRedactor(null, items.ToList());

		}

		[TestMethod("ErrorCheck2")]
		public async Task Method3()
		{
			var canc = new CancellationTokenSource();
			var source = new MyTaskSource<string>(canc.Token);

			try
			{
				var vvv = new MyRelayTask<string>(Task.Run(async () => {
					await Task.Delay(6000);
					return "fffffffff";
				}));
				Console.WriteLine(await vvv.TheTask);
			}
			catch (Exception e)
			{
				Debug.Assert(false, e.ToString());
			}
		}

		[TestMethod("ErrorCheck3")]
		public async Task TestMethod3()
		{
			var source = new MyTaskSource();

			Console.WriteLine(await source.TrySetCanceledAsync());

			try
			{
				await source.MyTask;
			}
			catch (Exception e)
			{
				Debug.Assert(false, e.ToString());
			}
		}
	}
}