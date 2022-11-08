using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatNew;
using Manito.Discord.Shop.BuyingSteps;

using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.Discord.Shop
{
	public class ShopDialogue : IDialogueNet
	{
		private DialogueTabSession<ShopContext> _session;

		public ShopDialogue(DialogueTabSession<ShopContext> session) => _session = session;

		public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

		private Task StopSession() => _session.EndSession();

		private IDialogueNet DialogNetwork(ShopItem item) => item.Category switch {
			ItemCategory.SatiationCarcass or ItemCategory.Carcass => new BuyingStepsForMeatFood(_session, item),
			ItemCategory.Plant => new BuyingStepsForPlantFood(_session, item),
			ItemCategory.Reskin => new Reskin(_session, item),
			ItemCategory.SwapGender => new GenderSwap(_session, item),
			ItemCategory.ResetTalent => new TalentsReset(_session, item),
			ItemCategory.EggCheck => new EggCheck(_session, item),
			_ => new BuyingStepsForError(_session),
		};

		public async Task<NextNetworkInstruction> EnterMenu(NetworkInstructionArgument arg)
		{
			try
			{
				await _session.DoLaterReply();
				var exbtn = new DiscordButtonComponent(ButtonStyle.Danger, "Exit", "Выйти");
				while (true)
				{
					var shopItems = _session.Context.CashRegister.GetShopItems();
					var items = _session.Context.Format.GetSelector(shopItems);
					var mg = (await _session.Context.Format.GetResponse(_session.Context.Format.GetShopItems(null, shopItems))).AddComponents(items).AddComponents(exbtn);
					await _session.SendMessage(mg);

					var argv = await _session.GetComponentInteraction();

					if (argv.CompareButton(exbtn))
						break;

					await _session.DoLaterReply();
					var chain = DialogNetwork(argv.GetOption(shopItems.ToDictionary(x => x.Name)));
					await NetworkCommon.RunNetwork(chain);
				}

				await _session.SendMessage(_session.Context.Format.BaseContent().WithDescription("Сессия успешно завершена."));
				await Task.Delay(5000);
				await _session.RemoveMessage();

				await StopSession();
			}
			catch (TimeoutException)
			{
				var ms = "Сессия завершена по причине привышения времени ожидания взаимодействия.";
				await _session.SendMessage(_session.Context.Format.GetDResponse(_session.Context.Format.BaseContent().WithDescription(ms)));
				await Task.Delay(5000);
				await _session.RemoveMessage();
				await StopSession();
				throw;
			}

			return new();
		}

		public NextNetworkInstruction GetStartingInstruction() => new(EnterMenu);

		public NextNetworkInstruction GetStartingInstruction(Object payload) => new(EnterMenu);
	}
}