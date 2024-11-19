using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static Iceshrimp.Backend.Core.Federation.ActivityPub.UserResolver;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Authenticate]
[Authorize]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/migration")]
[Produces(MediaTypeNames.Application.Json)]
public class MigrationController(
	DatabaseContext db,
	UserService userSvc,
	ActivityPub.UserResolver userResolver,
	IOptions<Config.InstanceSection> config
) : ControllerBase
{
	[HttpGet]
	[ProducesResults(HttpStatusCode.OK)]
	public MigrationSchemas.MigrationStatusResponse GetMigrationStatus()
	{
		var user = HttpContext.GetUserOrFail();
		return new MigrationSchemas.MigrationStatusResponse
		{
			Aliases = user.AlsoKnownAs ?? [], MovedTo = user.MovedToUri
		};
	}

	[HttpPost("aliases")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task AddAlias(MigrationSchemas.MigrationRequest rq)
	{
		var   user      = HttpContext.GetUserOrFail();
		User? aliasUser = null;

		if (rq.UserId is not null)
			aliasUser = await db.Users.IncludeCommonProperties().Where(p => p.Id == rq.UserId).FirstOrDefaultAsync();
		if (rq.UserUri is not null)
			aliasUser ??= await userResolver.ResolveOrNullAsync(rq.UserUri, EnforceUriFlags);
		if (aliasUser is null)
			throw GracefulException.NotFound("Alias user not found or not specified");

		await userSvc.AddAliasAsync(user, aliasUser);
	}

	[HttpDelete("aliases")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task RemoveAlias(MigrationSchemas.MigrationRequest rq)
	{
		var user     = HttpContext.GetUserOrFail();
		var aliasUri = rq.UserUri;

		if (rq.UserId is not null)
		{
			aliasUri ??= await db.Users.IncludeCommonProperties()
			                     .Where(p => p.Id == rq.UserId)
			                     .FirstOrDefaultAsync()
			                     .ContinueWithResultAsync(p => p is null
				                                              ? null
				                                              : p.Uri ?? p.GetPublicUri(config.Value));
		}

		if (aliasUri is null) throw GracefulException.NotFound("Alias user not found");
		await userSvc.RemoveAliasAsync(user, aliasUri);
	}

	[HttpPost("move")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task MoveTo(MigrationSchemas.MigrationRequest rq)
	{
		var   user       = HttpContext.GetUserOrFail();
		User? targetUser = null;

		if (rq.UserId is not null)
			targetUser = await db.Users.IncludeCommonProperties().Where(p => p.Id == rq.UserId).FirstOrDefaultAsync();
		if (rq.UserUri is not null)
			targetUser ??= await userResolver.ResolveOrNullAsync(rq.UserUri, EnforceUriFlags);
		if (targetUser is null)
			throw GracefulException.NotFound("Target user not found");

		await userSvc.MoveToUserAsync(user, targetUser);
	}

	[HttpDelete("move")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task UndoMove()
	{
		var user = HttpContext.GetUserOrFail();
		await userSvc.UndoMoveAsync(user);
	}
}