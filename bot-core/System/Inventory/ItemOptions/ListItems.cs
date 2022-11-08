using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.Client;

using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.Discord.Inventory
{
	public class ListItems : IDialogueNet
	{
		private InventorySession _session;
		private const int max = 25;
		private const int rows = 5;
		private int _page;
		private int _pageCount;
		private int _leftsp;
		private DiscordInteractionResponseBuilder _msg;
		private DiscordComponent[] _btnDef;
		private DiscordButtonComponent _firstList;
		private DiscordButtonComponent _prevList;
		private DiscordButtonComponent _exBtn;
		private DiscordButtonComponent _nextList;
		private DiscordButtonComponent _latterList;
		private string _navPrefix;
		private string _itmPrefix;
		private string _othPrefix;

		public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;

		public ListItems(InventorySession session, int startPage)
		{
			_session = session;
			_page = startPage;
			_navPrefix = "nav";
			_itmPrefix = "item";
		}

		private async Task<NextNetworkInstruction> Initiallize(NetworkInstructionArgument args)
		{
			_firstList = new DiscordButtonComponent(ButtonStyle.Success, $"{_navPrefix}_firstBtn", "Перв. стр",
			 false, new DiscordComponentEmoji(DiscordEmoji.FromUnicode("◀️")));
			_prevList = new DiscordButtonComponent(ButtonStyle.Success, $"{_navPrefix}_prevBtn", "Пред. стр",
			 false, new DiscordComponentEmoji(DiscordEmoji.FromUnicode("⬅️")));
			_exBtn = new DiscordButtonComponent(ButtonStyle.Danger, $"{_navPrefix}_exitBtn", "Закрыть",
			 false, new DiscordComponentEmoji(DiscordEmoji.FromUnicode("✖️")));
			_nextList = new DiscordButtonComponent(ButtonStyle.Success, $"{_navPrefix}_nextBtn", "След. стр",
			 false, new DiscordComponentEmoji(DiscordEmoji.FromUnicode("➡️")));
			_latterList = new DiscordButtonComponent(ButtonStyle.Success, $"{_navPrefix}_latterBtn", "Посл. стр",
			 false, new DiscordComponentEmoji(DiscordEmoji.FromUnicode("▶️")));
			_btnDef = new DiscordComponent[] { _firstList, _prevList, _exBtn, _nextList, _latterList };

			return new(PrintActions, NextNetworkActions.Continue);
		}

		private async Task<NextNetworkInstruction> PrintActions(NetworkInstructionArgument args)
		{
			_leftsp = max - _btnDef.Length;

			_msg = new DiscordInteractionResponseBuilder();
			var emb = new DiscordEmbedBuilder();

			var inv = _session.PInventory.GetInventoryItems();
			var invCount = inv.Count();

			var pages = inv.Select((x, y) => (x, y)).Chunk(_leftsp);

			_pageCount = Math.Max(pages.Count() - 1, 0);

			_page = Math.Clamp(_page, 0, _pageCount);
			IEnumerable<(ItemDescriptor, int)> itms = Enumerable.Empty<(ItemDescriptor, int)>();
			var btns = Enumerable.Empty<DiscordComponent>();
			if (invCount > 0)
			{
				itms = pages.ElementAtOrDefault(_page)?
					.Select(x => ((ItemDescriptor)new UniversalDescriptor(x.x), x.y));

				foreach (var (x, y) in itms)
					emb.AddField($"Предмет №{y}", x.GetEmbedDescriptor(), true);

				btns = itms.Select(x => new DiscordButtonComponent(ButtonStyle.Primary,
				   $"{_itmPrefix}_{x.Item1.Item.Id}", x.Item1.GetButtonDescriptor()));
			}
			else
			{
				emb.WithDescription("У вас пусто в инвентаре :(");
			}

			btns = btns.Concat(Enumerable.Range(1, _leftsp - btns.Count()).Select(x =>
			 new DiscordButtonComponent(ButtonStyle.Secondary, $"{x}dummy",
			 " ** ** ** ** ** ** ", true)));

			emb.WithFooter($"Всего предметов: {invCount}\nСтраница {_page + 1} из {_pageCount + 1}");

			if (_page == 0)
			{
				_firstList.Disable();
				_prevList.Disable();
			}
			else
			{
				_firstList.Enable();
				_prevList.Enable();
			}

			if (_page == _pageCount)
			{
				_nextList.Disable();
				_latterList.Disable();
			}
			else
			{
				_nextList.Enable();
				_latterList.Enable();
			}

			foreach (var btnsr in btns.Concat(_btnDef).Chunk(rows))
				_msg.AddComponents(btnsr);

			await _session.Respond(_msg.AddEmbed(emb));

			return new(WaitForResponse, NextNetworkActions.Continue, itms);
		}

		private async Task<NextNetworkInstruction> WaitForResponse(NetworkInstructionArgument args)
		{
			var inv = (IEnumerable<(ItemDescriptor, int)>)args.Payload;

			var resp = await _session.GetInteraction(_msg.Components);

			if (resp.ButtonId.StartsWith(_itmPrefix))
				return new(UseItem, NextNetworkActions.Continue, (resp, inv));

			if (resp.ButtonId.StartsWith(_navPrefix))
				return new(MoveToPage, NextNetworkActions.Continue, resp);

			return new();
		}

		private async Task<NextNetworkInstruction> UseItem(NetworkInstructionArgument args)
		{
			var resp = ((InteractiveInteraction, IEnumerable<(ItemDescriptor, int)>))args.Payload;

			var item = resp.Item2.FirstOrDefault(x => resp
				.Item1.ButtonId.Contains($"{x.Item1.Item.Id}"));

			var net = item.Item1.GetItemNet(_session,
				new NextNetworkInstruction(PrintActions, NextNetworkActions.Continue));

			return net.GetStartingInstruction();
		}

		private async Task<NextNetworkInstruction> MoveToPage(NetworkInstructionArgument args)
		{
			var resp = (InteractiveInteraction)args.Payload;

			if (resp.CompareButton(_firstList))
				_page = 0;

			if (resp.CompareButton(_prevList))
				_page--;

			if (resp.CompareButton(_nextList))
				_page++;

			if (resp.CompareButton(_latterList))
				_page = _pageCount;

			if (!resp.CompareButton(_exBtn))
				return new(PrintActions, NextNetworkActions.Continue);

			await _session.QuitSession();
			return new(null, NextNetworkActions.Stop);
		}

		public NextNetworkInstruction GetStartingInstruction(object payload) => GetStartingInstruction();

		public NextNetworkInstruction GetStartingInstruction() => new(Initiallize, NextNetworkActions.Continue);
	}
}