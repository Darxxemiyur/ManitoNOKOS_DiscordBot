using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatNew;
using Manito.Discord.ChatNew.Sessions;
using Manito.Discord.Database;

using Microsoft.EntityFrameworkCore;

using Name.Bayfaderix.Darxxemiyur.Common;
using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.Discord.Client
{
	public class BufferedIOBlock
	{
		private MyDomain _domain;

		public class Block
		{
			public Block(DiscordComponent button, IDialogueNet net)
			{
			}
		}

		public BufferedIOBlock(MyDomain domain)
		{
		}
	}

	/// <summary>
	/// Single net dialogue based Item selector.
	/// </summary>
	public class StandaloneInteractiveSelectMenu<TItem> : IDialogueNet
	{
		private readonly IDialogueSession _session;
		private IDialogueSession _controls;
		private const int max = 25;
		private const int rows = 5;

		private UniversalMessageBuilder _msg;
		private DiscordComponent[] _btnDef;
		private DiscordButtonComponent _firstList;
		private DiscordButtonComponent _prevList;
		private DiscordButtonComponent _exBtn;
		private DiscordButtonComponent _nextList;
		private DiscordButtonComponent _latterList;
		private readonly string _navPrefix;
		private readonly string _itmPrefix;
		private readonly string _othPrefix;
		public NodeResultHandler StepResultHandler => Common.DefaultNodeResultHandler;
		private readonly IPageReturner<TItem> _paginater;

		public StandaloneInteractiveSelectMenu(IDialogueSession session, IPageReturner<TItem> paginater)
		{
			_session = session;
			_paginater = paginater;
			_navPrefix = "nav";
			_itmPrefix = "item";
		}

		public async Task<IItemDescriptor<TItem>> EvaluateItem() =>
		 (IItemDescriptor<TItem>)await NetworkCommon.RunNetwork(this);

		private async Task<NextNetworkInstruction> Initiallize(NetworkInstructionArgument args)
		{
			_controls = await _session.PopNewLine();
			_session.OnSessionEnd += (x, y) => _controls.RemoveMessage();
			_session.OnSessionEnd += (x, y) => _controls.EndSession();
			_firstList = new DiscordButtonComponent(ButtonStyle.Success, $"{_navPrefix}_firstBtn",
			 "Перв. стр", false, new DiscordComponentEmoji(DiscordEmoji.FromUnicode("◀️")));
			_prevList = new DiscordButtonComponent(ButtonStyle.Success, $"{_navPrefix}_prevBtn",
			 "Пред. стр", false, new DiscordComponentEmoji(DiscordEmoji.FromUnicode("⬅️")));
			_exBtn = new DiscordButtonComponent(ButtonStyle.Danger, $"{_navPrefix}_exitBtn",
			 "Закрыть", false, new DiscordComponentEmoji(DiscordEmoji.FromUnicode("✖️")));
			_nextList = new DiscordButtonComponent(ButtonStyle.Success, $"{_navPrefix}_nextBtn",
			 "След. стр", false, new DiscordComponentEmoji(DiscordEmoji.FromUnicode("➡️")));
			_latterList = new DiscordButtonComponent(ButtonStyle.Success, $"{_navPrefix}_latterBtn",
			 "Посл. стр", false, new DiscordComponentEmoji(DiscordEmoji.FromUnicode("▶️")));
			_btnDef = new DiscordComponent[] { _firstList, _prevList, _exBtn, _nextList, _latterList };

			return new(PrintActions, NextNetworkActions.Continue);
		}

		private async Task<NextNetworkInstruction> PrintActions(NetworkInstructionArgument args)
		{
			_msg = new UniversalMessageBuilder();
			var emb = new DiscordEmbedBuilder();

			_paginater.PerPage = max;

			var invCount = await _paginater.GetTotal();

			var page = 0;
			var pages = await _paginater.GetPages();
			await _paginater.SetPage(page = await _paginater.GetPage());

			IEnumerable<IItemDescriptor<TItem>> itms = (await _paginater.GetListablePage())
				.Select((x, y) => x.SetLocalDisplayedOrder(y));

			var btns = itms?.Where(x => x.HasButton()).Select(x =>
				new DiscordButtonComponent(ButtonStyle.Primary,
				$"{_itmPrefix}_{x.GetButtonId()}", x.GetButtonName()))
				?? Enumerable.Empty<DiscordButtonComponent>();

			btns = btns.Concat(Enumerable.Range(1, max - btns.Count()).Select(x =>
				new DiscordButtonComponent(ButtonStyle.Secondary, $"{x}dummy",
				" ** ** ** ** ** ** ", true)));

			emb.WithFooter($"Всего предметов: {invCount}\nСтраница {page} из {pages}");

			if (page <= 1)
			{
				_firstList.Disable();
				_prevList.Disable();
			}
			else
			{
				_firstList.Enable();
				_prevList.Enable();
			}

			if (page >= pages)
			{
				_nextList.Disable();
				_latterList.Disable();
			}
			else
			{
				_nextList.Enable();
				_latterList.Enable();
			}

			foreach (var btnsr in btns.Chunk(rows))
				_msg.AddComponents(btnsr);

			await _session.SendMessage(_msg.AddEmbed(emb));
			await _controls.SendMessage(new UniversalMessageBuilder().AddComponents(_btnDef).AddContent("** **"));
			return new(WaitForResponse, itms);
		}

		private async Task<NextNetworkInstruction> WaitForResponse(NetworkInstructionArgument args)
		{
			var inv = (IEnumerable<IItemDescriptor<TItem>>)args.Payload;

			//var resp = await _puller.GetComponentInteraction(_msg.Components);
			var (diag, resp) = await new MultiSessionAwaiter(_session, _controls).GetComponentInteraction();
			await diag.DoLaterReply();

			if (resp.ButtonId.StartsWith(_navPrefix))
				return new(ReturnNoItem, NextNetworkActions.Continue, resp);

			await _controls.RemoveMessage();
			if (resp.ButtonId.StartsWith(_itmPrefix))
				return new(ReturnItem, NextNetworkActions.Continue, (resp, inv));

			return new(ExitThing);
		}

		private async Task<NextNetworkInstruction> ExitThing(NetworkInstructionArgument arg)
		{
			await _controls.EndSession();
			await _controls.RemoveMessage();
			return new();
		}

		private async Task<NextNetworkInstruction> ReturnItem(NetworkInstructionArgument args)
		{
			var resp = ((InteractiveInteraction, IEnumerable<IItemDescriptor<TItem>>))args.Payload;

			await _session.DoLaterReply();

			var item = resp.Item2.FirstOrDefault(x => resp.Item1.ButtonId.Contains($"_{x.GetButtonId()}"));

			return new(item);
		}

		private async Task<NextNetworkInstruction> ReturnNoItem(NetworkInstructionArgument args)
		{
			var resp = (InteractiveInteraction)args.Payload;

			if (resp.CompareButton(_exBtn))
				return new(ExitThing);

			await _session.DoLaterReply();

			var page = await _paginater.GetPage();
			if (resp.CompareButton(_firstList))
				page = 0;

			if (resp.CompareButton(_prevList))
				page--;

			if (resp.CompareButton(_nextList))
				page++;

			if (resp.CompareButton(_latterList))
				page = await _paginater.GetPages();

			await _paginater.SetPage(page);

			return new(PrintActions);
		}

		public NextNetworkInstruction GetStartingInstruction(object payload) => GetStartingInstruction();

		public NextNetworkInstruction GetStartingInstruction() => new(Initiallize);
	}

	public interface IItemDescriptor<TItem>
	{
		string GetButtonName();

		string GetButtonId();

		bool HasButton();

		bool HasField();

		string GetFieldName();

		string GetFieldBody();

		IItemDescriptor<TItem> SetGlobalDisplayedOrder(int i);

		IItemDescriptor<TItem> SetLocalDisplayedOrder(int i);

		int GetGlobalDisplayOrder();

		int GetLocalDisplayOrder();

		TItem GetCarriedItem();
	}

	public interface IPageReturner<TItem>
	{
		Task<IList<IItemDescriptor<TItem>>> GetListablePage();

		/// <summary>
		/// Displayed on pages
		/// </summary>
		/// <value></value>
		int PerPage {
			get; set;
		}

		Task PassMessage(object obj, string msg) => throw new NotImplementedException();

		/// <summary>
		/// Total on current page amount
		/// </summary>
		/// <value></value>
		Task<int> GetOnPage();

		/// <summary>
		/// Total item amount
		/// </summary>
		/// <value></value>
		Task<int> GetTotal();

		/// <summary>
		/// Total pages amount
		/// </summary>
		/// <value></value>
		Task<int> GetPages();

		/// <summary>
		/// Current page. Set, or Get
		/// </summary>
		/// <value></value>
		Task<int> GetPage();

		Task SetPage(int page);
	}

	public class EnumerablePageReturner<TItem> : IPageReturner<TItem>
	{
		public async Task<IList<IItemDescriptor<TItem>>> GetListablePage() => _list
			.Skip((_page - 1) * PerPage).Take(PerPage).Select(x => _convert(x)).ToList();

		public Int32 PerPage { get; set; } = 25;

		public async Task<Int32> GetOnPage() => (await GetListablePage()).Count;

		public async Task<int> GetTotal() => _list.Count;

		public async Task<int> GetPages() => Math.Max((int)Math.Ceiling((float)await GetTotal() / PerPage), 1);

		private int _page;

		public async Task<int> GetPage() => Math.Max(_page, 1);

		public async Task SetPage(int page)
		{
			_page = Math.Clamp(page, 1, await GetPages());
		}

		private List<TItem> _list;
		private Func<TItem, IItemDescriptor<TItem>> _convert;

		public EnumerablePageReturner(List<TItem> list, Func<TItem, IItemDescriptor<TItem>> converter)
		{
			_list = list;
			_convert = converter;
		}
	}

	public delegate Task<TSource> SourceGetter<TFactory, TSource>(TFactory factory) where TFactory : IMyDbFactory where TSource : IMyDatabase;

	public delegate Task<IQueryable<TItem>> ItemGetter<TFactory, TItem>(TFactory factory);

	public delegate Task<IItemDescriptor<TItem>> DescriptorGenerator<TItem>(TItem item);

	public class CompactQuerryReturner<TFactory, TSource, TItem> : IPageReturner<TItem> where TFactory : IMyDbFactory where TSource : IMyDatabase
	{
		private readonly TFactory _factory;
		private readonly SourceGetter<TFactory, TSource> _sourceGetter;
		private readonly ItemGetter<TSource, TItem> _itemGetter;
		private readonly DescriptorGenerator<TItem> _descriptorGenerator;
		private readonly AsyncLocker _lock = new();

		public CompactQuerryReturner(TFactory factory, SourceGetter<TFactory, TSource> sourceGetter, ItemGetter<TSource, TItem> itemGetter, DescriptorGenerator<TItem> descriptorGenerator)
		{
			_factory = factory;
			_sourceGetter = sourceGetter;
			_itemGetter = itemGetter;
			_descriptorGenerator = descriptorGenerator;
		}

		public int PerPage {
			get;
			set;
		}

		private int _page = 1;

		public async Task<IList<IItemDescriptor<TItem>>> GetListablePage()
		{
			await using var _ = await _lock.BlockAsyncLock();
			await using var db = await _sourceGetter(_factory);
			var itemsQ = await _itemGetter(db);
			var items = itemsQ.Skip((_page - 1) * PerPage).Take(PerPage).ToArray();
			var buffer = new List<IItemDescriptor<TItem>>(items.Length);
			foreach (var item in items)
				buffer.Add(await _descriptorGenerator(item));
			return buffer;
		}

		public async Task<int> GetOnPage() => (await GetListablePage()).Count;

		public async Task<int> GetPage()
		{
			await using var _ = await _lock.BlockAsyncLock();
			return _page;
		}

		public async Task<int> GetPages()
		{
			var total = await GetTotal();
			return Math.Max(1, (int)Math.Ceiling((double)total / PerPage));
		}

		public async Task<int> GetTotal()
		{
			await using var _ = await _lock.BlockAsyncLock();
			await using var db = await _sourceGetter(_factory);
			var itemsQ = await _itemGetter(db);
			return await itemsQ.CountAsync();
		}

		public async Task SetPage(int page)
		{
			var pagen = Math.Clamp(page, 1, await GetPages());
			await using var _ = await _lock.BlockAsyncLock();
			_page = pagen;
		}
	}
}