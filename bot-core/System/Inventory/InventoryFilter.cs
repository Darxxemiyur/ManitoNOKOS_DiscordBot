using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;

using Manito.Discord.Client;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.Discord.Inventory
{
	public class InventoryFilter : IModule
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

		private InventoryCommands _commands;
		private InventoryController _controller;
		public InventoryController Controller => _controller;
		private List<DiscordApplicationCommand> _commandList;
		private DiscordEventProxy<InteractionCreateEventArgs> _queue;

		public InventoryFilter(MyDomain service, EventBuffer eventBuffer)
		{
			_controller = new(service);
			_commands = new InventoryCommands(_controller, service.Inventory);
			_commandList = _commands.GetCommands().ToList();
			//service.MyDiscordClient.AppCommands.Add("Inventory", _commandList);
			_queue = new();
			//eventBuffer.Interact.OnMessage += _queue.Handle;
		}

		public async Task FilterMessage(DiscordClient client, InteractionCreateEventArgs args)
		{
			var res = _commands.Search(args.Interaction);
			if (res != null)
			{
				await res(args.Interaction);
				args.Handled = true;
			}
		}
	}
}