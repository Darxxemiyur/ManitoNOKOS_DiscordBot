using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatNew;
using Manito.Discord.Client;
using Manito.Discord.Orders;

using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Manito.Discord.Shop
{
	public class FoodOrderAwait : IDialogueNet
	{
		private readonly DialogueTabSession<ShopContext> _session;
		private readonly ShopItem.InCart _item;

		public FoodOrderAwait(DialogueTabSession<ShopContext> session, ShopItem.InCart item) => (_session, _item) = (session, item);

		public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

		public NextNetworkInstruction GetStartingInstruction() => new(GetId);

		public NextNetworkInstruction GetStartingInstruction(object payload) => throw new NotImplementedException();

		private async Task<NextNetworkInstruction> GetId(NetworkInstructionArgument args)
		{
			var idi = await Common.GetQuantity(new[] { -5, -2, 1, 2, 5 }, new[] { 1, 10, 100 }, _session, (x, y) => Task.FromResult(true), async x => await _session.Context.Format.GetResponse(_session.Context.Format.BaseContent().WithDescription($"ID получающий ваш заказ - {x}")), 0);

			if (idi is int id)
				return new(WaitForOrder, id);

			await _session.Context.Wallet.Deposit(_item.Price);

			return new(true);
		}

		private async Task<NextNetworkInstruction> WaitForOrder(NetworkInstructionArgument args)
		{
			var id = (int)args.Payload;

			var order = new Order(_session.Context.CustomerId, $"{_item.Name}({_item.Amount}шт) для {id}");
			var seq = new List<Order.Step> {
				new Order.ShowInfoStep($"Выдача `{_item.Name.ToLower()}` в размере {_item.Amount} единиц игроку {id}"),
				new Order.ConfirmationStep(id, $"Подтвердите получение `{_item.Name.ToLower()}` на {_item.Amount} единиц игроком с айди {id}", $"`/m {id} Вы подтверждаете получение {_item.Name.ToLower()} на {_item.Amount}? (Да/Нет)`", $"Игрок с айди {id} отклонил Ваш заказ."),
				new Order.ChangeStateStep(),
				new Order.InformStep(id, $"Уведомите игрока с айди {id} о выполнении заказа", $"`/m {id} Происходит исполнение заказа, пожалуйста не двигайтесь`"),
				new Order.CommandStep(id, $"Телепортирование к {id}", $"`TeleportToP {id}`")
			};

			var size = _item.Amount;
			var carcMax = 2000;
			var pieces = (int)Math.Ceiling((double)size / carcMax);
			var piece = 1;

			while (size > 0)
			{
				var single = Math.Min(size, carcMax);
				size -= single;
				var food = new ShopItem.InCart(_item.Item, single);
				var pieceStr = pieces > 1 ? $"\nЧасть {piece++} из {pieces}" : "";
				seq.Add(new Order.CommandStep(id, $"Выдача {food.Name.ToLower()} на {single} игроку с айди {id}{pieceStr}", food.RelatedCommand));
			}

			order.SetSteps(seq);

			await _session.Client.Domain.ExecutionThread.AddNew(new ExecThread.Job(async (x) => await NetworkCommon.RunNetwork(new OrderAwait(new(new SessionFromMessage(_session.Client, await _session.SessionChannel, _session.Context.CustomerId)), order, _item, _session.Context.Wallet))));

			return new(false);
		}
	}
}