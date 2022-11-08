using Manito.Discord.Cleaning;
using Manito.Discord.Client;
using Manito.Discord.Config;
using Manito.Discord.Database;
using Manito.Discord.Inventory;
using Manito.Discord.PermanentMessage;
using Manito.Discord.Shop;
using Manito.System.Economy;
using Manito.System.Logging;

using Microsoft.Extensions.DependencyInjection;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Manito.Discord
{
	public class MyDomain
	{
		private IServiceCollection _serviceCollection;
		private MyDbFactory _db;
		public MyDbFactory DbFactory => _db;
		private ApplicationCommands _appCommands;
		private MyClientBundle _myDiscordClient;
		private EventFilters _filters;
		private ExecThread _executionThread;
		private ServerEconomy _economy;
		private IInventorySystem _inventory;
		private ShopService _shopService;
		private RootConfig _rootConfig;
		private MessageController _msgWallCtr;
		private LoggingCenter _logging;
		private MessageRemover _msgRemover;
		public IServiceCollection ServiceCollection => _serviceCollection;
		public MyClientBundle MyDiscordClient => _myDiscordClient;
		public ExecThread ExecutionThread => _executionThread;
		public ServerEconomy Economy => _economy;
		public IInventorySystem Inventory => _inventory;
		public EventFilters Filters => _filters;
		public ShopService ShopService => _shopService;
		public RootConfig RootConfig => _rootConfig;
		public MessageController MsgWallCtr => _msgWallCtr;
		public LoggingCenter Logging => _logging;
		public MessageRemover MessageRemover => _msgRemover;

		public static async Task<MyDomain> Create()
		{
			var service = new MyDomain();

			await service.Initialize();

			return service;
		}

		private MyDomain()
		{
		}

		private async Task Initialize()
		{
			_rootConfig = RootConfig.GetConfig();
			_db = new(this, _rootConfig.DatabaseCfg);
			_executionThread = new(this);

			_inventory = new TestInventorySystem();
			_economy = new(this, _db);
			_myDiscordClient = new(this, _rootConfig);
			_msgWallCtr = new(this);
			_shopService = new(this);
			_appCommands = _myDiscordClient.AppCommands;
			_filters = new(this, _myDiscordClient.EventsBuffer);
			_logging = new(_myDiscordClient, _db);
			_msgRemover = new(this, _db);
			await _filters.Initialize();
		}

		public async Task StartBot()
		{
			await _appCommands.UpdateCommands();
			await _myDiscordClient.Start();
			await _filters.PostInitialize();
			await Task.WhenAll(GetTasks());
		}

		private IEnumerable<Task> GetTasks()
		{
			yield return _db.RunModule();
			yield return _myDiscordClient.StartLongTerm();
			yield return _executionThread.RunModule();
			yield return _executionThread.AddNew(new ExecThread.Job(_msgRemover.RunModule));
			yield return _executionThread.AddNew(new ExecThread.Job(_filters.RunModule));
			yield return _executionThread.AddNew(new ExecThread.Job(_economy.RunModule));
			yield return _executionThread.AddNew(new ExecThread.Job(_msgWallCtr.RunModule));
			yield return _executionThread.AddNew(new ExecThread.Job(_logging.RunModule));
		}
	}
}