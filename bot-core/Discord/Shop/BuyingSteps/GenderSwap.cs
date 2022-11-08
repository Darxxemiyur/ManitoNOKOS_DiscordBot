using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatNew;
using Manito.Discord.Client;
using Manito.Discord.Orders;
using Manito.System.Economy;

using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Manito.Discord.Shop.BuyingSteps
{
	public class GenderSwap : IDialogueNet
	{
		public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

		public NextNetworkInstruction GetStartingInstruction(object payload) => new(Start);

		public NextNetworkInstruction GetStartingInstruction() => new(Start);

		private ShopItem.InCart _item;
		private DialogueTabSession<ShopContext> _session;
		private int _id;
		private bool _isMale;
		private PlayerWallet Wallet => _session.Context.Wallet;

		public GenderSwap(DialogueTabSession<ShopContext> session, ShopItem item)
		{
			_session = session;
			_item = new ShopItem.InCart(item, 1);
		}

		private async Task<NextNetworkInstruction> CantAfford(NetworkInstructionArgument args)
		{
			var ms1 = $"Вы не можете позволить  {_item.Name} за {_item.Price}.";
			var rsp = await _session.Context.Format.GetResponse(_session.Context.Format.BaseContent().WithDescription($"{ms1}"));

			var cancel = new DiscordButtonComponent(ButtonStyle.Danger, "Cancel", "Ок");
			rsp.AddComponents(cancel);

			await _session.SendMessage(rsp);

			await _session.GetComponentInteraction();

			return new(true);
		}

		private async Task<NextNetworkInstruction> Start(NetworkInstructionArgument args)
		{
			var wallet = _session.Context.Wallet;
			var resp = _session.Context.Format;

			if (!await wallet.CanAfford(_item.Price))
				return new(CantAfford);

			await wallet.Withdraw(_item.Price, $"Покупка {_item.Name} за {_item.Price}");
			return new(SelectID);
		}

		private async Task<NextNetworkInstruction> SelectID(NetworkInstructionArgument args)
		{
			var idi = await Common.GetQuantity(new[] { -5, -2, 1, 2, 5 }, new[] { 1, 10, 100 }, _session, (x, y) => Task.FromResult(true), async x => await _session.Context.Format.GetResponse(_session.Context.Format.BaseContent().WithDescription($"ID получающий ваш заказ - {x}")), _id);

			if (idi is not int id)
				return new(Revert);

			_id = id;
			return new(SelectGender);
		}

		private async Task<NextNetworkInstruction> SelectGender(NetworkInstructionArgument args)
		{
			var male = new DiscordButtonComponent(ButtonStyle.Primary, "male", "Самец", false, new DiscordComponentEmoji("♂️"));
			var female = new DiscordButtonComponent(ButtonStyle.Primary, "female", "Самка", false, new DiscordComponentEmoji("♀️"));
			var ret = new DiscordButtonComponent(ButtonStyle.Danger, "exit", "Назад");

			var msg = (await _session.Context.Format.GetResponse(new DiscordEmbedBuilder().WithDescription("Выберите желаемый пол динозавра."))).AddComponents(male, female).AddComponents(ret);

			await _session.SendMessage(msg);
			var click = await _session.GetComponentInteraction();

			if (click.CompareButton(ret))
				return new(SelectID);

			if (click.CompareButton(male))
				_isMale = true;
			if (click.CompareButton(female))
				_isMale = false;

			return new(ExecuteTransaction);
		}

		private async Task<NextNetworkInstruction> Revert(NetworkInstructionArgument args)
		{
			await Wallet.Deposit(_item.Price);
			return new(false);
		}

		private async Task<NextNetworkInstruction> ExecuteTransaction(NetworkInstructionArgument args)
		{
			var id = _id;
			var order = new Order(_session.Context.CustomerId, $"{_item.Name} для игрока с айди {id}");
			var seq = new List<Order.Step> {
				new Order.ShowInfoStep($"Выдача `{_item.Name.ToLower()}` игроку {id}"),
				new Order.ConfirmationStep(id, $"Подтвердите `{_item.Name.ToLower()}` игроком с айди {id}", $"`/m {id} Вы подтверждаете {_item.Name.ToLower()} для Вашего дино? (Да/Нет)`", $"Игрок с айди {id} отклонил Ваш заказ."),
				new Order.ChangeStateStep(),
				new Order.InformStep(id, $"Уведомите игрока с айди {id} о выполнении заказа", $"`/m {id} Происходит исполнение заказа`"),
			};

			seq.Add(new Order.CommandStep(id, $"Выдача `{_item.Name.ToLower()}` игроку с айди {id}", string.Format(_item.Item.RelatedCommand, id, _isMale ? "male" : "female")));

			seq.Add(new Order.InformStep(id, $"Уведомите игрока с айди {id} о завершении заказа", $"`/m {id} Заказ №{order.OrderId}({_item.Name}) выполнен!`"));
			order.SetSteps(seq);

			await _session.Client.Domain.ExecutionThread.AddNew(new ExecThread.Job(async (x) => await NetworkCommon.RunNetwork(new OrderAwait(new(new SessionFromMessage(_session.Client, await _session.SessionChannel, _session.Context.CustomerId)), order, _item, Wallet))));

			return new(false);
		}
	}
}