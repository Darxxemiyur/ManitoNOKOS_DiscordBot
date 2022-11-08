using Manito.Discord.Database;

using System.Threading.Tasks;

namespace Manito.System.Logging
{
	public interface ILoggingDBFactory : IMyDbFactory
	{
		ILoggingDB CreateLoggingDBContext();

		Task<ILoggingDB> CreateLoggingDBContextAsync();
	}
}