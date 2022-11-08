using DisCatSharp.Entities;

using Manito.Discord.Client;

namespace Manito.Discord.ChatNew
{
	/// <summary>
	/// Has its fields non null according to Type's flags
	/// </summary>
	public class GeneralInteraction
	{
		public InteractionTypes Type {
			get; private set;
		}

		public InteractiveInteraction InteractiveInteraction {
			get; private set;
		}

		public DiscordMessage Message {
			get; private set;
		}

		public GeneralInteraction(InteractionTypes type,
			InteractiveInteraction interactiveInteraction = null, DiscordMessage message = null)
			=> (Type, InteractiveInteraction, Message) = (type, interactiveInteraction, message);
	}
}