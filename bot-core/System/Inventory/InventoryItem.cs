using System;

namespace Manito.Discord.Inventory
{
	public class InventoryItem : IItem
	{
		private IItem _realItem;

		public InventoryItem(IItem realItem)
		{
			_realItem = realItem;
		}

		public ulong Id => _realItem.Id;

		public ulong Owner {
			get => _realItem.Owner;
			set => _realItem.Owner = value;
		}

		public int Quantity {
			get => _realItem.Quantity;
			set => _realItem.Quantity = value;
		}

		public string ItemType {
			get => _realItem.ItemType;
			set => _realItem.ItemType = value;
		}

		public bool Equals(IItem other) => Id == other.Id;

		public override bool Equals(object obj) => obj is IItem other && Equals(other);

		public override int GetHashCode() => HashCode.Combine(Id, Owner, Quantity, ItemType);

		public static bool operator ==(InventoryItem item1, IItem item2) => item1.Equals(item2);

		public static bool operator !=(InventoryItem item1, IItem item2) => !item1.Equals(item2);
	}
}