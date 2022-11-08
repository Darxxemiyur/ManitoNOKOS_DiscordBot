using DisCatSharp;
using DisCatSharp.Common.Utilities;
using DisCatSharp.EventArgs;

using Name.Bayfaderix.Darxxemiyur.Common;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Manito.Discord.Client
{
	/// <summary>
	/// Event inliner. Intercepts events that are being listened for from existing "sessions" Pushes
	/// non-interesting events further to the EventFilter's buffer
	/// </summary>
	public class EventInline
	{
		public readonly PerEventInline<MessageCreateEventArgs> MessageBuffer;

		public readonly PerEventInline<InteractionCreateEventArgs> InteractionBuffer;

		public readonly PerEventInline<ComponentInteractionCreateEventArgs> CompInteractBuffer;

		public readonly PerEventInline<MessageReactionAddEventArgs> ReactAddBuffer;
		public readonly PerEventInline<ContextMenuInteractionCreateEventArgs> ContInteractBuffer;
		public readonly PerEventInline<MessageDeleteEventArgs> MessageDeleteBuffer;
		public readonly PerEventInline<ReadyEventArgs> OnReady;

		private EventBuffer _sourceEventBuffer;

		public EventInline(EventBuffer sourceEventBuffer)
		{
			_sourceEventBuffer = sourceEventBuffer;
			MessageBuffer = new(sourceEventBuffer.Message);
			InteractionBuffer = new(sourceEventBuffer.Interact);
			CompInteractBuffer = new(sourceEventBuffer.CompInteract);
			ReactAddBuffer = new(sourceEventBuffer.MsgAddReact);
			ContInteractBuffer = new(sourceEventBuffer.ContInteract);
			MessageDeleteBuffer = new(sourceEventBuffer.MsgDeletion);
			OnReady = new(sourceEventBuffer.OnReady);
		}

		public Task Run() => _sourceEventBuffer.EventLoops();
	}

	/// <summary>
	/// Single event pipe inliner. Forwards events to respective listeners, if not catched, forwards
	/// to base listeners.
	/// </summary>
	/// <typeparam name="TEvent"></typeparam>
	public class PerEventInline<TEvent> : IEventChainPasser<TEvent> where TEvent : DiscordEventArgs
	{
		public static int DefaultOrder = 10;
		private Dictionary<int, List<Predictator<TEvent>>> _predictators;
		private AsyncLocker _lock;
		public string TypeName => GetType().FullName;

		public event AsyncEventHandler<DiscordClient, TEvent> OnToNextLink;

		public PerEventInline(IEventChainPasser<TEvent> buf)
		{
			_lock = new();
			_predictators = new();
			buf.OnToNextLink += Check;
		}

		public async Task Add(int order, Predictator<TEvent> predictator)
		{
			await using var _ = await _lock.BlockAsyncLock();
			if (!_predictators.ContainsKey(order))
				_predictators[order] = new();

			_predictators[order].Add(predictator);
		}

		public Task Add(Predictator<TEvent> predictator) => Add(DefaultOrder, predictator);

		private async Task<IEnumerable<(int, Predictator<TEvent>)>> CheckEOL(IEnumerable<(int, Predictator<TEvent>)> input)
		{
			var rrr = Enumerable.Empty<(int, Predictator<TEvent>)>();
			foreach (var ch in input)
			{
				if (await ch.Item2.ShouldDelete())
					rrr = rrr.Append((ch.Item1, ch.Item2));
			}
			return rrr;
		}

		private async Task<IEnumerable<Predictator<TEvent>>> RunEvent(DiscordClient client, TEvent args, IEnumerable<Predictator<TEvent>> input)
		{
			var rrr = Enumerable.Empty<Predictator<TEvent>>();
			var handled = false;
			foreach (var chk in input)
			{
				if (handled && !chk.RunIfHandled || !await chk.IsFitting(client, args))
					continue;
				handled = true;
				rrr = rrr.Append(chk);
				args.Handled = true;
			}
			return rrr;
		}

		public async Task<bool> Check(DiscordClient client, TEvent args)
		{
			await using var __ = await _lock.BlockAsyncLock();

			var itms = _predictators.SelectMany(x => x.Value.Select(y => (x.Key, y)));

			var itmsToDlt = await CheckEOL(itms);
			//Deletes and works!
			_ = itmsToDlt.Where(x => _predictators[x.Item1].Remove(x.Item2)
			 && _predictators[x.Item1].Count == 0 && _predictators.Remove(x.Item1)).ToArray();

			var toRun = await RunEvent(client, args, itms.Select(x => x.y));

			foreach (var itm in toRun)
				await itm.Handle(client, args);

			itmsToDlt = await CheckEOL(itms);
			//Deletes and works!
			_ = itmsToDlt.Where(x => _predictators[x.Item1].Remove(x.Item2)
			 && _predictators[x.Item1].Count == 0 && _predictators.Remove(x.Item1)).ToArray();

			if (!toRun.Any() && OnToNextLink != null)
				await OnToNextLink(client, args);

			return !toRun.Any();
		}
	}

	public abstract class Predictator<TEvent> where TEvent : DiscordEventArgs
	{
		// Maybe create an ID for predictator, which client can receive buffered events
		public abstract Task<bool> IsFitting(DiscordClient client, TEvent args);

		public abstract bool RunIfHandled {
			get;
		}

		public abstract Task<bool> IsREOL();

		public async Task<bool> ShouldDelete() => await IsREOL() || _token.IsCancellationRequested;

		protected readonly DiscordEventProxy<(Predictator<TEvent>, TEvent)> _eventProxy;
		private readonly CancellationToken _token;

		protected Predictator(CancellationToken token = default) => (_eventProxy, _token) = (new(), token);

		public Task Handle(DiscordClient client, TEvent args) => _eventProxy.Handle(client, (this, args));

		public virtual Task<(DiscordClient, TEvent)> GetEvent(TimeSpan timeout, CancellationToken token)
		{
			return GetEvent(x => Task.Delay(timeout, CancellationTokenSource.CreateLinkedTokenSource(_token, token).Token));
		}

		public virtual async Task<(DiscordClient, TEvent)> GetEvent(CancellationToken token)
		{
			var result = await _eventProxy.GetData(CancellationTokenSource.CreateLinkedTokenSource(_token, token).Token);

			return (result.Item1, result.Item2.Item2);
		}

		/// <summary>
		/// Get event in specific period of time
		/// </summary>
		/// <param name="timeout">The time period</param>
		/// <returns></returns>
		/// <exception cref="TimeoutException"></exception>
		public virtual async Task<(DiscordClient, TEvent)> GetEvent(TimeSpan timeout)
		{
			try
			{
				return await GetEvent(x => Task.Delay(timeout));
			}
			catch (TaskCanceledException e)
			{
				throw new TimeoutException($"Event awaiting for {timeout} has timed out!", e);
			}
		}

		/// <summary>
		/// Gets event
		/// </summary>
		/// <param name="cancelTask"></param>
		/// <returns></returns>
		/// <exception cref="TaskCanceledException"></exception>
		public async Task<(DiscordClient, TEvent)> GetEvent(
			Func<Task<(DiscordClient, (Predictator<TEvent>, TEvent))>, Task> genCancel)
		{
			var tokenSource = new CancellationTokenSource();
			var gettingData = _eventProxy.GetData(CancellationTokenSource.CreateLinkedTokenSource(_token, tokenSource.Token).Token);
			var cancelTask = genCancel(gettingData);
			var either = await Task.WhenAny(cancelTask, gettingData);

			if (either == cancelTask)
			{
				tokenSource.Cancel();
				throw new TaskCanceledException($"Event awaiting has been cancelled!");
			}

			var result = await gettingData;

			return (result.Item1, result.Item2.Item2);
		}

		public virtual async Task<(DiscordClient, TEvent)> GetEvent()
		{
			var result = await _eventProxy.GetData(_token);

			return (result.Item1, result.Item2.Item2);
		}
	}
}