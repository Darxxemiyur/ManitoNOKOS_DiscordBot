using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatNew;

using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System.Threading.Tasks;

namespace Manito.Discord.PermanentMessage
{
	/// <summary>
	/// MessageWall Service Menu dialogue
	/// </summary>
	public class MsgWallPanel : IDialogueNet
	{
		private DialogueTabSession<MsgContext> _session;

		public MsgWallPanel(DialogueTabSession<MsgContext> session)
		{
			_session = session;
		}

		public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

		public NextNetworkInstruction GetStartingInstruction() => new(SelectWhatToDo);

		public NextNetworkInstruction GetStartingInstruction(object payload) => GetStartingInstruction();

		private async Task<NextNetworkInstruction> SelectWhatToDo(NetworkInstructionArgument arg)
		{
			var wallLine = new DiscordButtonComponent(ButtonStyle.Primary, "wallLine", "Строки-сироты");
			var wall = new DiscordButtonComponent(ButtonStyle.Primary, "wall", "Стены");
			var imported = new DiscordButtonComponent(ButtonStyle.Primary, "import", "Импортированые соо-ния");
			var wallTranslator = new DiscordButtonComponent(ButtonStyle.Primary, "wallTranslator", "Трансляторы");

			var exitBtn = new DiscordButtonComponent(ButtonStyle.Danger, "exit", "Выйти");

			await _session.SendMessage(new DiscordInteractionResponseBuilder()
				.WithContent("Выберите что хотите изменять")
				.AddComponents(wallLine, wall, wallTranslator, imported)
				.AddComponents(exitBtn));
			var response = await _session.GetComponentInteraction();

			if (response.CompareButton(wallLine))
			{
				var next = new MsgWallPanelWallLine(_session);
				await NetworkCommon.RunNetwork(next);
			}

			if (response.CompareButton(wall))
			{
				var next = new MsgWallPanelWall(_session);
				await NetworkCommon.RunNetwork(next);
			}

			if (response.CompareButton(wallTranslator))
			{
				var next = new MsgWallPanelWallTranslator(_session);
				await NetworkCommon.RunNetwork(next);
			}

			if (response.CompareButton(imported))
			{
				var next = new MsgWallPanelWallLineImport(_session);
				await NetworkCommon.RunNetwork(next);
			}

			if (response.CompareButton(exitBtn))
			{
				await _session.EndSession();
				await _session.RemoveMessage();
				return new();
			}

			return new(SelectWhatToDo);
		}
	}
}