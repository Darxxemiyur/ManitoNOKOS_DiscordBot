using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatNew;
using Manito.Discord.Client;

using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using static Manito.Discord.Orders.Order;

namespace Manito.Discord.Orders
{
	public class AdminOrderExec : IDialogueNet
	{
		public NodeResultHandler StepResultHandler {
			get;
		} = Common.DefaultNodeResultHandler;

		public readonly UniversalSession Session;
		private CancellationTokenSource _quitToken;
		private CancellationTokenSource _swapToken;
		private CancellationTokenSource _cancelOrder;
		private CancellationTokenSource _localToken;
		private readonly AdminOrderPool _pool;
		private readonly DiscordChannel _channel;
		private readonly DiscordUser _admin;
		private IEnumerator<Step> _steps;
		private Order _exOrder;

		public Order ExOrder {
			get => _exOrder;
			set {
				_exOrder = value;
				_steps = value?.Steps?.GetEnumerator();
			}
		}

		public AdminOrderExec(AdminOrderPool pool, UniversalSession session, DiscordChannel channel, DiscordUser user)
		{
			_pool = pool;
			_quitToken = new();
			_swapToken = new();
			_cancelOrder = new();
			Session = session;
			_channel = channel;
			_admin = user;
		}

		public Task StopExecuting() => Task.Run(() => _quitToken.Cancel());

		private async Task<NextNetworkInstruction> Decider(NetworkInstructionArgument arg)
		{
			_localToken = CancellationTokenSource.CreateLinkedTokenSource(_swapToken.Token, _quitToken.Token, _cancelOrder.Token, ExOrder?.PlayerOrderCancelToken ?? CancellationToken.None);
			_steps.MoveNext();
			var step = _steps.Current;
			if (step != null)
			{
				if (step.Type == StepType.ShowInfo)
					return new(DoShowInfo, step);
				if (step.Type == StepType.Confirmation)
					return new(DoConfirmation, step);
				if (step.Type == StepType.ChangeState)
					return new(MakeNonCancallable);
				if (step.Type == StepType.Command)
					return new(DoCommand, step);
				if (step.Type == StepType.Inform)
					return new(DoInform, step);

				throw new NotImplementedException();
			}

			await ExOrder.FinishOrder();
			ExOrder = null;

			return new(FetchNextStep);
		}

		private async Task<NextNetworkInstruction> DoInform(NetworkInstructionArgument arg)
		{
			try
			{
				var step = (InformStep)arg.Payload;

				var asked = new DiscordButtonComponent(ButtonStyle.Primary, "executed", "Выполнено.");
				var embed = new DiscordEmbedBuilder().WithColor(new DiscordColor(255, 255, 0)).WithDescription($"{step.Description}\nНапишите игроку \"{step.Info}\" и нажмите \"{asked.Label}\"").WithTimestamp(DateTimeOffset.Now);
				await Session.SendMessage(new UniversalMessageBuilder().AddEmbed(embed)
					.AddComponents(asked));

				await Session.GetComponentInteraction(_localToken.Token);
				await Session.DoLaterReply();

				return new(Decider);
			}
			catch (TaskCanceledException)
			{
				return new(DoOrderCancellation);
			}
		}

		private async Task<NextNetworkInstruction> DoOrderCancellation(NetworkInstructionArgument arg)
		{
			if (_quitToken.IsCancellationRequested)
			{
				_quitToken = new();
				await _pool.PlaceOrder(ExOrder);
				await _pool.StopAdministrating(this);
				await Session.RemoveMessage();
				await Session.EndSession();
				ExOrder = null;
				return new();
			}

			if (_swapToken.IsCancellationRequested)
			{
				_swapToken = new();
				await _pool.PlaceOrder(ExOrder);
			}

			if (_cancelOrder.Token.IsCancellationRequested)
			{
				_cancelOrder = new();
				await ExOrder.CancelOrder(_cancelReason);
				_cancelReason = null;
			}

			ExOrder = null;
			return new(FetchNextStep);
		}

		private string _cancelReason;

		public Task ChangeOrder() => Task.Run(_swapToken.Cancel);

