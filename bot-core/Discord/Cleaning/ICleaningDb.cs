using Manito.Discord.Database;

using Microsoft.EntityFrameworkCore;

using System.Threading.Tasks;

namespace Manito.Discord.Cleaning
{
	public interface ICleaningDb : IMyDatabase
	{
		DbSet<MessageToRemove> MsgsToRemove {
			get;
		}
	}

	public interface ICleaningDbFactory : IMyDbFactory
	{
		ICleaningDb CreateMyDbContext();

		Task<ICleaningDb> CreateMyDbContextAsync();
	}
}