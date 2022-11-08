using DisCatSharp.Entities;

using Manito.Discord;
using Manito.Discord.Client;

using Name.Bayfaderix.Darxxemiyur.Common;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.System.Economy
{
	public class PlayerWallet
	{
		private readonly ServerEconomy _economy;
		public ServerEconomy Economy => _economy;
		private readonly ulong _userId;
		public ulong UserId => _userId;
		public ulong CurrencyEmojiId => _economy.CurrencyEmojiId;
		public string CurrencyEmoji => _economy.CurrencyEmoji;

		public PlayerWallet(ServerEconomy economy, ulong userId)
		{
			_economy = economy;
			_userId = userId;
		}

		public Task<long> GetScales() => _economy.GetScales(_userId);

		public Task<long> TransferFunds(ulong to, long amount, string msg = null) =>
		 _economy.TransferFunds(_userId, to, amount, msg);

		public Task<long> Withdraw(long amount, string msg = null) => _economy.Withdraw(_userId, amount, msg);

		public Task<bool> CanAfford(long amount) => _economy.CanAfford(_userId, amount);

		public Task<long> Deposit(long amount, string msg = null) => _economy.Deposit(_userId, amount, msg);
	}

	public class ServerEconomy : IModule
	{
#if DEBUG
		private ulong _emojiId => 997272231384207470;
#else
		private ulong _emojiId => 997279571655282718;
#endif
		private string _emoji => $"<:{_emojiId}:{_emojiId}>";
		public ulong CurrencyEmojiId => _emojiId;
		public string CurrencyEmoji => _emoji;
		private IEconomyDbFactory _dbFactory;
		private EconomyLogging _logger;
		private AsyncLocker _lock;

		public MyDomain Service {
			get;
		}

		public Task RunModule() => _logger.RunModule();

		public ServerEconomy(MyDomain service, IEconomyDbFactory factory)
		{
			_dbFactory = factory;
			_lock = new();
			_logger = new(Service = service);
		}

		public async Task<long> GetScales(ulong whose)
		{
			await using var _ = await _lock.BlockAsyncLock();
			return await GetScalesUnlocked(whose);
		}

		private async Task<bool> Ensure(ulong id)
		{
			await using var db = await _dbFactory.CreateEconomyDbContextAsync();
			return await Ensure(db, id);
		}

		private async Task<bool> Ensure(IEconomyDb db, ulong id)
		{
			if (db.PlayerEconomies.Any(x => x.DiscordID == id))
				return db.PlayerEconomies.First(x => x.DiscordID == id) != null;

			db.PlayerEconomies.Add(new PlayerEconomyDeposit(id));
			await db.SaveChangesAsync();

			return await Ensure(id);
		}

		public PlayerWallet GetPlayerWallet(ulong id) => new(this, id);

		public PlayerWallet GetPlayerWallet(DiscordUser user) => GetPlayerWallet(user.Id);

		private Task ReportTransaction(string msg) => _logger.ReportTransaction($"Транзакция: {msg}");

		public async Task<bool> CanAfford(ulong who, long amount)
		{
			await using var _ = await _lock.BlockAsyncLock();
			return await CanAffordUnlocked(who, amount);
		}

		public async Task<long> TransferFunds(ulong fromId, ulong toId, long amount, string msg = null)
		{
			await using var _ = await _lock.BlockAsyncLock();
			return await TransferFundsUnlocked(fromId, toId, amount, msg);
		}

		public async Task<long> Withdraw(ulong from, long amount, string msg = null)
		{
			await using var _ = await _lock.BlockAsyncLock();
			return await WithdrawUnlocked(from, amount, msg);
		}

		public async Task<long> Deposit(ulong to, long amount, string msg = null)
		{
			await using var _ = await _lock.BlockAsyncLock();
			return await DepositUnlocked(to, amount, msg);
		}

		public async Task<long> TransferFundsUnlocked(ulong fromId, ulong toId, long amount, string msg = null)
		{
			await using var db = await _dbFactory.CreateEconomyDbContextAsync();
			await Ensure(db, fromId);
			await Ensure(db, toId);

			var from = db.PlayerEconomies.First(x => x.DiscordID == fromId);
			var to = db.PlayerEconomies.First(x => x.DiscordID == toId);

			amount = DoWithdraw(from, amount);
			var depamount = DoDeposit(to, amount);

			if (amount - depamount > 0)
				DoDeposit(from, amount - depamount);

			await db.SaveChangesAsync();

			await ReportTransaction($"Перевод игроку <@{toId}> от <@{fromId}> на сумму {depamount} {_emoji}\n{msg}");
			return amount;
		}

		/// <summary>
		/// Must be used only in lockable conditions!
		/// </summary>
		public async Task<long> WithdrawUnlocked(ulong from, long amount, string msg = null)
		{
			await using var db = await _dbFactory.CreateEconomyDbContextAsync();
			await Ensure(db, from);

			var dep = db.PlayerEconomies.First(x => x.DiscordID == from);

			amount = DoWithdraw(dep, amount);
			await db.SaveChangesAsync();

			await ReportTransaction($"Снятие {amount} {_emoji} у <@{from}>\n{msg}");
			return amount;
		}

		/// <summary>
		/// Must be used only in lockable conditions!
		/// </summary>
		public async Task<bool> CanAffordUnlocked(ulong who, long amount)
		{
			await using var db = await _dbFactory.CreateEconomyDbContextAsync();
			await Ensure(db, who);
			var dep = db.PlayerEconomies.First(x => x.DiscordID == who);

			return dep.ScalesCurr >= amount;
		}

		/// <summary>
		/// Must be used only in lockable conditions!
		/// </summary>
		public async Task<long> DepositUnlocked(ulong toId, long amount, string msg = null)
		{
			await using var db = await _dbFactory.CreateEconomyDbContextAsync();
			await Ensure(db, toId);

			var to = db.PlayerEconomies.First(x => x.DiscordID == toId);
			amount = DoDeposit(to, amount);
			await db.SaveChangesAsync();

			await ReportTransaction($"Зачисление {amount} {_emoji} у <@{toId}>\n{msg}");
			return amount;
		}

		/// <summary>
		/// Must be used only in lockable conditions!
		/// </summary>
		public async Task<long> GetScalesUnlocked(ulong whose)
		{
			await using var db = await _dbFactory.CreateEconomyDbContextAsync();
			await Ensure(db, whose);
			var dep = db.PlayerEconomies.First(x => x.DiscordID == whose);

			return dep.ScalesCurr;
		}

		private long DoDeposit(PlayerEconomyDeposit to, long amount)
		{
			amount = Math.Clamp(amount, 0, long.MaxValue - to.ScalesCurr);
			to.ScalesCurr += amount;
			return amount;
		}

		private long DoWithdraw(PlayerEconomyDeposit from, long amount)
		{
			amount = Math.Clamp(amount, 0, from.ScalesCurr);
			from.ScalesCurr -= amount;

			return amount;
		}
	}
}