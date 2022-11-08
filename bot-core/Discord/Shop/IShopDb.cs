using Manito.Discord.Database;

using Microsoft.EntityFrameworkCore;

namespace Manito.Discord.Shop
{
	public interface IShopDb : IMyDatabase
	{
		DbSet<ShopItem> ShopItems {
			get;
		}
	}
}