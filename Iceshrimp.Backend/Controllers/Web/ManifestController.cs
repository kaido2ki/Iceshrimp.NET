using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Web.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiExplorerSettings(IgnoreApi = true)]
[ApiController]
[EnableRateLimiting("sliding")]
[Route("/manifest.webmanifest")]
[Produces(MediaTypeNames.Application.Json)]
public class ManifestController(IOptions<Config.InstanceSection> config, MetaService metaSvc) : ControllerBase
{
	[HttpGet]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<WebManifest> GetWebManifestAsync()
	{
		var name = await metaSvc.GetAsync(MetaEntity.InstanceName);

		return new WebManifest
		{
			Name = name ?? config.Value.AccountDomain,
			ShortName = config.Value.AccountDomain
		};
	}
}