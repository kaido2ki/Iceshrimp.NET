using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityPub;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
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

		//TODO: resolve anything related to the note as well (reply thread, attachments, emoji, etc)

		var dbNote = new Note {
			Id     = IdHelpers.GenerateSlowflakeId(),
			Uri    = note.Id,
			Url    = note.Url?.Id,                   //FIXME: this doesn't seem to work yet
			Text   = note.MkContent ?? note.Content, //TODO: html-to-mfm
			UserId = user.Id,
			CreatedAt = note.PublishedAt?.ToUniversalTime() ??
			            throw new Exception("Missing or invalid PublishedAt field"),
			UserHost   = user.Host,
			Visibility = Note.NoteVisibility.Public //TODO: parse to & cc fields
		};

		await db.Notes.AddAsync(dbNote);
		await db.SaveChangesAsync();
		logger.LogDebug("Note {id} created successfully", dbNote.Id);
	}
}