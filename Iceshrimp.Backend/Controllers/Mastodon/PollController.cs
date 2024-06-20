using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Route("/api/v1/polls/{id}")]
[Authenticate("read:statuses")]
[EnableCors("mastodon")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
public class PollController(
	DatabaseContext db,
	PollRenderer pollRenderer,
	PollService pollSvc,
	IOptionsSnapshot<Config.SecuritySection> security
) : ControllerBase
{
	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PollEntity))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetPoll(string id)
	{
		var user = HttpContext.GetUser();
		if (security.Value.PublicPreview == Enums.PublicPreview.Lockdown && user == null)
			throw GracefulException.Forbidden("Public preview is disabled on this instance");

		var note = await db.Notes.Where(p => p.Id == id).EnsureVisibleFor(user).FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();
		var poll = await db.Polls.Where(p => p.Note == note).FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();
		var res = await pollRenderer.RenderAsync(poll, user);
		return Ok(res);
	}

	[HttpPost("votes")]
	[Authorize("read:statuses")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PollEntity))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> VotePoll(string id, [FromHybrid] PollSchemas.PollVoteRequest request)
	{
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes.Where(p => p.Id == id)
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();

		var poll = await db.Polls.Where(p => p.Note == note).FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();

		if (poll.ExpiresAt < DateTime.UtcNow)
			throw GracefulException.BadRequest("This poll is expired");

		var            existingVotes = await db.PollVotes.Where(p => p.User == user && p.Note == note).ToListAsync();
		List<PollVote> votes         = [];
		if (!poll.Multiple)
		{
			if (existingVotes.Count != 0)
				throw GracefulException.BadRequest("You already voted on this poll");
			if (request.Choices is not [var choice])
				throw GracefulException.BadRequest("You may only vote for one option");
			if (choice >= poll.Choices.Count)
				throw GracefulException.BadRequest($"This poll only has {poll.Choices.Count} options");

			var vote = new PollVote
			{
				Id        = IdHelpers.GenerateSlowflakeId(),
				CreatedAt = DateTime.UtcNow,
				User      = user,
				Note      = note,
				Choice    = choice
			};

			await db.AddAsync(vote);
			votes.Add(vote);
		}
		else
		{
			foreach (var choice in request.Choices.Except(existingVotes.Select(p => p.Choice)))
			{
				if (choice >= poll.Choices.Count)
					throw GracefulException.BadRequest($"This poll only has {poll.Choices.Count} options");

				var vote = new PollVote
				{
					Id        = IdHelpers.GenerateSlowflakeId(),
					CreatedAt = DateTime.UtcNow,
					User      = user,
					Note      = note,
					Choice    = choice
				};

				await db.AddAsync(vote);
				votes.Add(vote);
			}
		}

		await db.SaveChangesAsync();

		foreach (var vote in votes)
			await pollSvc.RegisterPollVote(vote, poll, note, votes.IndexOf(vote) == 0);

		await db.ReloadEntityAsync(poll);
		var res = await pollRenderer.RenderAsync(poll, user);
		return Ok(res);
	}
}