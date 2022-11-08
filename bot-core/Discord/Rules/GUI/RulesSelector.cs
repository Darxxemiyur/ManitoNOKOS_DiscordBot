using Manito.Discord.Chat.DialogueNet;
using Manito.Discord.ChatNew;
using Manito.Discord.Client;

using Name.Bayfaderix.Darxxemiyur.Common;
using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;
using System.Threading.Tasks;

namespace Manito.Discord.Rules.GUI
{
	public class RulesSelector : IDialogueNet
	{
		public NodeResultHandler StepResultHandler {
			get;
		}

		private RulesPoint _point;
		private DialogueTabSession<IRulesDbFactory> _session;

		public RulesSelector(DialogueTabSession<IRulesDbFactory> session, RulesPoint point) => (_session, _point) = (session, point);

		private async Task<NextNetworkInstruction> Main(NetworkInstructionArgument arg)
		{
			var selector = new StandaloneInteractiveSelectMenu<RulesPoint>(_session, new CompactQuerryReturner<IRulesDbFactory, IRulesDb, RulesPoint>(_session.Context, x => x.CreateMyDbContextAsync(), async x => x.Rules, async x => new Descriptor(x)));

			var item = await selector.EvaluateItem();

			return new();
		}

		public NextNetworkInstruction GetStartingInstruction() => new(Main);

		public NextNetworkInstruction GetStartingInstruction(object payload) => throw new NotImplementedException();

		private class Descriptor : IItemDescriptor<RulesPoint>
		{
			private readonly RulesPoint _wall;

			public Descriptor(RulesPoint wall) => _wall = wall;

			private int _lid;
			private int _gid;

			public string GetButtonId() => $"MessageWall{_lid}_{_wall.RuleId}";

			private string GetMyThing(string str) => $"Правило {str} ID:{_wall.RuleId}";

			public string GetButtonName() => GetMyThing(_wall.Content.DoStartAtMax(80 - GetMyThing("").Length));

			public RulesPoint GetCarriedItem() => _wall;

			public string GetFieldBody() => throw new NotImplementedException();

			public string GetFieldName() => throw new NotImplementedException();

			public int GetGlobalDisplayOrder() => _gid;

			public int GetLocalDisplayOrder() => _lid;

			public bool HasButton() => true;

			public bool HasField() => false;

			public IItemDescriptor<RulesPoint> SetGlobalDisplayedOrder(int i)
			{
				_gid = i;
				return this;
			}

			public IItemDescriptor<RulesPoint> SetLocalDisplayedOrder(int i)
			{
				_lid = i;
				return this;
			}
		}
	}
}