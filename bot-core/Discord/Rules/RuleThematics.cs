using System;

namespace Manito.Discord.Rules
{
	[Flags]
	public enum RuleThematics
	{
		None = 0,
		Contesting = 1 << 0,
		Nesting = 1 << 1,
		Hunt = 1 << 2,
		Fight = 1 << 3,
		Stealing = 1 << 4,
		BugUsage = 1 << 5,
		Limits = 1 << 6,
		Requirements = 1 << 6,
		Aftermath = 1 << 7,
		Beforemath = 1 << 8,
		Process = 1 << 9
	}
}