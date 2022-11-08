using Manito.Discord.Cleaning;
using Manito.Discord.PermanentMessage;
using Manito.Discord.Rules;
using Manito.Discord.Shop;
using Manito.System.Economy;
using Manito.System.Logging;

using Microsoft.EntityFrameworkCore;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Manito.Discord.Database
{
	public class MyDatabase : IShopDb, ICleaningDb, ILoggingDB, IPermMessageDb, IEconomyDb, IMyDatabase, IRulesDb
	{
		private bool disposedValue;

		public DbSet<ShopItem> ShopItems => ImplementedContext.ShopItems;
		public DbSet<PlayerEconomyDeposit> PlayerEconomies => ImplementedContext.PlayerEconomyDeposits;
		public DbSet<MessageWallTranslator> MessageWallTranslators => ImplementedContext.MessageWallTranslators;
		public DbSet<MessageWall> MessageWalls => ImplementedContext.MessageWalls;
		public DbSet<MessageWallLine> MessageWallLines => ImplementedContext.MessageWallLines;

		public DbContextImplementation ImplementedContext {
			get; private set;
		}

		public DbSet<LogLine> LogLines => ImplementedContext.LogLines;

		public DbSet<PlayerEconomyWork> PlayerWorks => ImplementedContext.PlayerWorks;

		public DbSet<MessageToRemove> MsgsToRemove => ImplementedContext.MessagesToRemove;

		public DbSet<RulesPoint> Rules => ImplementedContext.Rules;

		//public DbSet<ItemBase> InventoryItems => ImplementedContext.InventoryItems;

		public int SaveChanges() => ImplementedContext.SaveChanges();

		public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => ImplementedContext.SaveChangesAsync(cancellationToken);

		public void SetUpDatabase(IMyDbFactory factory) => ImplementedContext = factory.OriginalFactory.CreateDbContext(null);

		public Task SetUpDatabaseAsync(IMyDbFactory factory) =>
		 Task.Run(() => SetUpDatabase(factory));

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
					ImplementedContext.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged
		// resources ~MyDatabase() { // Do not change this code. Put cleanup code in 'Dispose(bool
		// disposing)' method Dispose(disposing: false); }

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		public ValueTask DisposeAsync()
		{
			GC.SuppressFinalize(this);
			return ImplementedContext.DisposeAsync();
		}
	}
}