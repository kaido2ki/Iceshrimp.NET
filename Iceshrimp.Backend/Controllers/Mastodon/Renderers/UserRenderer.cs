using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Mastodon.Renderers;

public class UserRenderer(IOptions<Config.InstanceSection> config, MfmConverter mfmConverter) {
	private readonly string _transparent = $"https://{config.Value.WebDomain}/assets/transparent.png";

	public async Task<Account> RenderAsync(User user, UserProfile? profile) {
		var acct = user.Username;
		if (user.Host != null)
			acct += $"@{user.Host}";

		var res = new Account {
			Id = user.Id,
			DisplayName = user.DisplayName ?? user.Username,
			AvatarUrl = user.AvatarUrl ?? user.GetIdenticonUrl(config.Value),
			Username = user.Username,
			Acct = acct,
			FullyQualifiedName = $"{user.Username}@{user.Host ?? config.Value.AccountDomain}",
			IsLocked = user.IsLocked,
			CreatedAt = user.CreatedAt.ToStringMastodon(),
			FollowersCount = user.FollowersCount,
			FollowingCount = user.FollowingCount,
			StatusesCount = user.NotesCount,
			Note = await mfmConverter.ToHtmlAsync(profile?.Description ?? "", [], user.Host),
			Url = profile?.Url ?? user.Uri ?? user.GetPublicUrl(config.Value),
			AvatarStaticUrl = user.AvatarUrl ?? user.GetIdenticonUrl(config.Value), //TODO
			HeaderUrl = user.BannerUrl ?? _transparent,
			HeaderStaticUrl = user.BannerUrl ?? _transparent, //TODO
			MovedToAccount = null, //TODO
			IsBot = user.IsBot,
			IsDiscoverable = user.IsExplorable,
			Fields = [] //TODO
		};

		return res;
	}

	public async Task<Account> RenderAsync(User user) {
		return await RenderAsync(user, user.UserProfile);
	}

	public async Task<IEnumerable<Account>> RenderManyAsync(IEnumerable<User> users) {
		return await users.Select(RenderAsync).AwaitAllAsync();
	}
}