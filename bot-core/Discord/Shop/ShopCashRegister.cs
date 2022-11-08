using DisCatSharp.Entities;

using System.Collections.Generic;

namespace Manito.Discord.Shop
{
	public class ShopCashRegister
	{
		private MyDomain _domain;

		public ShopCashRegister(MyDomain domain)
		{
			_domain = domain;
		}

		public IEnumerable<ShopItem> GetShopItems()
		{
			yield return new ShopItem {
				Name = "Каркас без насыщения",
				Category = ItemCategory.Carcass,
				RelatedCommand = "SpawnCarcass {0} {0} false",
				Price = 1,
			};
			yield return new ShopItem {
				Name = "Каркас c насыщением",
				Category = ItemCategory.SatiationCarcass,
				RelatedCommand = "SpawnCarcass {0} {0} true",
				Price = 2,
			};
			yield return new ShopItem {
				Name = "Светляк",
				Category = ItemCategory.Plant,
				RelatedCommand = "SpawnPlant",
				Price = 570,
			};
			yield return new ShopItem {
				Name = "Рескин",
				Category = ItemCategory.Reskin,
				RelatedCommand = "Reskin {0}",
				Price = 5700,
			};
			yield return new ShopItem {
				Name = "Сброс талантов",
				Category = ItemCategory.ResetTalent,
				RelatedCommand = "ResetTalents {0}",
				Price = 40000,
			};
			yield return new ShopItem {
				Name = "Смена пола",
				Category = ItemCategory.SwapGender,
				RelatedCommand = "SetGender {1} {0}",
				Price = 17000,
			};
			yield return new ShopItem {
				Name = "Проверка яиц",
				Category = ItemCategory.EggCheck,
				Price = 100,
				IsAvailable = false,
			};
			yield return new ShopItem {
				Name = "Воскрешение",
				Category = ItemCategory.Revive,
				RelatedCommand = "Reskin {0}",
				Price = 100,
				IsAvailable = false,
			};
			yield return new ShopItem {
				Name = "Телепорт",
				Category = ItemCategory.Teleport,
				RelatedCommand = "TeleportPtoP {0} {1}",
				Price = 100,
				IsAvailable = false,
			};
		}

		public DiscordEmbedBuilder Default(DiscordEmbedBuilder bld = null) =>
			(bld ?? new DiscordEmbedBuilder()).WithTitle("~Магазин Манито~").WithColor(DiscordColor.Blurple);
	}
}