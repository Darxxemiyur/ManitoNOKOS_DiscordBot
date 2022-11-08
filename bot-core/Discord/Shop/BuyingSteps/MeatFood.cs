using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatNew;

using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System.Threading.Tasks;

namespace Manito.Discord.Shop
{
	public class BuyingStepsForMeatFood : IDialogueNet
	{
		private ShopItem _food;
		private DialogueTabSession<ShopContext> _session;
		private int _quantity;
		public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

		public NextNetworkInstruction GetStartingInstruction(object payload) => GetStartingInstruction();

		public NextNetworkInstruction GetStartingInstruction() => new(SelectQuantity);

		public BuyingStepsForMeatFood(DialogueTabSession<ShopContext> session, ShopItem food)
		{
			_session = session;
			_food = food;
		}

		private async Task<NextNetworkInstruction> SelectQuantity(NetworkInstructionArgument args)
		{
			var ms1 = $"Выберите количество {_food.Name}";
			var price = _food.Price;

			var qua = await Common.GetQuantity(new[] { -5, -2, 1, 2, 5 }, new[] { 50, 100, 500 }, _session, async (x, y) => (y > 0 && await _session.Context.Wallet.CanAfford((x + y) * price)) || (y < 0 && x > 100), async x => await _session.Context.Format.GetResponse(_session.Context.Format.BaseContent().WithDescription($"{ms1}\nВыбранное количество {x} ед за {x * price}.")), _quantity, 100);

			if (!qua.HasValue)
				return new();
			_quantity = qua.Value;

			return new(ExecuteTransaction);
		}

		private async Task<NextNetworkInstruction> ExecuteTransaction(NetworkInstructionArgument args)
		{
			var wallet = _session.Context.Wallet;
			var resp = _session.Context.Format;

			var cart = new ShopItem.InCart(_food, _quantity);

			if (!await wallet.CanAfford(cart.Price))
				return new NextNetworkInstruction(ForceChange);

			await wallet.Withdraw(cart.Price, $"Покупка {_food.Name} за {_food.Price} в кол-ве {_quantity} за {cart.Price}");

			var res = (bool)await NetworkCommon.RunNetwork(new FoodOrderAwait(_session, cart));

			return res ? new(SelectQuantity) : new();
		}

		private async Task<NextNetworkInstruction> ForceChange(NetworkInstructionArgument args)
		{
			var wallet = _session.Context.Wallet;
			var resp = _session.Context.Format;

			var price = _quantity * _food.Price;
			var ms1 = $"Вы не можете позволить {_quantity} ед. {_food.Name} за {price}.";
			var ms2 = $"Пожалуйста измените выбранное количество {_food.Name} и попробуйте снова.";
			var rsp = await _session.Context.Format.GetResponse(_session.Context.Format.BaseContent().WithDescription($"{ms1}\n{ms2}"));

			var cancel = new DiscordButtonComponent(ButtonStyle.Danger, "Cancel", "Отмена");
			var chnamt = new DiscordButtonComponent(ButtonStyle.Primary, "Back", "Изменить кол-во");
			rsp.AddComponents(cancel, chnamt);

			await _session.SendMessage(rsp);

			var argv = await _session.GetComponentInteraction();

			if (argv.CompareButton(chnamt))
				return new(SelectQuantity);

			return new(null);
		}
	}
}