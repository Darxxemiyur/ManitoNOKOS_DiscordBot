using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Exceptions;

using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatNew;
using Manito.Discord.Client;

using Microsoft.EntityFrameworkCore;

using Name.Bayfaderix.Darxxemiyur.Common;
using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.Discord.PermanentMessage
{
	public class MsgWallPanelWallTranslator : INodeNetwork
	{
		private class Descriptor : IItemDescriptor<MessageWallTranslator>
		{
			private readonly MessageWallTranslator _wallLine;

			public Descriptor(MessageWallTranslator wallLine) => _wallLine = wallLine;

			private int _lid;
			private int _gid;

			public string GetButtonId() => $"Translator{_lid}_{_wallLine.ID}";

			private string GetMyThing(string str) => $"Транслятор {str} ID:<#{_wallLine.ChannelId}>";

			public string GetButtonName() => GetMyThing(_wallLine.MessageWall?.WallName.DoStartAtMax(80 - GetMyThing("").Length));

			public MessageWallTranslator GetCarriedItem() => _wallLine;

			public string GetFieldBody() => throw new NotImplementedException();

			public string GetFieldName() => throw new NotImplementedException();

			public int GetGlobalDisplayOrder() => _gid;

			public int GetLocalDisplayOrder() => _lid;

			public bool HasButton() => true;

			public bool HasField() => false;

			public IItemDescriptor<MessageWallTranslator> SetGlobalDisplayedOrder(int i)
			{
				_gid = i;
				return this;
			}

			public IItemDescriptor<MessageWallTranslator> SetLocalDisplayedOrder(int i)
			{
				_lid = i;
				return this;
			}
		}

		public class Editor : INodeNetwork
		{
			private DialogueTabSession<MsgContext> _session;
			private MessageWallTranslator _translator;
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
				var syncBtn = new DiscordButtonComponent(ButtonStyle.Primary,
				 "sync", "Синхронизировать", _translator.ChannelId == 0);
				var linkChnlBtn = new DiscordButtonComponent(ButtonStyle.Primary, "linkc", "Привязать к каналу");
				var linkWallBtn = new DiscordButtonComponent(ButtonStyle.Primary, "linkw", "Привязать к стене");
				var openWallBtn = new DiscordButtonComponent(ButtonStyle.Primary, "select",
				 "Открыть стену транслятора", _translator.MessageWall == null);
				var remBtn = new DiscordButtonComponent(ButtonStyle.Danger, "remove", "Удалить");
				var exitBtn = new DiscordButtonComponent(ButtonStyle.Danger, "exit", "Выйти");

				var emb = new DiscordEmbedBuilder();

				emb.WithAuthor("Транслятор стены сообщений в канал");

				emb.WithDescription(_translator?.MessageWall?.WallName + $" в <#{_translator?.ChannelId}>");

				emb.AddField("Что сделать?", "** **");

				await _session.SendMessage(new DiscordWebhookBuilder()
					.AddEmbed(emb).AddComponents(syncBtn, linkChnlBtn, linkWallBtn)
					.AddComponents(openWallBtn, remBtn, exitBtn));

				var response = await _session.GetComponentInteraction();

				if (response.CompareButton(linkChnlBtn))
					return new(LinkChannel);

				await _session.DoLaterReply();

				if (response.CompareButton(remBtn))
					return new(RemoveTranslator);

				if (response.CompareButton(syncBtn))
					return new(ForceSyncTranslator);

				if (response.CompareButton(linkWallBtn))
					return new(_wallSelector.SelectWall);

				if (response.CompareButton(openWallBtn))
					return new(OpenWall);

				return new(_ret);
			}

			private async Task<NextNetworkInstruction> ForceSyncTranslator(NetworkInstructionArgument arg)
			{
				await _session.SendMessage(new DiscordWebhookBuilder()
					.WithContent("Работаем..."));
				var result = await _session.Context.Domain.MsgWallCtr.PostMessageUpdate(_translator.ID, _session.Context);

				var changed = await result;
				await _session.SendMessage(new DiscordWebhookBuilder()
					.WithContent($"Обновлено {changed}"));
				await Task.Delay(TimeSpan.FromSeconds(5));

				return new(ShowOptions);
			}

			private async Task<NextNetworkInstruction> LinkChannel(NetworkInstructionArgument arg)
			{
				await _session.SendMessage(new DiscordInteractionResponseBuilder().WithContent("Введите id канала"));

				return new(WaitAndRetryLink);
			}

			private async Task<NextNetworkInstruction> WaitAndRetryLink(NetworkInstructionArgument arg)
			{
				var msg = await _session.GetMessageInteraction();

				if (!ulong.TryParse(msg.Content, out var id))
				{
					await _session.SendMessage(new DiscordWebhookBuilder().WithContent("Ошибка!"));
					return new(WaitAndRetryLink);
				}

				using var db = _session.Context.Factory.CreateMyDbContext();

				_translator.ChannelId = id;

				db.MessageWallTranslators.Update(_translator);
				await db.SaveChangesAsync();

				return new(ShowOptions);
			}

			private async Task<NextNetworkInstruction> RemoveTranslator(NetworkInstructionArgument arg)
			{
				var returnBtn = new DiscordButtonComponent(ButtonStyle.Success, "return", "Назад");
				var removeBtn = new DiscordButtonComponent(ButtonStyle.Danger, "remove", "***Удалить***");

				var emb = new DiscordEmbedBuilder();
				emb.WithDescription($"**ВЫ УВЕРЕНЫ ЧТО ХОТИТЕ УДАЛИТЬ ТРАНСЛЯТОР №{_translator.ID}?**");
				await _session.SendMessage(new DiscordWebhookBuilder()
					.AddEmbed(emb).AddComponents(returnBtn, removeBtn));

				var response = await _session.GetComponentInteraction();

				await _session.DoLaterReply();

				if (!response.CompareButton(removeBtn))
				{
					await _session.SendMessage(new DiscordInteractionResponseBuilder()
						.AddComponents(returnBtn.Disable(), removeBtn.Disable()));
				}

				if (response.CompareButton(returnBtn))
					return new(ShowOptions);

				using var db = _session.Context.Factory.CreateMyDbContext();

				try
				{
					try
					{
						var channel = await _session.Context.Client.Client.GetChannelAsync(_translator.ChannelId);
						foreach (var id in _translator.Translation)
						{
							try
							{
								var msg = await channel.GetMessageAsync(id);
								await msg.DeleteAsync();
							}
							catch (NotFoundException) { }
						}
					}
					catch (NotFoundException) { }

					db.MessageWallTranslators.Remove(_translator);
					await db.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException) { }

				_translator = null;

				return new(_ret);
			}

			private async Task<NextNetworkInstruction> ChangeWall(NetworkInstructionArgument args)
			{
				var itm = (MessageWall)args.Payload;

				if (itm == null)
				{
					await _session.DoLaterReply();
					return new(ShowOptions);
				}

				_translator.MessageWall = itm;

				using var db = await _session.Context.Factory.CreateMyDbContextAsync();
				db.MessageWalls.Update(itm);
				db.MessageWallTranslators.Update(_translator);
				await db.SaveChangesAsync();

				return new(ShowOptions);
			}

			private async Task<NextNetworkInstruction> OpenWall(NetworkInstructionArgument args)
			{
				await using var db = await _session.Context.Factory.CreateMyDbContextAsync();
				var wall = db.MessageWalls.First(x => x.ID == _translator.MessageWall.ID);

				return _wallEditor.GetStartingInstruction(wall);
			}

			public NextNetworkInstruction GetStartingInstruction()
			{
				throw new NotImplementedException();
			}

			public NextNetworkInstruction GetStartingInstruction(object payload)
			{
				_translator = (MessageWallTranslator)payload;
				if (_translator != null)
				{
					_wallEditor = new(_session, new(ShowOptions));
					_wallSelector = new(_session, ChangeWall);
				}
				return new(ShowOptions);
			}
		}

		public class Selector : INodeNetwork
		{
			private DialogueTabSession<MsgContext> _session;
			private StandaloneInteractiveSelectMenu<MessageWallTranslator> _selectMenu;
			private Node _ret;
			public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;
			public DiscordButtonComponent MkNewButton;
			public DiscordButtonComponent EditButton;
			private readonly MessageWall _wall;

			public Selector(DialogueTabSession<MsgContext> session, Node ret, MessageWall wall)
			{
				(_wall, _session, _ret) = (wall, session, ret);
				_selectMenu = new StandaloneInteractiveSelectMenu<MessageWallTranslator>(_session, new CompactQuerryReturner<IPermMessageDbFactory, IPermMessageDb, MessageWallTranslator>(session.Context.Factory, x => x.CreateMyDbContextAsync(), async x => x.MessageWallTranslators, async x => new Descriptor(x)));
				EditButton = new DiscordButtonComponent(ButtonStyle.Primary, "edit", "Изменить");
				MkNewButton = new DiscordButtonComponent(ButtonStyle.Success, "create", "Создать");
			}

			private async Task<NextNetworkInstruction> CreateNew(NetworkInstructionArgument args)
			{
				using var db = await _session.Context.Factory.CreateMyDbContextAsync();
				var line = new MessageWallTranslator();

				if (_wall != null)
				{
					line.MessageWall = _wall;
					db.MessageWalls.Update(_wall);
				}

				db.MessageWallTranslators.Add(line);
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

		public MsgWallPanelWallTranslator(DialogueTabSession<MsgContext> session)
		{
			_session = session;
			_selector = new(session, Decider, null);
			_editor = new(session, new(_selector.SelectToEdit));
		}

		private async Task<NextNetworkInstruction> EnterMenu(NetworkInstructionArgument args)
		{
			var syncBtn = new DiscordButtonComponent(ButtonStyle.Secondary, "syncall", "Синхронизировать всех");
			var exitBtn = new DiscordButtonComponent(ButtonStyle.Danger, "exit", "Выйти");

			await _session.SendMessage(new DiscordInteractionResponseBuilder()
				.WithContent("Добро пожаловать в меню управления транслятора стены строк!")
				.AddComponents(_selector.MkNewButton.Enable(), _selector.EditButton.Enable())
				.AddComponents(syncBtn, exitBtn));
			var response = await _session.GetComponentInteraction();

			if (response.CompareButton(exitBtn))
				return new();

			await _session.SendMessage(new DiscordInteractionResponseBuilder()
				.WithContent("Добро пожаловать в меню управления транслятора стены строк!")
				.AddComponents(_selector.MkNewButton.Disable(), _selector.EditButton.Disable())
				.AddComponents(syncBtn.Disable(), exitBtn.Disable()));

			if (response.CompareButton(syncBtn))
				return new(ForceSyncAll);

			return _selector.GetStartingInstruction(response);
		}

		private async Task<NextNetworkInstruction> ForceSyncAll(NetworkInstructionArgument arg)
		{
			var sent = _session.SendMessage(new DiscordWebhookBuilder()
				.WithContent($"Работаем..."));

			var list = new List<long>();

			using (var db = _session.Context.Factory.CreateMyDbContext())
			{
				list = db.MessageWallTranslators.Select(x => x.ID).ToList();
			}

			var changedAll = 0;
			foreach (var id in list)
			{
				changedAll += await await _session.Context.Domain.MsgWallCtr.PostMessageUpdate(id, _session.Context) ?? 0;
			}

			await sent;
			await _session.SendMessage(new DiscordWebhookBuilder()
				.WithContent($"Всего изменено {changedAll} строк у {list.Count} стен сообщений.")
				.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "ok", "Ок")));

			await _session.GetComponentInteraction();

			return new(EnterMenu);
		}

		private async Task<NextNetworkInstruction> Decider(NetworkInstructionArgument args)
		{
			var itm = (MessageWallTranslator)args.Payload;

			if (itm == null)
				return new(EnterMenu);

			return _editor.GetStartingInstruction(itm);
		}

		public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

		public NextNetworkInstruction GetStartingInstruction() => new(EnterMenu);

		public NextNetworkInstruction GetStartingInstruction(object payload) => GetStartingInstruction();
	}
}