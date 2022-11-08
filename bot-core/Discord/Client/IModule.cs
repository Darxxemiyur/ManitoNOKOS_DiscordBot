using System.Threading.Tasks;

namespace Manito.Discord.Client
{
	public interface IModule
	{
		Task RunModule();
	}
}