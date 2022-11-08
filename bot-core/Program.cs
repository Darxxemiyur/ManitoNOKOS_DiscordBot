using Manito.Discord;

using System.Threading.Tasks;

namespace Manito
{
	internal class Program
	{
		private static async Task Main(string[] args)
		{
			var service = await MyDomain.Create();

			await service.StartBot();
		}
	}
}