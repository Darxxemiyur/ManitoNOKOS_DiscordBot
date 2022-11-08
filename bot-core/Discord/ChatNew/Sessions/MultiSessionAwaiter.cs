using DisCatSharp.Entities;

using Manito.Discord.Client;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Manito.Discord.ChatNew.Sessions
{
	public class MultiSessionAwaiter
	{
		private IEnumerable<IDialogueSession> _sessions;

		public MultiSessionAwaiter(params IDialogueSession[] sessions) => _sessions = sessions;

		public MultiSessionAwaiter(IEnumerable<IDialogueSession> sessions) => _sessions = sessions;

		/// <summary>
		/// Gets message from user that is acceptable to SessionIdentifier.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task<(IDialogueSession, DiscordMessage)> GetMessageInteraction(CancellationToken token = default)
		{
			var localToken = new CancellationTokenSource();
			var commonToken = CancellationTokenSource.CreateLinkedTokenSource(localToken.Token, token);

			var first = await Task.WhenAny(_sessions.Select(async (x) => (x, await x.GetMessageInteraction(commonToken.Token))));
			localToken.Cancel();

			return await first;
		}

		/// <summary>
		/// Gets reply message from user that is acceptable to SessionIdentifier.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task<(IDialogueSession, DiscordMessage)> GetReplyInteraction(CancellationToken token = default)
		{
			var localToken = new CancellationTokenSource();
			var commonToken = CancellationTokenSource.CreateLinkedTokenSource(localToken.Token, token);

			var first = await Task.WhenAny(_sessions.Select(async (x) => (x, await x.GetReplyInteraction(commonToken.Token))));
			localToken.Cancel();

			return await first;
		}

		/// <summary>
		/// Get component interaction to a message that is acceptable to SessionIdentifier.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task<(IDialogueSession, InteractiveInteraction)> GetComponentInteraction(CancellationToken token = default)
		{
			var localToken = new CancellationTokenSource();
			var commonToken = CancellationTokenSource.CreateLinkedTokenSource(localToken.Token, token);

			var first = await Task.WhenAny(_sessions.Select(async (x) => (x, await x.GetComponentInteraction(commonToken.Token))));
			localToken.Cancel();

			return await first;
		}

		/// <summary>
		/// Get interaction to a message that is acceptable to SessionIdentifier.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task<(IDialogueSession, GeneralInteraction)> GetInteraction(InteractionTypes types, CancellationToken token = default)
		{
			var localToken = new CancellationTokenSource();
			var commonToken = CancellationTokenSource.CreateLinkedTokenSource(localToken.Token, token);

			var first = await Task.WhenAny(_sessions.Select(async (x) => (x, await x.GetInteraction(types, commonToken.Token))));
			localToken.Cancel();

			return await first;
		}
	}
}