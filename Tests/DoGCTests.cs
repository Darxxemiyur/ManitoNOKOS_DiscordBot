using Manito.Discord.PatternSystems.Common;
using Manito.Discord.Rules.GUI;

using MongoDB.Driver.Linq;

using Name.Bayfaderix.Darxxemiyur.Common;

using System.Diagnostics;

namespace Tests
{
	[TestClass]
	public class GcTests
	{
		[TestMethod("Collection Test")]
		public async Task TestMethod1()
		{
			for (int i = 0; i < 12; i++)
				await DoJub();
		}

		private static async Task DoJub()
		{
			var ggg = 15;

			var mem1 = GC.GetTotalMemory(false);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			var lcoll = new List<FIFOFBACollection<long>>();
			for (var v = 0; v < ggg; v++)
				lcoll.Add(new());

			var mem2 = GC.GetTotalMemory(false);
			GC.WaitForPendingFinalizers();

			foreach (var coll in lcoll)
				for (var i = 0; i < 1e4; i++)
					await coll.Handle(i);

			GC.Collect();
			GC.WaitForPendingFinalizers();

			var mem3 = GC.GetTotalMemory(false);
			GC.WaitForPendingFinalizers();
			foreach (var coll in lcoll)
			{
				coll.Dispose();
			}
			lcoll.Clear();
			lcoll = null;
			GC.Collect();
			GC.WaitForPendingFinalizers();
			var mem4 = GC.GetTotalMemory(false);
			GC.WaitForPendingFinalizers();

			Console.WriteLine($"{mem1} | {mem2} | {mem3} | {mem4}");
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
	}
}