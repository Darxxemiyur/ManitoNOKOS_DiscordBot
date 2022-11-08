using DisCatSharp.Entities;

using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatAbstract;
using Manito.Discord.ChatNew;
using Manito.Discord.Client;

using Name.Bayfaderix.Darxxemiyur.Common;

using System.Linq;
using System.Threading.Tasks;

namespace Manito.Discord.Shop
{
	public class ShopService
	{
		private MyDomain _service;
		private MyClientBundle _client;
		private ShopCashRegister _cashRegister;
		private DialogueNetSessionTab<ShopContext> _shopTab;
		private AsyncLocker _lock;

		public ShopService(MyDomain service)
		{
			_lock = new();
			_service = service;
			_client = service.MyDiscordClient;
			_shopTab = new(service);
			_cashRegister = new(service);
		}

		public async Task<DialogueTabSession<ShopContext>> StartSession(DiscordUser customer, DiscordInteraction intr)
		{
			await using var _ = await _lock.BlockAsyncLock();

			DialogueTabSession<ShopContext> session = null;

			if (_shopTab.Sessions.All(x => x.Context.CustomerId != customer.Id))
				session = await _shopTab.CreateSession(new(intr), new(customer.Id,
				_service.Economy.GetPlayerWallet(customer.Id), _cashRegister, this),
				(x) => Task.FromResult((IDialogueNet)new ShopDialogue(x)));

			return session;
		}

		public DiscordEmbedBuilder Default(DiscordEmbedBuilder bld = null) =>
			_cashRegister.Default(bld);
	}
}