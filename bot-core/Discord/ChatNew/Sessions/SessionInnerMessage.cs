using System.Threading;
using System.Threading.Tasks;

namespace Manito.Discord.ChatNew
{
	public struct SessionInnerMessage
	{
		public readonly object Generic;
		public readonly string Message;

		public SessionInnerMessage(object generic, string message) => (Generic, Message) = (generic, message);
	}

	public static class IDialogueExtender
	{
		public static Task<GeneralInteraction> GetInteraction(this IDialogueSession session, InteractionTypes types, CancellationToken token = default) => session.GetInteraction(types, token);
	}
}