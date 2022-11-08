using Manito.Discord.Inventory;
using Manito.Discord.Orders;
using Manito.Discord.PermanentMessage;
using Manito.Discord.Rules;
using Manito.Discord.Shop;
using Manito.Discord.Welcommer;
using Manito.System.Economy;
using Manito.System.UserAssociaton;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Manito.Discord.Client
{
	/// <summary>
	/// Holding class for event filters.
	/// </summary>
	public class EventFilters : IModule
	{
		private MyDomain _service;
		private EventBuffer _eventBuffer;
		private ShopFilter _shopFilter;
		private InventoryFilter _inventoryFilter;
		private EconomyFilter _economyFilter;
		private DebugFilter _debugFilter;
		private MsgWallFilter _msgWallFilter;
		private WelcomerFilter _welcomer;
		private AdminOrdersFilter _adminOrder;
		private RulesFilter _rulesFilter;
		private UserAssociationFilter _associationFilter;
		public AdminOrdersFilter AdminOrder => _adminOrder;
		public WelcomerFilter Welcomer => _welcomer;
		public EventBuffer MyEventBuffer => _eventBuffer;
		public ShopFilter Shop => _shopFilter;
		public InventoryFilter InventoryFilter => _inventoryFilter;
		public EconomyFilter Economy => _economyFilter;
		public DebugFilter Debug => _debugFilter;
		public UserAssociationFilter AssociationFilter => _associationFilter;

		public EventFilters(MyDomain service, EventBuffer eventBuffer)
		{
			_service = service;
			_eventBuffer = eventBuffer;
		}

		public async Task Initialize()
		{
			_msgWallFilter = new MsgWallFilter(_service, _eventBuffer);
			_economyFilter = new EconomyFilter(_service, _eventBuffer);
			_inventoryFilter = new InventoryFilter(_service, _eventBuffer);
			_welcomer = new WelcomerFilter(_service.MyDiscordClient);
			_shopFilter = new ShopFilter(_service, _eventBuffer);
			_debugFilter = new DebugFilter(_service, _eventBuffer);
			_adminOrder = new(_service, _eventBuffer);
			_associationFilter = new(_service, _eventBuffer);
			_rulesFilter = new(_service, _eventBuffer);
		}

		public async Task PostInitialize()
		{
		}

		private IEnumerable<Task> GetRuns()
		{
			yield return _inventoryFilter.RunModule();
			yield return _shopFilter.RunModule();
			yield return _economyFilter.RunModule();
			yield return _debugFilter.RunModule();
			yield return _msgWallFilter.RunModule();
			yield return _welcomer.RunModule();
			yield return _adminOrder.RunModule();
			yield return _associationFilter.RunModule();
			yield return _rulesFilter.RunModule();
		}

		public Task RunModule() => Task.WhenAll(GetRuns());
	}
}