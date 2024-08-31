using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Pleroma.Schemas;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Configuration;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Pleroma;

[MastodonApiController]
[EnableCors("mastodon")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
public class FrontendController : ControllerBase
{
	[HttpGet("/api/v1/pleroma/frontend_configurations")]
	[ProducesResults(HttpStatusCode.OK)]
	public FrontendConfigurationsResponse GetFrontendConfigurations([FromServices] IOptionsSnapshot<Config> config)
	{
		return new FrontendConfigurationsResponse();
	}
}
