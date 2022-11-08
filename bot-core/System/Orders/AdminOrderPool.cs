using Name.Bayfaderix.Darxxemiyur.Common;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Manito.Discord.Orders
{
	public class AdminOrderPool
	{
		public int AdminsOnline => _admins.Count;

		public bool AnyAdminOnline => AdminsOnline > 0;
		private readonly AsyncLocker _lock;
		private readonly FIFOPFIFOTCollection<Order> _pool;
		private readonly List<AdminOrderExec> _admins;

		public AdminOrderPool()
		{
			_lock = new();
			_admins = new();
			_pool = new();
		}

		public async Task StartAdministrating(AdminOrderExec admin)
		{
			await using var _ = await _lock.BlockAsyncLock();

			if (_admins.All(x => x != admin))
				_admins.Add(admin);
		}

		public async Task StopAdministrating(AdminOrderExec admin)
		{
			await using var _ = await _lock.BlockAsyncLock();

			_admins.RemoveAll(x => x == admin);

			while (!AnyAdminOnline && await _pool.AnyItems())
			{
				var order = await await _pool.GetItem();
				await order.CancelOrder("Последний администратор ушёл с поста. Средства возвращены.");
			}
		}

		public async Task<bool> IsAnyAdminOnline()
		{
			await using var _ = await _lock.BlockAsyncLock();
			return AnyAdminOnline;
		}

		public async Task<bool> PlaceOrder(Order order)
		{
			await using var _ = await _lock.BlockAsyncLock();
			if (order != null)
				if (AnyAdminOnline)
					await _pool.PlaceItem(order);
				else
					await order.CancelOrder("Администраторов в сети нет.");

			return AnyAdminOnline;
		}

		public async Task<Order> GetOrder(CancellationToken token = default)
		{
			var order = Task.FromResult<Order>(null);
			{
				await using var _ = await _lock.BlockAsyncLock();
				order = await _pool.GetItem(token);
			}
			return (await order).OrderCancelledTask.IsCompleted ? await GetOrder(token) : await order;
		}
	}
}