using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Mastodon.Renderers;

public class UserRenderer(IOptions<Config.InstanceSection> config, MfmConverter mfmConverter, DatabaseContext db)
{
	private readonly string _transparent = $"https://{config.Value.WebDomain}/assets/transparent.png";

	public async Task<AccountEntity> RenderAsync(
		User user, UserProfile? profile, IEnumerable<EmojiEntity>? emoji = null, bool source = false
	)
	{
		var acct = user.Username;
		if (user.Host != null)
			acct += $"@{user.Host}";

		var profileEmoji = emoji?.Where(p => user.Emojis.Contains(p.Id)).ToList() ?? await GetEmoji([user]);
		var mentions     = profile?.Mentions ?? [];
		var fields = profile != null
			? await profile.Fields
			               .Select(async p => new Field
			               {
				               Name  = p.Name,
				               Value = await mfmConverter.ToHtmlAsync(p.Value, mentions, user.Host),
				               VerifiedAt = p.IsVerified.HasValue && p.IsVerified.Value
					               ? DateTime.Now.ToStringIso8601Like()
					               : null
			               })
			               .AwaitAllAsync()
			: null;

		var fieldsSource = source
			? profile?.Fields.Select(p => new Field { Name = p.Name, Value = p.Value }).ToList() ?? []
			: [];

		var res = new AccountEntity
		{
			Id                 = user.Id,
			DisplayName        = user.DisplayName ?? user.Username,
			AvatarUrl          = user.AvatarUrl ?? user.GetIdenticonUrlPng(config.Value),
			Username           = user.Username,
			Acct               = acct,
			FullyQualifiedName = $"{user.Username}@{user.Host ?? config.Value.AccountDomain}",
			IsLocked           = user.IsLocked,
			CreatedAt          = user.CreatedAt.ToStringIso8601Like(),
			FollowersCount     = user.FollowersCount,
			FollowingCount     = user.FollowingCount,
			StatusesCount      = user.NotesCount,
			Note               = await mfmConverter.ToHtmlAsync(profile?.Description ?? "", mentions, user.Host),
			Url                = profile?.Url ?? user.Uri ?? user.GetPublicUrl(config.Value),
			AvatarStaticUrl    = user.AvatarUrl ?? user.GetIdenticonUrlPng(config.Value), //TODO
			HeaderUrl          = user.BannerUrl ?? _transparent,
			HeaderStaticUrl    = user.BannerUrl ?? _transparent, //TODO
			MovedToAccount     = null,                           //TODO
			IsBot              = user.IsBot,
			IsDiscoverable     = user.IsExplorable,
			Fields             = fields?.ToList() ?? [],
			Emoji              = profileEmoji
		};

		if (source)
		{
			//TODO: populate these
			res.Source = new AccountSource
			{
				Fields   = fieldsSource,
				Language = "",
				Note     = profile?.Description ?? "",
				Privacy = StatusEntity.EncodeVisibility(user.UserSettings?.DefaultNoteVisibility ??
				                                        Note.NoteVisibility.Public),
				Sensitive = false
			};
		}

		return res;
	}

	private async Task<List<EmojiEntity>> GetEmoji(IEnumerable<User> users)
	{
		var ids = users.SelectMany(p => p.Emojis).ToList();
		if (ids.Count == 0) return [];

		return await db.Emojis
		               .Where(p => ids.Contains(p.Id))
		               .Select(p => new EmojiEntity
		               {
			               Id              = p.Id,
			               Shortcode       = p.Name,
			               Url             = p.PublicUrl,
			               StaticUrl       = p.PublicUrl, //TODO
			               VisibleInPicker = true
		               })
		               .ToListAsync();
	}

	public async Task<AccountEntity> RenderAsync(User user, List<EmojiEntity>? emoji = null)
	{
		return await RenderAsync(user, user.UserProfile, emoji);
	}

	public async Task<IEnumerable<AccountEntity>> RenderManyAsync(IEnumerable<User> users)
	{
		var userList = users.ToList();
		if (userList.Count == 0) return [];
		var emoji = await GetEmoji(userList);
		return await userList.Select(p => RenderAsync(p, emoji)).AwaitAllAsync();
	}
}