using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Web.Renderers;

public class UserRenderer(IOptions<Config.InstanceSection> config, DatabaseContext db, MetaService metaSvc) : IScopedService
{
	private UserResponse Render(User user, UserRendererDto data)
	{
		var instance = user.IsRemoteUser ? data.InstanceData.FirstOrDefault(p => p.Host == user.Host) : null;

		var instanceName  = user.IsLocalUser ? data.LocalInstanceData!.Name : instance?.Name;
		var instanceColor = user.IsLocalUser ? data.LocalInstanceData!.ThemeColor : instance?.ThemeColor;
		var instanceIcon = user.IsLocalUser
			? data.LocalInstanceData!.FaviconUrl
			: instance?.GetFaviconAccessUrl(config.Value);

		if (!data.Emojis.TryGetValue(user.Id, out var emoji))
			throw new Exception("DTO didn't contain emoji for user");

		var avatarAlt = data.AvatarAlt.GetValueOrDefault(user.Id);
		var bannerAlt = data.BannerAlt.GetValueOrDefault(user.Id);

		return new UserResponse
		{
			Id              = user.Id,
			Username        = user.Username,
			Host            = user.Host,
			DisplayName     = user.DisplayName,
			AvatarUrl       = user.GetAvatarUrl(config.Value),
			AvatarAlt       = avatarAlt,
			BannerUrl       = user.GetBannerUrl(config.Value),
			BannerAlt       = bannerAlt,
			InstanceName    = instanceName,
			InstanceIconUrl = instanceIcon,
			InstanceColor   = instanceColor,
			Emojis          = emoji,
			MovedTo         = user.MovedToUri,
			IsSuspended     = user.IsSuspended,
			IsBot           = user.IsBot,
			IsCat           = user.IsCat,
			SpeakAsCat      = user is { IsCat: true, SpeakAsCat: true }
		};
	}

	public async Task<UserResponse> RenderOne(User user)
	{
		var instanceData = await GetInstanceDataAsync([user]);
		var emojis       = await GetEmojisAsync([user]);
		var avatarAlt    = await GetAvatarAltAsync([user]);
		var bannerAlt    = await GetBannerAltAsync([user]);

		LocalInstance? localInstance = null;
		if (user.IsLocalUser)
		{
			var iconId  = await metaSvc.GetAsync(MetaEntity.IconFileId);
			var favicon = iconId != null ? await db.DriveFiles.FirstOrDefaultAsync(p => p.Id == iconId) : null;

			localInstance = new LocalInstance
			{
				Name       = await metaSvc.GetAsync(MetaEntity.InstanceName) ?? config.Value.AccountDomain,
				FaviconUrl = favicon?.PublicUrl ?? favicon?.RawAccessUrl,
				ThemeColor = await metaSvc.GetAsync(MetaEntity.ThemeColor)
			};
		}

		var data = new UserRendererDto
		{
			Emojis            = emojis,
			InstanceData      = instanceData,
			AvatarAlt         = avatarAlt,
			BannerAlt         = bannerAlt,
			LocalInstanceData = localInstance
		};

		return Render(user, data);
	}

	private async Task<List<Instance>> GetInstanceDataAsync(IEnumerable<User> users)
	{
		var hosts = users.Select(p => p.Host).Where(p => p != null).Distinct().Cast<string>();
		return await db.Instances.Where(p => hosts.Contains(p.Host)).ToListAsync();
	}

	private async Task<Dictionary<string, string?>> GetAvatarAltAsync(IEnumerable<User> users)
	{
		var ids = users.Select(p => p.Id).ToList();
		return await db.Users
		               .Where(p => ids.Contains(p.Id))
		               .Include(p => p.Avatar)
		               .ToDictionaryAsync(p => p.Id, p => p.Avatar?.Comment);
	}

	private async Task<Dictionary<string, string?>> GetBannerAltAsync(IEnumerable<User> users)
	{
		var ids = users.Select(p => p.Id).ToList();
		return await db.Users
		               .Where(p => ids.Contains(p.Id))
		               .Include(p => p.Banner)
		               .ToDictionaryAsync(p => p.Id, p => p.Banner?.Comment);
	}

	public async Task<IEnumerable<UserResponse>> RenderManyAsync(IEnumerable<User> users)
	{
		var userList = users.ToList();

		LocalInstance? localInstance = null;
		if (userList.Any(p => p.IsLocalUser))
		{
			var iconId  = await metaSvc.GetAsync(MetaEntity.IconFileId);
			var favicon = iconId != null ? await db.DriveFiles.FirstOrDefaultAsync(p => p.Id == iconId) : null;

			localInstance = new LocalInstance
			{
				Name       = await metaSvc.GetAsync(MetaEntity.InstanceName) ?? config.Value.AccountDomain,
				FaviconUrl = favicon?.PublicUrl ?? favicon?.RawAccessUrl,
				ThemeColor = await metaSvc.GetAsync(MetaEntity.ThemeColor)
			};
		}

		var data = new UserRendererDto
		{
			InstanceData      = await GetInstanceDataAsync(userList),
			Emojis            = await GetEmojisAsync(userList),
			AvatarAlt         = await GetAvatarAltAsync(userList),
			BannerAlt         = await GetBannerAltAsync(userList),
			LocalInstanceData = localInstance
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
			                    Tags      = p.Tags,
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
		public required Dictionary<string, string?>             AvatarAlt;
		public required Dictionary<string, string?>             BannerAlt;
		public required LocalInstance?                          LocalInstanceData;
	}

	private class LocalInstance
	{
		public required string  Name       { get; set; }
		public required string? FaviconUrl { get; set; }
		public required string? ThemeColor { get; set; }
	}
}
