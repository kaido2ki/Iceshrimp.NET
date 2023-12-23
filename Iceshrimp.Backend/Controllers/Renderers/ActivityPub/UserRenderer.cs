using AngleSharp.Text;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Renderers.ActivityPub;

public static class ActivityPubUserRenderer {
	public static async Task<ASActor> Render(User user) {
		if (user.Host != null) throw new Exception();

		var db      = new DatabaseContext();
		var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

		var id = $"{Config.Instance.Url}/users/{user.Id}";
		var type = Constants.SystemUsers.Contains(user.UsernameLower)
			? "Application"
			: user.IsBot
				? "Service"
				: "Person";

		return new ASActor {
			Id   = id,
			Type = [type],
			//Inbox = $"{id}/inbox",
			//Outbox = $"{id}/outbox",
			//Followers = $"{id}/followers",
			//Following = $"{id}/following",
			//SharedInbox = $"{Config.Instance.Url}/inbox",
			//Endpoints = new Dictionary<string, string> { { "SharedInbox", $"{Config.Instance.Url}/inbox" } },
			Url            = new ASLink($"{Config.Instance.Url}/@{user.Username}"),
			Username       = user.Username,
			DisplayName    = user.Name ?? user.Username,
			Summary        = profile?.Description != null ? "Not implemented" : null, //TODO: convert to html
			MkSummary      = profile?.Description,
			IsCat          = user.IsCat,
			IsDiscoverable = user.IsExplorable,
			IsLocked       = user.IsLocked
		};
	}
}