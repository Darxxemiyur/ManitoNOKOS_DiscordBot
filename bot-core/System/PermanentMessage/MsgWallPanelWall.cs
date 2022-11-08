using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatNew;
using Manito.Discord.Client;

using Microsoft.EntityFrameworkCore;

using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.Discord.PermanentMessage
{
	/// <summary>
	/// MessageWall wall submenu
	/// </summary>
	public class MsgWallPanelWall : INodeNetwork
	{
		/// <summary>
		/// MessageWall Wall Editor dialogue
		/// </summary>
		public class Editor : INodeNetwork
		{
			private DialogueTabSession<MsgContext> _session;
			private MessageWall _wall;
			private MsgWallPanelWallLine.Selector _lineSelector;
			private MsgWallPanelWallLine.Editor _lineEditor;
			private NextNetworkInstruction _ret;
			public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

			public Editor(DialogueTabSession<MsgContext> session, NextNetworkInstruction ret)
			{
				_session = session;
				_ret = ret;
			}

			private async Task<NextNetworkInstruction> ActionsChoose(NetworkInstructionArgument args)
			{
				var renameBtn = new DiscordButtonComponent(ButtonStyle.Secondary, "rename", "Назвать");
				var listBtn = new DiscordButtonComponent(ButtonStyle.Primary, "list", "Показать строки");
				var remBtn = new DiscordButtonComponent(ButtonStyle.Danger, "remove", "Удалить");
				var exitBtn = new DiscordButtonComponent(ButtonStyle.Danger, "exit", "Выйти");
				var emb = new DiscordEmbedBuilder();

				emb.WithAuthor("Стена сообщений");
				emb.WithDescription(_wall.WallName);
				emb.AddField("Что сделать?", "** **");

				var sel = _lineSelector;

				await _session.SendMessage(new DiscordWebhookBuilder()
					.AddEmbed(emb).AddComponents(renameBtn, listBtn)
					.AddComponents(remBtn, exitBtn));

				var response = await _session.GetComponentInteraction();

				await _session.DoLaterReply();

				if (response.CompareButton(renameBtn))
					return new(RenameWall);

				if (response.CompareButton(listBtn))
					return new(SelectWallChildren);

				if (response.CompareButton(remBtn))
					return new(RemoveWall);

				return new(_ret);
			}

			private async Task<NextNetworkInstruction> SelectWallChildren(NetworkInstructionArgument args)
			{
				var exitBtn = new DiscordButtonComponent(ButtonStyle.Danger, "exit", "Назад");

				var sel = _lineSelector;

				await _session.SendMessage(new DiscordWebhookBuilder()
					.WithContent("Добро пожаловать в дочернее меню управления строками!")
					.AddComponents(sel.MkNewButton.Enable(), sel.EditButton.Enable(), exitBtn));

				var response = await _session.GetComponentInteraction();
				if (response.CompareButton(exitBtn))
				{
					await _session.DoLaterReply();
					return new(ActionsChoose);
				}

				await _session.SendMessage(new DiscordInteractionResponseBuilder()
					.WithContent("Добро пожаловать в дочернее меню управления строками!")
					.AddComponents(sel.MkNewButton.Disable(),
					 sel.EditButton.Disable(), exitBtn.Disable()));

				return sel.GetStartingInstruction(response);
			}

			private async Task<NextNetworkInstruction> OnChildSelected(NetworkInstructionArgument arg)
			{
				var itm = (MessageWallLine)arg.Payload;

				if (itm == null)
				{
					await _session.DoLaterReply();
					return new(SelectWallChildren);
				}

				return _lineEditor.GetStartingInstruction(itm);
			}

			private async Task<NextNetworkInstruction> RenameWall(NetworkInstructionArgument args)
			{
				using var db = await _session.Context.Factory.CreateMyDbContextAsync();
				db.MessageWalls.Update(_wall);

				await _session.SendMessage(new DiscordWebhookBuilder()
					.WithContent("Напишите желаемое название стены сообщений"));

				var msg = await _session.GetMessageInteraction();

				_wall.SetName(msg.Content);

				try
				{
					await db.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					await _session.SendMessage(new DiscordWebhookBuilder()
						.WithContent("Стена была удалена во время редактирования."));
				}

				return new(ActionsChoose);
			}

			private async Task<NextNetworkInstruction> RemoveWall(NetworkInstructionArgument args)
			{
				var returnBtn = new DiscordButtonComponent(ButtonStyle.Success, "return", "Назад");
				var removeBtn = new DiscordButtonComponent(ButtonStyle.Danger, "remove", "***Удалить***");

				var emb = new DiscordEmbedBuilder();
				emb.WithDescription($"**ВЫ УВЕРЕНЫ ЧТО ХОТИТЕ УДАЛИТЬ {_wall.ID}?**");
				await _session.SendMessage(new DiscordWebhookBuilder()
					.AddEmbed(emb).AddComponents(returnBtn, removeBtn));

				var response = await _session.GetComponentInteraction();

				if (!response.CompareButton(removeBtn))
				{
					await _session.SendMessage(new DiscordInteractionResponseBuilder()
						.AddComponents(returnBtn.Disable(), removeBtn.Disable()));
				}

				if (response.CompareButton(returnBtn))
					return new(ActionsChoose);

				try
				{
					using var db = await _session.Context.Factory.CreateMyDbContextAsync();
					db.MessageWalls.Update(_wall);
					await db.ImplementedContext.Entry(_wall)
					.Collection(x => x.Msgs).Query().LoadAsync();
					_wall.Msgs.Clear();

					foreach (var translator in db.MessageWallTranslators
						.Where(x => x.MessageWall == _wall).ToList())
					{
						translator.MessageWall = null;
					}

					db.MessageWalls.Remove(_wall);
					await db.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException) { }

				_wall = null;

				await _session.DoLaterReply();

				return new(_ret);
			}

			public NextNetworkInstruction GetStartingInstruction()
			{
				throw new NotImplementedException();
			}

			public NextNetworkInstruction GetStartingInstruction(object payload)
			{
				_wall = (MessageWall)payload;
				_lineEditor = new(_session, new(SelectWallChildren));
				_lineSelector = new(_session, OnChildSelected, _wall);
				return new(ActionsChoose);
			}
		}

		public class Selector : INodeNetwork
		{
			/// <summary>
			/// Selector Menu Descriptor
			/// </summary>
			private class Descriptor : IItemDescriptor<MessageWall>
			{
				private readonly MessageWall _wall;

				public Descriptor(MessageWall wall) => _wall = wall;

				private int _lid;
				private int _gid;

				public string GetButtonId() => $"MessageWall{_lid}_{_wall.ID}";

				private string GetMyThing(string str) => $"Стена {str} ID:{_wall.ID}";

				public string GetButtonName() => GetMyThing(_wall.WallName
					[..Math.Min(_wall.WallName.Length, 80 - GetMyThing("").Length)]);

				public MessageWall GetCarriedItem() => _wall;

				public string GetFieldBody() => throw new NotImplementedException();

				public string GetFieldName() => throw new NotImplementedException();

				public int GetGlobalDisplayOrder() => _gid;

				public int GetLocalDisplayOrder() => _lid;

				public bool HasButton() => true;

				public bool HasField() => false;

				public IItemDescriptor<MessageWall> SetGlobalDisplayedOrder(int i)
				{
					_gid = i;
					return this;
				}

				public IItemDescriptor<MessageWall> SetLocalDisplayedOrder(int i)
				{
					_lid = i;
					return this;
				}
			}

			private DialogueTabSession<MsgContext> _session;
			private StandaloneInteractiveSelectMenu<MessageWall> _selectMenu;
			private Node _ret;
			public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;
			public DiscordButtonComponent CreateButton;
			public DiscordButtonComponent SelectButton;

			public Selector(DialogueTabSession<MsgContext> session, Node ret)
			{
				CreateButton = new DiscordButtonComponent(ButtonStyle.Success, "create", "Создать");
				SelectButton = new DiscordButtonComponent(ButtonStyle.Primary, "select", "Выбрать");
				(_session, _ret) = (session, ret);
				_selectMenu = new StandaloneInteractiveSelectMenu<MessageWall>(_session, new CompactQuerryReturner<IPermMessageDbFactory, IPermMessageDb, MessageWall>(_session.Context.Factory, x => x.CreateMyDbContextAsync(), async x => x.MessageWalls, async x => new Descriptor(x)));
			}

			private async Task<NextNetworkInstruction> CreateWall(NetworkInstructionArgument args)
			{
				using var db = await _session.Context.Factory.CreateMyDbContextAsync();

				var wall = new MessageWall();
				db.MessageWalls.Add(wall);
				await db.SaveChangesAsync();
				wall.WallName = $"Безимянная стена №{wall.ID}";
				await db.SaveChangesAsync();

				return new(_ret, wall);
			}

			public async Task<NextNetworkInstruction> SelectWall(NetworkInstructionArgument args)
			{
				var wall = (await _selectMenu.EvaluateItem())?.GetCarriedItem();

				return new(_ret, wall);
			}

			public NextNetworkInstruction GetStartingInstruction()
			{
				throw new NotImplementedException();
			}

			public NextNetworkInstruction GetStartingInstruction(object payload)
			{
				var resp = payload as InteractiveInteraction;

				if (resp.CompareButton(SelectButton))
					return new(SelectWall);

				if (resp.CompareButton(CreateButton))
					return new(CreateWall);

				throw new NotImplementedException();
			}
		}

		private DialogueTabSession<MsgContext> _session;
		private Editor _editor;
		private Selector _selector;

		public MsgWallPanelWall(DialogueTabSession<MsgContext> session)
		{
			_session = session;
			_selector = new(session, Decider);
			_editor = new(session, new(_selector.SelectWall));
		}

		private async Task<NextNetworkInstruction> EnterMenu(NetworkInstructionArgument args)
		{
			var exitBtn = new DiscordButtonComponent(ButtonStyle.Danger, "exit", "Выйти");

			await _session.SendMessage(new DiscordInteractionResponseBuilder()
				.WithContent("Добро пожаловать в меню управления стены строк!")
				.AddComponents(_selector.CreateButton.Enable(), _selector.SelectButton.Enable(), exitBtn));
			var response = await _session.GetComponentInteraction();

			if (response.CompareButton(exitBtn))
				return new();

			await _session.SendMessage(new DiscordInteractionResponseBuilder()
				.WithContent("Добро пожаловать в меню управления стены строк!")
				.AddComponents(_selector.CreateButton.Disable(),
				 _selector.SelectButton.Disable(), exitBtn.Disable()));

			return _selector.GetStartingInstruction(response);
		}

		private async Task<NextNetworkInstruction> Decider(NetworkInstructionArgument args)
		{
			var itm = (MessageWall)args.Payload;

			if (itm == null)
				return new(EnterMenu);

			return _editor.GetStartingInstruction(itm);
		}

		public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

		public NextNetworkInstruction GetStartingInstruction() => new(EnterMenu);

		public NextNetworkInstruction GetStartingInstruction(object payload) => GetStartingInstruction();
	}
}