using Manito.System.Economy;

namespace Manito.Discord.Shop
{
	public class ShopContext
	{
		public ShopCashRegister CashRegister {
			get; private set;
		}

		public PlayerWallet Wallet {
			get; private set;
		}

		//private PlayerInventory _inventory;
		//public PlayerInventory Inventory => _inventory;
		public ShopService ShopService {
			get; private set;
		}

		public ShopResponseFormat Format {
			get; private set;
		}

		public ulong CustomerId {
			get; private set;
		}

		public ShopContext(ulong customerId, PlayerWallet wallet, ShopCashRegister cashRegister, ShopService shopService)
		{
			CustomerId = customerId;
			CashRegister = cashRegister;
			Wallet = wallet;
			Format = new(CashRegister, Wallet);
			ShopService = shopService;
		}
	}
}