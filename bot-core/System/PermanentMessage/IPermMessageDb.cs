using Manito.Discord.Database;

using Microsoft.EntityFrameworkCore;

namespace Manito.Discord.PermanentMessage
{
	public interface IPermMessageDb : IMyDatabase
	{
		DbSet<MessageWallTranslator> MessageWallTranslators {
			get;
		}

		DbSet<MessageWall> MessageWalls {
			get;
		}

		DbSet<MessageWallLine> MessageWallLines {
			get;
		}
	}
}