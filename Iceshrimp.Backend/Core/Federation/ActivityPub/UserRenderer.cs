using AngleSharp.Text;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class UserRenderer(IOptions<Config.InstanceSection> config, DatabaseContext db) {
	public async Task<ASActor> RenderAsync(User user) {
		if (user.Host != null) {
			return new ASActor {
				Id = user.Uri ?? throw new GracefulException("Remote user must have an URI")
			};
		}

		var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.User == user);
		var keypair = await db.UserKeypairs.FirstOrDefaultAsync(p => p.User == user);

		if (keypair == null) throw new GracefulException("User has no keypair");

		var id = $"https://{config.Value.WebDomain}/users/{user.Id}";
		var type = Constants.SystemUsers.Contains(user.UsernameLower)
			? "Application"
			: user.IsBot
				? "Service"
				: "Person";

		return new ASActor {
			Id = id,
			Type = type,
			Inbox = new ASLink($"{id}/inbox"),
			Outbox = new ASCollection<ASObject>($"{id}/outbox"),
			Followers = new ASCollection<ASObject>($"{id}/followers"),
			Following = new ASCollection<ASObject>($"{id}/following"),
			SharedInbox = new ASLink($"https://{config.Value.WebDomain}/inbox"),
			Url = new ASLink($"https://{config.Value.WebDomain}/@{user.Username}"),
			Username = user.Username,
			DisplayName = user.Name ?? user.Username,
			Summary = profile?.Description != null ? await MfmConverter.FromHtmlAsync(profile.Description) : null,
			MkSummary = profile?.Description,
			IsCat = user.IsCat,
			IsDiscoverable = user.IsExplorable,
			IsLocked = user.IsLocked,
			Endpoints = new ASEndpoints {
				SharedInbox = new LDIdObject($"https://{config.Value.WebDomain}/inbox")
			},
			PublicKey = new ASPublicKey {
				Id        = $"{id}#main-key",
				Owner     = new LDIdObject(id),
				PublicKey = keypair.PublicKey,
				Type      = "Key"
			}
		};
	}
}