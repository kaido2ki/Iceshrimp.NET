using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityPub;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

public class NoteService(ILogger<NoteService> logger, DatabaseContext db, UserResolver userResolver) {
	public async Task CreateNote(ASNote note, ASActor actor) {
		if (await db.Notes.AnyAsync(p => p.Uri == note.Id)) {
			logger.LogDebug("Note '{id}' already exists, skipping", note.Id);
			return;
		}

		logger.LogDebug("Creating note: {id}", note.Id);

		var user = await userResolver.Resolve(actor.Id);
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
		//TODO: parse note visibility
		//TODO: resolve anything related to the note as well (reply thread, attachments, emoji, etc)

		var dbNote = new Note {
			Id     = IdHelpers.GenerateSlowflakeId(),
			Uri    = note.Id,
			Url    = note.Url?.Id, //FIXME: this doesn't seem to work yet
			Text   = note.MkContent ?? await MfmHelpers.FromHtml(note.Content),
			UserId = user.Id,
			CreatedAt = note.PublishedAt?.ToUniversalTime() ??
			            throw GracefulException.UnprocessableEntity("Missing or invalid PublishedAt field"),
			UserHost   = user.Host,
			Visibility = Note.NoteVisibility.Public //TODO: parse to & cc fields
		};

		await db.Notes.AddAsync(dbNote);
		await db.SaveChangesAsync();
		logger.LogDebug("Note {id} created successfully", dbNote.Id);
	}
}