using DisCatSharp;
using DisCatSharp.Common.Utilities;
using DisCatSharp.EventArgs;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Manito.Discord.Client
{
	public class EventBuffer
	{
		public readonly SingleEventBuffer<MessageCreateEventArgs> Message;
		public readonly SingleEventBuffer<InteractionCreateEventArgs> Interact;
		public readonly SingleEventBuffer<ComponentInteractionCreateEventArgs> CompInteract;
		public readonly SingleEventBuffer<ContextMenuInteractionCreateEventArgs> ContInteract;
		public readonly SingleEventBuffer<MessageReactionAddEventArgs> MsgAddReact;
		public readonly SingleEventBuffer<MessageDeleteEventArgs> MsgDeletion;
		public readonly SingleEventBuffer<ReadyEventArgs> OnReady;

		public EventBuffer(DiscordClient client)
		{
			Message = new(x => client.MessageCreated += x, x => client.MessageCreated -= x);
			Interact = new(x => client.InteractionCreated += x, x => client.InteractionCreated -= x);
			CompInteract = new(x => client.ComponentInteractionCreated += x, x => client.ComponentInteractionCreated -= x);
			MsgAddReact = new(x => client.MessageReactionAdded += x, x => client.MessageReactionAdded -= x);
			ContInteract = new(x => client.ContextMenuInteractionCreated += x, x => client.ContextMenuInteractionCreated -= x);
			MsgDeletion = new(x => client.MessageDeleted += x, x => client.MessageDeleted -= x);
			OnReady = new(x => client.Ready += x, x => client.Ready -= x);
		}

		public EventBuffer(EventInline client)
		{
			Message = new(client.MessageBuffer);
			Interact = new(client.InteractionBuffer);
			CompInteract = new(client.CompInteractBuffer);
			MsgAddReact = new(client.ReactAddBuffer);
			ContInteract = new(client.ContInteractBuffer);
			MsgDeletion = new(client.MessageDeleteBuffer);
			OnReady = new(client.OnReady);
		}

		private IEnumerable<Task> GetLoops()
		{
			yield return Message.Loop();
			yield return Interact.Loop();
			yield return CompInteract.Loop();
			yield return MsgAddReact.Loop();
			yield return ContInteract.Loop();
			yield return MsgDeletion.Loop();
			yield return OnReady.Loop();
		}

		public Task EventLoops() => Task.WhenAll(GetLoops());
	}

	public class SingleEventBuffer<TEvent> : IEventChainPasser<TEvent> where TEvent : DiscordEventArgs
	{
		private DiscordEventProxy<TEvent> _eventBuffer;
		private Action<AsyncEventHandler<DiscordClient, TEvent>> _unlinker;

		public event AsyncEventHandler<DiscordClient, TEvent> OnToNextLink;

		private void CreateEventBuffer()
		{
			_eventBuffer = new();
		}

		public SingleEventBuffer(Action<AsyncEventHandler<DiscordClient, TEvent>> linker,
		 Action<AsyncEventHandler<DiscordClient, TEvent>> unlinker)
		{
			CreateEventBuffer();
			linker(_eventBuffer.Handle);
			_unlinker = unlinker;
		}

		public SingleEventBuffer(IEventChainPasser<TEvent> linker)
		{
			CreateEventBuffer();
			linker.OnToNextLink += _eventBuffer.Handle;
		}

		~SingleEventBuffer()
		{
			_unlinker(_eventBuffer.Handle);
		}

		public async Task Loop()
		{
			while (true)
			{
				var data = await _eventBuffer.GetData();
				if (OnToNextLink != null)
					await OnToNextLink(data.Item1, data.Item2);
			}
		}
	}
}