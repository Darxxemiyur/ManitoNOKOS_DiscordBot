using DisCatSharp.Entities;

using System;
using System.Threading.Tasks;

namespace Manito.Discord.Inventory.Items
{
	public class ItemBuilder
	{
		private IInventorySystem _inventory;
		private DiscordUser _owner;
		private int _quantity;

		public ItemBuilder(IInventorySystem inventory) => _inventory = inventory;

		public async Task<IItem> Build()
		{
			throw new NotImplementedException();
		}
	}
}