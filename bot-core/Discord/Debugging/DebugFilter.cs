using DisCatSharp;
using DisCatSharp.EventArgs;

using Manito.Discord;
using Manito.Discord.Client;

using System.Threading.Tasks;

namespace Manito.System.Economy
{
	public class DebugFilter : IModule
	{
		public async Task RunModule()
		{
			while (true)
			{
				var data = await _queue.GetData();
				await _service.ExecutionThread.AddNew(new ExecThread.Job(() => FilterMessage(data.Item1, data.Item2)));
			}
		}

		private readonly DebugCommands _commands;
		private readonly DiscordEventProxy<InteractionCreateEventArgs> _queue;
		private readonly MyDomain _service;

		public DebugFilter(MyDomain service, EventBuffer eventBuffer)
		{
			_commands = new DebugCommands(service);
			service.MyDiscordClient.AppCommands.Add("Debug", _commands.GetCommands());
			_queue = new();
			_service = service;
			eventBuffer.Interact.OnToNextLink += _queue.Handle;
			eventBuffer.ContInteract.OnToNextLink += _queue.Handle;
		}

		public async Task FilterMessage(DiscordClient client, InteractionCreateEventArgs args)
		{
			var res = _commands.Search(args.Interaction);
			var checker = _service.Filters.AssociationFilter.PermissionChecker;
			if (res == null || !await checker.IsGod(args.Interaction.User))
				return;

			await res(args.Interaction);
			args.Handled = true;
		}
	}
}