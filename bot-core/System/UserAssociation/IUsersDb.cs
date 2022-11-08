using Manito.Discord.Database;

using Microsoft.EntityFrameworkCore;

namespace Manito.System.UserAssociaton
{
	public interface IUsersDb : IMyDatabase
	{
		DbSet<UserRecord> UserRecords {
			get;
		}
	}
}