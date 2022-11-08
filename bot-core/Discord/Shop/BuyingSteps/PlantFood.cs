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

namespace Manito.Discord.Shop
{
	public class BuyingStepsForPlantFood : IDialogueNet
	{
		private ShopItem _food;
		private DialogueTabSession<ShopContext> _session;
		private int _quantity;
		private PlayerWallet Wallet => _session.Context.Wallet;

		public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

		public NextNetworkInstruction GetStartingInstruction(object payload) => GetStartingInstruction();

		public NextNetworkInstruction GetStartingInstruction() => new(SelectQuantity, NextNetworkActions.Continue);

		public BuyingStepsForPlantFood(DialogueTabSession<ShopContext> session, ShopItem food)
		{
			_session = session;
			_food = food;
		}

		private async Task<NextNetworkInstruction> SelectQuantity(NetworkInstructionArgument args)
		{
			var ms1 = $"Выберите количество {_food.Name}";
			var price = _food.Price;
			var wallet = _session.Context.Wallet;
			var resp = _session.Context.Format;

			var qua = await Common.GetQuantity(new[] { -5, -2, 1, 2, 5 }, new[] { 1, 5 }, _session, async (x, y) => (y > 0 && await _session.Context.Wallet.CanAfford((x + y) * price)) || (y < 0 && x > 1), async x => await resp.GetResponse(_session.Context.Format.BaseContent().WithDescription($"{ms1}\nВыбранное количество {x} шт за {x * price}.")), _quantity, 1);

			if (!qua.HasValue)
				return new();

			_quantity = qua.Value;

			return new(ExecuteTransaction);
		}

		private async Task<NextNetworkInstruction> ExecuteTransaction(NetworkInstructionArgument args)
		{
			var wallet = _session.Context.Wallet;
			var resp = _session.Context.Format;
			var food = new ShopItem.InCart(_food, _quantity);

			if (!await wallet.CanAfford(food.Price))
				return new(ForceChange);

			await wallet.Withdraw(food.Price, $"Покупка {food.Item.Name} за {food.Item.Price} в кол-ве {food.Amount} за {food.Price}");

			var idi = await Common.GetQuantity(new[] { -5, -2, 1, 2, 5 }, new[] { 1, 10, 100 }, _session, (x, y) => Task.FromResult(true), async x => await _session.Context.Format.GetResponse(_session.Context.Format.BaseContent().WithDescription($"ID получающий ваш заказ - {x}")), 0);

			if (idi is not int id)
			{
				await Wallet.Deposit(food.Price);
				return new();
			}

			var order = new Order(_session.Context.CustomerId, $"{food.Name}({food.Amount}шт) для игрока с айди {id}");
			var seq = new List<Order.Step> {
				new Order.ShowInfoStep($"Выдача `{food.Item.Name.ToLower()}` в размере {food.Amount} единиц игроку {id}"),
				new Order.ConfirmationStep(id, $"Подтвердите получение `{food.Item.Name.ToLower()}` на {food.Amount} единиц игроком с айди {id}", $"`/m {id} Вы подтверждаете получение {food.Item.Name.ToLower()} на {food.Amount}? (Да/Нет)`", $"Игрок с айди {id} отклонил Ваш заказ."),
				new Order.ChangeStateStep(),
				new Order.InformStep(id, $"Уведомите игрока с айди {id} о выполнении заказа", $"`/m {id} Происходит исполнение заказа, пожалуйста не двигайтесь`"),
				new Order.CommandStep(id, $"Телепортирование к {id}", $"`TeleportToP {id}`")
			};

			for (int i = 0; i < food.Amount; i++)
			{
				var pieceStr = food.Amount > 1 ? $"\nЧасть {i + 1} из {food.Amount}" : "";
				seq.Add(new Order.CommandStep(id, $"Выдача `{food.Item.Name.ToLower()}` игроку с айди {id}{pieceStr}", food.RelatedCommand));
			}

			order.SetSteps(seq);

			await _session.Client.Domain.ExecutionThread.AddNew(new ExecThread.Job(async (x) => await NetworkCommon.RunNetwork(new OrderAwait(new(new SessionFromMessage(_session.Client, await _session.SessionChannel, _session.Context.CustomerId)), order, food, _session.Context.Wallet))));

			return new();
		}

		private async Task<NextNetworkInstruction> ForceChange(NetworkInstructionArgument args)
		{
			var wallet = _session.Context.Wallet;
			var resp = _session.Context.Format;

			var price = _quantity * _food.Price;
			var ms1 = $"Вы не можете позволить {_quantity} {_food.Name} за {price}.";
			var ms2 = $"Пожалуйста измените выбранное количество {_food.Name} и попробуйте снова.";
			var rsp = await resp.GetResponse(_session.Context.Format.BaseContent().WithDescription($"{ms1}\n{ms2}"));

			var cancel = new DiscordButtonComponent(ButtonStyle.Danger, "Cancel", "Отмена");
			var chnamt = new DiscordButtonComponent(ButtonStyle.Primary, "Back", "Изменить кол-во");
			rsp.AddComponents(cancel, chnamt);

			await _session.SendMessage(rsp);

			var argv = await _session.GetComponentInteraction();

			if (argv.CompareButton(chnamt))
				return new(SelectQuantity);

			return new();
		}
	}
}