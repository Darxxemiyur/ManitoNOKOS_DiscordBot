using DisCatSharp.Entities;

using Manito.Discord.Client;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Manito.Discord.ChatNew
{
	/// <summary>
	/// Session that relies on webhooks.
	/// </summary>
	public class WebhookMessageSession : IDialogueSession
	{
		public MyClientBundle Client {
			get;
		}

		public ISessionState Identifier {
			get;
		}

		public Task<DiscordMessage> SessionMessage {
			get;
		}

		public Task<DiscordChannel> SessionChannel {
			get;
		}

		public bool IsAutomaticallyDeleted {
			get;
		}

		public event Func<IDialogueSession, SessionInnerMessage, Task> OnStatusChange;

		public event Func<IDialogueSession, SessionInnerMessage, Task> OnSessionEnd;

		public event Func<IDialogueSession, Task<bool>> OnRemove;

		public Task SendMessage(UniversalMessageBuilder msg) => throw new NotImplementedException();

		public Task DoLaterReply() => throw new NotImplementedException();

		public Task EndSession() => throw new NotImplementedException();

		public Task<InteractiveInteraction> GetComponentInteraction(CancellationToken token = default) => throw new NotImplementedException();

		public Task<DiscordMessage> GetMessageInteraction(CancellationToken token = default) => throw new NotImplementedException();

		public Task<DiscordMessage> GetReplyInteraction(CancellationToken token = default) => throw new NotImplementedException();

		public Task RemoveMessage() => throw new NotImplementedException();

		public UniversalSession ToUniversal() => throw new NotImplementedException();

		public Task<UniversalSession> PopNewLine() => throw new NotImplementedException();

		public Task<UniversalSession> PopNewLine(DiscordMessage msg) => throw new NotImplementedException();

		public Task<UniversalSession> PopNewLine(DiscordChannel msg, DiscordUser usr) => throw new NotImplementedException();

		public Task<UniversalSession> PopNewLine(DiscordUser msg) => throw new NotImplementedException();
	}
}