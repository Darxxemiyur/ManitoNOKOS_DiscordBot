using System;
using System.IO;
using System.Text.Json;

namespace Manito.System.Logging
{
	public class LogLine : IDisposable
	{
		public long ID {
			get; set;
		}

		public DateTimeOffset LoggedTime {
			get; set;
		}

		public string District {
			get; set;
		}

		public string Category {
			get; set;
		}

		public JsonDocument Data {
			get; set;
		}

		public LogLine(string district, string category, JsonDocument data)
		{
			District = district;
			LoggedTime = DateTimeOffset.UtcNow;
			Category = category;
			Data = data;
		}

		public LogLine(LogLine line)
		{
			District = line.District;
			LoggedTime = line.LoggedTime;
			Category = line.Category;
			using var mem = new MemoryStream();
			using var wr = new Utf8JsonWriter(mem);
			line.Data.WriteTo(wr);
			wr.Flush();
			mem.Position = 0;
			Data = JsonDocument.Parse(mem);
		}

		public void Dispose() => Data?.Dispose();
	}
}