namespace Manito.System.Economy
{
	public class PlayerEconomyDeposit
	{
		public ulong DiscordID {
			get; set;
		}

		public long ScalesCurr {
			get; set;
		}

		public long ChupatCurr {
			get; set;
		}

		public long DonatCurr {
			get; set;
		}

		public bool IsFrozen {
			get; set;
		}

		public PlayerEconomyDeposit(ulong discordID)
		{
			DiscordID = discordID;
			ScalesCurr = 10000;
			ChupatCurr = 0;
			DonatCurr = 0;
			IsFrozen = false;
		}
	}
}