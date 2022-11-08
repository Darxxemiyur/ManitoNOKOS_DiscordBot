using DisCatSharp.Entities;

using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatAbstract;
using Manito.Discord.Client;

using Microsoft.EntityFrameworkCore;

using Name.Bayfaderix.Darxxemiyur.Common;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.Discord.PermanentMessage
{
	public class MsgContext
	{
		public MyClientBundle Client => Domain.MyDiscordClient;
		public IPermMessageDbFactory Factory => Domain.DbFactory;

		public MyDomain Domain {
			get; private set;
		}

		public MsgContext(MyDomain domain) => this.Domain = domain;
	}

	public class MessageController : IModule
	{
		private MyDomain _domain;
		private DialogueNetSessionTab<MsgContext> _sessionTab;

		public List<ImportedMessage> ImportedMessages {
			get; private set;
		}

		public MessageController(MyDomain domain)
		{
			_domain = domain;
			_sessionTab = new DialogueNetSessionTab<MsgContext>(domain);
			_postMessageUpdateQueue = new();
			ImportedMessages = new();
		}

		public async Task RunModule()
		{
			await Task.WhenAll(PostMessageUpdateLoop());
		}

		private async Task PostMessageUpdateLoop()
		{
			while (true)
			{
				var (id, context, tsk) = await _postMessageUpdateQueue.GetData();
				try
				{
					using var db = await context.Factory.CreateMyDbContextAsync();

					var translator = db.MessageWallTranslators
						.Where(x => x.ID == id)
						.Include(x => x.MessageWall)
						.ThenInclude(x => x.Msgs)
						.OrderBy(x => x.ID)
						.FirstOrDefault();

					if (translator != null)
					{
						var updateResult = await translator.SubmitUpdate(context.Client.Client);
						tsk.TrySetResult(updateResult);
						await db.SaveChangesAsync();
						continue; // "yield" the loop to the next item.
					}
				}
				catch (Exception e)
				{
					tsk.TrySetException(e);
				}
			}
		}

		/// <summary>
		/// Posts an update request and return proxy for result.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<Task<int?>> PostMessageUpdate(long id, MsgContext context)
		{
			var callback = new MyTaskSource<int?>();

			await _postMessageUpdateQueue.Handle((id, context, callback));

			return callback.MyTask;
		}

		/// <summary>
		/// List of post update requests containing translator ID and a callback that resolves after update;
		/// </summary>
		private readonly FIFOFBACollection<(long, MsgContext, MyTaskSource<int?>)> _postMessageUpdateQueue;

		public async Task StartSession(DiscordInteraction args)
		{
			await _sessionTab.CreateSession(new InteractiveInteraction(args),
				new MsgContext(_domain), (x) => Task.FromResult<IDialogueNet>(new MsgWallPanel(x)));
		}
	}
}