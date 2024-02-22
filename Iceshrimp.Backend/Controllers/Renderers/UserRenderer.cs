using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Renderers;

public class UserRenderer(IOptions<Config.InstanceSection> config)
{
	public UserResponse RenderOne(User user)
	{
		return new UserResponse
		{
			Id        = user.Id,
			Username  = user.Username,
			AvatarUrl = user.AvatarUrl ?? $"https://{config.Value.WebDomain}/identicon/{user.Id}",
			BannerUrl = user.BannerUrl
		};
	}

	public IEnumerable<UserResponse> RenderMany(IEnumerable<User> users)
	{
		return users.Select(RenderOne);
	}
}