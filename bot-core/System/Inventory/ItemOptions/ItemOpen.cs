using Manito.Discord.Chat.DialogueNet;

using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;
using System.Threading.Tasks;

namespace Manito.Discord.Inventory
{
	public class ItemSelect : IDialogueNet
	{
		private IItem _item;
		private InventorySession _session;

		public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

		public ItemSelect(IItem item, InventorySession session)
		{
			_item = item;
			_session = session;
		}

		private async Task<NextNetworkInstruction> ShowOptions(NetworkInstructionArgument args)
		{
			throw new NotImplementedException();
		}

		public NextNetworkInstruction GetStartingInstruction(object payload) => GetStartingInstruction();

		public NextNetworkInstruction GetStartingInstruction() => new(ShowOptions, NextNetworkActions.Continue);
	}
}