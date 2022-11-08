using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatNew;
using Manito.Discord.Client;

using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Manito.Discord.ChatAbstract
{
	public class DialogueNetSessionTab<T>
	{
		private DialogueTabSessionTab<T> _sessionTab;
		public IReadOnlyList<DialogueTabSession<T>> Sessions => _sessionTab.Sessions;
		private MyDomain _domain;

		public DialogueNetSessionTab(MyDomain domain)
		{
			_sessionTab = new(domain.MyDiscordClient);
			_domain = domain;
		}

		public async Task<DialogueTabSession<T>> CreateSession(InteractiveInteraction interactive, T context, Func<DialogueTabSession<T>, Task<IDialogueNet>> builder)
		{
			var session = await _sessionTab.CreateSync(interactive, context);

			await _domain.ExecutionThread.AddNew(new ExecThread.Job(async (x) => {
				try
				{
					await NetworkCommon.RunNetwork(await builder(session));
				}
				catch (Exception e)
				{
					try
					{
						await session.SendMessage(new UniversalMessageBuilder().SetContent("Сессия завершена из-за ошибки!"));
						await session.EndSession();
					}
					catch (Exception ee)
					{
						await session.EndSession();
						throw new AggregateException(e, ee);
					}
					throw new AggregateException(e);
				}
			}));

			return session;
		}
	}
}