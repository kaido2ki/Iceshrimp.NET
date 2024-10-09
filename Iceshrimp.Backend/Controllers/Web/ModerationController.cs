using System.Net;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Web;

[Authenticate]
[Authorize("role:moderator")]
[ApiController]
[Route("/api/iceshrimp/moderation")]
public class ModerationController(DatabaseContext db, NoteService noteSvc, UserService userSvc) : ControllerBase
{
	[HttpPost("notes/{id}/delete")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task DeleteNote(string id)
	{
		var note = await db.Notes.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id) ??
		           throw GracefulException.NotFound("Note not found");

		await noteSvc.DeleteNoteAsync(note);
	}

	[HttpPost("users/{id}/suspend")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task SuspendUser(string id)
	{
		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id) ??
		           throw GracefulException.NotFound("User not found");

		await userSvc.SuspendUserAsync(user);
	}
	
	[HttpPost("users/{id}/unsuspend")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task UnsuspendUser(string id)
	{
		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id) ??
		           throw GracefulException.NotFound("User not found");
		
		await userSvc.UnsuspendUserAsync(user);
	}
	
	[HttpPost("users/{id}/delete")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task DeleteUser(string id)
	{
		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id) ??
		           throw GracefulException.NotFound("User not found");

		await userSvc.DeleteUserAsync(user);
	}
	
	[HttpPost("users/{id}/purge")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task PurgeUser(string id)
	{
		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id) ??
		           throw GracefulException.NotFound("User not found");

		await userSvc.PurgeUserAsync(user);
	}
}