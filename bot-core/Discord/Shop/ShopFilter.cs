using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;

using Manito.Discord.Client;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.Discord.Shop
{
	public class ShopFilter : IModule
	{
		public async Task RunModule()
		{
			while (true)
			{
				var data = (await _queue.GetData()).Item2;
				await _service.ExecutionThread.AddNew(new ExecThread.Job(() => HandleAsCommand(data)));
			}
		}

		private ShopService _shopService;
		private MyDomain _service;
		private List<DiscordApplicationCommand> _commandList;
		private DiscordEventProxy<DiscordInteraction> _queue;

		public ShopFilter(MyDomain service, EventBuffer eventBuffer)
		{
			_service = service;
			_shopService = service.ShopService;
			_commandList = GetCommands().ToList();
			service.MyDiscordClient.AppCommands.Add("Shop", _commandList);
			_queue = new();
			eventBuffer.Interact.OnToNextLink += FilterMessage;
		}

		private IEnumerable<DiscordApplicationCommand> GetCommands()
		{
			yield return new DiscordApplicationCommand("shopping", "Начать шоппинг");
		}

		private async Task FilterMessage(DiscordClient client, InteractionCreateEventArgs args)
		{
			if (!_commandList.Any(x => args.Interaction.Data.Name.Contains(x.Name)))
				return;

			await _queue.Handle(client, args.Interaction);
			args.Handled = true;
		}

		private async Task HandleAsCommand(DiscordInteraction args)
		{
			if (await _shopService.StartSession(args.User, args) == null)
			{
				await args.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(_shopService.Default().WithDescription("Вы уже открыли магазин!")).AsEphemeral(true));
			}
		}
	}
}