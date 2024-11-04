using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Web.Renderers;

public class UserRenderer(IOptions<Config.InstanceSection> config, DatabaseContext db)
{
	public async Task<UserResponse> RenderOne(User user, UserRendererDto? data = null)
	{
		var instance = user.IsLocalUser
			? null
			: (data?.InstanceData ?? await GetInstanceData([user])).FirstOrDefault(p => p.Host == user.Host);

		//TODO: populate the below two lines for local users
		var instanceName = user.IsLocalUser ? config.Value.AccountDomain : instance?.Name;
		var instanceIcon = user.IsLocalUser ? null : instance?.FaviconUrl;

		var emojis = await GetEmojis([user]);

		return new UserResponse
		{
			Id              = user.Id,
			Username        = user.Username,
			Host            = user.Host,
			DisplayName     = user.DisplayName,
			AvatarUrl       = user.AvatarUrl ?? user.GetIdenticonUrl(config.Value),
			BannerUrl       = user.BannerUrl,
			InstanceName    = instanceName,
			InstanceIconUrl = instanceIcon,
			Emojis          = emojis,
			MovedTo         = user.MovedToUri
		};
	}

	private async Task<List<Instance>> GetInstanceData(IEnumerable<User> users)
	{
		var hosts = users.Select(p => p.Host).Where(p => p != null).Distinct().Cast<string>();
		return await db.Instances.Where(p => hosts.Contains(p.Host)).ToListAsync();
	}

	public async Task<IEnumerable<UserResponse>> RenderMany(IEnumerable<User> users)
	{
		var userList = users.ToList();
		var data     = new UserRendererDto { InstanceData = await GetInstanceData(userList) };
		return await userList.Select(p => RenderOne(p, data)).AwaitAllAsync();
	}

	private async Task<List<EmojiResponse>> GetEmojis(IEnumerable<User> users)
	{
		var ids = users.SelectMany(p => p.Emojis).ToList();
		if (ids.Count == 0) return [];

		return await db.Emojis
		               .Where(p => ids.Contains(p.Id))
		               .Select(p => new EmojiResponse
		               {
			               Id        = p.Id,
			               Name      = p.Name,
			               Uri       = p.Uri,
			               Aliases   = p.Aliases,
			               Category  = p.Category,
			               PublicUrl = p.PublicUrl,
			               License   = p.License,
			               Sensitive = p.Sensitive
		               })
		               .ToListAsync();
	}

	public class UserRendererDto
	{
		public List<Instance>? InstanceData;
	}
}