using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord.Chat.DialogueNet;

using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System.Threading.Tasks;

namespace Manito.Discord.Inventory
{
	public class PlantInteraction : IDialogueNet
	{
		private DialogueNetSession _session;
		private NextNetworkInstruction _ret;
		private IItem _item;
		private int _playerId;
		private int _amount;
		public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

		public PlantInteraction(DialogueNetSession session, IItem item, NextNetworkInstruction ret) =>
		 (_session, _item, _ret) = (session, item, ret);

		private async Task<NextNetworkInstruction> Initiallize(NetworkInstructionArgument args)
		{
			var resp = new DiscordInteractionResponseBuilder();
			resp.WithContent("Меня пока-что нельзя использовать, извините.");
			resp.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "back", "Назад."));
			await _session.Respond(resp);

			var intr = await _session.GetInteraction();

			return _ret;
		}

		public NextNetworkInstruction GetStartingInstruction(object payload) => GetStartingInstruction();

		public NextNetworkInstruction GetStartingInstruction() => new(Initiallize, NextNetworkActions.Continue);
	}
}