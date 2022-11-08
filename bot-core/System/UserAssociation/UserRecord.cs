using System.Collections.Generic;

namespace Manito.System.UserAssociaton
{
	public class UserRecord
	{
		public ulong ID {
			get; set;
		}

		public ulong DiscordID {
			get; set;
		}

		public List<ulong> SteamIDs {
			get; set;
		}
	}
}