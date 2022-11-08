using DisCatSharp.Entities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.Discord.Inventory
{
	public interface IInventorySystem
	{
		PlayerInventory GetPlayerInventory(DiscordUser user);

		Task AddItem(DiscordUser user, Func<Item, Item> itemCreator);

		Task ApplyItem(DiscordUser user, IItem item);

		IEnumerable<InventoryItem> GetPlayerItems(DiscordUser user);

		Task<bool> HasItem(DiscordUser user, IItem item);

		Task RemoveItem(DiscordUser user, IItem item);
	}

	public class InventorySystem : IInventorySystem
	{
		private List<Item> _items;
		private Dictionary<ulong, List<Item>> _itemstest;

		public InventorySystem()
		{
			_items = new();
			_itemstest = new();
		}

		public IEnumerable<InventoryItem> GetPlayerItems(DiscordUser user)
		{
			return _itemstest.FirstOrDefault(x => x.Key == user.Id).Value
				.Select(x => new InventoryItem(x));
		}

		public async Task AddItem(DiscordUser user, Func<Item, Item> itemCreator)
		{
			throw new NotImplementedException();
		}

		public async Task RemoveItem(DiscordUser user, IItem item)
		{
			throw new NotImplementedException();
		}

		public async Task ApplyItem(DiscordUser user, IItem item)
		{
			throw new NotImplementedException();
		}

		public async Task<bool> HasItem(DiscordUser user, IItem item)
		{
			throw new NotImplementedException();
		}

		public PlayerInventory GetPlayerInventory(DiscordUser user) => new(this, user);
	}

	public class TestInventorySystem : IInventorySystem
	{
		private List<Item> _items;
		private Dictionary<ulong, List<Item>> _itemstest;

		public TestInventorySystem()
		{
			_items = new();
			_itemstest = new();
		}

		private static ulong itemId = 0;

		private IEnumerable<Item> GenerateNewUserItems(ulong id)
		{
			for (var i = 0; i < 1; i++)
			{
				yield return new Item() { Id = itemId++, Owner = id, ItemType = $"Bonus{i}" };
			}
		}

		private void DoCheck(DiscordUser user)
		{
			if (!_itemstest.ContainsKey(user.Id))
				_itemstest.Add(user.Id, GenerateNewUserItems(user.Id).ToList());
		}

		public IEnumerable<InventoryItem> GetPlayerItems(DiscordUser user)
		{
			DoCheck(user);

			return _itemstest.FirstOrDefault(x => x.Key == user.Id).Value
				.Select(x => new InventoryItem(x));
		}

		#region TEST

		public async Task AddItem(DiscordUser user, Func<Item, Item> itemCreator)
		{
			DoCheck(user);
			_itemstest[user.Id].Add(itemCreator(
				new Item() { Id = itemId++, Owner = user.Id, ItemType = $"Bonus{itemId}" }));
		}

		public async Task RemoveItem(DiscordUser user, IItem item)
		{
			DoCheck(user);
			_itemstest[user.Id].RemoveAll(x => x == item);
		}

		public async Task ApplyItem(DiscordUser user, IItem item)
		{
			DoCheck(user);
			throw new NotImplementedException();
		}

		public async Task<bool> HasItem(DiscordUser user, IItem item)
		{
			DoCheck(user);
			return _itemstest[user.Id].Any(x => x == item);
		}

		public PlayerInventory GetPlayerInventory(DiscordUser user) => new(this, user);

		#endregion TEST
	}
}