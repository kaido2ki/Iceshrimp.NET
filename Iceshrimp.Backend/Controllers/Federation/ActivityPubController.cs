using System.Net;
using System.Text;
using Iceshrimp.Backend.Controllers.Federation.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Queues;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
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
	IOptions<Config.InstanceSection> config,
	IOptionsSnapshot<Config.SecuritySection> security
) : ControllerBase, IScopedService
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
	[OverrideResultType<ASActivity>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<JObject> GetNoteActivity(string id)
	{
		var actor = HttpContext.GetActor();

		var note = await db.Notes
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(actor)
		                   .Where(p => p.Id == id && p.UserHost == null)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("Note not found");

		var noteActor = userRenderer.RenderLite(note.User);
		ASActivity activity = note is { IsPureRenote: true, Renote: not null }
			? ActivityPub.ActivityRenderer.RenderAnnounce(noteRenderer.RenderLite(note.Renote),
			                                              note.GetPublicUri(config.Value), noteActor, note.Visibility,
			                                              note.User.GetPublicUri(config.Value) + "/followers")
			: ActivityPub.ActivityRenderer.RenderCreate(await noteRenderer.RenderAsync(note), noteActor);

		return activity.Compact();
	}

	[HttpGet("/notes/{id}/replies")]
	[AuthorizedFetch]
	[OverrideResultType<ASOrderedCollection>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<JObject> GetNoteReplies(string id)
	{
		var actor = HttpContext.GetActor();
		var note = await db.Notes
		                   .EnsureVisibleFor(actor)
		                   .FirstOrDefaultAsync(p => p.Id == id && p.User.IsLocalUser) ??
		           throw GracefulException.NotFound("Note not found");

		var replies = await db.Notes
		                      .Where(p => p.ReplyId == id)
		                      .EnsureVisibleFor(actor)
		                      .OrderByDescending(p => p.Id)
		                      .Select(p => new Note { Id = p.Id, Uri = p.Uri })
		                      .ToListAsync();

		var rendered = replies.Select(noteRenderer.RenderLite).Cast<ASObject>().ToList();
		var res = new ASOrderedCollection
		{
			Id         = $"{note.GetPublicUri(config.Value)}/replies",
			TotalItems = (ulong)rendered.Count,
			Items      = rendered
		};

		return res.Compact();
	}

	[HttpGet("/threads/{id}")]
	[AuthorizedFetch]
	[OverrideResultType<ASOrderedCollection>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<JObject> GetThread(string id)
	{
		var actor = HttpContext.GetActor();
		var thread = await db.NoteThreads
		                     .Include(p => p.User)
		                     .FirstOrDefaultAsync(p => p.Id == id && p.User != null && p.User.IsLocalUser) ??
		           throw GracefulException.NotFound("Thread not found");

		var notes = await db.Notes
		                      .Where(p => p.ThreadId == id)
		                      .EnsureVisibleFor(actor)
		                      .OrderByDescending(p => p.Id)
		                      .Select(p => new Note { Id = p.Id, Uri = p.Uri })
		                      .ToListAsync();

		var rendered = notes.Select(noteRenderer.RenderLite).Cast<ASObject>().ToList();
		var res = new ASOrderedCollection
		{
			Id         = thread.GetPublicUri(config.Value),
			AttributedTo = [new ASObjectBase(thread.User!.GetPublicUri(config.Value))],
			TotalItems = (ulong)rendered.Count,
			Items      = rendered
		};

		return res.Compact();
	}

	[HttpGet("/users/{id}")]
	[AuthorizedFetch]
	[OutputCache(PolicyName = "federation")]
	[MediaTypeRouteFilter("application/activity+json", "application/ld+json")]
	[OverrideResultType<ASActor>]
	[ProducesResults(HttpStatusCode.OK, HttpStatusCode.MovedPermanently)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<ActionResult<JObject>> GetUser(string id)
	{
		var user = await db.Users
		                   .IncludeCommonProperties()
		                   .Include(p => p.Avatar)
		                   .Include(p => p.Banner)
		                   .FirstOrDefaultAsync(p => p.Id == id);

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
	[OutputCache(PolicyName = "federation")]
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

	[HttpGet("/users/{id}/outbox")]
	[AuthorizedFetch]
	[OutputCache(PolicyName = "federation")]
	[OverrideResultType<ASOrderedCollectionPage>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<JObject> GetUserOutbox(string id)
	{
		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id && p.IsLocalUser);
		if (user == null) throw GracefulException.NotFound("User not found");

		var res = new ASOrderedCollection
		{
			Id = $"{user.GetPublicUri(config.Value)}/outbox",
			First = new ASOrderedCollectionPage($"{user.GetPublicUri(config.Value)}/outbox/page"),
		};

		return res.Compact();
	}

	[HttpGet("/users/{id}/outbox/page")]
	[AuthorizedFetch]
	[OutputCache(PolicyName = "federation")]
	[OverrideResultType<ASOrderedCollectionPage>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<JObject> GetUserOutboxPage(string id, [FromQuery] string? maxId)
	{
		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id && p.IsLocalUser);
		if (user == null) throw GracefulException.NotFound("User not found");

		var actor = HttpContext.GetActor();
		if (actor == null && security.Value.PublicPreview == Enums.PublicPreview.Lockdown)
			throw new PublicPreviewDisabledException();

		var notes = await db.Notes.Where(p => p.UserId == id)
		                    .Include(p => p.User)
		                    .Include(p => p.Renote)
		                    .EnsureVisibleFor(actor)
		                    .Paginate(new PaginationQuery { MaxId = maxId }, 20, 20)
		                    .ToArrayAsync();

		var noteActor = userRenderer.RenderLite(user);
		var rendered = notes.Select(note => note is { IsPureRenote: true, Renote: not null }
			                            ? (ASObject)ActivityPub.ActivityRenderer.RenderAnnounce(noteRenderer.RenderLite(note.Renote),
					                             note.GetPublicUri(config.Value), noteActor,
					                             note.Visibility,
					                             note.User.GetPublicUri(config.Value) + "/followers")
			                            : ActivityPub.ActivityRenderer.RenderCreate(noteRenderer.RenderLite(note), noteActor))
		                    .ToList();

		var last = notes.LastOrDefault();
		var res = new ASOrderedCollectionPage
		{
			Id = $"{user.GetPublicUri(config.Value)}/outbox/page{(maxId != null ? $"?maxId={maxId}" : "")}",
			Next = last != null ? new ASOrderedCollectionPage($"{user.GetPublicUri(config.Value)}/outbox/page?maxId={last.Id}") : null,
			Items = rendered
		};

		return res.Compact();
	}

	[HttpGet("/@{acct}")]
	[AuthorizedFetch]
	[OutputCache(PolicyName = "federation")]
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
			                                                   p.Host == split[1].ToPunycodeLower());

			if (remoteUser?.Uri != null)
				return RedirectPermanent(remoteUser.Uri);
			throw GracefulException.NotFound("User not found");
		}

		var user = await db.Users
		                   .IncludeCommonProperties()
		                   .Include(p => p.Avatar)
		                   .Include(p => p.Banner)
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
	[OutputCache(PolicyName = "federation")]
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
			Name  = $":{emoji.Name}:",
			Image = new ASImage { Url = new ASLink(emoji.RawPublicUrl), MediaType = emoji.Type }
		};

		return LdHelpers.Compact(rendered);
	}
}
