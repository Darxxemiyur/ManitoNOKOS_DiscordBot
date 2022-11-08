using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatNew;
using Manito.Discord.Client;

using Microsoft.EntityFrameworkCore;

using Name.Bayfaderix.Darxxemiyur.Common;
using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;
using System.Linq;
using System.Threading.Tasks;

using static Manito.Discord.PermanentMessage.MsgWallPanelWallLineImport;

namespace Manito.Discord.PermanentMessage
{
	public class MsgWallPanelWallLine : INodeNetwork
	{
		public class Editor : INodeNetwork
		{
			private DialogueTabSession<MsgContext> _session;
			private MessageWallLine _line;
			private MsgWallPanelWall.Selector _wallSelector;
			private MsgWallPanelWall.Editor _wallEditor;
			private NextNetworkInstruction _ret;
			public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

			public Editor(DialogueTabSession<MsgContext> session, NextNetworkInstruction ret)
			{
				_session = session;
				_ret = ret;
			}

			private async Task<NextNetworkInstruction> ShowOptions(NetworkInstructionArgument args)
			{
				var editContentBtn = new DiscordButtonComponent(ButtonStyle.Primary,
				 "editc", "Редактировать содержимое");
				var changeWallBtn = new DiscordButtonComponent(ButtonStyle.Primary, "editw", "Привязать к стене");
				var importBtn = new DiscordButtonComponent(ButtonStyle.Primary, "import", "Импортировать");
				var openWallBtn = new DiscordButtonComponent(ButtonStyle.Primary, "select",
				 "Открыть стену строки", _line.MessageWall == null);
				var remBtn = new DiscordButtonComponent(ButtonStyle.Danger, "remove", "Удалить");
				var exitBtn = new DiscordButtonComponent(ButtonStyle.Danger, "exit", "Выйти");
				var emb = new DiscordEmbedBuilder();

				emb.WithAuthor("Стена сообщения");

				var msg = _line?.WallLine?.Replace("`", "\\`")?.DoStartAtMax(4090 - 9) ?? "Пусто";
				emb.WithDescription($"```{msg}```");

				emb.AddField("Что сделать?", "** **");

				await _session.SendMessage(new DiscordWebhookBuilder()
					.AddEmbed(emb).AddComponents(editContentBtn, changeWallBtn)
					.AddComponents(importBtn, openWallBtn)
					.AddComponents(remBtn, exitBtn));

				var response = await _session.GetComponentInteraction();

				await _session.DoLaterReply();

				if (response.CompareButton(remBtn))
					return new(RemoveLine);

				if (response.CompareButton(editContentBtn))
					return new(EditContent);

				if (response.CompareButton(importBtn))
					return new(ImportMessage);

				if (response.CompareButton(changeWallBtn))
					return new(_wallSelector.SelectWall);

				if (response.CompareButton(openWallBtn))
					return new(OpenWall);

				return new(_ret);
			}

			private async Task<NextNetworkInstruction> ImportMessage(NetworkInstructionArgument arg)
			{
				var selectMenu = new StandaloneInteractiveSelectMenu<ImportedMessage>(_session, new EnumerablePageReturner<ImportedMessage>(_session.Context.Domain.MsgWallCtr.ImportedMessages, (x) => new Descriptor(x)));

				var line = (await selectMenu.EvaluateItem())?.GetCarriedItem();

				if (line == null)
				{
					await _session.DoLaterReply();
					return new(ShowOptions);
				}

				_line.SetLine(line.Message);
				_session.Context.Domain.MsgWallCtr.ImportedMessages.Remove(line);

				using var db = await _session.Context.Factory.CreateMyDbContextAsync();
				try
				{
					db.MessageWallLines.Update(_line);
					await db.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException) { }

				return new(ShowOptions);
			}

