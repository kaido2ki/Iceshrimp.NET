using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Mastodon.Renderers;

public class UserRenderer(
	IOptions<Config.InstanceSection> config,
	IOptionsSnapshot<Config.SecuritySection> security,
	MfmConverter mfmConverter,
	DatabaseContext db
) : IScopedService
{
	private readonly string _transparent = $"https://{config.Value.WebDomain}/assets/transparent.png";

	public Task<AccountEntity> RenderAsync(User user, UserProfile? profile, User? localUser, bool source = false)
		=> RenderAsync(user, profile, localUser, null, source);

	private async Task<AccountEntity> RenderAsync(
		User user, UserProfile? profile, User? localUser, UserRendererDto? data = null, bool source = false
	)
	{
		var acct = user.Username;
		if (user.IsRemoteUser)
			acct += $"@{user.Host}";

		var profileEmoji = data?.Emoji.Where(p => user.Emojis.Contains(p.Id)).ToList() ?? await GetEmojiAsync([user]);
		var mentions     = profile?.Mentions ?? [];
		var fields = profile != null
			? await profile.Fields
			               .Select(async p => new Field
			               {
				               Name  = p.Name,
				               Value = (await mfmConverter.ToHtmlAsync(p.Value, mentions, user.Host)).Html,
				               VerifiedAt = p.IsVerified.HasValue && p.IsVerified.Value
					               ? DateTime.Now.ToStringIso8601Like()
					               : null
			               })
			               .AwaitAllAsync()
			: null;

		var fieldsSource = source
			? profile?.Fields.Select(p => new Field { Name = p.Name, Value = p.Value }).ToList() ?? []
			: [];

		var avatarAlt = data?.AvatarAlt.GetValueOrDefault(user.Id);
		var bannerAlt = data?.BannerAlt.GetValueOrDefault(user.Id);

		var res = new AccountEntity
		{
			Id                 = user.Id,
			DisplayName        = user.DisplayName ?? user.Username,
			AvatarUrl          = user.GetAvatarUrl(config.Value),
			Username           = user.Username,
			Acct               = acct,
			FullyQualifiedName = $"{user.Username}@{user.Host ?? config.Value.AccountDomain}",
			IsLocked           = user.IsLocked,
			CreatedAt          = user.CreatedAt.ToStringIso8601Like(),
			FollowersCount     = user.FollowersCount,
			FollowingCount     = user.FollowingCount,
			StatusesCount      = user.NotesCount,
			Note               = (await mfmConverter.ToHtmlAsync(profile?.Description ?? "", mentions, user.Host)).Html,
			Url                = profile?.Url ?? user.Uri ?? user.GetPublicUrl(config.Value),
			Uri                = user.Uri ?? user.GetPublicUri(config.Value),
			AvatarStaticUrl    = user.GetAvatarUrl(config.Value), //TODO
			AvatarDescription  = avatarAlt ?? "",
			HeaderUrl          = user.GetBannerUrl(config.Value) ?? _transparent,
			HeaderStaticUrl    = user.GetBannerUrl(config.Value) ?? _transparent, //TODO
			HeaderDescription  = bannerAlt ?? "",
			MovedToAccount     = null, //TODO
			IsBot              = user.IsBot,
			IsDiscoverable     = user.IsExplorable,
			Fields             = fields?.ToList() ?? [],
			Emoji              = profileEmoji
		};

		if (localUser is null && security.Value.PublicPreview == Enums.PublicPreview.RestrictedNoMedia) //TODO
		{
			res.AvatarUrl       = user.GetIdenticonUrl(config.Value);
			res.AvatarStaticUrl = user.GetIdenticonUrl(config.Value);
			res.HeaderUrl       = _transparent;
			res.HeaderStaticUrl = _transparent;
		}

		if (source)
		{
			//TODO: populate these
			res.Source = new AccountSource
			{
				Fields   = fieldsSource,
				Language = "",
				Note     = profile?.Description ?? "",
				Privacy =
					StatusEntity.EncodeVisibility(user.UserSettings?.DefaultNoteVisibility
					                              ?? Note.NoteVisibility.Public),
				Sensitive          = false,
				FollowRequestCount = await db.FollowRequests.CountAsync(p => p.Followee == user)
			};
		}

		return res;
	}

	private async Task<List<EmojiEntity>> GetEmojiAsync(IEnumerable<User> users)
	{
		var ids = users.SelectMany(p => p.Emojis).ToList();
		if (ids.Count == 0) return [];

		return await db.Emojis
		               .Where(p => ids.Contains(p.Id))
		               .Select(p => new EmojiEntity
		               {
			               Id              = p.Id,
			               Shortcode       = p.Name,
			               Url             = p.GetAccessUrl(config.Value),
			               StaticUrl       = p.GetAccessUrl(config.Value), //TODO
			               VisibleInPicker = true,
			               Category        = p.Category
		               })
		               .ToListAsync();
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

	public async Task<AccountEntity> RenderAsync(User user, User? localUser, List<EmojiEntity>? emoji = null)
	{
		var data = new UserRendererDto
		{
			Emoji     = emoji ?? await GetEmojiAsync([user]),
			AvatarAlt = await GetAvatarAltAsync([user]),
			BannerAlt = await GetBannerAltAsync([user])
		};

		return await RenderAsync(user, user.UserProfile, localUser, data);
	}

	public async Task<IEnumerable<AccountEntity>> RenderManyAsync(IEnumerable<User> users, User? localUser)
	{
		var userList = users.ToList();
		if (userList.Count == 0) return [];

		var data = new UserRendererDto
		{
			Emoji     = await GetEmojiAsync(userList),
			AvatarAlt = await GetAvatarAltAsync(userList),
			BannerAlt = await GetBannerAltAsync(userList)
		};

		return await userList.Select(p => RenderAsync(p, p.UserProfile, localUser, data)).AwaitAllAsync();
	}

	private class UserRendererDto
	{
		public required List<EmojiEntity>           Emoji;
		public required Dictionary<string, string?> AvatarAlt;
		public required Dictionary<string, string?> BannerAlt;
	}
}
