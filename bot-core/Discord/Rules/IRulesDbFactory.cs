using Manito.Discord.Database;

using System.Threading.Tasks;

namespace Manito.Discord.Rules
{
	public interface IRulesDbFactory : IMyDbFactory
	{
		IRulesDb CreateMyDbContext();

		Task<IRulesDb> CreateMyDbContextAsync();
	}
}