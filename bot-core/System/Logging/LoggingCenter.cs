using DisCatSharp;
using DisCatSharp.EventArgs;

using Manito.Discord.Client;

using Name.Bayfaderix.Darxxemiyur.Common;

using Newtonsoft.Json;

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Manito.System.Logging
{
	public class LoggingCenter : IModule
	{
		private readonly FIFOPTACollection<LogLine> _queue;
		private readonly MyClientBundle _client;
		private readonly ILoggingDBFactory _factory;
		private readonly FIFOFBACollection<(string, object)> _relay;

		public LoggingCenter(MyClientBundle client, ILoggingDBFactory factory)
		{
			(_client, _factory) = (client, factory);
			var dc = client.Client;
			_relay = new();
			_queue = new();

			dc.PayloadReceived += Dc_PayloadReceived;
		}

		~LoggingCenter()
		{
			_client.Client.PayloadReceived -= Dc_PayloadReceived;
		}

		private Task Dc_PayloadReceived(DiscordClient sender, string e) => _client.Domain.ExecutionThread.AddNew(new ExecThread.Job(() => WriteClassedLog("DiscordBotLog", e)));

		private Task Dc_PayloadReceived(DiscordClient sender, PayloadReceivedEventArgs e) => Dc_PayloadReceived(sender, e.Json);

		public async Task WriteErrorClassedLog(string district, Exception err, bool isHandled)
		{
#if DEBUG
			await Console.Out.WriteLineAsync("!!!Exception " + (isHandled ? "safely handled" : "not handled") + $"\n{err}\n\n\n");
#endif
			await WriteClassedLog(district, new
			{
				type = "error",
				isHandled,
				data = new
				{
					exception = err,
					digested_log = err.ToString()
				}
			});
		}

		public async Task WriteClassedLog(string district, object log) => await _relay.Handle((district, await ConvertTo(new
		{
			type = "classedlog",
			dataType = "ManuallyConvertedDueToNotBeingJsonInTheFirstPlace",
			data = await GetFromJson(log)
		})));

		private Task InnerWriteLogToDB(string district, JsonDocument jlog) => _queue.Place(new LogLine("Discord", district, jlog));

		public async Task WriteLog(string district, object log) => await InnerWriteLogToDB(district, await ParseJsonDocument(log));

		private static async Task<object> GetFromJson(object input) => input is not string json ? input : await ConvertFrom(json) ?? new
		{
			type = "ConvertedToJson",
			data = json
		};

		private async Task<string> GetToJson(object input) => await ConvertTo(await GetFromJson(input));

		private static JsonSerializerSettings Settings = new JsonSerializerSettings {
			MaxDepth = null
		};

		private static Task<string> ConvertTo(object itm) => Task.Run(() => JsonConvert.SerializeObject(itm, Settings));

		private static async Task<object> ConvertFrom(string json)
		{
			try
			{
				return await Task.Run(() => JsonConvert.DeserializeObject(json, Settings));
			}
			catch
			{
				return null;
			}
		}

		private async Task<JsonDocument> ParseJsonDocument(object jsono)
		{
			var stream = new MemoryStream();
			var writer = new StreamWriter(stream);
			writer.Write(await GetToJson(jsono));
			writer.Flush();
			stream.Position = 0;
			return await JsonDocument.ParseAsync(stream);
		}

		public Task RunModule() => Task.WhenAll(DiscordEventLogging(), RunDbLogging());

		private async Task DiscordEventLogging()
		{
			while (true)
			{
				var (district, log) = await _relay.GetData();
				await WriteLog(district, log);
			}
		}

		private async Task RunDbLogging()
		{
			while (true)
			{
				try
				{
					await _queue.UntilPlaced();

					var job = new ExecThread.Job(async () => {
						var range = await _queue.GetAll();
						try
						{
							await using var db = await _factory.CreateLoggingDBContextAsync();
							await db.LogLines.AddRangeAsync(range);
							await db.SaveChangesAsync();
							await Task.Delay(TimeSpan.FromSeconds(1));
							await Task.Run(() => {
								foreach (var item in range)
									item.Dispose();
							});
						}
						catch
						{
							await _queue.Place(range.Select(x => new LogLine(x)));
							throw;
						}
					});
					await _client.Domain.ExecutionThread.AddNew(job);

					await job.Result;
				}
				catch { await Task.Delay(TimeSpan.FromMinutes(2)); }
			}
		}
	}
}