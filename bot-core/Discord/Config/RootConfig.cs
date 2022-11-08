using System;

namespace Manito.Discord.Config
{
	[Serializable]
	public class RootConfig
	{
		public DiscordClientConfig ClientCfg;
		public DatabaseConfig DatabaseCfg;

		public RootConfig()
		{
		}

		public static RootConfig GetConfig()
		{
			return new() {
				ClientCfg = new(), DatabaseCfg = new()
			};
		}
	}
}