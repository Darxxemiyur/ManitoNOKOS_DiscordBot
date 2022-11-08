using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
	/// <summary>
	/// Nested Interlinked Information Placement Model
	/// </summary>
	[TestClass]
	public class NIIPM
	{
		[TestMethod("Test Adding to pure")]
		public async Task TATPu()
		{

		}
		[TestMethod("Test Adding to populated")]
		public async Task TATPo()
		{

		}
	}
	public interface IRuleItem
	{
		string Content {
			get; set;
		}
		int Number {
			get; set;
		}
		RuleItemDisplay Display {
			get; set;
		}
		RuleItemCategory Category {
			get; set;
		}
	}
	public enum RuleItemCategory
	{
		StandaloneRule,
		BridgeRule,
		RootFolder
	}
	public enum RuleItemDisplay
	{
		NotationUp,
		NotationRight,
		NotationDown,
		NotationNumber,
	}
	public class RRuleItem : IRuleItem
	{
		public string Content {
			get;
			set;
		}
		public int Number {
			get;
			set;
		}
		public RuleItemDisplay Display {
			get;
			set;
		}
		public RuleItemCategory Category {
			get;
			set;
		}
		public RRuleItem()
		{
			Display = RuleItemDisplay.NotationNumber;
			Category = RuleItemCategory.RootFolder;
		}
	}
	public class BRuleItem : IRuleItem
	{
		public string Content {
			get;
			set;
		}
		public int Number {
			get;
			set;
		}
		public RuleItemDisplay Display {
			get;
			set;
		}
		public RuleItemCategory Category {
			get;
			set;
		}
		public BRuleItem()
		{
			Display = RuleItemDisplay.NotationNumber;
			Category = RuleItemCategory.StandaloneRule;
		}
	}
	public class SRuleItem : IRuleItem
	{
		public string Content {
			get;
			set;
		}
		public int Number {
			get;
			set;
		}
		public RuleItemDisplay Display {
			get;
			set;
		}
		public RuleItemCategory Category {
			get;
			set;
		}
		public SRuleItem()
		{
			Display = RuleItemDisplay.NotationNumber;
			Category = RuleItemCategory.StandaloneRule;
		}
	}
}
