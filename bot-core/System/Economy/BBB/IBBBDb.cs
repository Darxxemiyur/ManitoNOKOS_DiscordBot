using Microsoft.EntityFrameworkCore;

namespace Manito.System.Economy.BBB
{
	public interface IBBBDb
	{
		DbSet<ItemBase> InventoryItems {
			get;
		}
	}
}