using DisCatSharp.Entities;

using Manito.Discord.Client;

using System;
using System.Threading.Tasks;

namespace Manito.Discord.ChatNew
{
	public interface IDialogueSession : IReplyAwaitable
	{
		/// <summary>
		/// My own discord client wrapper.
		/// </summary>
		MyClientBundle Client {
			get;
		}

		/// <summary>
		/// Identifier of message to pull events for.
		/// </summary>
		ISessionState Identifier {
			get;
		}

		bool IsAutomaticallyDeleted {
			get;
		}

		/// <summary>
		/// Send/update session message.
		/// </summary>
		/// <param name="msg">The new message.</param>
		/// <returns></returns>
		Task SendMessage(UniversalMessageBuilder msg);

		/// <summary>
		/// Respond to an interaction to reply later.
		/// </summary>
		/// <returns></returns>
		Task DoLaterReply();

		/// <summary>
		/// Delete message.
		/// </summary>
		/// <returns></returns>
		Task RemoveMessage();

		/// <summary>
		/// Creates a new Session related to this session
		/// </summary>
		/// <returns></returns>
		Task<UniversalSession> PopNewLine();

		/// <summary>
		/// Creates a new Session related to this message
		/// </summary>
		/// <returns></returns>
		Task<UniversalSession> PopNewLine(DiscordMessage msg);

		/// <summary>
		/// Creates a new Session related to this channel and user
		/// </summary>
		/// <returns></returns>
		Task<UniversalSession> PopNewLine(DiscordChannel msg, DiscordUser usr);

		/// <summary>
		/// Creates a new Session related to this user
		/// </summary>
		/// <returns></returns>
		Task<UniversalSession> PopNewLine(DiscordUser msg);

		/// <summary>
		/// Ends session.
		/// </summary>
		/// <returns></returns>
		Task EndSession();

		/// <summary>
		/// Gets message of the session.
		/// </summary>
		Task<DiscordMessage> SessionMessage {
			get;
		}

		/// <summary>
		/// Gets message of the session.
		/// </summary>
		Task<DiscordChannel> SessionChannel {
			get;
		}

		/// <summary>
		/// Used to inform subscribers about message passed through this session.
		/// </summary>
		//public event Func<IDialogueSession, SessionInnerMessage, Task> OnPassMessage;
		/// <summary>
		/// Used to inform subscribers about session status change.
		/// </summary>
		public event Func<IDialogueSession, SessionInnerMessage, Task> OnStatusChange;

		/// <summary>
		/// Used to inform subscribers about session end.
		/// </summary>
		public event Func<IDialogueSession, SessionInnerMessage, Task> OnSessionEnd;

		/// <summary>
		/// Used to inform subscribers about session removal.
		/// </summary>
		public event Func<IDialogueSession, Task<bool>> OnRemove;

		UniversalSession ToUniversal();
	}
}