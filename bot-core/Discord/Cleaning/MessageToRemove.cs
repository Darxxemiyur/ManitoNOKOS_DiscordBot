using System;

namespace Manito.Discord.Cleaning
{
	public class MessageToRemove
	{
		public MessageToRemove(ulong messageID, ulong channelID, DateTimeOffset expiration, int lastStartId)
		{
			MessageID = messageID;
			ChannelID = channelID;
			Expiration = expiration;
			LastStartId = lastStartId;
		}

		public MessageToRemove(MessageToRemove msg)
		{
			MessageID = msg.MessageID;
			ChannelID = msg.ChannelID;
			LastStartId = msg.LastStartId;
			Expiration = msg.Expiration;
		}

		public ulong MessageID {
			get; set;
		}

		public ulong ChannelID {
			get; set;
		}

		public DateTimeOffset Expiration {
			get; set;
		}

		public int TimesFailed {
			get; set;
		}

		public int LastStartId {
			get; set;
		}
	}
}