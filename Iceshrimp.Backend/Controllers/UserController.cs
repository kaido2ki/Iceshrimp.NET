using Iceshrimp.Backend.Controllers.Renderers;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[Tags("User")]
[Produces("application/json")]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/v1/user/{id}")]
public class UserController(DatabaseContext db) : Controller
{
	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetUser(string id)
	{
		var user = await db.Users.FirstOrDefaultAsync(p => p.Id == id);
		if (user == null) return NotFound();
		return Ok(user);
	}

	[HttpGet("notes")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TimelineResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetUserNotes(string id)
	{
		var user = await db.Users.FirstOrDefaultAsync(p => p.Id == id);
		if (user == null) return NotFound();

		var limit = 10;
		var notes = db.Notes
		              .Include(p => p.User)
		              .Where(p => p.UserId == id)
		              .HasVisibility(Note.NoteVisibility.Public)
		              .OrderByDescending(p => p.Id)
		              .Take(limit)
		              .ToList();

		return Ok(TimelineRenderer.Render(notes, limit));
	}
}