using Manito.Discord.Cleaning;
using Manito.Discord.PermanentMessage;
using Manito.Discord.Rules;
using Manito.Discord.Shop;
using Manito.System.Economy;
using Manito.System.Logging;

using Microsoft.EntityFrameworkCore;

namespace Manito.Discord.Database
{
	public class DbContextImplementation : DbContext
	{
		public DbContextImplementation(DbContextOptions<DbContextImplementation> options) : base(options)
		{
		}

		public DbSet<ShopItem> ShopItems {
			get; set;
		}

		public DbSet<PlayerEconomyDeposit> PlayerEconomyDeposits {
			get; set;
		}

		public DbSet<MessageWallTranslator> MessageWallTranslators {
			get; set;
		}

		public DbSet<MessageWall> MessageWalls {
			get; set;
		}

		public DbSet<MessageWallLine> MessageWallLines {
			get; set;
		}

		public DbSet<LogLine> LogLines {
			get; set;
		}

		public DbSet<PlayerEconomyWork> PlayerWorks {
			get; set;
		}

		public DbSet<MessageToRemove> MessagesToRemove {
			get; set;
		}

		public DbSet<RulesPoint> Rules {
			get; set;
		}

		/*public DbSet<ItemBase> InventoryItems {
			get; set;
		}*/

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<MessageWallTranslator>().HasKey(x => x.ID);
			modelBuilder.Entity<MessageWallTranslator>().Property(x => x.ID).UseIdentityByDefaultColumn();

			modelBuilder.Entity<MessageWallTranslator>().Ignore(x => x.Translation);

			modelBuilder.Entity<MessageWall>().HasKey(x => x.ID);
			modelBuilder.Entity<MessageWall>().Property(x => x.ID).UseIdentityByDefaultColumn();

			modelBuilder.Entity<MessageWallLine>().HasKey(x => x.ID);
			modelBuilder.Entity<MessageWallLine>().Property(x => x.ID).UseIdentityByDefaultColumn();

			modelBuilder.Entity<PlayerEconomyDeposit>().HasKey(x => x.DiscordID);

			modelBuilder.Entity<PlayerEconomyWork>(x => x.HasKey(x => x.DiscordID));

			modelBuilder.Entity<MessageToRemove>(x => x.HasKey(x => x.MessageID));

			modelBuilder.Entity<RulesPoint>(x => x.HasKey(x => x.RuleId));
			modelBuilder.Entity<ShopItem>().HasNoKey();

			/*modelBuilder.Entity<ItemMisc>().Property(x => x.Properties).HasColumnType("jsonb").HasColumnName("Properties");
			modelBuilder.Entity<ItemEgg>().Property(x => x.Properties).HasColumnType("jsonb").HasColumnName("Properties");
			modelBuilder.Entity<ItemFood>().Property(x => x.Properties).HasColumnType("jsonb").HasColumnName("Properties");
			modelBuilder.Entity<ItemBase>().Property(x => x.Id).UseIdentityByDefaultColumn();
			modelBuilder.Entity<ItemBase>().HasKey(x => x.Id);*/

			modelBuilder.Entity<LogLine>(x => {
				x.HasKey(x => x.ID);
				x.Property(x => x.ID).UseIdentityByDefaultColumn();
				x.Property(b => b.Data).HasColumnType("jsonb");
			});
		}
	}
}