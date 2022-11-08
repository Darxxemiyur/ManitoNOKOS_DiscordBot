using Manito.Discord.Database;

using System.Threading.Tasks;

namespace Manito.System.UserAssociaton
{
	public interface IUserDbFactory : IMyDbFactory
	{
		new IUsersDb CreateMyDbContext();

		new Task<IUsersDb> CreateMyDbContextAsync();
	}
}