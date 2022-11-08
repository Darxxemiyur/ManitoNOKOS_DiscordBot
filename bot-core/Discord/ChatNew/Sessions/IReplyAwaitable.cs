using DisCatSharp.Entities;

using Manito.Discord.Client;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Manito.Discord.ChatNew
{
	public interface IReplyAwaitable
	{
		/// <summary>
		/// Gets message from user that is acceptable to SessionIdentifier.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		Task<DiscordMessage> GetMessageInteraction(CancellationToken token = default);

		/// <summary>
		/// Gets reply message from user that is acceptable to SessionIdentifier.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		Task<DiscordMessage> GetReplyInteraction(CancellationToken token = default);

		/// <summary>
		/// Get component interaction to a message that is acceptable to SessionIdentifier.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		Task<InteractiveInteraction> GetComponentInteraction(CancellationToken token = default);

		/// <summary>
		/// Get interaction to a message that is acceptable to SessionIdentifier.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task<GeneralInteraction> GetInteraction(InteractionTypes types, CancellationToken token = default)
		{
			CancellationTokenSource my = new();
			var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token, my.Token);

			var tasks = new List<(InteractionTypes, Task)>();

			if (types.HasFlag(InteractionTypes.Component))
				tasks.Add((InteractionTypes.Component, GetComponentInteraction(cancellation.Token)));

			if (types.HasFlag(InteractionTypes.Message))
				tasks.Add((InteractionTypes.Message, GetMessageInteraction(cancellation.Token)));

			if (types.HasFlag(InteractionTypes.Reply))
				tasks.Add((InteractionTypes.Reply, GetReplyInteraction(cancellation.Token)));

			var first = await Task.WhenAny(tasks.Select(x => x.Item2));

			if (cancellation.IsCancellationRequested)
				return new GeneralInteraction(InteractionTypes.Cancelled);

			if (cancellation.Token.CanBeCanceled)
				my.Cancel();

			var couple = tasks.First(x => x.Item2 == first);

			return new GeneralInteraction(couple.Item1,
				couple.Item2 is Task<InteractiveInteraction> i ? await i : null,
				couple.Item2 is Task<DiscordMessage> m ? await m : null);
		}
	}
}