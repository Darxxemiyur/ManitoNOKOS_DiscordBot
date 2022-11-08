using Manito.Discord.Chat.DialogueNet;

using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;

namespace Manito.Discord.Inventory
{
	public class DescriptorGetter
	{
		public ItemDescriptor GetDescriptor(IItem item)
		{
			if (item.ItemType.Contains("Bonus", StringComparison.OrdinalIgnoreCase))
				return new DefaultDescriptor(item);
			if (item.ItemType.Contains("Plant", StringComparison.OrdinalIgnoreCase))
				return new PlantDescriptor(item);
			if (item.ItemType.Contains("Carcass", StringComparison.OrdinalIgnoreCase))
				return new CarcassDescriptor(item);

			throw new NotImplementedException();
		}
	}

	public class UniversalDescriptor : ItemDescriptor
	{
		private readonly ItemDescriptor _inner;

		public UniversalDescriptor(IItem item) : base(item)
		{
			_inner = new DescriptorGetter().GetDescriptor(Item);
		}

		public override string GetButtonDescriptor()
		{
			return _inner.GetButtonDescriptor();
		}

		public override string GetEmbedDescriptor()
		{
			return _inner.GetEmbedDescriptor();
		}

		public override string GetItemIcon()
		{
			throw new NotImplementedException();
		}

		public override IDialogueNet GetItemNet(DialogueNetSession session, NextNetworkInstruction ret, object arg = null)
		{
			return _inner.GetItemNet(session, ret, arg);
		}
	}

	public class CarcassDescriptor : ItemDescriptor
	{
		private readonly bool _s; // _sATIATION;

		public CarcassDescriptor(IItem item) : base(item)
		{
			_s = item.ItemType.Contains("Satiation", StringComparison.OrdinalIgnoreCase);
		}

		public override string GetEmbedDescriptor() => $"{Item.Quantity}кг "
		 + $"Каркаса {(_s ? "с" : "без")} насыщени{(_s ? "ем" : "я")}";

		public override string GetButtonDescriptor() => $"Каркас {(_s ? "с" : "без")} насыщени{(_s ? "ем" : "я")}";

		public override IDialogueNet GetItemNet(DialogueNetSession session,
		 NextNetworkInstruction ret, object arg = default)
		{
			return new CarcassInteraction(session, Item, ret);
		}

		public override string GetItemIcon() => ":meat_on_bone:";
	}

	public class PlantDescriptor : ItemDescriptor
	{
		public PlantDescriptor(IItem item) : base(item)
		{
		}

		public override string GetEmbedDescriptor() => $"{Item.Quantity}шт Светляка";

		public override string GetButtonDescriptor() => $"Светляк{(Item.Quantity > 1 ? "и" : "")}";

		public override IDialogueNet GetItemNet(DialogueNetSession session,
		 NextNetworkInstruction ret, object arg = default)
		{
			return new PlantInteraction(session, Item, ret);
		}

		public override string GetItemIcon() => ":potted_plant:";
	}

	public class DefaultDescriptor : ItemDescriptor
	{
		public DefaultDescriptor(IItem item) : base(item)
		{
		}

		public override string GetEmbedDescriptor() => $"{Item.ItemType} x{Item.Quantity}";

		public override string GetButtonDescriptor() => $"{Item.ItemType}";

		public override IDialogueNet GetItemNet(DialogueNetSession session,
		 NextNetworkInstruction ret, object arg = default)
		{
			throw new NotImplementedException();
		}

		public override string GetItemIcon() => ":x:";
	}

	public abstract class ItemDescriptor
	{
		public IItem Item {
			get;
		}

		public ItemDescriptor(IItem item)
		{
			Item = item;
		}

		public abstract string GetEmbedDescriptor();

		public abstract string GetButtonDescriptor();

		public abstract string GetItemIcon();

		public abstract IDialogueNet GetItemNet(DialogueNetSession session,
		 NextNetworkInstruction ret, object arg = default);
	}
}