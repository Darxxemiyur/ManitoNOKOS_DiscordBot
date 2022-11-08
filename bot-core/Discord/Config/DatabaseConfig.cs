using System;

namespace Manito.Discord.Config
{
	[Serializable]
	public class DatabaseConfig
	{
		public string Address {
			get;
		}

		public string Port {
			get;
		}

		public string Login {
			get;
		}

		public string Password {
			get;
		}

		public string Database {
			get;
		}

		public DatabaseConfig()
		{
			Address = "localhost";
			Port = "5432";
#if DEBUG
			// The sensetive info display, but it's not in prod so it's fine.
			Login = "postgres;Include Error Detail=true";
			Password = "postgres";
#else
			Login = "ManitoStuff";
			Password = "6A8C6D7C3188AB4A203A51149FDB66F098B925D57D75F15CB902FB82F68F1436";
#endif
			Database = "Manito";
		}

		private string LoginS => $"Username={Login};Password={Password}";
		private string ConnectS => $"Host={Address};Port={Port}";

		private string LoginString => $"{LoginS};{ConnectS};Database={Database}";
		private string OptionsString => "Minimum Pool Size=10";
		public string ConnectionString => $"{LoginString};{OptionsString}";
	}
}