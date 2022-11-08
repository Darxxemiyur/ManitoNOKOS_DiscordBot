using Manito.Discord.Database;

using System.Threading.Tasks;

namespace Manito.System.Economy
{
	public interface IEconomyDbFactory : IMyDbFactory
	{
		IEconomyDb CreateEconomyDbContext();

		Task<IEconomyDb> CreateEconomyDbContextAsync();
	}
}