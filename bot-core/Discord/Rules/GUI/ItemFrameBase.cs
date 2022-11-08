namespace Manito.Discord.Rules.GUI
{
	public abstract class ItemFrameBase
	{
		public abstract string FrameName {
			get;
		}

		public abstract EditorType EditedBy {
			get;
		}
	}
}