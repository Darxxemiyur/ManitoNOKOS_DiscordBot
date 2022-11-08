using DisCatSharp.Entities;

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Manito.Discord.ChatNew
{
	/// <summary>
	/// Universal message container that is converted to respected message update call's data!
	/// </summary>
	public class UniversalMessageBuilder
	{
		private List<List<DiscordComponent>> _components;
		public IReadOnlyList<IReadOnlyList<DiscordComponent>> Components => _components;
		private List<DiscordEmbedBuilder> _embeds;
		public IReadOnlyList<DiscordEmbedBuilder> Embeds => _embeds;
		private Dictionary<string, Stream> _files;
		public IReadOnlyDictionary<string, Stream> Files => _files;
		private List<IMention> _mentions;
		public IReadOnlyList<IMention> Mentions => _mentions;
		private string _content;
		public string Content => _content;

		public UniversalMessageBuilder(DiscordMessage msg)
		{
			_components = msg.Components?.Select(x => x.Components.ToList())?.ToList();
			_embeds = msg.Embeds?.Select(x => new DiscordEmbedBuilder(x))?.ToList();
			_mentions = null;
			_content = msg.Content;
		}

		public UniversalMessageBuilder(params UniversalMessageBuilder[] umm)
		{
			var um = umm.Where(x => x != null);
			_embeds = um?.Where(x => x._embeds != null).SelectMany(x => x._embeds)?.ToList();
			_components = um?.Where(x => x._components != null).SelectMany(x => x._components)?.ToList();
			_files = um?.SelectMany(x => x._files)?.ToDictionary(x => x.Key, x => x.Value);
			_mentions = um?.Where(x => x._mentions != null).SelectMany(x => x._mentions)?.ToList();
			_content = string.Concat(um?.Select(x => x._content)?.Where(x => x != null));
		}

		public UniversalMessageBuilder(DiscordWebhookBuilder builder)
		{
			_components = builder?.Components?.Where(x => x.Components != null)?.Select(x => x.Components.ToList())?.ToList();
			_embeds = builder?.Embeds?.Select(x => new DiscordEmbedBuilder(x))?.ToList();
			_files = builder?.Files?.ToDictionary(x => x.FileName, x => x.Stream);
			_mentions = builder?.Mentions?.ToList();
			_content = builder?.Content;
		}

		public UniversalMessageBuilder(DiscordMessageBuilder builder)
		{
			_components = builder?.Components?.Where(x => x.Components != null)?.Select(x => x.Components.ToList())?.ToList();
			_embeds = builder?.Embeds?.Select(x => new DiscordEmbedBuilder(x))?.ToList();
			_files = builder?.Files?.ToDictionary(x => x.FileName, x => x.Stream);
			_mentions = builder?.Mentions?.ToList();
			_content = builder?.Content;
		}

		public UniversalMessageBuilder(DiscordInteractionResponseBuilder builder)
		{
			_components = builder?.Components?.Where(x => x.Components != null)?.Select(x => x.Components.ToList())?.ToList();
			_embeds = builder?.Embeds?.Select(x => new DiscordEmbedBuilder(x)).ToList();
			_files = builder?.Files?.ToDictionary(x => x.FileName, x => x.Stream);
			_mentions = builder?.Mentions?.ToList();
			_content = builder?.Content;
		}

		public UniversalMessageBuilder() => ResetBuilder();

		public UniversalMessageBuilder SetContent(string content)
		{
			_content = content;
			return this;
		}

		public UniversalMessageBuilder NewWithDisabledComponents()
		{
			var newBld = new UniversalMessageBuilder(this);
			var components = newBld.Components.Select(y => y.Select(x => {
				if (x is DiscordButtonComponent f)
					return new DiscordButtonComponent(f).Disable();
				if (x is DiscordSelectComponent g)
					return new DiscordSelectComponent(g.Placeholder, g.Options, g.CustomId, (int)g.MinimumSelectedValues, (int)g.MaximumSelectedValues, true);
				return x;
			}).ToArray()).ToArray();
			return newBld.SetComponents(components);
		}

		public UniversalMessageBuilder AddContent(string content) => SetContent(_content + content);

		public UniversalMessageBuilder AddComponents(params DiscordComponent[] components)
		{
			_components.Add(components.ToList());
			return this;
		}

		public UniversalMessageBuilder AddComponents(params DiscordComponent[][] components)
		{
			foreach (var row in components)
				AddComponents(row);

			return this;
		}

		public UniversalMessageBuilder SetComponents(params DiscordComponent[][] components)
		{
			_components = components.Select(x => x.ToList()).ToList();
			return this;
		}

		public UniversalMessageBuilder AddEmbed(DiscordEmbedBuilder embed)
		{
			_embeds.Add(embed);
			return this;
		}

		public UniversalMessageBuilder AddEmbeds(params DiscordEmbedBuilder[] components)
		{
			foreach (var row in components)
				AddEmbed(row);

			return this;
		}

		public UniversalMessageBuilder SetEmbeds(params DiscordEmbedBuilder[] components)
		{
			_embeds = components.ToList();

			return this;
		}

		public UniversalMessageBuilder SetFile(string name, Stream file)
		{
			_files[name] = file;

			return this;
		}

		public UniversalMessageBuilder OverrideFiles(Dictionary<string, Stream> files)
		{
			_files = files;

			return this;
		}

		public UniversalMessageBuilder SetFiles(Dictionary<string, Stream> files)
		{
			foreach (var file in files)
				_files[file.Key] = file.Value;

			return this;
		}

		public UniversalMessageBuilder AddMention(IMention mention)
		{
			_mentions.Add(mention);

			return this;
		}

		public UniversalMessageBuilder AddMentions(IEnumerable<IMention> mentions)
		{
			_mentions.AddRange(mentions);

			return this;
		}

		public UniversalMessageBuilder ResetBuilder()
		{
			_components = new();
			_embeds = new();
			_files = new();
			_mentions = new();
			_content = "";

			return this;
		}

		public static implicit operator UniversalMessageBuilder(string msg) => new UniversalMessageBuilder().SetContent(msg);

		public static implicit operator UniversalMessageBuilder(DiscordComponent[][] msg) => new UniversalMessageBuilder().SetComponents(msg);

		public static implicit operator UniversalMessageBuilder(DiscordComponent[] msg) => new UniversalMessageBuilder().SetComponents(msg);

		public static implicit operator UniversalMessageBuilder(DiscordEmbedBuilder msg) => new UniversalMessageBuilder().AddEmbed(msg);

		public static implicit operator UniversalMessageBuilder(DiscordEmbedBuilder[] msg) => new UniversalMessageBuilder().AddEmbeds(msg);

		public static implicit operator UniversalMessageBuilder(DiscordWebhookBuilder msg) => new(msg);

		public static implicit operator UniversalMessageBuilder(DiscordMessageBuilder msg) => new(msg);

		public static implicit operator UniversalMessageBuilder(DiscordInteractionResponseBuilder msg) => new(msg);

		public static implicit operator DiscordWebhookBuilder(UniversalMessageBuilder msg)
		{
			var wbh = new DiscordWebhookBuilder();

			if (msg._components != null)
				foreach (var row in msg._components)
					wbh.AddComponents(row);

			if (msg._embeds != null)
				wbh.AddEmbeds(msg._embeds.Select(x => x.Build()));

			if (msg._content != null)
				wbh.WithContent(msg._content);

			if (msg._files != null)
				wbh.AddFiles(msg._files);

			if (msg._mentions != null)
				wbh.AddMentions(msg._mentions);

			return wbh;
		}

		public static implicit operator DiscordMessageBuilder(UniversalMessageBuilder msg)
		{
			var wbh = new DiscordMessageBuilder();

			if (msg._components != null)
				foreach (var row in msg._components)
					wbh.AddComponents(row);

			if (msg._embeds != null)
				wbh.AddEmbeds(msg._embeds.Select(x => x.Build()));

			if (msg._embeds != null)
				wbh.WithContent(msg._content);

			if (msg._embeds != null)
				wbh.WithFiles(msg._files);

			if (msg._embeds != null)
				wbh.WithAllowedMentions(msg._mentions);

			return wbh;
		}

		public static implicit operator DiscordInteractionResponseBuilder(UniversalMessageBuilder msg)
		{
			var dirb = new DiscordInteractionResponseBuilder();

			if (msg._components != null)
				foreach (var row in msg._components)
					dirb.AddComponents(row);

			if (msg._embeds != null)
				dirb.AddEmbeds(msg._embeds.Select(x => x.Build()));

			if (msg._content != null)
				dirb.WithContent(msg._content);

			if (msg._files != null)
				dirb.AddFiles(msg._files);

			if (msg._mentions != null)
				dirb.AddMentions(msg._mentions);

			return dirb;
		}
	}
}