using System;

namespace Manito.Discord.Inventory
{
	public class Item : IItem
	{
		public ulong Id {
			get; set;
		}

		public ulong Owner {
			get; set;
		}

		public int Quantity {
			get; set;
		}

		public string ItemType {
			get; set;
		}

		public string Custom {
			get; set;
		}

		public bool Equals(IItem other) => Id == other.Id;

		public override bool Equals(object obj) => (obj is IItem other) && Equals(other);

		public override int GetHashCode() => HashCode.Combine(Id.GetHashCode(),
																Owner.GetHashCode(),
																Quantity.GetHashCode(),
																ItemType.GetHashCode(),
																Custom.GetHashCode());

		public static bool operator ==(Item item1, IItem item2) => item1.Equals(item2);

		public static bool operator !=(Item item1, IItem item2) => !item1.Equals(item2);
	}
}