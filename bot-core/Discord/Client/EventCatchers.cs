using DisCatSharp;
using DisCatSharp.EventArgs;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Manito.Discord.Client
{
	public class SingleEventCatcher<TEvent> : Predictator<TEvent> where TEvent : DiscordEventArgs
	{
		private Func<TEvent, bool> _predictator;
		private bool _runIfHandled;
		private bool _hasRan;

		public SingleEventCatcher(Func<TEvent, bool> predictator, bool runIfHandled = false, CancellationToken token = default) : base(token)
		{
			_runIfHandled = runIfHandled;
			_predictator = predictator;
		}

		public override Task<bool> IsFitting(DiscordClient client, TEvent args)
		{
			var res = _predictator(args);

			var bran = _hasRan;
			_hasRan = _hasRan || res;

			return Task.FromResult((!bran || _runIfHandled) && res);
		}

		public override bool RunIfHandled => _runIfHandled;

		public override Task<bool> IsREOL() => Task.FromResult(_hasRan);
	}
}