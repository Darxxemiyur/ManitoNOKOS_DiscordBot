using DisCatSharp;
using DisCatSharp.Entities;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Manito.Discord.Client
{
	public class AppCommandArgsTools
	{
		private readonly DiscordInteraction _intArgs;
		private IEnumerable<string> _reqArgs;
		private IEnumerable<string> _optArgs;

		public AppCommandArgsTools(DiscordInteraction intArgs,
		 IEnumerable<string> reqArgs, IEnumerable<string> optArgs)
		{
			_intArgs = intArgs;
			_reqArgs = reqArgs;
			_optArgs = optArgs;
		}

		public AppCommandArgsTools(DiscordInteraction intArgs,
		 IEnumerable<string> reqArgs) : this(intArgs, reqArgs, Array.Empty<string>())
		{
		}

		public AppCommandArgsTools(DiscordInteraction intArgs) : this(intArgs, Array.Empty<string>())
		{
		}

		public AppCommandArgsTools(DiscordInteraction intArgs, IEnumerable<(bool, string)> args)
		: this(intArgs,
		 args.Where(x => x.Item1).Select(x => x.Item2),
		 args.Where(x => !x.Item1).Select(x => x.Item2))
		{
		}

		public string AddReqArg(string argName)
		{
			_reqArgs = _reqArgs.Contains(argName) ? _reqArgs : _reqArgs.Append(argName);
			return argName;
		}

		public string AddOptArg(string argName)
		{
			_optArgs = _optArgs.Contains(argName) ? _optArgs : _optArgs.Append(argName);
			return argName;
		}

		public string AddArg(bool required, string argName) => required ? AddReqArg(argName) : AddOptArg(argName);

		private bool Recur(DiscordInteractionDataOption option, string arg)
		{
			return option.Name == arg || Recur(option.Options, arg);
		}

		private bool Recur(IEnumerable<DiscordInteractionDataOption> options, string arg)
		{
			return options?.Any(x => Recur(x, arg)) ?? false;
		}

		private object GetArg(DiscordInteractionDataOption option, string arg)
		{
			return option.Name == arg ? option.Value : GetArg(option.Options, arg);
		}

		private object GetArg(IEnumerable<DiscordInteractionDataOption> options, string arg)
		{
			return options?.Select(x => GetArg(x, arg))?.FirstOrDefault(x => x != null);
		}

		public bool DoHaveReqArgs() => _reqArgs.All(x => Recur(_intArgs.Data.Options, x));

		public bool AnyOptArgs() => _optArgs.Any(x => Recur(_intArgs.Data.Options, x));

		private IEnumerable<(string, object)> GetArgPairs(IEnumerable<string> tgt) =>
		 tgt.Select(x => (x, GetArg(_intArgs.Data.Options, x))).Where(x => x.Item2 != null);

		public ArgListTools GetOptional() => new(GetArgPairs(_optArgs)
		 .ToDictionary(x => x.Item1, x => x.Item2));

		public ArgListTools GetReq() => new(GetArgPairs(_reqArgs)
		 .ToDictionary(x => x.Item1, x => x.Item2));

		private ArgListTools GetOp(bool r) => r ? GetReq() : GetOptional();

		public byte? GetByteArg(string arg, bool required = true)
		{
			return GetOp(required).GetByteArg(arg);
		}

		public short? GetShortArg(string arg, bool required = true)
		{
			return GetOp(required).GetShortArg(arg);
		}

		public int? GetIntArg(string arg, bool required = true)
		{
			return GetOp(required).GetIntArg(arg);
		}

		public long? GetLongArg(string arg, bool required = true)
		{
			return GetOp(required).GetLongArg(arg);
		}

		public string GetStringArg(string arg, bool required = true)
		{
			return GetOp(required).GetStringArg(arg);
		}

		public object GetArg(string arg, bool required = true)
		{
			return GetOp(required).GetArg(arg);
		}

		public T GetArg<T>(string arg, bool required = true)
		{
			return GetOp(required).GetArg<T>(arg);
		}
	}

	public class ArgListTools : IReadOnlyDictionary<string, object>
	{
		private readonly IReadOnlyDictionary<string, object> _list;

		public ArgListTools(IReadOnlyDictionary<string, object> list)
		{
			_list = list;
		}

		public byte? GetByteArg(string arg)
		{
			var higher = GetLongArg(arg);
			return higher.HasValue ? (byte?)Math.Clamp(higher.Value, byte.MinValue, byte.MaxValue) : null;
		}

		public short? GetShortArg(string arg)
		{
			var higher = GetLongArg(arg);
			return higher.HasValue ? (short?)Math.Clamp(higher.Value, short.MinValue, short.MaxValue) : null;
		}

		public int? GetIntArg(string arg)
		{
			var higher = GetLongArg(arg);
			return higher.HasValue ? (int?)Math.Clamp(higher.Value, int.MinValue, int.MaxValue) : null;
		}

		public long? GetLongArg(string arg)
		{
			return GetArg<long?>(arg);
		}

		public string GetStringArg(string arg)
		{
			return (string)GetArg(arg);
		}

		public T GetArg<T>(string arg)
		{
			return (T)(_list.ContainsKey(arg) ? _list[arg] : default(T));
		}

		public object GetArg(string arg) => GetArg<object>(arg);

		#region LIST IMPOSING

		public object this[string key] => _list[key];

		public IEnumerable<string> Keys => _list.Keys;

		public IEnumerable<object> Values => _list.Values;

		public int Count => _list.Count;

		public bool ContainsKey(string key) => _list.ContainsKey(key);

		public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
		{
			return _list.TryGetValue(key, out value);
		}

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _list.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_list).GetEnumerator();

		#endregion LIST IMPOSING
	}
}