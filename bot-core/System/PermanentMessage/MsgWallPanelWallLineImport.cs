using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatNew;
using Manito.Discord.Client;

using Name.Bayfaderix.Darxxemiyur.Common;

using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;
using System.Threading.Tasks;

namespace Manito.Discord.PermanentMessage
{
	public class MsgWallPanelWallLineImport : INodeNetwork
	{
		public class Editor : INodeNetwork
		{
			private DialogueTabSession<MsgContext> _session;
			private ImportedMessage _line;
			private NextNetworkInstruction _ret;
			public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

			public Editor(DialogueTabSession<MsgContext> session, NextNetworkInstruction ret)
			{
				_session = session;
				_ret = ret;
			}

			private async Task<NextNetworkInstruction> ShowOptions(NetworkInstructionArgument args)
			{
				var remBtn = new DiscordButtonComponent(ButtonStyle.Danger, "remove", "Удалить");
				var exitBtn = new DiscordButtonComponent(ButtonStyle.Danger, "exit", "Выйти");
				var emb = new DiscordEmbedBuilder();

				emb.WithAuthor("Стена сообщения");

				var msg = _line.Message?.Replace("`", "\\`")?.DoStartAtMax(4090 - 9) ?? "Пусто";
				emb.WithDescription($"```{msg}```");

				emb.AddField("Что сделать?", "** **");

				await _session.SendMessage(new DiscordWebhookBuilder()
					.AddEmbed(emb).AddComponents(remBtn, exitBtn));

				var response = await _session.GetComponentInteraction();

				await _session.DoLaterReply();

				return response.CompareButton(remBtn) ? new(RemoveLine) : _ret;
			}

			private async Task<NextNetworkInstruction> RemoveLine(NetworkInstructionArgument args)
			{
				var returnBtn = new DiscordButtonComponent(ButtonStyle.Success, "return", "Назад");
				var removeBtn = new DiscordButtonComponent(ButtonStyle.Danger, "remove", "***Удалить***");

				var emb = new DiscordEmbedBuilder();
				emb.WithDescription($"**ВЫ УВЕРЕНЫ ЧТО ХОТИТЕ УДАЛИТЬ {_line.MessageId}?**");
				await _session.SendMessage(new DiscordWebhookBuilder()
					.AddEmbed(emb).AddComponents(returnBtn, removeBtn));

				var response = await _session.GetComponentInteraction();

				if (!response.CompareButton(removeBtn))
				{
					await _session.SendMessage(new DiscordInteractionResponseBuilder()
						.AddComponents(returnBtn.Disable(), removeBtn.Disable()));
				}

				if (response.CompareButton(returnBtn))
					return new(ShowOptions);

				_session.Context.Domain.MsgWallCtr.ImportedMessages.Remove(_line);

				await _session.DoLaterReply();

				return new(_ret);
			}

			public NextNetworkInstruction GetStartingInstruction()
			{
				throw new NotImplementedException();
			}

			public NextNetworkInstruction GetStartingInstruction(object payload)
			{
				_line = (ImportedMessage)payload;

				return new(ShowOptions);
			}
		}

		public class Descriptor : IItemDescriptor<ImportedMessage>
		{
			private readonly ImportedMessage _wallLine;

			public Descriptor(ImportedMessage wallLine) => _wallLine = wallLine;

			private int _lid;
			private int _gid;

			public string GetButtonId() => $"Importer{_lid}_{_wallLine.MessageId}";

			public string GetButtonName()
			{
				var wallName = $"{_wallLine.Message ?? ""}".Trim();

				return wallName.DoStartAtMax(80).Trim();
			}

			public ImportedMessage GetCarriedItem() => _wallLine;

			public string GetFieldBody() => throw new NotImplementedException();

			public string GetFieldName() => throw new NotImplementedException();

			public int GetGlobalDisplayOrder() => _gid;

			public int GetLocalDisplayOrder() => _lid;

			public bool HasButton() => true;

			public bool HasField() => false;

			public IItemDescriptor<ImportedMessage> SetGlobalDisplayedOrder(int i)
			{
				_gid = i;
				return this;
			}

			public IItemDescriptor<ImportedMessage> SetLocalDisplayedOrder(int i)
			{
				_lid = i;
				return this;
			}
		}

		private StandaloneInteractiveSelectMenu<ImportedMessage> _selectMenu;

		private DialogueTabSession<MsgContext> _session;
		private Editor _editor;

		public MsgWallPanelWallLineImport(DialogueTabSession<MsgContext> session)
		{
			_editor = new(session, new(Choose));
			_session = session;
			_selectMenu = new StandaloneInteractiveSelectMenu<ImportedMessage>(_session,
				new EnumerablePageReturner<ImportedMessage>(
					_session.Context.Domain.MsgWallCtr.ImportedMessages, (x) => new Descriptor(x)));
		}

		private async Task<NextNetworkInstruction> EnterMenu(NetworkInstructionArgument args)
		{
			await _session.DoLaterReply();
			return new(Choose);
		}

		private async Task<NextNetworkInstruction> Choose(NetworkInstructionArgument arg)
		{
			var line = (await _selectMenu.EvaluateItem())?.GetCarriedItem();

			return new(Decider, line);
		}

		private async Task<NextNetworkInstruction> Decider(NetworkInstructionArgument args)
		{
			var itm = (ImportedMessage)args.Payload;

			if (itm == null)
				return new();

			return _editor.GetStartingInstruction(itm);
		}

		public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

		public NextNetworkInstruction GetStartingInstruction() => new(EnterMenu);

		public NextNetworkInstruction GetStartingInstruction(object payload)
		 => GetStartingInstruction();
	}
}