using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord;
using Manito.Discord.ChatNew;
using Manito.Discord.Client;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.System.Economy
{
	public class DebugCommands
	{
		private const string Locale = "ru";
		private MyDomain _bot;

		public DebugCommands(MyDomain dom) => (_bot) = (dom);

		public Func<DiscordInteraction, Task> Search(DiscordInteraction command)
		{
			if (command.Data.Type == ApplicationCommandType.Message && command.Data.Name == "Mark for deletion")
				return MarkToDelete;

			foreach (var item in GetCommands())
			{
				if (command.Data.Name.Contains(item.Name))
				{
					foreach (var subItem in GetSubCommands())
					{
						if (command.Data.Options.First().Name.Contains(subItem.Item1.Name))
							return subItem.Item2;
					}
				}
			}
			return null;
		}

		private DiscordApplicationCommandLocalization GetLoc(string trans) => new DiscordApplicationCommandLocalization(new Dictionary<string, string>() { { Locale, trans } });

		public IEnumerable<DiscordApplicationCommand> GetCommands()
		{
			yield return new DiscordApplicationCommand("debug", "Debug",
			 GetSubCommands().Select(x => x.Item1),
			 ApplicationCommandType.ChatInput,
			 GetLoc("дебаг"),
			 GetLoc("Дебаг"));
			yield return new DiscordApplicationCommand("Mark for deletion", "", null,
			 ApplicationCommandType.Message,
			 GetLoc("Отметить для удаления"));
		}

		private IEnumerable<(DiscordApplicationCommandOption, Func<DiscordInteraction, Task>)> GetSubCommands()
		{
			yield return (new DiscordApplicationCommandOption("test_time", "Test time",
			 ApplicationCommandOptionType.SubCommand, false, null, new[] {
				new DiscordApplicationCommandOption("time", "Time", ApplicationCommandOptionType.String,
				 false, nameLocalizations: GetLoc( "время"),
				 descriptionLocalizations: GetLoc( "Время")),
				new DiscordApplicationCommandOption("msg", "Msg", ApplicationCommandOptionType.String,
				 false, nameLocalizations: GetLoc( "msg"),
				 descriptionLocalizations: GetLoc( "Msg"))
			 },
			 nameLocalizations: GetLoc("проверить_время"),
			 descriptionLocalizations: GetLoc("Проверить формат времени")),
			 GetAccountDeposit);
			yield return (new DiscordApplicationCommandOption("reset_db", "Reset database",
			 ApplicationCommandOptionType.SubCommand,
			 nameLocalizations: GetLoc("сбросить_бд"),
			 descriptionLocalizations: GetLoc("Сбросить базу данных")),
			 ResetDatabase);
			yield return (new DiscordApplicationCommandOption("popu_db", "Populate database",
			 ApplicationCommandOptionType.SubCommand),
			 PopulateDatabase);
			yield return (new DiscordApplicationCommandOption("check_wm", "Check welcomming message",
			 ApplicationCommandOptionType.SubCommand,
			 nameLocalizations: GetLoc("проверить_пс"),
			 descriptionLocalizations: GetLoc("Проверить приветственное сообщение")),
			 CheckMessage);
			yield return (new DiscordApplicationCommandOption("check_ds", "Check dialogue system",
			 ApplicationCommandOptionType.SubCommand,
			 nameLocalizations: GetLoc("проверить_дс"),
			 descriptionLocalizations: GetLoc("Проверить диалоговую систему")),
			 CheckDialogue);
		}

		private async Task PopulateDatabase(DiscordInteraction args)
		{
			var session = new ComponentDialogueSession(_bot.MyDiscordClient, args).ToUniversal();

			await using var fdb = await _bot.DbFactory.CreateMyDbContextAsync();
		}

		private async Task MarkToDelete(DiscordInteraction args)
		{
			var rs = new ComponentDialogueSession(_bot.MyDiscordClient, args).ToUniversal();

			var msg = args.Data.Resolved.Messages.First().Value;

			await _bot.MessageRemover.RemoveMessage(msg, TimeSpan.FromMinutes(10));

			await rs.SendMessage($"Сообщение с айди {msg.Id} успешно помечно для удаления через 10 минут");

			await _bot.MessageRemover.RemoveMessage(rs.Identifier.ChannelId, rs.Identifier.MessageId ?? 0, TimeSpan.FromMinutes(1));

			await rs.EndSession();
		}

		private async Task CheckDialogue(DiscordInteraction args)
		{
			var rs = new ComponentDialogueSession(_bot.MyDiscordClient, args).ToUniversal();
			await rs.DoLaterReply();

			var chnl = await _bot.MyDiscordClient.Client.GetChannelAsync(rs.Identifier.ChannelId);

			//var whook = await chnl.CreateWebhookAsync("Chronos");

			var btn = new DiscordButtonComponent(ButtonStyle.Primary, "theidthing", "Press me!!!!");
			//var msg = await whook.ExecuteAsync(((UniversalMessageBuilder)"Shit").AddComponents(btn));
			for (var i = 0; i < 6; i++)
			{
				//await whook.EditMessageAsync(msg.Id, );
				//await intre.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, ((UniversalMessageBuilder)$"Shit{i * 86}").AddComponents(btn));
				for (int j = 1; j < 86; j++)
					await rs.SendMessage(((UniversalMessageBuilder)$"Shit{(i * 86) + j}").AddComponents(btn));
				var intre = await rs.GetComponentInteraction();
			}
			//var mmsg = await chnl.GetMessageAsync(msg.Id);
			//await mmsg.DeleteAsync();

			//await whook.DeleteAsync();

			await rs.SendMessage(new UniversalMessageBuilder().SetContent("Goodi job!").AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "theidthing", "Press me!")));

			var intr = await rs.GetInteraction(InteractionTypes.Component | InteractionTypes.Message);
			if (intr.Type == InteractionTypes.Component)
				await rs.SendMessage(new UniversalMessageBuilder().SetContent("Comp interaction!"));

			if (intr.Type == InteractionTypes.Message)
				await rs.SendMessage(new UniversalMessageBuilder().SetContent("Msg interaction!"));

			await rs.DoLaterReply();
		}

		private async Task CheckMessage(DiscordInteraction args)
		{
			var (guild, msgs) = await _bot.Filters.Welcomer.GetMsg(args.User.Id);
			await args.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

			foreach (var msg in msgs)
				await args.Channel.SendMessageAsync(msg);
		}

		private async Task ResetDatabase(DiscordInteraction args)
		{
			await args.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

			await using var fdb = await _bot.DbFactory.CreateMyDbContextAsync();
			var db = fdb.ImplementedContext.Database;

			await db.EnsureDeletedAsync();
			await db.EnsureCreatedAsync();

			await args.EditOriginalResponseAsync(new UniversalMessageBuilder().SetContent("Done"));
		}

		private async Task CopyStructure()
		{
		}

		/// <summary>
		/// Get user's Account deposit.
		/// </summary>
		/// <returns></returns>
		private async Task GetAccountDeposit(DiscordInteraction args)
		{
			var tools = new AppCommandArgsTools(args);

			var timeString = tools.AddOptArg("time");
			var msgString = tools.AddOptArg("msg");

			DateTimeOffset time = DateTimeOffset.Now + TimeSpan.FromMinutes(10);
			if (tools.GetOptional().Any(x => x.Key == timeString) &&
			 !DateTimeOffset.TryParse(tools.GetStringArg(timeString, false), out time))
				return;

			var msg = new DiscordInteractionResponseBuilder(new DiscordMessageBuilder()
			 .WithEmbed(new DiscordEmbedBuilder()
			 .WithDescription($"<t:{time.ToUnixTimeSeconds()}:R>\n"
			  + $"<t:{time.ToUnixTimeSeconds()}>\n{time}\n{tools.GetStringArg(msgString, false)}")));

			await args.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, msg);
		}
	}
}