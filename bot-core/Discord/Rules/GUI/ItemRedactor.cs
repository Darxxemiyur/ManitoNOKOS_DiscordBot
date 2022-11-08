using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatNew;

using Name.Bayfaderix.Darxxemiyur.Common;
using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.Discord.Rules.GUI
{
	public class ItemRedactor : IDialogueNet
	{
		private List<ItemFrameBase> _list;
		private List<DiscordButtonComponent[]> _buttons;
		private Func<string, string> Sanitizer => (x) => $"btns_of_{x}";
		private DiscordButtonComponent _left;
		private DiscordButtonComponent _right;
		private int _slice = 0;

		private readonly UniversalSession _session;

		public ItemRedactor(UniversalSession session, List<ItemFrameBase> frames)
		{
			_list = frames;
			_session = session;
			_left = new DiscordButtonComponent(ButtonStyle.Primary, "toleft", null, false, new DiscordComponentEmoji("⬅️"));
			_right = new DiscordButtonComponent(ButtonStyle.Primary, "toright", null, false, new DiscordComponentEmoji("➡️"));

			_buttons = TurnToChunkTape(frames.Select((x, y) => (x.FrameName, Sanitizer($"{y}"))).Select(x => new DiscordButtonComponent(ButtonStyle.Primary, x.Item2, x.FrameName)).ToList(), 5, true).ToList();
		}

		public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

		private IEnumerable<DiscordButtonComponent[]> TurnToChunkTape(IEnumerable<DiscordButtonComponent> components, int maxPerChunk, bool fillWithEmpty) => fillWithEmpty ? components.AsSaturatedTape(x => _left, x => _right, maxPerChunk, x => new DiscordButtonComponent(ButtonStyle.Secondary, $"dis{x}", "** **", true)).Chunk(maxPerChunk) : components.AsMarkedTape(x => _left, x => _right, maxPerChunk).Chunk(maxPerChunk);

		private async Task<NextNetworkInstruction> StartRedacting(NetworkInstructionArgument args)
		{
			var session = _session;
			while (true)
			{
				var btns = (args.Payload as IEnumerable<DiscordButtonComponent[]> ?? Array.Empty<DiscordButtonComponent[]>()).Take(4).Prepend(_buttons[_slice]);

				var message = new UniversalMessageBuilder();

				message.AddEmbed(new DiscordEmbedBuilder().WithDescription($"Выберите редактуремую часть! {_slice + 1} из {_buttons.Count}"));

				foreach (var bnts in btns)
					message.AddComponents(bnts);

				await session.SendMessage(message);

				var answer = await session.GetComponentInteraction();
				await session.DoLaterReply();
				if (answer.CompareButton(_left))
				{
					_slice--;
					continue;
				}
				if (answer.CompareButton(_right))
				{
					_slice++;
					continue;
				}

				var btn = btns.SelectMany(x => x).First(x => answer.CompareButton(x));
			}
		}

		private async Task<NextNetworkInstruction> SelectPressed(NetworkInstructionArgument args)
		{
			throw new NotImplementedException();
		}

		private async Task<NextNetworkInstruction> RestartRedacting(NetworkInstructionArgument args)
		{
			throw new NotImplementedException();
		}

		public NextNetworkInstruction GetStartingInstruction() => new(StartRedacting);

		public NextNetworkInstruction GetStartingInstruction(object payload) => throw new NotImplementedException();
	}

	/// <summary>
	/// Awaits button interaction
	/// </summary>
	public class NumberEditor
	{
		public NumberEditor()
		{
		}

		public EditorType Type => EditorType.Number;
	}

	/// <summary>
	/// Awaits button interaction
	/// </summary>
	public class TypeEditor
	{
		public EditorType Type => EditorType.Type;
	}

	/// <summary>
	/// Awaits message
	/// </summary>
	public class StringEditor
	{
		public readonly string VariableName;
		private readonly string innerData;
		public string Data => innerData;
		public EditorType Type => EditorType.String;

		public StringEditor(string name, string data)
		{
			VariableName = name;
			innerData = data;
		}
	}
}