using Manito.Discord.Client;

using System;
using System.Threading.Tasks;

namespace Manito.Discord.ChatNew
{
	public class DialogueTabSession<T> : UniversalSession
	{
		/// <summary>
		/// Tab this session belongs to
		/// </summary>
		public DialogueTabSessionTab<T> Tab {
			get; private set;
		}

		/// <summary>
		/// Session context
		/// </summary>
		public T Context {
			get; private set;
		}

		/// <summary>
		/// Used to inform subscribers about session status change.
		/// </summary>
		public new event Func<DialogueTabSession<T>, SessionInnerMessage, Task> OnStatusChange;

		public new event Func<DialogueTabSession<T>, SessionInnerMessage, Task> OnSessionEnd;

		public new event Func<DialogueTabSession<T>, Task<bool>> OnRemove;

		public DialogueTabSession(DialogueTabSessionTab<T> tab, InteractiveInteraction start, T context)
			: base(new ComponentDialogueSession(tab.Client, new DialogueCommandState(start), start))
		{
			(Tab, Context) = (tab, context);
			base.OnStatusChange += StatusChange;
			base.OnSessionEnd += SessionEnd;
			base.OnRemove += Remove;
		}

		private async Task StatusChange(IDialogueSession x, SessionInnerMessage y)
		{
			if (OnStatusChange != null)
				await OnStatusChange(x as DialogueTabSession<T>, y);
		}

		private async Task SessionEnd(IDialogueSession x, SessionInnerMessage y)
		{
			if (OnSessionEnd != null)
				await OnSessionEnd(x as DialogueTabSession<T>, y);
		}

		private async Task<bool> Remove(IDialogueSession x)
		{
			return OnRemove != null && await OnRemove(x as DialogueTabSession<T>);
		}
	}
}