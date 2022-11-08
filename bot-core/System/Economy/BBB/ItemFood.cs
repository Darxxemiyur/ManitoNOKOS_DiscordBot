namespace Manito.System.Economy.BBB
{
	public class ItemFood : ItemBase
	{
		public FoodProperties Properties {
			get; set;
		}
	}

	public enum FoodTypes
	{
		Plant,
		Carcass,
		Satiation
	}

	public class FoodProperties
	{
		public FoodTypes FoodType {
			get; set;
		}

		public int Quantity {
			get; set;
		}
	}
}