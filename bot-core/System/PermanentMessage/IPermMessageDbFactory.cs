using Manito.Discord.Database;

using System.Threading.Tasks;

namespace Manito.Discord.PermanentMessage
{
	public interface IPermMessageDbFactory : IMyDbFactory
	{
		new IPermMessageDb CreateMyDbContext();

		new Task<IPermMessageDb> CreateMyDbContextAsync();
	}
}