			private async Task<NextNetworkInstruction> RemoveLine(NetworkInstructionArgument args)
			{
				var returnBtn = new DiscordButtonComponent(ButtonStyle.Success, "return", "Назад");
				var removeBtn = new DiscordButtonComponent(ButtonStyle.Danger, "remove", "***Удалить***");

				var emb = new DiscordEmbedBuilder();
				emb.WithDescription($"**ВЫ УВЕРЕНЫ ЧТО ХОТИТЕ УДАЛИТЬ {_line.ID}?**");
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

				using var db = await _session.Context.Factory.CreateMyDbContextAsync();
				try
				{
					db.MessageWallLines.Remove(_line);
					await db.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException) { }

				_line = null;

				await _session.DoLaterReply();

				return new(_ret);
			}

			private async Task<NextNetworkInstruction> EditContent(NetworkInstructionArgument args)
			{
				await _session.SendMessage(new DiscordWebhookBuilder()
					.WithContent("Напишите содержимое стены сообщением"));

				var message = await _session.GetMessageInteraction();

				var msg = message.Content;

				msg = msg.Replace("```ff", "").Trim();

				using var db = await _session.Context.Factory.CreateMyDbContextAsync();
				_line.SetLine(msg);
				db.MessageWallLines.Update(_line);

				try
				{
					await db.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					await _session.SendMessage(new DiscordWebhookBuilder()
						.WithContent("Строка была удалена во время редактирования."));
				}

				return new(ShowOptions);
			}

			private async Task<NextNetworkInstruction> ChangeWall(NetworkInstructionArgument args)
			{
				var itm = (MessageWall)args.Payload;

				if (itm == null)
				{
					await _session.DoLaterReply();
					return new(ShowOptions);
				}

				_line.MessageWall = itm;

				using var db = await _session.Context.Factory.CreateMyDbContextAsync();
				db.MessageWalls.Update(itm);
				db.MessageWallLines.Update(_line);
				await db.SaveChangesAsync();

				return new(ShowOptions);
			}

			private async Task<NextNetworkInstruction> OpenWall(NetworkInstructionArgument args)
			{
				await using var db = await _session.Context.Factory.CreateMyDbContextAsync();
				var wall = db.MessageWalls.First(x => x.ID == _line.MessageWall.ID);

				return _wallEditor.GetStartingInstruction(wall);
			}

			public NextNetworkInstruction GetStartingInstruction()
			{
				throw new NotImplementedException();
			}

			public NextNetworkInstruction GetStartingInstruction(object payload)
			{
				_line = (MessageWallLine)payload;
				if (_line != null)
				{
					_wallEditor = new(_session, new(ShowOptions));
					_wallSelector = new(_session, ChangeWall);
				}
				return new(ShowOptions);
			}
		}

		public class Selector : INodeNetwork
		{
			private class Descriptor : IItemDescriptor<MessageWallLine>
			{
				private readonly MessageWallLine _wallLine;

				public Descriptor(MessageWallLine wallLine) => _wallLine = wallLine;

				private int _lid;
				private int _gid;

				public string GetButtonId() => $"MessageWallLine{_lid}_{_wallLine.ID}";

				public string GetButtonName()
				{
					var idStr = $" ID:{_wallLine.ID}";
					var wallName = $"{_wallLine.MessageWall?.WallName ?? ""}".Trim();

					return (string.Concat(wallName.Take(80 - idStr.Length)) + idStr).Trim();
				}

				public MessageWallLine GetCarriedItem() => _wallLine;

				public string GetFieldBody() => throw new NotImplementedException();

				public string GetFieldName() => throw new NotImplementedException();

				public int GetGlobalDisplayOrder() => _gid;

				public int GetLocalDisplayOrder() => _lid;

				public bool HasButton() => true;

				public bool HasField() => false;

				public IItemDescriptor<MessageWallLine> SetGlobalDisplayedOrder(int i)
				{
					_gid = i;
					return this;
				}

				public IItemDescriptor<MessageWallLine> SetLocalDisplayedOrder(int i)
				{
					_lid = i;
					return this;
				}
			}

