using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Manito.Discord;

using System.Linq;
using System.Threading.Tasks;

namespace Manito.System.UserAssociation
{
	public class UserPermissionChecker
	{
		private readonly MyDomain _domain;

		public UserPermissionChecker(MyDomain domain) => _domain = domain;

		public ulong GodId => 860897395109789706;

		public Task<bool> IsGod(DiscordUser user) => Task.FromResult(GodId == user.Id);

		public async Task<bool> DoesHaveAdminPermission(object location, DiscordUser user, object salt = default)
		{
			if (await IsGod(user))
				return true;

			var guild = await _domain.MyDiscordClient.ManitoGuild;

			var guser = await guild.GetMemberAsync(user.Id, true);

			var admRoles = new ulong[] { 916654996081217596, 915927647756877865, 916296659472891974, 1006227774526210098, 915927566152511519 };

			return guser.Permissions.HasFlag(Permissions.Administrator) || guser.Roles.Any(x => admRoles.Any(y => y == x.Id));
		}
	}
}