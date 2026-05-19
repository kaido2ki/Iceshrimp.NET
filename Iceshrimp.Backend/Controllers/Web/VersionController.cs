using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Shared.Helpers;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Authenticate]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/version")]
[Produces(MediaTypeNames.Application.Json)]
[EnableCors("iceshrimp")]
public class VersionController : ControllerBase
{
	/// <summary>
	/// Get version
	/// </summary>
	/// <response code="200">Version information</response>
	[HttpGet]
	[ProducesResults(HttpStatusCode.OK)]
	public VersionResponse GetVersion()
	{
		var version = VersionHelpers.VersionInfo.Value;
		return new VersionResponse
		{
			Codename   = version.Codename,
			CommitHash = version.CommitHash,
			Edition    = version.Edition,
			Version    = version.Version,
			RawVersion = version.RawVersion,
			ReleaseUrl = $"{Constants.RepositoryUrl}/releases/tag/v{version.RawVersion}"
		};
	}
}