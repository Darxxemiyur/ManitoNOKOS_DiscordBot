using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord.Client;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.Discord.Inventory
{
	public class InventoryCommands
	{
		private const string Locale = "ru";
		private IInventorySystem _inventory;
		private InventoryController _controller;

		public InventoryCommands(InventoryController controller, IInventorySystem inventory) =>
		 (_controller, _inventory) = (controller, inventory);

		public Func<DiscordInteraction, Task> Search(DiscordInteraction command)
		{
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

		private DiscordApplicationCommandLocalization GetLoc(string trans) => new(new() { { Locale, trans } });

		private IEnumerable<(DiscordApplicationCommandOption, Func<DiscordInteraction, Task>)> GetSubCommands()
		{
			yield return (new DiscordApplicationCommandOption("open", "Open inventory",
			 ApplicationCommandOptionType.SubCommand, false, null, new[] {
				 new DiscordApplicationCommandOption("page","Page",
				 ApplicationCommandOptionType.Integer, false),
			 },
			 nameLocalizations: GetLoc("открыть"),
			 descriptionLocalizations: GetLoc("Открыть инвентарь")),
			 ShowInventory);
		}

		public IEnumerable<DiscordApplicationCommand> GetCommands()
		{
			yield return new DiscordApplicationCommand("inventory", "Inventory",
			 GetSubCommands().Select(x => x.Item1),
			 ApplicationCommandType.ChatInput,
			 GetLoc("инвентарь"), GetLoc("Инвентарь"));
		}

		/// <summary>
		/// Show user's inventory
		/// </summary>
		/// <returns></returns>
		private async Task ShowInventory(DiscordInteraction args)
		{
			var tools = new AppCommandArgsTools(args);
			var numArg = tools.AddOptArg("page");
			var page = tools.GetIntArg(numArg, false) ?? 1;

			await _controller.StartSession(args, (x) => new ListItems(x, page - 1));
		}
	}
}