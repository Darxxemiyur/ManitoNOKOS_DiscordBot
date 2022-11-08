using DisCatSharp.Entities;

using Manito.Discord.Chat.DialogueNet;

using System;
using System.Threading.Tasks;

namespace Manito.Discord.Inventory
{
	public class InventoryController : DialogueNetSessionControls<InventorySession>
	{
		public InventoryController(MyDomain service) : base(service)
		{
		}

		public Task<InventorySession> StartSession(DiscordInteraction args,
		 Func<InventorySession, IDialogueNet> getNet) => StartSession(() =>
		 new InventorySession(new(args), Service.MyDiscordClient, Service.Inventory, args.User, this), getNet);
	}
}