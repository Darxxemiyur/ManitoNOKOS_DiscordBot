using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatNew;
using Manito.Discord.Client;

using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Manito.Discord.Orders
{
	public class AdminOrderControl : IDialogueNet
	{
		public NodeResultHandler StepResultHandler {
			get;
		} = Common.DefaultNodeResultHandler;

		private readonly DialogueTabSession<AdminOrderContext> _session;
		public DialogueTabSession<AdminOrderContext> Session => _session;
		private readonly CancellationTokenSource _remover;
		private MyDomain Domain => Client.Domain;
		private MyClientBundle Client => _session.Client;
		private AdminOrderExec _execSession;
		private DiscordButtonComponent _beginButton;
		private DiscordButtonComponent _changeButton;
		private DiscordButtonComponent _endButton;
		private DiscordButtonComponent _exitButton;
		private readonly AdminOrderPool _pool;
		private ExecThread.Job _thread;
		public AdminOrderPool Pool => _pool;

		public AdminOrderControl(DialogueTabSession<AdminOrderContext> session, AdminOrderPool pool)
		{
			_pool = pool;
			(_session = session).Context.Control = this;
			_remover = new();
			_beginButton = new(ButtonStyle.Success, "beginworking", "Начать работу");
			_changeButton = new(ButtonStyle.Primary, "changeorder", "Сменить заказ", true);
			_endButton = new(ButtonStyle.Danger, "endworking", "Закончить работу", true);
			_exitButton = new(ButtonStyle.Danger, "quitworking", "Выйти");
			_session.OnSessionEnd += OnSessionEndHandle;
		}

		private Task OnSessionEndHandle(DialogueTabSession<AdminOrderContext> arg1, SessionInnerMessage arg2) => Domain.ExecutionThread.AddNew(new ExecThread.Job(RemoveAndEndShift));

		private async Task TryNotifyAboutShift(UniversalMessageBuilder msg)
		{
			try
			{
#if DEBUG
				ulong shiftChannelId = 965561900517716008;
#else
				ulong shiftChannelId = 1017841450941173760;
#endif
				var shiftChannel = await _session.Client.Client.GetChannelAsync(shiftChannelId);

				await shiftChannel.SendMessageAsync(msg);
			}
			catch { }
		}

		private async Task<NextNetworkInstruction> BeginOrderExecution(NetworkInstructionArgument arg)
		{
			var (channel, id) = ((DiscordChannel, DiscordUser))arg.Payload;
			_execSession = new(_pool, new(new SessionFromMessage(_session.Client, channel, id.Id)), channel, id);

			_thread = await Domain.ExecutionThread.AddNew(new ExecThread.Job(() => NetworkCommon.RunNetwork(_execSession)));

			await TryNotifyAboutShift($"<@{_session.Identifier.UserId}> Начал смену в <t:{DateTimeOffset.Now.ToUnixTimeSeconds()}>");

			_beginButton.Disable();
			_changeButton.Enable();
			_endButton.Enable();
			return new(Waiting);
		}

		private async Task<NextNetworkInstruction> ChangeOrder(NetworkInstructionArgument arg)
		{
			await _execSession.ChangeOrder();

			//_changeButton.Disable();
			_endButton.Enable();
			return new(Waiting);
		}

		private async Task<NextNetworkInstruction> EndOrderExecution(NetworkInstructionArgument arg)
		{
			await EndShift();
			_beginButton.Enable();
			_changeButton.Disable();
			_endButton.Disable();
			return new(Waiting);
		}

		public Task QuitControl() => Task.Run(() => _remover.Cancel());

		private async Task<NextNetworkInstruction> Waiting(NetworkInstructionArgument arg)
		{
			try
			{
				var t1 = "**\\*Исполнение заказов\\***";
				var t2 = "**Исполнение заказов подразумевает что вы находитесь в [1]'режиме свободного полёта камеры' и со включеным [2]'отображением информации об игроках'!**";
				var t3 = "[1\\*]Для включения режима полёта камеры напишите в консоль `EnterSpectate`!\nДля выключения `LeaveSpectate`!";
				var t4 = "[2\\*]Для переключения отображения информации напишите в консоль `TogglePlayerNameTags`!";
				var t5 = "__Тщательно читайте и следуйте инструкциям!\nВ них содержатся готовые для копирования команды в консоль и чат__!";
				var t6 = $"__Делайте только то, что написано в сообщении ниже__!\nПо вопросам к <@{_session.Client.Domain.Filters.AssociationFilter.PermissionChecker.GodId}>";
				var t7 = "Лёгкой и удачной работы, дорогой Администратор!";
				var tv = $"{t2}\n{t3}\n{t4}\n{t5}\n{t6}\n{t7}";
				var msg = new UniversalMessageBuilder().AddComponents(_beginButton, _changeButton, _endButton, _exitButton).AddEmbed(new DiscordEmbedBuilder().WithTitle(t1).WithDescription(tv).WithColor(DiscordColor.Sienna).WithTimestamp(DateTimeOffset.Now));

				await _session.SendMessage(msg);

				var comp = await _session.GetComponentInteraction(_remover.Token);
				await _session.DoLaterReply();

				if (comp.CompareButton(_beginButton))
					return new(BeginOrderExecution, (comp.Interaction.Channel, comp.Interaction.User));
				if (comp.CompareButton(_changeButton))
					return new(ChangeOrder);
				if (comp.CompareButton(_endButton))
					return new(EndOrderExecution);

				//In case _exitButton is clicked
			}
			catch (TaskCanceledException) { }
			await _session.EndSession();
			return new();
		}

		private async Task EndShift()
		{
			if (_execSession != null)
				await _execSession.StopExecuting();
			if (_thread != null)
				await _thread.Result;
			if (_execSession != null)
				await _session.Client.Domain.ExecutionThread.AddNew(new ExecThread.Job(() => TryNotifyAboutShift($"<@{_session.Identifier.UserId}> Завершил смену в <t:{DateTimeOffset.Now.ToUnixTimeSeconds()}>")));
			_execSession = null;
			_thread = null;
		}

		private async Task RemoveAndEndShift()
		{
			await EndShift();
			await _session.RemoveMessage();
		}

		public NextNetworkInstruction GetStartingInstruction() => new(Waiting);

		public NextNetworkInstruction GetStartingInstruction(Object payload) => throw new NotImplementedException();
	}
}