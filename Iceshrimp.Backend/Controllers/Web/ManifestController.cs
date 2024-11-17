using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Web.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[EnableRateLimiting("sliding")]
[Route("/manifest.webmanifest")]
[Produces(MediaTypeNames.Application.Json)]
public class ManifestController(IOptions<Config.InstanceSection> config) : ControllerBase
{
	[HttpGet]
	[ProducesResults(HttpStatusCode.OK)]
	public WebManifest GetWebManifest()
	{
		return new WebManifest
		{
			Name = config.Value.AccountDomain, ShortName = config.Value.AccountDomain
		};
	}
}


