using Manito.Discord;
using Manito.Discord.Client;

using Name.Bayfaderix.Darxxemiyur.Common;

using System.Threading.Tasks;

namespace Manito.System.Economy
{
	public class EconomyLogging : IModule
	{
		#region ToRework

#if DEBUG
		private ulong logid = 973271532681982022;
#else
		private ulong logid = 1027659223070425229;
#endif
		private FIFOFBACollection<string> _logQueue = new();
		private MyDomain _service;

		public EconomyLogging(MyDomain service)
		{
			_service = service;
		}

		public Task ReportTransaction(string str) => _logQueue.Handle(str);

		public Task RunModule() => LogTransactions();

		private async Task LogTransactions()
		{
			while (true)
			{
				var str = await _logQueue.GetData();
				await _service.Logging.WriteLog("TransactionLogging", str);
				var ch = await _service.MyDiscordClient.Client.GetChannelAsync(logid);
				await ch.SendMessageAsync(str);
			}
		}

		#endregion ToRework
	}
}