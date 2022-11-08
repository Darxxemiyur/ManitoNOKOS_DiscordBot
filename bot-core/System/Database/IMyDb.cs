using System;
using System.Threading;
using System.Threading.Tasks;

namespace Manito.Discord.Database
{
	/// <summary>
	/// Base interface for all derivative db interfaces
	/// </summary>
	public interface IMyDatabase : IDisposable, IAsyncDisposable
	{
		DbContextImplementation ImplementedContext {
			get;
		}

		/// <summary>
		/// Used to setup inner Db.
		/// </summary>
		/// <param name="factory">The factory to use</param>
		/// <returns></returns>
		void SetUpDatabase(IMyDbFactory factory);

		Task SetUpDatabaseAsync(IMyDbFactory factory);

		int SaveChanges();

		Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
	}
}