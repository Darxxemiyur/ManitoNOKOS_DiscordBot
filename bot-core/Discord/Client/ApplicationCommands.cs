using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.Discord.Client
{
	public class ApplicationCommands
	{
		private MyDomain _collection;
		public DiscordClient Client => _collection.MyDiscordClient.Client;

		public ApplicationCommands(MyDomain collection)
		{
			_collection = collection;
			Commands = new Dictionary<string, IEnumerable<DiscordApplicationCommand>>();
		}

		public async Task UpdateCommands()
		{
			Client.Ready += DoUpdateCommands;
		}

		public void Add(string key, IEnumerable<DiscordApplicationCommand> value) =>
			Commands.Add(key, value);

		private async Task NotifyAdminOnline()
		{
			while (true)
			{
				var admins = _collection.Filters.AdminOrder.Pool.AdminsOnline;

				var msg = $"за {admins} админами в сети";
				var activity = new DiscordActivity(msg, ActivityType.Watching);
				try
				{
					await _collection.MyDiscordClient.Client.UpdateStatusAsync(activity);
				}
				catch { }
				await Task.Delay(20000);
			}
		}

		private async Task DoUpdateCommands(DiscordClient client, ReadyEventArgs args)
		{
			Client.Ready -= DoUpdateCommands;
			var commands = Commands.SelectMany(x => x.Value);
			args.Handled = true;
			await _collection.ExecutionThread.AddNew(new ExecThread.Job(() => Client.BulkOverwriteGlobalApplicationCommandsAsync(commands)));
			await _collection.ExecutionThread.AddNew(new ExecThread.Job(() => NotifyAdminOnline()));
		}

		public readonly Dictionary<string, IEnumerable<DiscordApplicationCommand>> Commands;

		public async Task Autocomplete()
		{
		}
	}
}