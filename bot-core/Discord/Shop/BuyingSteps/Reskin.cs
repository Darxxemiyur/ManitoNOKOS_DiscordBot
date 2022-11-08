﻿using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatNew;
using Manito.Discord.Client;
using Manito.Discord.Orders;
using Manito.System.Economy;

using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Manito.Discord.Shop.BuyingSteps
{
	public class Reskin : IDialogueNet
	{
		public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

		public NextNetworkInstruction GetStartingInstruction(object payload) => throw new NotImplementedException();

		public NextNetworkInstruction GetStartingInstruction() => new(ExecuteTransaction);

		private ShopItem _item;
		private DialogueTabSession<ShopContext> _session;
		private PlayerWallet Wallet => _session.Context.Wallet;

		public Reskin(DialogueTabSession<ShopContext> session, ShopItem item)
		{
			_session = session;
			_item = item;
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

		private async Task<NextNetworkInstruction> ExecuteTransaction(NetworkInstructionArgument args)
		{
			var wallet = _session.Context.Wallet;
			var resp = _session.Context.Format;
			var item = new ShopItem.InCart(_item, 1);

			if (!await wallet.CanAfford(item.Price))
				return new(CantAfford);

			await wallet.Withdraw(item.Price, $"Покупка {item.Name} за {item.Price}");

			var idi = await Common.GetQuantity(new[] { -5, -2, 1, 2, 5 }, new[] { 1, 10, 100 }, _session, (x, y) => Task.FromResult(true), async x => await _session.Context.Format.GetResponse(_session.Context.Format.BaseContent().WithDescription($"ID получающий ваш заказ - {x}")), 0);

			if (idi is not int id)
			{
				await Wallet.Deposit(_item.Price);
				return new();
			}

			var order = new Order(_session.Context.CustomerId, $"{_item.Name} для игрока с айди {id}");
			var seq = new List<Order.Step> {
				new Order.ShowInfoStep($"Выдача `{_item.Name.ToLower()}` игроку {id}"),
				new Order.ConfirmationStep(id, $"Подтвердите `{_item.Name.ToLower()}` игроком с айди {id}", $"`/m {id} Вы подтверждаете исполнение заказа №{order.OrderId}({_item.Name.ToLower()}) для Вашего дино? (Да/Нет)`", $"Игрок с айди {id} отклонил Ваш заказ."),
				new Order.ChangeStateStep(),
				new Order.InformStep(id, $"Уведомите игрока с айди {id} о выполнении заказа", $"`/m {id} Происходит исполнение заказа №{order.OrderId}({_item.Name.ToLower()}).`"),
			};

			seq.Add(new Order.CommandStep(id, $"Выдача `{_item.Name.ToLower()}` игроку с айди {id}", string.Format(_item.RelatedCommand, id)));

			order.SetSteps(seq);

			await _session.Client.Domain.ExecutionThread.AddNew(new ExecThread.Job(async (x) => await NetworkCommon.RunNetwork(new OrderAwait(new(new SessionFromMessage(_session.Client, await _session.SessionChannel, _session.Context.CustomerId)), order, item, Wallet))));

			return new(false);
		}
	}
}