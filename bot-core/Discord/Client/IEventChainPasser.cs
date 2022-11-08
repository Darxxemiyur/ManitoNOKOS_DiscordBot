using DisCatSharp;
using DisCatSharp.Common.Utilities;
using DisCatSharp.EventArgs;

namespace Manito.Discord.Client
{
	public interface IEventChainPasser<TEvent> where TEvent : DiscordEventArgs
	{
		public event AsyncEventHandler<DiscordClient, TEvent> OnToNextLink;
	}
}