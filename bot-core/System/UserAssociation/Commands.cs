using DisCatSharp.Entities;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Manito.System.UserAssociaton
{
	public class UserAssociatonCommands
	{
		public UserAssociatonCommands()
		{
		}

		public List<DiscordApplicationCommand> GetCommands()
		{
			return new();
		}

		public Func<DiscordInteraction, Task> Search(DiscordInteraction command)
		{
			return null;
		}

		public async Task EnterMenu()
		{
		}

		public async Task LinkAccount()
		{
		}
	}
}