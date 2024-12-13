using Iceshrimp.Backend.Components.PublicPreview.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Components.PublicPreview.Renderers;

public class UserRenderer(
	DatabaseContext db,
	MfmRenderer mfm,
	IOptions<Config.InstanceSection> instance,
	IOptionsSnapshot<Config.SecuritySection> security
) : IScopedService
{
	public async Task<PreviewUser?> RenderOne(User? user)
	{
		if (user == null) return null;
		var emoji = await GetEmojiAsync([user]);
		return await RenderAsync(user, emoji);
	}

	private async Task<PreviewUser> RenderAsync(User user, Dictionary<string, List<Emoji>> emoji)
	{
		var mentions = user.UserProfile?.Mentions ?? [];

		// @formatter:off
		var res = new PreviewUser
		{
			Id             = user.Id,
			Username       = user.Username,
			Host           = user.Host ?? instance.Value.AccountDomain,
			Url            = user.UserProfile?.Url ?? user.Uri ?? user.PublicUrlPath,
			AvatarUrl      = user.AvatarUrl ?? user.IdenticonUrlPath,
			BannerUrl      = user.BannerUrl,
			RawDisplayName = user.DisplayName,
			DisplayName    = await mfm.RenderSimpleAsync(user.DisplayName, user.Host, mentions, emoji[user.Id], "span"),
			Bio            = await mfm.RenderSimpleAsync(user.UserProfile?.Description, user.Host, mentions, emoji[user.Id], "span"),
			MovedToUri     = user.MovedToUri
		};
		// @formatter:on

		if (security.Value.PublicPreview is Enums.PublicPreview.RestrictedNoMedia)
		{
			res.AvatarUrl = user.IdenticonUrlPath;
			res.BannerUrl = null;
		}

		return res;
	}

	private async Task<Dictionary<string, List<Emoji>>> GetEmojiAsync(List<User> users)
	{
		var ids = users.SelectMany(n => n.Emojis).Distinct().ToList();
		if (ids.Count == 0) return users.ToDictionary<User, string, List<Emoji>>(p => p.Id, _ => []);

		var emoji = await db.Emojis.Where(p => ids.Contains(p.Id)).ToListAsync();
		return users.ToDictionary(p => p.Id, p => emoji.Where(e => p.Emojis.Contains(e.Id)).ToList());
	}

	public async Task<List<PreviewUser>> RenderManyAsync(List<User> users)
	{
		var emoji = await GetEmojiAsync(users);
		return await users.Select(p => RenderAsync(p, emoji)).AwaitAllAsync().ToListAsync();
	}
}