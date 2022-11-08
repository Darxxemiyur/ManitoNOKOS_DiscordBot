using Manito.Discord.GuideBook;

using Name.Bayfaderix.Darxxemiyur.Common;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Manito.Discord.Rules
{
	public class RulesPoint : IGlobalLinkable
	{
		public long RuleId {
			get; set;
		}

		public string RuleName {
			get; set;
		}

		public int Order {
			get; set;
		}

		public string[] ImageDecoration {
			get; set;
		}

		public ulong ChannelId {
			get; set;
		}

		public ulong MessageId {
			get; set;
		}

		public RuleThematics Thematics {
			get;
		}

		public List<RulesPoint> SubPoints {
			get; set;
		}

		public RulesPoint Parent {
			get; set;
		}

		public RuleThematics RulesThematics => Thematics | SubPoints.Select(x => x.RulesThematics).Aggregate((x, y) => x | y);
		public string DistrictName => "Правила";
		public string LocalName => RuleName;
		public string Content => RulesThematics.ToString() + "Attachements:" + string.Join(" ", ImageDecoration);

		public override int GetHashCode() => HashCode.Combine(SubPoints.GetSequenceHashCode(), MessageId, ChannelId, Thematics, ImageDecoration.GetSequenceHashCode(), Order, LocalName, HashCode.Combine(DistrictName, RuleId, RuleName, ((IGlobalLinkable)this).WholeObject));

		public int GetHash() => GetHashCode();
	}
}