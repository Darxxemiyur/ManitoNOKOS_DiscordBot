using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord.ChatNew;

using Name.Bayfaderix.Darxxemiyur.Node.Network;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Manito.Discord.Chat.DialogueNet
{
	public static class Common
	{
		public static (DiscordButtonComponent, int)[][] Generate(
		 int[] nums, int[] muls) => muls.Select(y => nums.Select(x => (new DiscordButtonComponent(
			ButtonStyle.Secondary, $"{(x > 0 ? "add" : "sub")}{Math.Abs(x * y)}r_{Math.Abs(y)}",
			 (x > 0 ? "+" : "") + $"{x * y}"), x * y)).ToArray()).ToArray();

		public static async Task<int?> GetQuantity(int[] nums, int[] muls, DialogueNetSession session,
		 Func<int, int, Task<bool>> limiter, Func<int, Task<DiscordInteractionResponseBuilder>> responder,
		 int starting = 0)
		{
			var quantity = starting;
			var btns = Generate(nums, muls);
			var exbtn = new DiscordButtonComponent(ButtonStyle.Danger, "Exit", "Назад");
			var sbmbtn = new DiscordButtonComponent(ButtonStyle.Success, "Submit", "Выбрать");
			while (true)
			{
				var mg2 = await responder(quantity);

				foreach (var btn in btns.SelectMany(x => x))
				{
					if (await limiter(quantity, int.Parse(btn.Item1.Label)))
						btn.Item1.Enable();
					else
						btn.Item1.Disable();
				}

				foreach (var btnrs in btns)
					mg2.AddComponents(btnrs.Select(x => x.Item1));

				mg2.AddComponents(exbtn, sbmbtn);

				await session.Respond(mg2);

				await session.GetInteraction(mg2.Components);

				if (session.IArgs.CompareButton(exbtn))
					return null;

				if (session.IArgs.CompareButton(sbmbtn))
					return quantity;

				var pressed = session.IArgs.GetButton(btns.SelectMany(x => x)
					.Select(x => x.Item1).ToDictionary(x => x.CustomId));
				var change = int.Parse(pressed.Label);

				quantity = Math.Clamp(quantity + change, 0, int.MaxValue);
			}
		}

		public static async Task<int?> GetQuantity(int[] nums, int[] muls, IDialogueSession session, Func<int, int, Task<bool>> limiter, Func<int, Task<DiscordInteractionResponseBuilder>> responder, int starting = 0, int minimum = 0)
		{
			var quantity = Math.Max(minimum, starting);
			var btns = Generate(nums, muls);
			var exbtn = new DiscordButtonComponent(ButtonStyle.Danger, "exit", "Назад");
			var resbtn = new DiscordButtonComponent(ButtonStyle.Primary, "reset", "Сбросить");
			var sbmbtn = new DiscordButtonComponent(ButtonStyle.Success, "submit", "Выбрать");
			while (true)
			{
				var mg2 = await responder(quantity);

				foreach (var btn in btns.SelectMany(x => x).Select(x => x.Item1))
				{
					if (await limiter(quantity, int.Parse(btn.Label)))
						btn.Enable();
					else
						btn.Disable();
				}

				foreach (var btnrs in btns)
					mg2.AddComponents(btnrs.Select(x => x.Item1));

				mg2.AddComponents(exbtn, resbtn, sbmbtn);

				await session.SendMessage(mg2);

				var comp = await session.GetComponentInteraction();

				if (comp.CompareButton(exbtn))
					return null;

				if (comp.CompareButton(sbmbtn))
					return quantity;

				if (comp.CompareButton(resbtn))
				{
					quantity = starting;
					continue;
				}

				var pressed = comp.GetButton(btns.SelectMany(x => x)
					.Select(x => x.Item1).ToDictionary(x => x.CustomId));
				var change = int.Parse(pressed.Label);

				quantity = Math.Clamp(quantity + change, minimum, int.MaxValue);
			}
		}

		public static NodeResultHandler DefaultNodeResultHandler =>
			(x, y) => Task.FromResult(x.NextAction == NextNetworkActions.Continue && !y.IsCancellationRequested);
	}
}