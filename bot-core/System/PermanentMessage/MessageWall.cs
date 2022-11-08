using DisCatSharp.Entities;

using System.Collections.Generic;
using System.Linq;

namespace Manito.Discord.PermanentMessage
{
	/// <summary>
	/// SetOf Messages
	/// </summary>
	public class MessageWall
	{
		public long ID {
			get; set;
		}

		public string WallName {
			get; set;
		}

		public List<MessageWallLine> Msgs {
			get; set;
		}

		public MessageWall() => (Msgs, WallName) = (new(), "");

		public MessageWall(string name) => (Msgs, WallName) = (new(), name);

		public void SetName(string name) => WallName = name;

		public void AddMessage(MessageWallLine msg)
		{
			Msgs.Add(msg);
			Compact();
		}

		public void Compact() => Msgs = Msgs.Where(x => x.IsNull()).ToList();

		public IEnumerable<DiscordEmbedBuilder> GetEmbeds() => Msgs
			.Select(x => new DiscordEmbedBuilder().WithDescription(x));
	}
}