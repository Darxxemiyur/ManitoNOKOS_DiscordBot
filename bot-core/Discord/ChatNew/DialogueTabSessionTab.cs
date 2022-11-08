using Manito.Discord.Client;

using Name.Bayfaderix.Darxxemiyur.Common;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Manito.Discord.ChatNew
{
	/// <summary>
	/// Dialogue Session tab Controls creation of new sessions and keeps the created ones.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class DialogueTabSessionTab<T>
	{
		public MyClientBundle Client {
			get; private set;
		}

		// Keeps list of created sessions.
		private List<DialogueTabSession<T>> _sessions;

		public IReadOnlyList<DialogueTabSession<T>> Sessions => _sessions;

		// Used to sync creation and deletion of sessions
		private AsyncLocker _sync;

		public DialogueTabSessionTab(MyClientBundle client)
		{
			_sync = new();
			_sessions = new();
			Client = client;
		}

		public async Task<DialogueTabSession<T>> CreateSync(InteractiveInteraction interactive, T context)
		{
			await using var _ = await _sync.BlockAsyncLock();

			var session = new DialogueTabSession<T>(this, interactive, context);
			session.OnRemove += RemoveSession;
			_sessions.Add(session);

			return session;
		}

		public async Task<bool> RemoveSession(IDialogueSession session)
		{
			await using var _ = await _sync.BlockAsyncLock();
			session.OnRemove -= RemoveSession;
			var res = _sessions.Remove(session as DialogueTabSession<T>);

			return res;
		}
	}
}