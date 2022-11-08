using DisCatSharp;
using DisCatSharp.EventArgs;

using Manito.Discord;
using Manito.Discord.Client;

using System.Threading.Tasks;

namespace Manito.System.Economy
{
	public class EconomyFilter : IModule
	{
		public Task RunModule() => HandleLoop();

		private async Task HandleLoop()
		{
			while (true)
			{
				var data = await _queue.GetData();
				await FilterMessage(data.Item1, data.Item2);
			}
		}

		private EconomyCommands _commands;
		private DiscordEventProxy<InteractionCreateEventArgs> _queue;
		private MyDomain _domain;

		public EconomyFilter(MyDomain service, EventBuffer eventBuffer)
		{
			_commands = new EconomyCommands(service.Economy, service.MyDiscordClient);
			(_domain = service).MyDiscordClient.AppCommands.Add("Economy", _commands.GetCommands());
			_queue = new();
			eventBuffer.Interact.OnToNextLink += _queue.Handle;
		}

		public async Task FilterMessage(DiscordClient client, InteractionCreateEventArgs args)
		{
			var res = _commands.Search(args.Interaction);
			if (res == null)
				return;

			await _domain.ExecutionThread.AddNew(new ExecThread.Job(() => res(args.Interaction)));
			args.Handled = true;
		}
	}
}