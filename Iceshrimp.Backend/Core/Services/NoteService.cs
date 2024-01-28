using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityPub;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class NoteService(
	ILogger<NoteService> logger,
	DatabaseContext db,
	UserResolver userResolver,
	IOptions<Config.InstanceSection> config,
	UserService userSvc,
	ActivityFetcherService fetchSvc,
	ActivityDeliverService deliverSvc,
	NoteRenderer noteRenderer,
	UserRenderer userRenderer
) {
	private readonly List<string> _resolverHistory = [];
	private          int          _recursionLimit  = 100;

	public async Task<Note> CreateNoteAsync(User user, Note.NoteVisibility visibility, string? text = null,
	                                        string? cw = null, Note? reply = null, Note? renote = null) {
		var actor = await userRenderer.RenderAsync(user);

		var note = new Note {
			Id         = IdHelpers.GenerateSlowflakeId(),
			Text       = text,
			Cw         = cw,
			Reply      = reply,
			Renote     = renote,
			UserId     = user.Id,
			CreatedAt  = DateTime.UtcNow,
			UserHost   = null,
			Visibility = visibility
		};
		await db.AddAsync(note);
		await db.SaveChangesAsync();

		var obj      = noteRenderer.Render(note);
		var activity = ActivityRenderer.RenderCreate(obj, actor);

		await deliverSvc.DeliverToFollowersAsync(activity, user);

		return note;
	}

	public async Task<Note?> ProcessNoteAsync(ASNote note, ASActor actor) {
		if (await db.Notes.AnyAsync(p => p.Uri == note.Id)) {
			logger.LogDebug("Note '{id}' already exists, skipping", note.Id);
			return null;
		}

		if (_resolverHistory is [])
			_resolverHistory.Add(note.Id);
		logger.LogDebug("Creating note: {id}", note.Id);

		var user = await userResolver.ResolveAsync(actor.Id);
		logger.LogDebug("Resolved user to {userId}", user.Id);

		// Validate note
		if (note.AttributedTo is not { Count: 1 } || note.AttributedTo[0].Id != user.Uri)
			throw GracefulException.UnprocessableEntity("User.Uri doesn't match Note.AttributedTo");
		if (user.Uri == null)
			throw GracefulException.UnprocessableEntity("User.Uri is null");
		if (new Uri(note.Id).IdnHost != new Uri(user.Uri).IdnHost)
			throw GracefulException.UnprocessableEntity("User.Uri host doesn't match Note.Id host");
		if (!note.Id.StartsWith("https://"))
			throw GracefulException.UnprocessableEntity("Note.Id schema is invalid");
		if (note.Url?.Link != null && !note.Url.Link.StartsWith("https://"))
			throw GracefulException.UnprocessableEntity("Note.Url schema is invalid");
		if (note.PublishedAt is null or { Year: < 2007 } || note.PublishedAt > DateTime.Now + TimeSpan.FromDays(3))
			throw GracefulException.UnprocessableEntity("Note.PublishedAt is nonsensical");
		if (user.IsSuspended)
			throw GracefulException.Forbidden("User is suspended");

		//TODO: validate AP object type
		//TODO: resolve anything related to the note as well (attachments, emoji, etc)

		var dbNote = new Note {
			Id     = IdHelpers.GenerateSlowflakeId(),
			Uri    = note.Id,
			Url    = note.Url?.Id, //FIXME: this doesn't seem to work yet
			Text   = note.MkContent ?? await MfmHelpers.FromHtmlAsync(note.Content),
			UserId = user.Id,
			CreatedAt = note.PublishedAt?.ToUniversalTime() ??
			            throw GracefulException.UnprocessableEntity("Missing or invalid PublishedAt field"),
			UserHost   = user.Host,
			Visibility = note.GetVisibility(actor),
			Reply      = note.InReplyTo?.Id != null ? await ResolveNoteAsync(note.InReplyTo.Id) : null
			//TODO: parse to fields for specified visibility & mentions
		};

		await db.Notes.AddAsync(dbNote);
		await db.SaveChangesAsync();
		logger.LogDebug("Note {id} created successfully", dbNote.Id);
		return dbNote;
	}

	public async Task<Note?> ResolveNoteAsync(string uri) {
		//TODO: is this enough to prevent DoS attacks?
		if (_recursionLimit-- <= 0)
			throw GracefulException.UnprocessableEntity("Refusing to resolve threads this long");
		if (_resolverHistory.Contains(uri))
			throw GracefulException.UnprocessableEntity("Refusing to resolve circular threads");
		_resolverHistory.Add(uri);

		var note = uri.StartsWith($"https://{config.Value.WebDomain}/notes/")
			? await db.Notes.FirstOrDefaultAsync(p => p.Id ==
			                                          uri.Substring($"https://{config.Value.WebDomain}/notes/".Length))
			: await db.Notes.FirstOrDefaultAsync(p => p.Uri == uri);
		if (note != null) return note;

		//TODO: should we fall back to a regular user's keypair if fetching with instance actor fails & a local user is following the actor?
		var instanceActor        = await userSvc.GetInstanceActorAsync();
		var instanceActorKeypair = await db.UserKeypairs.FirstAsync(p => p.User == instanceActor);
		var fetchedNote          = await fetchSvc.FetchNoteAsync(uri, instanceActor, instanceActorKeypair);
		if (fetchedNote?.AttributedTo is not [{ Id: not null } attrTo]) {
			logger.LogDebug("Invalid Note.AttributedTo, skipping");
			return null;
		}

		//TODO: we don't need to fetch the actor every time, we can use userResolver here
		var actor = await fetchSvc.FetchActorAsync(attrTo.Id, instanceActor, instanceActorKeypair);

		try {
			return await ProcessNoteAsync(fetchedNote, actor);
		}
		catch (Exception e) {
			logger.LogDebug("Failed to create resolved note: {error}", e.Message);
			return null;
		}
	}
}