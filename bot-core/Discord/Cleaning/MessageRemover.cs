using DisCatSharp.Entities;

using Manito.Discord.Client;

using Microsoft.EntityFrameworkCore;

using Name.Bayfaderix.Darxxemiyur.Common;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.Discord.Cleaning
{
	public class MessageRemover : IModule
	{
		private AsyncLocker _lock = new();
		private MyDomain _domain;
		private ICleaningDbFactory _dbFactory;
		private MyTaskSource<int> LSI;

		public MessageRemover(MyDomain domain, ICleaningDbFactory dbFactory)
		{
			_domain = domain;
			_dbFactory = dbFactory;
			LSI = new();
		}

		public Task RemoveMessage(ulong channelId, ulong messageId, bool isOnNext = false) => RemoveMessage(channelId, messageId, DateTimeOffset.UtcNow, isOnNext);

		public Task RemoveMessage(DiscordMessage message, bool isOnNext = false) => RemoveMessage(message.ChannelId, message.Id, DateTimeOffset.UtcNow, isOnNext);

		public Task RemoveMessage(IEnumerable<DiscordMessage> messages) => RemoveMessage(messages.Select(x => (x, DateTimeOffset.UtcNow)));

		public Task RemoveMessage(IEnumerable<(DiscordMessage, DateTimeOffset)> messages) => RemoveMessage(messages.ToDictionary(x => x.Item1.ChannelId, x => (x.Item1.Id, x.Item2)));

		public Task RemoveMessage(IDictionary<ulong, ulong> messages) => RemoveMessage(messages.ToDictionary(x => x.Key, x => (x.Value, DateTimeOffset.UtcNow)));

		public Task RemoveMessage(DiscordMessage message, TimeSpan timeout) => RemoveMessage(message.ChannelId, message.Id, timeout);

		public Task RemoveMessage(IEnumerable<(DiscordMessage, TimeSpan)> messages) => RemoveMessage(messages.ToDictionary(x => x.Item1.ChannelId, x => (x.Item1.Id, x.Item2)));

		public Task RemoveMessage(IDictionary<ulong, (ulong, TimeSpan)> messages) => RemoveMessage(messages.ToDictionary(x => x.Key, x => (x.Value.Item1, DateTimeOffset.UtcNow + x.Value.Item2)));

		public Task RemoveMessage(ulong channelId, ulong messageId, TimeSpan timeout) => RemoveMessage(channelId, messageId, DateTimeOffset.UtcNow + timeout);

		public Task RemoveMessage(DiscordMessage message, DateTimeOffset time, bool isOnNext = false) => RemoveMessage(message.ChannelId, message.Id, time, isOnNext);

		public async Task RemoveMessage(ulong channelId, ulong messageId, DateTimeOffset time, bool isOnNext = false)
		{
			await using var _ = await _lock.BlockAsyncLock();
			await using var db = await _dbFactory.CreateMyDbContextAsync();
			await CreateOrUpdate(channelId, messageId, time, db, isOnNext);
			await db.SaveChangesAsync();
		}

		public async Task RemoveMessage(MessageToRemove msg, bool isOnNext)
		{
			await using var _ = await _lock.BlockAsyncLock();
			await using var db = await _dbFactory.CreateMyDbContextAsync();
			await CreateOrUpdate(msg, db, isOnNext);
			await db.SaveChangesAsync();
		}

		private Task CreateOrUpdate(ulong channelId, ulong messageId, DateTimeOffset time, ICleaningDb db, bool isOnNext = false) => CreateOrUpdate(new MessageToRemove(messageId, channelId, time, 0), db, isOnNext);

		private async Task CreateOrUpdate(MessageToRemove msg, ICleaningDb db, bool isOnNext) => await CreateOrUpdate(new MessageToRemove(msg) { LastStartId = await LSI.MyTask + (isOnNext ? 1 : 0) }, db);

		private async Task CreateOrUpdate(MessageToRemove msg, ICleaningDb db)
		{
			if (await db.MsgsToRemove.AnyAsync(x => x.MessageID == msg.MessageID))
			{
				await foreach (var dmsg in db.MsgsToRemove.Where(x => x.MessageID == msg.MessageID).AsAsyncEnumerable())
				{
					dmsg.Expiration = msg.Expiration;
					dmsg.LastStartId = msg.LastStartId;
					db.MsgsToRemove.Update(dmsg);
				}
			}
			else
			{
				await db.MsgsToRemove.AddAsync(new MessageToRemove(msg));
			}
		}

		public async Task RemoveMessage(IDictionary<ulong, (ulong, DateTimeOffset)> messages)
		{
			await using var _ = await _lock.BlockAsyncLock();
			await using var db = await _dbFactory.CreateMyDbContextAsync();
			foreach (var msg in messages)
				await CreateOrUpdate(msg.Key, msg.Value.Item1, msg.Value.Item2, db);

			await db.MsgsToRemove.AddRangeAsync();
			await db.SaveChangesAsync();
		}

		public async Task RunModule()
		{
			while (true)
			{
				var delayStart = DateTimeOffset.UtcNow;
				var span = TimeSpan.FromMilliseconds(1250);
				try
				{
					await using var _ = await _lock.BlockAsyncLock();
					await using var db = await _dbFactory.CreateMyDbContextAsync();

					if (!LSI.MyTask.IsCompleted)
					{
						var sorted = await db.MsgsToRemove.OrderByDescending(x => x.LastStartId).Take(1).FirstOrDefaultAsync();

						await LSI.TrySetResultAsync(sorted?.LastStartId ?? 0);
					}

					var res = await LSI.MyTask;

					var msgs = await db.MsgsToRemove.Where(x => x.LastStartId <= res && x.Expiration <= DateTimeOffset.UtcNow).ToListAsync();
					var attemptAgain = new List<MessageToRemove>(msgs.Count);
					var toClear = new List<MessageToRemove>(msgs.Count);

					foreach (var msg in msgs)
					{
						if (await AttemptToRemove(msg))
						{
							toClear.Add(msg);
						}
						else
						{
							msg.Expiration = delayStart + TimeSpan.FromMinutes(2 + (Random.Shared.NextDouble() * 28));
							msg.TimesFailed += 1;
							attemptAgain.Add(msg);
						}
					}

					db.MsgsToRemove.RemoveRange(toClear);
					db.MsgsToRemove.UpdateRange(attemptAgain);

					await db.SaveChangesAsync();
				}
				catch (Exception e)
				{
					await _domain.Logging.WriteErrorClassedLog(GetType().Name, e, false);
				}
				await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(1, (delayStart + span - DateTimeOffset.UtcNow).TotalMilliseconds)));
			}
		}

		private async Task<bool> AttemptToRemove(MessageToRemove msgt)
		{
			try
			{
				var client = _domain.MyDiscordClient.Client;

				var channel = await client.GetChannelAsync(msgt.ChannelID);

				var message = await channel.GetMessageAsync(msgt.MessageID, true);

				await message.DeleteAsync();
				return true;
			}
			catch { }
			return msgt.TimesFailed > 20;
		}
	}
}