using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using DisCatSharp.Net;

using Manito.Discord.Client;

using System;
using System.Threading.Tasks;

namespace Manito.Discord.ChatNew
{
	[Flags]
	public enum SessionKinds
	{
		OnGuildChannel = 1 << 0,
		OnDMChannel = 1 << 1,
		IsWebhook = 1 << 2,
		IsEphemeral = 1 << 3,
	}

	public interface ISessionInteractionIdentifier
	{
		Task<bool> DoesEventBelongToUs(ComponentInteractionCreateEventArgs discordEvent);

		Task<bool> DoesEventBelongToUs(DiscordMessage discordEvent);

		Task<bool> DoesEventBelongToUs(DiscordEventArgs discordEvent);
	}

	/// <summary>
	/// Contract that allows for identification, classification and holding various properties that
	/// prove to be useful in working with dialogue sessions.
	/// </summary>
	public interface ISessionState
	{
		/// <summary>
		/// Checks whether an interaction belongs to Dialogue session.
		/// </summary>
		/// <param name="interaction">The interaction being checked</param>
		/// <returns>true if it does, false if it doesn't</returns>
		bool DoesBelongToUs(InteractiveInteraction interaction);

		/// <summary>
		/// Describes how much it wants the interaction;
		/// </summary>
		/// <param name="interaction">The interaction to be checked</param>
		/// <returns>Want value</returns>
		int HowBadWants(InteractiveInteraction interaction);

		int HowBadIfWants(InteractiveInteraction interaction) => DoesBelongToUs(interaction) ? HowBadWants(interaction) : -1;

		/// <summary>
		/// Checks whether an interaction belongs to Dialogue session.
		/// </summary>
		/// <param name="interaction">The interaction being checked</param>
		/// <returns>true if it does, false if it doesn't</returns>
		bool DoesBelongToUs(DiscordMessage interaction);

		/// <summary>
		/// Describes how much it wants the interaction;
		/// </summary>
		/// <param name="interaction">The interaction to be checked</param>
		/// <returns>Want value</returns>
		int HowBadWants(DiscordMessage interaction);

		int HowBadIfWants(DiscordMessage interaction) => DoesBelongToUs(interaction) ? HowBadWants(interaction) : -1;

		/// <summary>
		/// Either only or the first user's id
		/// </summary>
		ulong? UserId {
			get;
		}

		/// <summary>
		/// All users' ids
		/// </summary>
		ulong[] UserIds {
			get;
		}

		/// <summary>
		/// Channel id
		/// </summary>
		ulong ChannelId {
			get;
		}

		/// <summary>
		/// Id of the message, if it's not ephemeral
		/// </summary>
		ulong? MessageId {
			get;
		}

		/// <summary>
		/// Kind of Dialogue
		/// </summary>
		SessionKinds Kind {
			get;
		}

		/// <summary>
		/// Quick way to access client.
		/// </summary>
		MyClientBundle Bundle {
			get;
		}

		/// <summary>
		/// Client used in session.
		/// </summary>
		DiscordApiClient UsedClient {
			get;
		}
	}
}