using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Web.Renderers;

public class UserRenderer(IOptions<Config.InstanceSection> config, DatabaseContext db) : IScopedService
{
	private UserResponse Render(User user, UserRendererDto data)
	{
		var instance = user.IsRemoteUser ? data.InstanceData.FirstOrDefault(p => p.Host == user.Host) : null;

		//TODO: populate the below two lines for local users
		var instanceName = user.IsLocalUser ? config.Value.AccountDomain : instance?.Name;
		var instanceIcon = user.IsLocalUser ? null : instance?.FaviconUrl;

		if (!data.Emojis.TryGetValue(user.Id, out var emoji))
			throw new Exception("DTO didn't contain emoji for user");

		return new UserResponse
		{
			Id              = user.Id,
			Username        = user.Username,
			Host            = user.Host,
			DisplayName     = user.DisplayName,
			AvatarUrl       = user.GetAvatarUrl(config.Value),
			BannerUrl       = user.GetBannerUrl(config.Value),
			InstanceName    = instanceName,
			InstanceIconUrl = instanceIcon,
			Emojis          = emoji,
			MovedTo         = user.MovedToUri,
			IsBot           = user.IsBot,
			IsCat           = user.IsCat
		};
	}

	public async Task<UserResponse> RenderOne(User user)
	{
		var instanceData = await GetInstanceDataAsync([user]);
		var emojis       = await GetEmojisAsync([user]);
		var data         = new UserRendererDto { Emojis = emojis, InstanceData = instanceData };

		return Render(user, data);
	}

	private async Task<List<Instance>> GetInstanceDataAsync(IEnumerable<User> users)
	{
		var hosts = users.Select(p => p.Host).Where(p => p != null).Distinct().Cast<string>();
		return await db.Instances.Where(p => hosts.Contains(p.Host)).ToListAsync();
	}

	public async Task<IEnumerable<UserResponse>> RenderManyAsync(IEnumerable<User> users)
	{
		var userList = users.ToList();
		var data = new UserRendererDto
		{
			InstanceData = await GetInstanceDataAsync(userList), Emojis = await GetEmojisAsync(userList)
		};

		return userList.Select(p => Render(p, data));
	}

	private async Task<Dictionary<string, List<EmojiResponse>>> GetEmojisAsync(ICollection<User> users)
	{
		var ids = users.SelectMany(p => p.Emojis).ToList();
		if (ids.Count == 0) return users.ToDictionary<User, string, List<EmojiResponse>>(p => p.Id, _ => []);

		var emoji = await db.Emojis
		                    .Where(p => ids.Contains(p.Id))
		                    .Select(p => new EmojiResponse
		                    {
			                    Id        = p.Id,
			                    Name      = p.Name,
			                    Uri       = p.Uri,
			                    Aliases   = p.Aliases,
			                    Category  = p.Category,
			                    PublicUrl = p.GetAccessUrl(config.Value),
			                    License   = p.License,
			                    Sensitive = p.Sensitive
		                    })
		                    .ToListAsync();

		return users.ToDictionary(p => p.Id, p => emoji.Where(e => p.Emojis.Contains(e.Id)).ToList());
	}

	private class UserRendererDto
	{
		public required List<Instance>                          InstanceData;
		public required Dictionary<string, List<EmojiResponse>> Emojis;
	}
}