		private async Task<NextNetworkInstruction> DoConfirmation(NetworkInstructionArgument arg)
		{
			try
			{
				var step = (ConfirmationStep)arg.Payload;

				var asked = new DiscordButtonComponent(ButtonStyle.Primary, "asked", "Опрошено");
				var success = new DiscordButtonComponent(ButtonStyle.Success, "success", "Подтвердить", true);
				var fail = new DiscordButtonComponent(ButtonStyle.Danger, "fail", "Отклонить", true);
				var embed = new DiscordEmbedBuilder().WithColor(new DiscordColor(255, 255, 0)).WithDescription($"{step.Description}\nНе опрошено.\nНапишите в чат \"{step.Question}\" и нажмите \"{asked.Label}\"").WithTimestamp(DateTimeOffset.Now);
				await Session.SendMessage(new UniversalMessageBuilder().AddEmbed(embed)
					.AddComponents(asked).AddComponents(fail, success));

				await Session.GetComponentInteraction(_localToken.Token);

				asked.Disable();
				success.Enable();
				fail.Enable();
				embed.WithDescription($"{step.Description}\nОпрошено.\nДождитесь ответа игрока.\nВ случае `Нет`, жмите \"{fail.Label}\", в случае `Да`, жмите \"{success.Label}\"").WithTimestamp(DateTimeOffset.Now);

				await Session.SendMessage(new UniversalMessageBuilder().AddEmbed(embed)
					.AddComponents(asked).AddComponents(fail, success));

				var answer = await Session.GetComponentInteraction(_localToken.Token);

				if (answer.CompareButton(fail))
				{
					_cancelOrder.Cancel();
					_cancelReason = step.FailReason;
					await Task.FromCanceled(_cancelOrder.Token);
				}

				return new(Decider);
			}
			catch (TaskCanceledException)
			{
				return new(DoOrderCancellation);
			}
		}

		private async Task<NextNetworkInstruction> DoCommand(NetworkInstructionArgument arg)
		{
			try
			{
				var step = (CommandStep)arg.Payload;

				var asked = new DiscordButtonComponent(ButtonStyle.Primary, "executed", "Выполнено.");
				var embed = new DiscordEmbedBuilder();
				embed.WithColor(new DiscordColor(255, 255, 0));
				embed.WithDescription($"{step.Description}\nНапишите в консоль \"{step.Command}\" и нажмите \"{asked.Label}\"");
				await Session.SendMessage(new UniversalMessageBuilder().AddEmbed(embed.WithTimestamp(DateTimeOffset.Now))
					.AddComponents(asked));

				await Session.GetComponentInteraction(_localToken.Token);
				await Session.DoLaterReply();

				return new(Decider);
			}
			catch (TaskCanceledException)
			{
				return new(DoOrderCancellation);
			}
		}

		private async Task<NextNetworkInstruction> MakeNonCancallable(NetworkInstructionArgument arg)
		{
			try
			{
				await ExOrder.MakeUncancellable();

				return new(Decider);
			}
			catch (TaskCanceledException)
			{
				return new(DoOrderCancellation);
			}
		}

		private async Task<NextNetworkInstruction> DoShowInfo(NetworkInstructionArgument arg)
		{
			try
			{
				var step = (ShowInfoStep)arg.Payload;

				var change = new DiscordButtonComponent(ButtonStyle.Primary, "change", "Выбрать другой заказ.");
				var cont = new DiscordButtonComponent(ButtonStyle.Success, "continue", "Продолжить.");
				var embed = new DiscordEmbedBuilder().WithTimestamp(DateTimeOffset.Now);
				embed.WithColor(new DiscordColor(255, 255, 0));
				embed.WithDescription($"{step.Description}");
				await Session.SendMessage(new UniversalMessageBuilder()
					.AddEmbed(embed).AddComponents(change, cont));
				var timeout = TimeSpan.FromSeconds(20);

				var getter = Session.GetComponentInteraction(_localToken.Token);
				var timer = Task.Delay(timeout);
				var any = await Task.WhenAny(getter, timer);

				if (any == timer)
				{
					await ChangeOrder();
					await Task.FromCanceled(_swapToken.Token);
				}

				var res = await getter;

				if (res.CompareButton(change))
				{
					await ChangeOrder();
					await Task.FromCanceled(_swapToken.Token);
				}

				return new(Decider);
			}
			catch (TaskCanceledException)
			{
				return new(DoOrderCancellation);
			}
		}

		private async Task<NextNetworkInstruction> FetchNextStep(NetworkInstructionArgument arg)
		{
			try
			{
				_localToken = CancellationTokenSource.CreateLinkedTokenSource(_swapToken.Token, _quitToken.Token, _cancelOrder.Token, ExOrder?.PlayerOrderCancelToken ?? CancellationToken.None);

				var embed = new DiscordEmbedBuilder();
				embed.WithColor(new DiscordColor(255, 255, 0));
				embed.WithDescription($"Ожидание заказов...");
				await Session.SendMessage(new UniversalMessageBuilder().AddEmbed(embed.WithTimestamp(DateTimeOffset.Now)));

				ExOrder = await _pool.GetOrder(_localToken.Token);

				var msg = await _channel.SendMessageAsync(new UniversalMessageBuilder().SetContent($"<@{_admin.Id}>").AddMention(new UserMention(_admin)));

				await Session.Client.Domain.ExecutionThread.AddNew(new ExecThread.Job(() => msg.DeleteAsync()));

				return new(Decider);
			}
			catch (TaskCanceledException)
			{
				return new(DoOrderCancellation);
			}
		}

		private async Task<NextNetworkInstruction> SetupStep(NetworkInstructionArgument arg)
		{
			await _pool.StartAdministrating(this);
			return new(FetchNextStep);
		}

		public NextNetworkInstruction GetStartingInstruction() => new(SetupStep);

		public NextNetworkInstruction GetStartingInstruction(Object payload) => throw new NotImplementedException();
	}
}