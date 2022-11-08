using Name.Bayfaderix.Darxxemiyur.Node.Network;

namespace Manito.Discord.Chat.DialogueNet
{
	public interface IDialogueNet : INodeNetwork
	{
		new NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;
	}
}