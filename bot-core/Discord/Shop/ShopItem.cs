namespace Manito.Discord.Shop
{
	public class ShopItem
	{
		/// <summary>
		/// Name of the Item
		/// </summary>
		public string Name;

		/// <summary>
		/// Category of the Item
		/// </summary>
		public ItemCategory Category;

		/// <summary>
		/// Spawn command
		/// </summary>
		public string RelatedCommand;

		/// <summary>
		/// Price for unit of Item
		/// </summary>
		public int Price;

		public bool IsAvailable = true;

		public struct InCart
		{
			public readonly ShopItem Item;
			public readonly int Amount;
			public string Name => Item.Name;
			public ItemCategory Category => Item.Category;
			public int Price => Item.Price * Amount;
			public string RelatedCommand => string.Format(Item.RelatedCommand, Amount);

			public InCart(ShopItem item, int amount) => (Item, Amount) = (item, amount);
		}
	}
}