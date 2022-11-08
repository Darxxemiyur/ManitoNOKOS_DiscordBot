using Manito.Discord.Cleaning;
using Manito.Discord.Client;
using Manito.Discord.Config;
using Manito.Discord.PermanentMessage;
using Manito.Discord.Rules;
using Manito.Discord.Shop;
using Manito.System.Economy;
using Manito.System.Logging;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using Name.Bayfaderix.Darxxemiyur.Common;

using System.Threading.Tasks;

namespace Manito.Discord.Database
{
	public class MyDbFactory : IShopDbFactory, ICleaningDbFactory, IEconomyDbFactory, IRulesDbFactory, IPermMessageDbFactory, ILoggingDBFactory, IMyDbFactory, IModule
	{
		private class DTDCF : IDbContextFactory<DbContextImplementation>, IDesignTimeDbContextFactory<DbContextImplementation>
		{
			private readonly DatabaseConfig _dbConfig;

			public DTDCF()
			{
			}

			public DTDCF(DatabaseConfig dbConfig) => _dbConfig = dbConfig;

			public DbContextImplementation CreateDbContext(string[] args) => CreateDbContext();

			public DbContextImplementation CreateDbContext()
			{
				var optionsBuilder = new DbContextOptionsBuilder<DbContextImplementation>();

				optionsBuilder.UseNpgsql(_dbConfig?.ConnectionString ?? "Data Source=blog.db");

				optionsBuilder.EnableDetailedErrors();
				optionsBuilder.EnableSensitiveDataLogging();

				return new(optionsBuilder.Options);
			}
		}

		public IDesignTimeDbContextFactory<DbContextImplementation> OriginalFactory {
			get; private set;
		}

		public MyDomain Domain {
			get;
		}

		private AsyncLocker _lock;

		public MyDbFactory(MyDomain domain, DatabaseConfig dbConfig)
		{
			Domain = domain;
			OriginalFactory = new DTDCF(dbConfig);
			_lock = new();
		}

		public MyDatabase CreateMyDbContext()
		{
			using var _ = _lock.BlockLock();
			var db = new MyDatabase();
			db.SetUpDatabase(this);
			return db;
		}

		public async Task<MyDatabase> CreateMyDbContextAsync()
		{
			await using var _ = await _lock.BlockAsyncLock();
			return await InternalCreateAsync();
		}

		private async Task<MyDatabase> InternalCreateAsync()
		{
			var db = new MyDatabase();
			await db.SetUpDatabaseAsync(this);
			return db;
		}

		IShopDb IShopDbFactory.CreateMyDbContext() => CreateMyDbContext();

		async Task<IShopDb> IShopDbFactory.CreateMyDbContextAsync() => await CreateMyDbContextAsync();

		IMyDatabase IMyDbFactory.CreateMyDbContext() => CreateMyDbContext();

		async Task<IMyDatabase> IMyDbFactory.CreateMyDbContextAsync() => await CreateMyDbContextAsync();

		IPermMessageDb IPermMessageDbFactory.CreateMyDbContext() => CreateMyDbContext();

		async Task<IPermMessageDb> IPermMessageDbFactory.CreateMyDbContextAsync() => await CreateMyDbContextAsync();

		IEconomyDb IEconomyDbFactory.CreateEconomyDbContext() => CreateMyDbContext();

		async Task<IEconomyDb> IEconomyDbFactory.CreateEconomyDbContextAsync() => await CreateMyDbContextAsync();

		ILoggingDB ILoggingDBFactory.CreateLoggingDBContext() => CreateMyDbContext();

		async Task<ILoggingDB> ILoggingDBFactory.CreateLoggingDBContextAsync() => await CreateMyDbContextAsync();

		public async Task RunModule()
		{
#if !DEBUG
			await using var _ = await _lock.BlockAsyncLock();
			await using var db = await InternalCreateAsync();

			await db.ImplementedContext.Database.MigrateAsync();
#endif
		}

		ICleaningDb ICleaningDbFactory.CreateMyDbContext() =>
			 CreateMyDbContext();

		async Task<ICleaningDb> ICleaningDbFactory.CreateMyDbContextAsync() =>
			 await CreateMyDbContextAsync();

		IRulesDb IRulesDbFactory.CreateMyDbContext() =>
			 CreateMyDbContext();

		async Task<IRulesDb> IRulesDbFactory.CreateMyDbContextAsync() =>
			 await CreateMyDbContextAsync();
	}
}