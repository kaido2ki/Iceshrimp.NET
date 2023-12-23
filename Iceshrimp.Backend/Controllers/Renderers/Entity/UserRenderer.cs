using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database.Tables;

namespace Iceshrimp.Backend.Controllers.Renderers.Entity;

public class UserRenderer {
	public static UserResponse RenderOne(User user) {
		return new UserResponse {
			Id        = user.Id,
			Username  = user.Username,
			AvatarUrl = user.AvatarUrl,
			BannerUrl = user.BannerUrl
		};
	}

	public static IEnumerable<UserResponse> RenderMany(IEnumerable<User> users) {
		return users.Select(RenderOne);
	}
}