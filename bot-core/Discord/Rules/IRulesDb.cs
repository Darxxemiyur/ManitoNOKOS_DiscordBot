using Manito.Discord.Database;

using Microsoft.EntityFrameworkCore;

namespace Manito.Discord.Rules
{
	public interface IRulesDb : IMyDatabase
	{
		DbSet<RulesPoint> Rules {
			get;
		}
	}
}