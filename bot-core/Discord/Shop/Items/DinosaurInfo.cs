namespace Manito.Discord.Shop.Items
{
	/// <summary>
	/// Dino species
	/// </summary>
	public enum DinosaurSpecie
	{
		//Land Carnivores
		Мегалозавр,

		Мегараптор,
		Велоцираптор,
		Акрокантозавр,
		Тирранозавр,

		//Sky carnivores
		Тропеогнат,

		Птерозавр,

		//Water carnivores
		Ихтиовинатор,

		Мозазавр,
		Эласмозарв,
		Кронозавр,

		//Herbivores
		Апатозавр,

		Паразаулоф,
		Зайхания,
		Лурдозавр,
		Ориктодромей,

		//Omnivores
		Пахицефалозавр,

		Коахуилацератопс,
	}

	public class DinosaurInfo
	{
		public float Growth;
		public DinosaurSpecie Specie;
		public bool IsMale;
	}
}