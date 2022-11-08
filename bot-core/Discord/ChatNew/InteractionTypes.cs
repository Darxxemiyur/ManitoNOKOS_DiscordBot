using System;

namespace Manito.Discord.ChatNew
{
	/// <summary>
	/// Interaction type flags
	/// </summary>
	[Flags]
	public enum InteractionTypes
	{
		Component = 1 << 0,
		Message = 1 << 1,
		Reply = 1 << 2,
		Context = 1 << 3,
		Command = 1 << 4,
		Cancelled = 1 << 5,
	}
}