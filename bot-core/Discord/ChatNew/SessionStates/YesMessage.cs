using DisCatSharp.Entities;
using DisCatSharp.Net;

using Manito.Discord.Client;

using System;

namespace Manito.Discord.ChatNew
{
	/// <summary>
	/// Secondary Message Identifier
	/// </summary>
	public class YesMessageState : ISessionState
	{
		public YesMessageState(DiscordMessage message, ulong userId)
		{
			UserId = userId;
			ChannelId = message.ChannelId;
			MessageId = message.Id;
		}

		public ulong? UserId {
			get;
		}

		public ulong ChannelId {
			get;
		}

		public ulong? MessageId {
			get;
		}

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

		public bool DoesBelongToUs(InteractiveInteraction interaction)
		{
			var mid = interaction.Message.Id;
			var cid = interaction.Message.ChannelId;
			var uid = interaction.Interaction.User.Id;

			return ChannelId == cid && mid == MessageId && uid == UserId;
		}

		public Int32 HowBadWants(InteractiveInteraction interaction) => 10;

		public bool DoesBelongToUs(DiscordMessage interaction)
		{
			var cid = interaction.ChannelId;
			var uid = interaction.Author.Id;

			return ChannelId == cid && uid == UserId;
		}

		public Int32 HowBadWants(DiscordMessage interaction) => 10;
	}
}