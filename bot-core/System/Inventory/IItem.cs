using System;

namespace Manito.Discord.Inventory
{
	public interface IItem : IEquatable<IItem>
	{
		ulong Id {
			get;
		}

		ulong Owner {
			get;
			set;
		}

		int Quantity {
			get;
			set;
		}

		string ItemType {
			get;
			set;
		}
	}
}