using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Federation.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Federation.WebFinger;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Federation;

[FederationApiController]
[Route("/nodeinfo")]
[EnableCors("well-known")]
[Produces(MediaTypeNames.Application.Json)]
public class NodeInfoController(IOptions<Config.InstanceSection> config, DatabaseContext db) : ControllerBase
{
	[HttpGet("2.1")]
	[HttpGet("2.0")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<NodeInfoResponse> GetNodeInfo()
	{
		var cutoffMonth    = DateTime.UtcNow - TimeSpan.FromDays(30);
		var cutoffHalfYear = DateTime.UtcNow - TimeSpan.FromDays(180);
		var instance       = config.Value;
		var totalUsers =
			await db.Users.LongCountAsync(p => p.IsLocalUser && !Constants.SystemUsers.Contains(p.UsernameLower));
		var activeMonth =
			await db.Users.LongCountAsync(p => p.IsLocalUser &&
			                                   !Constants.SystemUsers.Contains(p.UsernameLower) &&
			                                   p.LastActiveDate > cutoffMonth);
		var activeHalfYear =
			await db.Users.LongCountAsync(p => p.IsLocalUser &&
			                                   !Constants.SystemUsers.Contains(p.UsernameLower) &&
			                                   p.LastActiveDate > cutoffHalfYear);
		var localPosts = await db.Notes.LongCountAsync(p => p.UserHost == null);

		return new NodeInfoResponse
		{
			Version = Request.Path.Value?.EndsWith("2.1") ?? false ? "2.1" : "2.0",
			Software = new NodeInfoResponse.NodeInfoSoftware
			{
				Name     = "Iceshrimp.NET",
				Version  = instance.Version,
				Codename = instance.Codename,
				Edition  = instance.Edition,
				Homepage = new Uri(Constants.ProjectHomepageUrl),
				Repository = Request.Path.Value?.EndsWith("2.1") ?? false
					? new Uri(Constants.RepositoryUrl)
					: null
			},
			Protocols = ["activitypub"],
			Services  = new NodeInfoResponse.NodeInfoServices { Inbound = [], Outbound = ["atom1.0", "rss2.0"] },
			Usage = new NodeInfoResponse.NodeInfoUsage
			{
				//FIXME Implement members
				Users = new NodeInfoResponse.NodeInfoUsers
				{
					Total          = totalUsers,
					ActiveMonth    = activeMonth,
					ActiveHalfYear = activeHalfYear
				},
				LocalComments = 0,
				LocalPosts    = localPosts
			},
			Metadata = new NodeInfoResponse.NodeInfoMetadata
			{
				//FIXME Implement members
				NodeName                   = "Iceshrimp.NET",
				NodeDescription            = "An Iceshrimp.NET instance",
				Maintainer                 = new NodeInfoResponse.Maintainer { Name = "todo", Email = "todo" },
				Languages                  = [],
				TosUrl                     = "todo",
				RepositoryUrl              = new Uri(Constants.RepositoryUrl),
				FeedbackUrl                = new Uri(Constants.IssueTrackerUrl),
				ThemeColor                 = "#000000",
				DisableRegistration        = true,
				DisableLocalTimeline       = false,
				DisableRecommendedTimeline = false,
				DisableGlobalTimeline      = false,
				EmailRequiredForSignup     = false,
				PostEditing                = false,
				PostImports                = false,
				EnableHCaptcha             = false,
				EnableRecaptcha            = false,
				MaxNoteTextLength          = 0,
				MaxCaptionTextLength       = 0,
				EnableGithubIntegration    = false,
				EnableDiscordIntegration   = false,
				EnableEmail                = false
			},
			OpenRegistrations = false
		};
	}
}