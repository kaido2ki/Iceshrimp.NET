using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/instance")]
[Produces(MediaTypeNames.Application.Json)]
public class InstanceController(
	DatabaseContext db,
	UserRenderer userRenderer,
	IOptions<Config.InstanceSection> instanceConfig,
	IOptions<Config.SecuritySection> securityConfig,
	MetaService meta
) : ControllerBase
{
	[HttpGet]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<InstanceResponse> GetInfo()
	{
		return new InstanceResponse
		{
			AccountDomain = instanceConfig.Value.AccountDomain,
			WebDomain     = instanceConfig.Value.WebDomain,
			Registration  = (Registrations)securityConfig.Value.Registrations,
			Name          = await meta.GetAsync(MetaEntity.InstanceName)
		};
	}

	[HttpGet("staff")]
	[Authenticate]
	[Authorize]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<StaffResponse> GetStaff()
	{
		var admins = db.Users
					   .Where(p => p.IsAdmin == true)
					   .OrderBy(p => p.UsernameLower);
		var adminList = await userRenderer.RenderManyAsync(admins)
										  .ToListAsync();

		var moderators = db.Users
						   .Where(p => p.IsAdmin == false && p.IsModerator == true)
						   .OrderBy(p => p.UsernameLower);
		var moderatorList = await userRenderer.RenderManyAsync(moderators)
											  .ToListAsync();

		return new StaffResponse { Admins = adminList, Moderators = moderatorList };
	}
}
