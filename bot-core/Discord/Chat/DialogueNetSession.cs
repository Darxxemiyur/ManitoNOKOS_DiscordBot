using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord.Client;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Manito.Discord.Chat.DialogueNet
{
	public abstract class DialogueNetSession
	{
		protected DiscordUser _user;
		protected MyClientBundle _client;
		protected InteractiveInteraction _iargs;
		protected ulong? _gdId;
		protected ulong? _chId;
		protected ulong? _msId;
		private bool _irtt;
		public DiscordUser User => _user;
		public MyClientBundle Client => _client;
		public DiscordInteraction Args => IArgs.Interaction;
		public InteractiveInteraction IArgs => _iargs;
		public ulong? GdId => _gdId;
		public ulong? ChId => _chId;
		public ulong? MsId => _msId;

		public bool IsEphemeral {
			get;
		}

		/// <summary>
		/// Create Dialogue network Session from Interactive args, bot client and sessioned user.
		/// </summary>
		/// <param name="iargs"></param>
		/// <param name="client"></param>
		/// <param name="user"></param>
		/// <param name="isEphemeral"></param>
		public DialogueNetSession(InteractiveInteraction iargs, MyClientBundle client,
		 DiscordUser user, bool isEphemeral = false)
		{
			_iargs = iargs;
			_client = client;
			_user = user;
			_gdId = null;
			_chId = null;
			_msId = null;
			_cancellation = new();
			IsEphemeral = isEphemeral;
		}

		private InteractionResponseType IRT => !_irtt && (_irtt = true)
			? InteractionResponseType.ChannelMessageWithSource
			: InteractionResponseType.UpdateMessage;

		/// <summary>
		/// Respond to an interaction.
		/// </summary>
		/// <param name="bld"></param>
		/// <returns></returns>
		public Task Respond(DiscordInteractionResponseBuilder bld = default)
		{
			return Respond(IRT, bld?.AsEphemeral(IsEphemeral));
		}

		/// <summary>
		/// Fires Respond and then fires GetInteraction against components placed in the fired
		/// message body.
		/// </summary>
		/// <param name="bld"></param>
		/// <returns></returns>
		public Task<InteractiveInteraction> RespondAndWait(DiscordInteractionResponseBuilder bld = default)
		{
			return RespondAndWait(IRT, bld?.AsEphemeral(IsEphemeral));
		}

		/// <summary>
		/// Fires Respond and then fires GetInteraction against components placed in the fired
		/// message body.
		/// </summary>
		/// <param name="bld"></param>
		/// <returns></returns>
		public async Task<InteractiveInteraction> RespondAndWait(InteractionResponseType rsptp,
		 DiscordInteractionResponseBuilder bld = default)
		{
			await Respond(rsptp, bld);
			return await GetInteraction(bld.Components);
		}

		/// <summary>
		/// Responds to an interaction with given response type and optional response body.
		/// </summary>
		/// <param name="rsptp"></param>
		/// <param name="bld"></param>
		/// <returns></returns>
		public async Task Respond(InteractionResponseType rsptp,
		 DiscordInteractionResponseBuilder bld = default)
		{
			await Args.CreateResponseAsync(rsptp, bld?.AsEphemeral(IsEphemeral));
			if (!IsEphemeral && _chId == null)
				_chId = (await Args.GetOriginalResponseAsync()).ChannelId;
			if (!IsEphemeral && _msId == null)
				_msId = (await Args.GetOriginalResponseAsync()).Id;
		}

		public async Task<DiscordMessage> GetSessionMessage()
		{
			return (await _client.ActivityTools.WaitForMessage((x) => x.Author.Id == Args.User.Id
				&& x.Message.ChannelId == _chId)).Message;
		}

		private Func<DialogueNetSession, Task> _controller;

		public void ConnectManager(Func<DialogueNetSession, Task> manager) => _controller = manager;

		/// <summary>
		/// Handles the exception thrown within the session;
		/// </summary>
		/// <returns></returns>
		public virtual async Task SessionExceptionHandle(Exception e)
		{
			_cancellation.Cancel();
			await _controller(this);
			try
			{
				await SendExceptionMessage(e);
			}
			catch (Exception err)
			{
				e = new AggregateException(e, err).Flatten();
			}
			throw new AggregateException(e).Flatten();
		}

		protected async Task SendExceptionMessage(Exception e)
		{
			await (await GetSessionBotMessage()).ModifyAsync(x => x
				.WithContent("Сессия завершена из-за ошибки."));
		}

		private CancellationTokenSource _cancellation;

		private async Task<DiscordMessage> GetSessionBotMessage()
		{
			var chnl = await _client.Client.GetChannelAsync(_chId.Value);
			return await chnl.GetMessageAsync(_msId.Value);
		}

		public virtual async Task QuitSession()
		{
			if (_chId != null && _msId != null && !IsEphemeral)
				await (await GetSessionBotMessage()).DeleteAsync();

			await _controller(this);
			_irtt = false;
		}

		public Task<InteractiveInteraction> GetInteraction(
		 IEnumerable<DiscordActionRowComponent> components) =>
		 GetInteraction(x => x.AnyComponents(components));

		public Task<InteractiveInteraction> GetInteraction(
		 params DiscordComponent[] components) =>
		 GetInteraction(x => x.AnyComponents(components));

		public async Task<InteractiveInteraction> GetInteraction(
		 Func<InteractiveInteraction, bool> checker)
		{
			var theEvent = await _client.ActivityTools.WaitForComponentInteraction(
				x => x.User.Id == Args.User.Id && x.Message.ChannelId == _chId
			&& (IsEphemeral || x.Message.Id == _msId) && checker(new(x)), _cancellation.Token);

			return _iargs = new(theEvent);
		}

		public Task<InteractiveInteraction> GetInteraction() => GetInteraction(_ => true);
	}
}