			private DialogueTabSession<MsgContext> _session;
			private StandaloneInteractiveSelectMenu<MessageWallLine> _selectMenu;
			private Node _ret;
			public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;
			public DiscordButtonComponent MkNewButton;
			public DiscordButtonComponent EditButton;
			private readonly MessageWall _wall;

			public Selector(DialogueTabSession<MsgContext> session, Node ret, MessageWall wall)
			{
				(_wall, _session, _ret) = (wall, session, ret);
				_selectMenu = new StandaloneInteractiveSelectMenu<MessageWallLine>(_session, new CompactQuerryReturner<IPermMessageDbFactory, IPermMessageDb, MessageWallLine>(_session.Context.Factory, x => x.CreateMyDbContextAsync(), async x => x.MessageWallLines.Where(y => y.MessageWall == _wall), async x => new Descriptor(x)));
				EditButton = new DiscordButtonComponent(ButtonStyle.Primary, "edit", "Изменить");
				MkNewButton = new DiscordButtonComponent(ButtonStyle.Primary, "create", "Создать");
			}

			private async Task<NextNetworkInstruction> CreateNew(NetworkInstructionArgument args)
			{
				using var db = await _session.Context.Factory.CreateMyDbContextAsync();
				var line = new MessageWallLine();

				if (_wall != null)
				{
					line.MessageWall = _wall;
					db.MessageWalls.Update(_wall);
				}

				db.MessageWallLines.Add(line);
				await db.SaveChangesAsync();

				return new(_ret, line);
			}

			public async Task<NextNetworkInstruction> SelectToEdit(NetworkInstructionArgument args)
			{
				var line = (await _selectMenu.EvaluateItem())?.GetCarriedItem();

				return new(_ret, line);
			}

			public NextNetworkInstruction GetStartingInstruction()
			{
				throw new NotImplementedException();
			}

			public NextNetworkInstruction GetStartingInstruction(object payload)
			{
				var resp = payload as InteractiveInteraction;

				if (resp.CompareButton(EditButton))
					return new(SelectToEdit);

				if (resp.CompareButton(MkNewButton))
					return new(CreateNew);

				throw new NotImplementedException();
			}
		}

		private DialogueTabSession<MsgContext> _session;
		private Editor _editor;
		private Selector _selector;

		public MsgWallPanelWallLine(DialogueTabSession<MsgContext> session)
		{
			_selector = new(session, Decider, null);
			_editor = new(session, new(_selector.SelectToEdit));
			_session = session;
		}

		private async Task<NextNetworkInstruction> EnterMenu(NetworkInstructionArgument args)
		{
			var exitBtn = new DiscordButtonComponent(ButtonStyle.Danger, "exit", "Выйти");

			await _session.SendMessage(new DiscordInteractionResponseBuilder()
				.WithContent("Добро пожаловать в меню управления строками!")
				.AddComponents(_selector.MkNewButton.Enable(), _selector.EditButton.Enable(), exitBtn));

			var response = await _session.GetComponentInteraction();

			if (response.CompareButton(exitBtn))
				return new();

			await _session.SendMessage(new DiscordInteractionResponseBuilder()
				.WithContent("Добро пожаловать в меню управления строками!")
				.AddComponents(_selector.MkNewButton.Disable(),
				 _selector.EditButton.Disable(), exitBtn.Disable()));

			return _selector.GetStartingInstruction(response);
		}

		private async Task<NextNetworkInstruction> Decider(NetworkInstructionArgument args)
		{
			var itm = (MessageWallLine)args.Payload;

			if (itm == null)
				return new(EnterMenu);

			return _editor.GetStartingInstruction(itm);
		}

		public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

		public NextNetworkInstruction GetStartingInstruction() => new(EnterMenu);

		public NextNetworkInstruction GetStartingInstruction(object payload)
		 => GetStartingInstruction();
	}
}