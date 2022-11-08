using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;

using Manito.Discord.Client;

using Name.Bayfaderix.Darxxemiyur.Common;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Manito.Discord.ChatNew
{
	public class ComponentDialogueSession : IDialogueSession
	{
		private InteractiveInteraction Interactive {
			get; set;
		}

		private UniversalMessageBuilder _innerMsgBuilder;
		private DiscordMessage _msg;

		public MyClientBundle Client {
			get;
		}

		public ISessionState Identifier {
			get; private set;
		}

		public InteractionResponseType NextType {
			get; private set;
		}

		private readonly AsyncLocker _lock = new();

		/// <summary>
		/// Used to inform subscribers about session status change.
		/// </summary>
		public event Func<IDialogueSession, SessionInnerMessage, Task> OnStatusChange;

		public event Func<IDialogueSession, SessionInnerMessage, Task> OnSessionEnd;

		public event Func<IDialogueSession, Task<bool>> OnRemove;

		public bool IsAutomaticallyDeleted {
			get;
		}

		public async Task EndSession()
		{
			if (OnSessionEnd != null)
				await OnSessionEnd(this, new(null, "Ended"));
			if (OnRemove != null)
				await OnRemove(this);
		}

		public async Task SendMessage(UniversalMessageBuilder message)
		{
			await using var _ = await _lock.BlockAsyncLock();
			try
			{
				try
				{
					await SendMessageLocal(message);
				}
				catch (Exception e) when (NextType != InteractionResponseType.Pong)
				{
					NextType = InteractionResponseType.Pong;
					try
					{
						await SendMessageLocal(message);
					}
					catch (Exception ee)
					{
						throw new AggregateException(e, ee);
					}
				}

				if (IsAutomaticallyDeleted)
					await Client.Remover.RemoveMessage(Identifier.ChannelId, Identifier.MessageId ?? 0, true);
			}
			catch (Exception)
			{
				await FallBackToMessageSession();
				throw;
			}
		}

		private async Task SendMessageLocal(UniversalMessageBuilder message)
		{
			switch (NextType)
			{
				case InteractionResponseType.ChannelMessageWithSource:
					await Interactive.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, message);
					NextType = InteractionResponseType.Pong;
					await MarkTheMessage();
					break;

				case InteractionResponseType.UpdateMessage:
					await Interactive.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, message);
					NextType = InteractionResponseType.Pong;
					break;

				case InteractionResponseType.Pong:
					await MarkTheMessage(await Interactive.Interaction.EditOriginalResponseAsync(message));
					break;
			}
			_innerMsgBuilder = message;
		}

		private async Task MarkTheMessage(DiscordMessage msg = default)
		{
			_msg = msg ?? await SessionMessage;
			Identifier = new ComponentInteractionState(Client, new(Interactive.Interaction, _msg));
		}

		private async Task CancelClickability()
		{
			var msg = _innerMsgBuilder;
			await SendMessageLocal(msg.NewWithDisabledComponents());
			_innerMsgBuilder = msg;
		}

		private async Task RespondToAnInteraction()
		{
			switch (NextType)
			{
				case InteractionResponseType.ChannelMessageWithSource:
					await Interactive.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
					NextType = InteractionResponseType.Pong;
					await MarkTheMessage();
					break;

				case InteractionResponseType.UpdateMessage:
					await Interactive.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
					NextType = InteractionResponseType.Pong;
					break;
			}
		}

		private async Task FallBackToMessageSession()
		{
			if (OnStatusChange == null)
				return;

			var chnl = await Client.Client.GetChannelAsync(Identifier.ChannelId);
			try
			{
				var msg = await chnl.GetMessageAsync(Identifier.MessageId ?? 0, true);
				await OnStatusChange(this, new SessionInnerMessage((msg, _innerMsgBuilder, Identifier.UserId ?? 0), "ConvertMeToMsg1"));
			}
			catch (Exception)
			{
				await OnStatusChange(this, new SessionInnerMessage((chnl, _innerMsgBuilder, Identifier.UserId ?? 0), "ConvertMeToMsg2"));
			}
			Client.EventsBuffer.MsgDeletion.OnToNextLink -= MsgDeletion_OnMessage;
		}

		public async Task DoLaterReply()
		{
			await using var _ = await _lock.BlockAsyncLock();
			try
			{
				if (_innerMsgBuilder?.Components?.SelectMany(x => x)?.Any() == true)
					await CancelClickability();
				else
					await RespondToAnInteraction();

				if (IsAutomaticallyDeleted)
					await Client.Remover.RemoveMessage(Identifier.ChannelId, Identifier.MessageId ?? 0, true);
			}
			catch (Exception e)
			{
				await Client.Domain.Logging.WriteErrorClassedLog(GetType().Name, e, true);
				await FallBackToMessageSession();
				throw;
			}
		}

		public async Task<DiscordMessage> GetMessageInteraction(CancellationToken token = default)
		{
			var msg = await Client.ActivityTools.WaitForMessage(x => Identifier.DoesBelongToUs(x.Message), token);

			return msg.Message;
		}

		public async Task<InteractiveInteraction> GetComponentInteraction(CancellationToken token = default)
		{
			InteractiveInteraction intr = await Client.ActivityTools.WaitForComponentInteraction(x => Identifier.DoesBelongToUs(x), token);

			await using var _ = await _lock.BlockAsyncLock();
			Identifier = new ComponentInteractionState(Client, Interactive = intr);
			NextType = InteractionResponseType.UpdateMessage;

			return intr;
		}

		private async Task RemoveMessageLocal()
		{
			try
			{
				await Client.Remover.RemoveMessage(Identifier.ChannelId, Identifier.MessageId ?? 0);
			}
			catch (Exception e1)
			{
				await Client.Domain.Logging.WriteErrorClassedLog(GetType().Name, e1, true);
			}
		}

		public async Task RemoveMessage()
		{
			await using var _ = await _lock.BlockAsyncLock();
			await RemoveMessageLocal();
		}

		public Task<DiscordMessage> GetReplyInteraction(CancellationToken token = default) => throw new NotImplementedException();

		public UniversalSession ToUniversal() => new(this);

		public Task<DiscordMessage> SessionMessage => Interactive.Interaction.GetOriginalResponseAsync();

		public Task<DiscordChannel> SessionChannel => Task.Run(async () => await Client.Client.GetChannelAsync((await SessionMessage).ChannelId));

		public ComponentDialogueSession(MyClientBundle client, ComponentInteractionState id, InteractiveInteraction interactive, UniversalMessageBuilder bld, bool isAutomaticallyDeleted = true) : this(client, isAutomaticallyDeleted)
		{
			(Interactive, Identifier) = (interactive, id);
			NextType = id.MessageId.HasValue ? InteractionResponseType.UpdateMessage : InteractionResponseType.ChannelMessageWithSource;
			_innerMsgBuilder = bld;
		}

		public ComponentDialogueSession(MyClientBundle client, DialogueCommandState id, InteractiveInteraction interactive, bool isAutomaticallyDeleted = true) : this(client, isAutomaticallyDeleted)
		{
			(Interactive, Identifier) = (interactive, id);
			NextType = id.MessageId.HasValue ? InteractionResponseType.UpdateMessage : InteractionResponseType.ChannelMessageWithSource;
		}

		public ComponentDialogueSession(MyClientBundle client, DiscordInteraction interaction, bool isAutomaticallyDeleted = true) : this(client, isAutomaticallyDeleted)
		{
			Identifier = new DialogueCommandState(Interactive = new(interaction));
			NextType = InteractionResponseType.ChannelMessageWithSource;
		}

		private ComponentDialogueSession(MyClientBundle client, bool isAutomaticallyDeleted)
		{
			IsAutomaticallyDeleted = isAutomaticallyDeleted;
			Client = client;
			Client.EventsBuffer.MsgDeletion.OnToNextLink += MsgDeletion_OnMessage;
		}

		~ComponentDialogueSession()
		{
		}

		private async Task MsgDeletion_OnMessage(DiscordClient arg1, MessageDeleteEventArgs arg2)
		{
			if (arg2.Message.Id != Identifier.MessageId)
				return;

			await Client.Domain.ExecutionThread.AddNew(new ExecThread.Job(EndSession));
		}

		public async Task<UniversalSession> PopNewLine()
		{
			return await PopNewLine(await SessionChannel, await Client.Client.GetUserAsync(Identifier.UserId ?? 0));
		}

		public Task<UniversalSession> PopNewLine(DiscordMessage msg) => PopNewLine(msg.Channel, msg.Author);

		public async Task<UniversalSession> PopNewLine(DiscordChannel msg, DiscordUser usr)
		{
			await using var _ = await _lock.BlockAsyncLock();
			return new SessionFromMessage(Client, msg, _innerMsgBuilder, usr.Id).ToUniversal();
		}

		public async Task<UniversalSession> PopNewLine(DiscordUser msg)
		{
			return await PopNewLine(await (await msg.ConvertToMember(Client.Client.Guilds.FirstOrDefault(x => x.Value.Members.FirstOrDefault(y => y.Key == msg.Id).Value != null).Value)).CreateDmChannelAsync(), msg);
		}

		public static implicit operator UniversalSession(ComponentDialogueSession msg) => new(msg);
	}
}