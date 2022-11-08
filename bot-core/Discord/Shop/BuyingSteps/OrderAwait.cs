using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatNew;
using Manito.Discord.Orders;
using Manito.System.Economy;

using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Manito.Discord.Shop
{
	public class OrderAwait : IDialogueNet
	{
		private readonly UniversalSession _session;
		private readonly Order _order;
		private readonly ShopItem.InCart _item;
		private readonly PlayerWallet _wallet;

		public OrderAwait(UniversalSession session, Order order, ShopItem.InCart item, PlayerWallet wallet) => (_session, _order, _item, _wallet) = (session, order, item, wallet);

		private async Task<NextNetworkInstruction> ReleaseAwaiting(NetworkInstructionArgument args)
		{
			var completed = _order.OrderCompleteTask;
			var noRet = new CancellationTokenSource();
			var nonCanc = Task.Run(async () => {
				await _order.OrderNonCancellableTask;
				await Task.Run(noRet.Cancel);
			});
			var cancelled = _order.OrderCancelledTask;

			var time = DateTimeOffset.Now;

			var ms1 = $"Ожидание исполнения Вашего заказа №{_order.OrderId}\nОписание: `{_order.Description}`.";
			var mm1 = $"<t:{time.ToUnixTimeSeconds()}>";
			var mm2 = $"<t:{time.ToUnixTimeSeconds()}:R>";
			var ms2 = $"Поставлен в очередь {mm2}(в {mm1})";

			var rmsg = new DiscordEmbedBuilder().WithDescription($"{ms1}\n{ms2}").WithColor(new DiscordColor(255, 255, 0)).WithTimestamp(DateTimeOffset.Now);

			var cancelBtn = new DiscordButtonComponent(ButtonStyle.Primary, "cancel", "Отменить");
			await _session.SendMessage(new UniversalMessageBuilder().AddEmbed(rmsg).AddComponents(cancelBtn));
			var both = CancellationTokenSource.CreateLinkedTokenSource(_order.AdminOrderCancelToken, noRet.Token);
			var doCancel = _session.GetComponentInteraction(both.Token);
			var list = new List<Task> { completed, nonCanc, cancelled, doCancel };

			await _session.Client.Domain.Filters.AdminOrder.Pool.PlaceOrder(_order);
			var timeout = TimeSpan.FromSeconds(20);

			while (true)
			{
				var first = await Task.WhenAny(list);
				list.Remove(first);

				if (first == nonCanc)
				{
					await _session.SendMessage(((UniversalMessageBuilder)rmsg).AddComponents(cancelBtn.Disable()));
				}
				if ((first == doCancel && !first.IsCanceled) || first == cancelled)
				{
					await _order.TryCancelOrder();

					await _session.SendMessage(new UniversalMessageBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription($"Заказ №{_order.OrderId} отменён.\nПричина: {await cancelled}\nЗакрытие окна через <t:{(DateTimeOffset.Now + timeout).AddSeconds(.85).ToUnixTimeSeconds()}:R>.").WithColor(new DiscordColor(255, 50, 50)).WithTimestamp(DateTimeOffset.Now)));

					await _wallet.Deposit(_item.Price, $"Возврат средств за отменённый заказ №{_order.OrderId}.\nПричина: {await cancelled}");

					break;
				}
				if (first == nonCanc)
				{
					await completed;
					await _session.SendMessage(new UniversalMessageBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription($"Заказ №{_order.OrderId} выполнен.\nЗакрытие окна <t:{(DateTimeOffset.Now + timeout).AddSeconds(.85).ToUnixTimeSeconds()}:R>.").WithColor(new DiscordColor(50, 255, 50)).WithTimestamp(DateTimeOffset.Now)));
					break;
				}
			}

			await Task.Delay(timeout);

			await _session.RemoveMessage();
			await _session.EndSession();

			return new();
		}

		public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

		public NextNetworkInstruction GetStartingInstruction() => new(ReleaseAwaiting);

		public NextNetworkInstruction GetStartingInstruction(object payload) => throw new NotImplementedException();
	}
}