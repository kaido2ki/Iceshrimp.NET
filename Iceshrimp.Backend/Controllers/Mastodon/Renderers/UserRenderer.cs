using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Mastodon.Renderers;

public class UserRenderer(IOptions<Config.InstanceSection> config, DatabaseContext db) {
	private readonly string _transparent = $"https://{config.Value.WebDomain}/assets/transparent.png";

	public async Task<Account> RenderAsync(User user, UserProfile? profile) {
		var acct = user.Username;
		if (user.Host != null)
			acct += $"@{user.Host}";

		//TODO: respect ffVisibility for follower/following count

		var res = new Account {
			Id = user.Id,
			DisplayName = user.Name ?? user.Username,
			AvatarUrl = user.AvatarUrl ?? _transparent,
			Username = user.Username,
			Acct = acct,
			FullyQualifiedName = $"{user.Username}@{user.Host ?? config.Value.AccountDomain}",
			IsLocked = user.IsLocked,
			CreatedAt = user.CreatedAt.ToString("O")[..^5],
			FollowersCount = user.FollowersCount,
			FollowingCount = user.FollowingCount,
			StatusesCount = user.NotesCount,
			Note = await MfmConverter.ToHtmlAsync(profile?.Description ?? ""),
			Url = profile?.Url ?? user.Uri ?? $"https://{user.Host ?? config.Value.WebDomain}/@{user.Username}",
			AvatarStaticUrl = user.AvatarUrl ?? _transparent, //TODO
			HeaderUrl = user.BannerUrl ?? _transparent,
			HeaderStaticUrl = user.BannerUrl ?? _transparent, //TODO
			MovedToAccount = null, //TODO
			IsBot = user.IsBot,
			IsDiscoverable = user.IsExplorable
		};

		return res;
	}

	public async Task<Account> RenderAsync(User user) {
		var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.User == user);
		return await RenderAsync(user, profile);
	}
}