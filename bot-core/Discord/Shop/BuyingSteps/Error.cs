using DisCatSharp.Entities;

using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatNew;

using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System.Threading.Tasks;

namespace Manito.Discord.Shop
{
	public class BuyingStepsForError : IDialogueNet
	{
		private DialogueTabSession<ShopContext> _session;

		public BuyingStepsForError(DialogueTabSession<ShopContext> session) => _session = session;

		public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

		public NextNetworkInstruction GetStartingInstruction(object payload) => GetStartingInstruction();

		public NextNetworkInstruction GetStartingInstruction() => new(SelectQuantity);

		private async Task<NextNetworkInstruction> SelectQuantity(NetworkInstructionArgument args)
		{
			await _session.SendMessage(new DiscordEmbedBuilder().WithDescription("Меня пока нельзя купить!\nПриносим свои извинения :c").WithColor(new DiscordColor(200, 50, 20)));
			await _session.DoLaterReply();
			return new(null);
		}
	}
}