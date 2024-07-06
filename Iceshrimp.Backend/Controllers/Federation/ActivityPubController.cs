using System.Net;
using System.Text;
using Iceshrimp.Backend.Controllers.Federation.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Queues;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Backend.Controllers.Federation;

[FederationApiController]
[FederationSemaphore]
[UseNewtonsoftJson]
[ProducesActivityStreamsPayload]
public class ActivityPubController(
	DatabaseContext db,
	QueueService queues,
	ActivityPub.NoteRenderer noteRenderer,
	ActivityPub.UserRenderer userRenderer,
	IOptions<Config.InstanceSection> config
) : ControllerBase
{
	[HttpGet("/notes/{id}")]
	[AuthorizedFetch]
	[MediaTypeRouteFilter("application/activity+json", "application/ld+json")]
	[OverrideResultType<ASNote>]
	[ProducesResults(HttpStatusCode.OK, HttpStatusCode.MovedPermanently)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<ActionResult<JObject>> GetNote(string id)
	{
		var actor = HttpContext.GetActor();
		var note = await db.Notes
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(actor)
		                   .FirstOrDefaultAsync(p => p.Id == id);
		if (note == null) throw GracefulException.NotFound("Note not found");
		if (note.User.IsRemoteUser)
			return RedirectPermanent(note.Uri ?? throw new Exception("Refusing to render remote note without uri"));
		var rendered = await noteRenderer.RenderAsync(note);
		return rendered.Compact();
	}

	[HttpGet("/notes/{id}/activity")]
	[AuthorizedFetch]
	[OverrideResultType<ASAnnounce>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<JObject> GetRenote(string id)
	{
		var actor = HttpContext.GetActor();

		var note = await db.Notes
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(actor)
		                   .Where(p => p.Id == id && p.UserHost == null && p.IsPureRenote && p.Renote != null)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("Note not found");

		return ActivityPub.ActivityRenderer
		                  .RenderAnnounce(noteRenderer.RenderLite(note.Renote!),
		                                  note.GetPublicUri(config.Value),
		                                  userRenderer.RenderLite(note.User),
		                                  note.Visibility,
		                                  note.User.GetPublicUri(config.Value) + "/followers")
		                  .Compact();
	}

	[HttpGet("/users/{id}")]
	[AuthorizedFetch]
	[MediaTypeRouteFilter("application/activity+json", "application/ld+json")]
	[OverrideResultType<ASActor>]
	[ProducesResults(HttpStatusCode.OK, HttpStatusCode.MovedPermanently)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<ActionResult<JObject>> GetUser(string id)
	{
		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id);
		if (user == null) throw GracefulException.NotFound("User not found");
		if (user.IsRemoteUser)
		{
			if (user.Uri != null)
				return RedirectPermanent(user.Uri);
			throw GracefulException.NotFound("User not found");
		}

		var rendered = await userRenderer.RenderAsync(user);
		return ((ASObject)rendered).Compact();
	}

	[HttpGet("/users/{id}/collections/featured")]
	[AuthorizedFetch]
	[OverrideResultType<ASOrderedCollection>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<JObject> GetUserFeatured(string id)
	{
		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id && p.IsLocalUser);
		if (user == null) throw GracefulException.NotFound("User not found");

		var pins = await db.UserNotePins.Where(p => p.User == user)
		                   .OrderByDescending(p => p.Id)
		                   .Include(p => p.Note.User.UserProfile)
		                   .Include(p => p.Note.Renote!.User.UserProfile)
		                   .Include(p => p.Note.Reply!.User.UserProfile)
		                   .Select(p => p.Note)
		                   .Take(10)
		                   .ToListAsync();

		var rendered = pins.Select(noteRenderer.RenderLite).ToList();
		var res = new ASOrderedCollection
		{
			Id         = $"{user.GetPublicUri(config.Value)}/collections/featured",
			TotalItems = (ulong)rendered.Count,
			Items      = rendered.Cast<ASObject>().ToList()
		};

		return res.Compact();
	}

	[HttpGet("/@{acct}")]
	[AuthorizedFetch]
	[MediaTypeRouteFilter("application/activity+json", "application/ld+json")]
	[OverrideResultType<ASActor>]
	[ProducesResults(HttpStatusCode.OK, HttpStatusCode.MovedPermanently)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<ActionResult<JObject>> GetUserByUsername(string acct)
	{
		var split = acct.Split('@');
		if (acct.Split('@').Length > 2) throw GracefulException.NotFound("User not found");
		if (split.Length == 2)
		{
			var remoteUser = await db.Users
			                         .IncludeCommonProperties()
			                         .FirstOrDefaultAsync(p => p.UsernameLower == split[0].ToLowerInvariant() &&
			                                                   p.Host == split[1].ToLowerInvariant().ToPunycode());

			if (remoteUser?.Uri != null)
				return RedirectPermanent(remoteUser.Uri);
			throw GracefulException.NotFound("User not found");
		}

		var user = await db.Users
		                   .IncludeCommonProperties()
		                   .FirstOrDefaultAsync(p => p.UsernameLower == acct.ToLowerInvariant() && p.IsLocalUser);

		if (user == null) throw GracefulException.NotFound("User not found");
		var rendered = await userRenderer.RenderAsync(user);
		return ((ASObject)rendered).Compact();
	}

	[HttpPost("/inbox")]
	[HttpPost("/users/{id}/inbox")]
	[InboxValidation]
	[EnableRequestBuffering(1024 * 1024)]
	[ConsumesActivityStreamsPayload]
	[ProducesResults(HttpStatusCode.Accepted)]
	public async Task<AcceptedResult> Inbox(string? id)
	{
		using var reader = new StreamReader(Request.Body, Encoding.UTF8, true, 1024, true);
		var       body   = await reader.ReadToEndAsync();
		Request.Body.Position = 0;

		await queues.InboxQueue.EnqueueAsync(new InboxJobData
		{
			Body                = body,
			InboxUserId         = id,
			AuthenticatedUserId = HttpContext.GetActor()?.Id
		});

		return Accepted();
	}

	[HttpGet("/emoji/{name}")]
	[AuthorizedFetch]
	[MediaTypeRouteFilter("application/activity+json", "application/ld+json")]
	[OverrideResultType<ASEmoji>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<ActionResult<JObject>> GetEmoji(string name)
	{
		var emoji = await db.Emojis.FirstOrDefaultAsync(p => p.Name == name && p.Host == null);
		if (emoji == null) throw GracefulException.NotFound("Emoji not found");

		var rendered = new ASEmoji
		{
			Id    = emoji.GetPublicUri(config.Value),
			Name  = emoji.Name,
			Image = new ASImage { Url = new ASLink(emoji.PublicUrl) }
		};

		return LdHelpers.Compact(rendered);
	}
}