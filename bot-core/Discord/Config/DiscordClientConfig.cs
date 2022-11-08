using System;

namespace Manito.Discord.Config
{
	[Serializable]
	public class DiscordClientConfig
	{
		public string ClientKey {
			get;
		}

		public DiscordClientConfig()
		{
#if DEBUG
			//DEBUG
			ClientKey = "";
#else
			//RELEASE
			ClientKey = "";
#endif
		}
	}
}