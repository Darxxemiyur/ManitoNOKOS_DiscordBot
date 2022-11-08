using Name.Bayfaderix.Darxxemiyur.Common;

namespace Manito.Discord.Rules.GUI
{
	public class ItemFrame<T> : ItemFrameBase
	{
		public override string FrameName {
			get;
		}

		public override EditorType EditedBy {
			get;
		}

		public readonly T? Initial;
		public T? Intermmediate;
		public readonly MyTaskSource<T?> Callback = new();

		public ItemFrame(string frameName, EditorType editedBy, T? initial)
		{
			FrameName = frameName;
			EditedBy = editedBy;
			Intermmediate = Initial = initial;
		}
	}
}