using DisCatSharp;

using System.Threading.Tasks;

namespace Manito.Discord.Client
{
	//TODO: Make safe for use client impementation.
	public class MyDiscordClient : IModule
	{
		private DiscordClient _client;
		private MyClientBundle _bundle;

		//TODO: Make safe for use client impementation.
		public MyDiscordClient(MyClientBundle bundle)
		{
			_bundle = bundle;
			var config = new DiscordConfiguration {
				Token = bundle.Domain.RootConfig.ClientCfg.ClientKey,
				Intents = DiscordIntents.All
			};
			//_client = new(config);
		}

		//TODO: Make safe for use client impementation.
		public Task RunModule() => Task.CompletedTask;
	}
}