using DisCatSharp.Entities;
using DisCatSharp.EventArgs;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Manito.Discord.Client
{
	public class InteractiveInteraction
	{
		public DiscordInteraction Interaction {
			get;
		}

		public DiscordMessage Message {
			get;
		}

		private DiscordInteractionData Data => Interaction.Data;
		public IEnumerable<DiscordComponent> Components => Message?.Components?.SelectMany(x => x.Components);

		public static implicit operator InteractiveInteraction(ComponentInteractionCreateEventArgs x) => new(x);

		/// <summary>
		/// Interactive interaction from command
		/// </summary>
		/// <param name="interaction"></param>
		public InteractiveInteraction(DiscordInteraction interaction) => Interaction = interaction;

		/// <summary>
		/// Interactive interaction from interaction
		/// </summary>
		/// <param name="interaction"></param>
		public InteractiveInteraction(ComponentInteractionCreateEventArgs interaction)
		{
			Interaction = interaction.Interaction;
			Message = interaction.Message;
		}

		public InteractiveInteraction(DiscordInteraction interaction, DiscordMessage message)
		{
			Interaction = interaction;
			Message = message;
		}

		public bool CompareButton(string name) => ButtonId == name && (Components == null
			|| Components.First(x => x.CustomId == ButtonId) is DiscordButtonComponent btn && !btn.Disabled);

		public bool CompareButton(DiscordButtonComponent btn) => CompareButton(btn.CustomId);

		public bool IsSelected(string option) => Data.Values.Any(x => x == option);

		public string[] GetSelected() => Data.Values;

		public string GetFirstSelected() => Data.Values.FirstOrDefault();

		public IEnumerable<TOption> GetButtons<TOption>(IDictionary<string, TOption> options)
		 where TOption : class => options.Select(x => new Tuple<string, TOption>(x.Key, x.Value))
		 .Where(y => CompareButton(y?.Item1)).Select(x => x?.Item2);

		public TOption GetButton<TOption>(IDictionary<string, TOption> options)
		 where TOption : class => GetButtons(options).FirstOrDefault();

		public IEnumerable<TOption> GetOptions<TOption>(IDictionary<string, TOption> options)
		 where TOption : class => options.Select(x => new Tuple<string, TOption>(x.Key, x.Value))
		 .Where(y => Data.Values.Any(x => x == y.Item1)).Select(x => x?.Item2);

		public TOption GetOption<TOption>(IDictionary<string, TOption> options)
		 where TOption : class => GetOptions(options).FirstOrDefault();

		public bool AnyComponents(IEnumerable<string> ids) => ids.Any(x => ButtonId == x);

		public string ButtonId => Data.CustomId;

		public bool AnyComponents(IEnumerable<DiscordComponent> components) =>
		 AnyComponents(components.Select(x => x.CustomId));

		public bool AnyComponents(IEnumerable<DiscordActionRowComponent> components) =>
		 AnyComponents(components.SelectMany(x => x.Components));

		//public DiscordComponent IsAnyPressed() => _interaction
	}
}