namespace Manito.Discord.PermanentMessage
{
	public class MessageWallLine
	{
		public long ID {
			get; set;
		}

		public MessageWall MessageWall {
			get; set;
		}

		public string WallLine {
			get; private set;
		}

		public MessageWallLine()
		{
		}

		public MessageWallLine(string line) => WallLine = line;

		public void SetLine(string line) => WallLine = line;

		public bool IsNull() => string.IsNullOrWhiteSpace(WallLine);

		public static implicit operator string(MessageWallLine mwl) => mwl.WallLine;
	}
}