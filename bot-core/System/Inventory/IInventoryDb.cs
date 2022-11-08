using Manito.Discord.Database;

using Microsoft.EntityFrameworkCore;

namespace Manito.Discord.Inventory
{
	public interface IInventoryDb : IMyDatabase
	{
		DbSet<Item> ItemsDb {
			get;
		}
	}
}