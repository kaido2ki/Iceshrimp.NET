using AngleSharp.Text;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class UserRenderer(
	IOptions<Config.InstanceSection> config,
	DatabaseContext db,
	MfmConverter mfmConverter
) : IScopedService
{
	/// <summary>
	///     This function is meant for compacting an actor into the @id form as specified in ActivityStreams
	/// </summary>
	/// <param name="user">Any local or remote user</param>
	/// <returns>ASActor with only the Id field populated</returns>
	public ASActor RenderLite(User user)
	{
		return user.IsRemoteUser
			? new ASActor { Id = user.Uri ?? throw new Exception("Remote user must have an URI") }
			: new ASActor { Id = user.GetPublicUri(config.Value) };
	}

	public async Task<ASActor> RenderAsync(User user)
	{
		if (user.IsRemoteUser)
		{
			return new ASActor { Id = user.Uri ?? throw new Exception("Remote user must have an URI") };
		}

		var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.User == user);
		var keypair = await db.UserKeypairs.FirstOrDefaultAsync(p => p.User == user);

		if (keypair == null) throw new Exception("User has no keypair");

		// Fetch avatar/banner relations if missing
		if ((user.Avatar == null && user.AvatarId != null) || (user.Banner == null && user.BannerId != null))
		{
			var newUser = await db.Users
			                      .IncludeCommonProperties()
			                      .Include(p => p.Avatar)
			                      .Include(p => p.Banner)
			                      .FirstOrDefaultAsync(p => p.Id == user.Id);

			if (newUser != null)
				user = newUser;
		}

		var id = user.GetPublicUri(config.Value);
		var type = Constants.SystemUsers.Contains(user.UsernameLower)
			? ASActor.Types.Application
			: user.IsBot
				? ASActor.Types.Service
				: ASActor.Types.Person;

		var emoji = user.Emojis.Count != 0
			? await db.Emojis.Where(p => user.Emojis.Contains(p.Id) && p.Host == null).ToListAsync()
			: [];

		var tags = user.Tags
		               .Select(tag => new ASHashtag
		               {
			               Name = $"#{tag}",
			               Href = new ASObjectBase($"https://{config.Value.WebDomain}/tags/{tag}")
		               })
		               .Concat<ASTag>(emoji.Select(e => new ASEmoji
		               {
			               Id    = e.GetPublicUri(config.Value),
			               Name  = e.Name,
			               Image = new ASImage { Url = new ASLink(e.RawPublicUrl) }
		               }))
		               .ToList();

		var attachments = profile?.Fields
		                         .Select(p => new ASField { Name = p.Name, Value = RenderFieldValue(p.Value) })
		                         .Concat<ASAttachment>(profile.Pronouns != null && profile.Pronouns.Count != 0
			                                               ?
			                                               [
				                                               profile.Pronouns.TryGetValue("", out var pronouns)
					                                               ? new ASPronouns { Name = new LDLocalizedString(null, pronouns) }
					                                               : new ASPronouns { Name = new LDLocalizedString { Values = profile.Pronouns! } }
			                                               ]
			                                               : [])
		                         .ToList();

		var summary = profile?.Description != null
			? (await mfmConverter.ToHtmlAsync(profile.Description, profile.Mentions, user.Host)).Html
			: null;

		return new ASActor
		{
			Id               = id,
			Type             = type,
			Inbox            = new ASLink($"{id}/inbox"),
			Outbox           = new ASCollection($"{id}/outbox"),
			Followers        = new ASOrderedCollection($"{id}/followers"),
			Following        = new ASOrderedCollection($"{id}/following"),
			SharedInbox      = new ASLink($"https://{config.Value.WebDomain}/inbox"),
			Url              = new ASLink(user.GetPublicUrl(config.Value)),
			Username         = user.Username,
			WebfingerAddress = user.IsLocalUser ? $"{user.Username}@{config.Value.AccountDomain}" : null,
			DisplayName      = user.DisplayName ?? user.Username,
			Summary          = summary,
			MkSummary        = profile?.Description,
			IsCat            = user.IsCat,
			IsDiscoverable   = user.IsExplorable,
			IsLocked         = user.IsLocked,
			Location         = profile?.Location,
			Birthday         = profile?.Birthday,
			AlsoKnownAs      = user.AlsoKnownAs?.Select(p => new ASLink(p)).ToList(),
			MovedTo          = user.MovedToUri is not null ? new ASLink(user.MovedToUri) : null,
			Featured         = new ASOrderedCollection($"{id}/collections/featured"),
			Avatar = user.Avatar != null
				? new ASImage
				{
					Url         = new ASLink(user.Avatar.RawAccessUrl),
					Description = user.Avatar?.Comment,
					MediaType   = user.Avatar?.Type
				}
				: null,
			Banner = user.Banner != null
				? new ASImage
				{
					Url         = new ASLink(user.Banner.RawAccessUrl),
					Description = user.Banner?.Comment,
					MediaType   = user.Banner?.Type
				}
				: null,
			Endpoints = new ASEndpoints { SharedInbox = new ASObjectBase($"https://{config.Value.WebDomain}/inbox") },
			PublicKey = new ASPublicKey
			{
				Id        = $"{id}#main-key",
				Owner     = new ASObjectBase(id),
				PublicKey = keypair.PublicKey
			},
			Tags        = tags,
			Attachments = attachments
		};
	}

	private static string RenderFieldValue(string value)
	{
		if (!value.StartsWith("http://") && !value.StartsWith("https://")) return value;
		if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
			return value;

		var displayUri = uri.Host + uri.PathAndQuery + uri.Fragment;
		return $"<a href=\"{uri.ToString()}\" rel=\"me nofollow noopener\" target=\"_blank\">{displayUri}</a>";
	}
}
