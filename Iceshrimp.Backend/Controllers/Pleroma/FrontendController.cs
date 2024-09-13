using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Pleroma.Schemas;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Iceshrimp.Backend.Controllers.Pleroma;

[MastodonApiController]
[EnableCors("mastodon")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
public class FrontendController : ControllerBase
{
	[HttpGet("/api/pleroma/frontend_configurations")]
	[ProducesResults(HttpStatusCode.OK)]
	public FrontendConfigurationsResponse GetFrontendConfigurations()
	{
		return new FrontendConfigurationsResponse();
	}
}