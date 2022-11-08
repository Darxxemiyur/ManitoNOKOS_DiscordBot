using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;

using Manito.Discord.ChatAbstract;
using Manito.Discord.Client;
using Manito.Discord.Rules.GUI;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.Discord.Rules
{
	public class RulesFilter : IModule
	{
		public Task RunModule() => HandleLoop();

		private async Task HandleLoop()
		{
			while (true)
			{
				var data = (await _queue.GetData()).Item2;
				await HandleAsCommand(data);
			}
		}

		private readonly DiscordEventProxy<DiscordInteraction> _queue;
		private readonly DialogueNetSessionTab<IRulesDbFactory> _aoTab;
		private readonly MyDomain _domain;

		private IEnumerable<DiscordApplicationCommand> GetCommands()
		{
			yield return new DiscordApplicationCommand("rules", "Open rules");
			yield return new DiscordApplicationCommand("rules_edit", "Edit rules");
		}

		private readonly List<DiscordApplicationCommand> _commandList;

		public RulesFilter(MyDomain service, EventBuffer eventBuffer)
		{
			_commandList = GetCommands().ToList();
			_queue = new();
			_aoTab = new(service);
			service.MyDiscordClient.AppCommands.Add("RulesFlt", _commandList);
			eventBuffer.Interact.OnToNextLink += FilterMessage;
		}

		private Task<bool> IsWorthy(DiscordUser user) => _domain.Filters.AssociationFilter.PermissionChecker.DoesHaveAdminPermission(this, user);

		private Task<bool> IsGod(DiscordUser user) => _domain.Filters.AssociationFilter.PermissionChecker.IsGod(user);

		private async Task HandleAsCommand(DiscordInteraction args)
		{
			if (args.Data.Name.Equals("rules"))
			{
				var items = Enumerable.Range(1, 8).Select(x => (ItemFrameBase)new ItemFrame<string>($"name1{x}", EditorType.String, null)).ToList();
				await Task.WhenAll(_aoTab.Sessions.Select(x => x.EndSession()));
				await _aoTab.CreateSession(new(args), _domain.DbFactory, async x => new RulesSelector(x, null));
				return;
			}

			if (!await IsGod(args.User))
			{
				var msgnw = new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription("Недостаточно прав!")).AsEphemeral();
				await args.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, msgnw);
				return;
			}

			if (!await IsWorthy(args.User))
			{
			}

			if (args.Data.Name.Equals("rules_edit"))
			{
			}
		}

		private async Task FilterMessage(DiscordClient client, InteractionCreateEventArgs args)
		{
			if (!_commandList.Any(x => args.Interaction.Data.Name.Contains(x.Name)))
				return;

			await _queue.Handle(client, args.Interaction);
			args.Handled = true;
		}
	}
}