using Manito.Discord.Database;

using Microsoft.EntityFrameworkCore;

namespace Manito.System.Logging
{
	public interface ILoggingDB : IMyDatabase
	{
		DbSet<LogLine> LogLines {
			get;
		}
	}
}