using Manito.Discord.Client;

using Name.Bayfaderix.Darxxemiyur.Common;
using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.Discord.Chat.DialogueNet
{
	public abstract class DialogueNetSessionControls<T> where T : DialogueNetSession
	{
		private List<T> _sessions;

		private MyDomain _service;
		public MyDomain Service => _service;
		public MyClientBundle Client => Service.MyDiscordClient;
		private AsyncLocker _lock;

		public DialogueNetSessionControls(MyDomain service)
		{
			_service = service;
			_sessions = new();
			_lock = new();
		}

		public bool SessionExists(Func<T, bool> predictate) =>
			_sessions.Any(predictate);

		public bool StopSession(T session) => StopSession(x => x == session);

		public bool StopSession(Predicate<T> predicate) => _sessions.RemoveAll(predicate) > 0;

		public async Task<T1> Atomary<T1>(Func<DialogueNetSessionControls<T>, Task<T1>> run)
		{
			await using var _ = await _lock.BlockAsyncLock();
			var res = await run(this);
			return res;
		}

		protected async Task<T> StartSession(Func<T> createSession, Func<T, IDialogueNet> getNet)
		{
			var sess = createSession();
			sess.ConnectManager((x) => Task.FromResult(StopSession(x as T)));
			_sessions.Add(sess);
			await _service.ExecutionThread.AddNew(new ExecThread.Job(async (x) => {
				try
				{
					await NetworkCommon.RunNetwork(getNet(sess));
				}
				catch (Exception e) { await sess.SessionExceptionHandle(e); }
			}));

			return sess;
		}
	}
}