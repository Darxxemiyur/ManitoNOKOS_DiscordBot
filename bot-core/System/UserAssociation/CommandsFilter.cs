using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;

using Manito.Discord;
using Manito.Discord.Client;
using Manito.System.UserAssociation;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Manito.System.UserAssociaton
{
	public class UserAssociationFilter : IModule
	{
		private MyDomain _service;
		private DiscordEventProxy<InteractionCreateEventArgs> _queue;
		private UserAssociatonCommands _commands;
		private List<DiscordApplicationCommand> _commandList;

		public UserPermissionChecker PermissionChecker {
			get; private set;
		}

		public UserAssociationFilter(MyDomain service, EventBuffer eventBuffer)
		{
			_queue = new();
			_commands = new();
			PermissionChecker = new(_service = service);
			_commandList = _commands.GetCommands();
			//service.MyDiscordClient.AppCommands.Add("UserAssoc", _commandList);
			//eventBuffer.Interact.OnMessage += FilterMessage;
		}

		public async Task FilterMessage(DiscordClient client, InteractionCreateEventArgs args)
		{
			var res = _commands.Search(args.Interaction);
			if (res == null)
				return;

			await res(args.Interaction);
			args.Handled = true;
		}

		public Task RunModule() => Task.CompletedTask;
	}
}