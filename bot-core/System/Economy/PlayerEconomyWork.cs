using System;

namespace Manito.System.Economy
{
	/// <summary>
	/// Player's work command info
	/// </summary>
	public class PlayerEconomyWork
	{
		public ulong DiscordID {
			get; set;
		}

		public DateTimeOffset LastWork {
			get; set;
		}

		public int TimesWorked {
			get; set;
		}

		public PlayerEconomyWork()
		{
		}

		public PlayerEconomyWork(ulong discordId)
		{
			DiscordID = discordId;
			LastWork = DateTimeOffset.MinValue;
			TimesWorked = 0;
		}
	}
}