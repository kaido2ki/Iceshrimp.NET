using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Federation.WebFinger;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[Tags("Federation")]
[Route("/nodeinfo")]
[EnableCors("well-known")]
public class NodeInfoController(IOptions<Config.InstanceSection> config) : Controller {
	[HttpGet("2.1")]
	[HttpGet("2.0")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WebFingerResponse))]
	public IActionResult GetNodeInfo() {
		var instance = config.Value;
		var result = new NodeInfoResponse {
			Version = instance.Version,
			Software = new NodeInfoResponse.NodeInfoSoftware {
				Version  = instance.Version,
				Name     = "Iceshrimp.NET",
				Homepage = new Uri("https://iceshrimp.dev/iceshrimp/Iceshrimp.NET"),
				Repository = Request.Path.Value?.EndsWith("2.1") ?? false
					? new Uri("https://iceshrimp.dev/iceshrimp/Iceshrimp.NET")
					: null
			},
			Protocols = ["activitypub"],
			Services = new NodeInfoResponse.NodeInfoServices {
				Inbound  = [],
				Outbound = ["atom1.0", "rss2.0"]
			},
			Usage = new NodeInfoResponse.NodeInfoUsage {
				//FIXME Implement members
				Users = new NodeInfoResponse.NodeInfoUsers {
					Total          = 0,
					ActiveMonth    = 0,
					ActiveHalfYear = 0
				},
				LocalComments = 0,
				LocalPosts    = 0
			},
			Metadata = new NodeInfoResponse.NodeInfoMetadata {
				//FIXME Implement members
				NodeName        = "Iceshrimp.NET",
				NodeDescription = "An Iceshrimp.NET instance",
				Maintainer = new NodeInfoResponse.Maintainer {
					Name  = "todo",
					Email = "todo"
				},
				Languages                  = [],
				TosUrl                     = "todo",
				RepositoryUrl              = new Uri("https://iceshrimp.dev/iceshrimp/Iceshrimp.NET"),
				FeedbackUrl                = new Uri("https://iceshrimp.dev/iceshrimp/Iceshrimp.NET"),
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

		return Ok(result);
	}
}