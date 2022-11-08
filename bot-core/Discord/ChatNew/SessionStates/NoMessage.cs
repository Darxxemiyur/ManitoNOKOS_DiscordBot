using DisCatSharp.Entities;
using DisCatSharp.Net;

using Manito.Discord.Client;

using System;

namespace Manito.Discord.ChatNew
{
	public class NoMessageState : ISessionState
	{
		public ulong? UserId {
			get;
		}

		public ulong ChannelId {
			get;
		}

		public ulong? MessageId => null;

		public ulong[] UserIds => new[]
		{
			UserId ?? 0
		};

		public SessionKinds Kind => SessionKinds.OnDMChannel | SessionKinds.OnGuildChannel;

		public MyClientBundle Bundle {
			get;
		}

		public DiscordApiClient UsedClient {
			get;
		}

		public NoMessageState(ulong channelId, ulong userId)
		{
			UserId = userId;
			ChannelId = channelId;
		}

		public bool DoesBelongToUs(InteractiveInteraction interaction) => false;

		public Int32 HowBadWants(InteractiveInteraction interaction) => 100;

		public bool DoesBelongToUs(DiscordMessage interaction) => false;

		public Int32 HowBadWants(DiscordMessage interaction) => 100;
	}
}