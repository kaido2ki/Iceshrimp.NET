using AngleSharp.Text;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Renderers.ActivityPub;

public class APUserRenderer(IOptions<Config.InstanceSection> config, DatabaseContext db) {
	public async Task<ASActor> Render(User user) {
		if (user.Host != null) throw new Exception("Refusing to render remote user");

		var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
		var keypair = await db.UserKeypairs.FirstOrDefaultAsync(p => p.UserId == user.Id);

		if (keypair == null) throw new Exception("User has no keypair");

		var id = $"https://{config.Value.WebDomain}/users/{user.Id}";
		var type = Constants.SystemUsers.Contains(user.UsernameLower)
			? "Application"
			: user.IsBot
				? "Service"
				: "Person";

		return new ASActor {
			Id             = id,
			Type           = type,
			Inbox          = new ASLink($"{id}/inbox"),
			Outbox         = new ASCollection<ASActivity>($"{id}/outbox"),
			Followers      = new ASCollection<ASActor>($"{id}/followers"),
			Following      = new ASCollection<ASActor>($"{id}/following"),
			SharedInbox    = new ASLink($"https://{config.Value.WebDomain}/inbox"),
			Url            = new ASLink($"https://{config.Value.WebDomain}/@{user.Username}"),
			Username       = user.Username,
			DisplayName    = user.Name ?? user.Username,
			Summary        = profile?.Description != null ? "Not implemented" : null, //TODO: convert to html
			MkSummary      = profile?.Description,
			IsCat          = user.IsCat,
			IsDiscoverable = user.IsExplorable,
			IsLocked       = user.IsLocked,
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