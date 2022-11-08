using Cyriller;

using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;

using Manito.Discord.Client;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.Discord.PermanentMessage
{
	public class MsgWallFilter : IModule
	{
		private MyDomain _domain;
		private List<DiscordApplicationCommand> _commandList;
		private DiscordEventProxy<DiscordInteraction> _queue;

		public MsgWallFilter(MyDomain domain, EventBuffer buffer)
		{
			(_domain, _queue, _commandList) = (domain, new(), GetCommands().ToList());
			domain.MyDiscordClient.AppCommands.Add("MsgControll", _commandList);
			buffer.Interact.OnToNextLink += FilterMessage;
			buffer.ContInteract.OnToNextLink += FilterMessage;
		}

		private const string Locale = "ru";

		private DiscordApplicationCommandLocalization GetLoc(string trans) => new(new() { { Locale, trans } });

		private IEnumerable<DiscordApplicationCommand> GetCommands()
		{
			yield return new DiscordApplicationCommand("msgwall", "Edit wall", null,
				ApplicationCommandType.ChatInput, GetLoc("стенасооб"), GetLoc("Редактировать стены"));
			yield return new DiscordApplicationCommand("Message wall import", "", null,
				ApplicationCommandType.Message, GetLoc("Импорт сообщения в строку стены"));
		}

		private async Task FilterMessage(DiscordClient client, InteractionCreateEventArgs args)
		{
			if (!_commandList.Any(x => args.Interaction.Data.Name.Contains(x.Name)))
				return;

			if (!await IsWorthy(args.Interaction))
				return;

			await _queue.Handle(client, args.Interaction);
			args.Handled = true;
		}

		private async Task FilterMessage(DiscordClient client, ContextMenuInteractionCreateEventArgs args)
		{
			if (!_commandList.Any(x => args.Interaction.Data.Name.Contains(x.Name)))
				return;

			if (!await IsWorthy(args.Interaction))
				return;

			await _queue.Handle(client, args.Interaction);
			args.Handled = true;
		}

		private Task<bool> IsWorthy(DiscordInteraction interaction) => IsWorthy(interaction.User);

		private Task<bool> IsWorthy(DiscordUser user) => _domain.Filters.AssociationFilter.PermissionChecker.DoesHaveAdminPermission(this, user);

		public async Task RunModule()
		{
			while (true)
			{
				var data = (await _queue.GetData()).Item2;
				await HandleAsCommand(data);
			}
		}

		private async Task HandleAsCommand(DiscordInteraction args)
		{
			if (args.Data.Type == ApplicationCommandType.ChatInput)
				await _domain.MsgWallCtr.StartSession(args);

			if (args.Data.Type == ApplicationCommandType.Message)
				await ImportMessage(args);
		}

		private async Task ImportMessage(DiscordInteraction args)
		{
			var msg = args.Data.Resolved.Messages.First().Value;

			_domain.MsgWallCtr.ImportedMessages.Add(new ImportedMessage {
				Message = msg.Embeds.FirstOrDefault()?.Description ??
				(msg.Content.IsNullOrEmpty() ? "" : msg.Content),
				MessageId = msg.Id,
				UserId = args.User.Id
			});

			await args.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
				new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder().
				WithDescription(msg.Content).WithFooter("Успешно импортировано!")).AsEphemeral(true));
		}
	}
}