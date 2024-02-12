using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityPub;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Types;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

using MentionQuad =
	(List<string> mentionedUserIds,
	List<Note.MentionedUser> mentions,
	List<Note.MentionedUser> remoteMentions,
	Dictionary<(string usernameLower, string webDomain), string> splitDomainMapping);

[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor",
                 Justification = "We need IOptionsSnapshot for config hot reload")]
public class NoteService(
	ILogger<NoteService> logger,
	DatabaseContext db,
	UserResolver userResolver,
	IOptionsSnapshot<Config.InstanceSection> config,
	ActivityFetcherService fetchSvc,
	ActivityDeliverService deliverSvc,
	NoteRenderer noteRenderer,
	UserRenderer userRenderer,
	MentionsResolver mentionsResolver,
	MfmConverter mfmConverter
) {
	private readonly List<string> _resolverHistory = [];
	private          int          _recursionLimit  = 100;

	public async Task<Note> CreateNoteAsync(User user, Note.NoteVisibility visibility, string? text = null,
	                                        string? cw = null, Note? reply = null, Note? renote = null) {
		if (text?.Length > config.Value.CharacterLimit)
			throw GracefulException.BadRequest($"Text cannot be longer than {config.Value.CharacterLimit} characters");

		if (text is { Length: > 100000 })
			throw GracefulException.BadRequest("Text cannot be longer than 100.000 characters");

		var (mentionedUserIds, mentions, remoteMentions, splitDomainMapping) = await ResolveNoteMentionsAsync(text);

		if (text != null)
			text = await mentionsResolver.ResolveMentions(text, null, mentions, splitDomainMapping);

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
			Visibility = visibility,

			Mentions             = mentionedUserIds,
			VisibleUserIds       = visibility == Note.NoteVisibility.Specified ? mentionedUserIds : [],
			MentionedRemoteUsers = remoteMentions,
		};

		user.NotesCount++;
		await db.AddAsync(note);
		await db.SaveChangesAsync();

		var obj      = await noteRenderer.RenderAsync(note, mentions);
		var activity = ActivityRenderer.RenderCreate(obj, actor);

		var recipients = await db.Users
		                         .Where(p => mentionedUserIds.Contains(p.Id))
		                         .Select(p => new User {
			                         Host  = p.Host,
			                         Inbox = p.Inbox
		                         })
		                         .ToListAsync();

		if (note.Visibility == Note.NoteVisibility.Specified) {
			await deliverSvc.DeliverToAsync(activity, user, recipients);
		}
		else {
			await deliverSvc.DeliverToFollowersAsync(activity, user, recipients);
		}

		return note;
	}

	public async Task DeleteNoteAsync(ASTombstone note, ASActor actor) {
		// ReSharper disable once EntityFramework.NPlusOne.IncompleteDataQuery (it doesn't know about IncludeCommonProperties())
		var dbNote = await db.Notes.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Uri == note.Id);
		if (dbNote == null) {
			logger.LogDebug("Note '{id}' isn't known, skipping", note.Id);
			return;
		}

		var user = await userResolver.ResolveAsync(actor.Id);

		// ReSharper disable once EntityFramework.NPlusOne.IncompleteDataUsage (same reason as above)
		if (dbNote.User != user) {
			logger.LogDebug("Note '{id}' isn't owned by actor requesting its deletion, skipping", note.Id);
			return;
		}

		logger.LogDebug("Deleting note '{id}' owned by {userId}", note.Id, user.Id);

		user.NotesCount--;
		db.Remove(dbNote);

		await db.SaveChangesAsync();
	}

	public async Task<Note> ProcessNoteAsync(ASNote note, ASActor actor) {
		var dbHit = await db.Notes.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Uri == note.Id);

		if (dbHit != null) {
			logger.LogDebug("Note '{id}' already exists, skipping", note.Id);
			return dbHit;
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

		//TODO: resolve anything related to the note as well (attachments, emoji, etc)

		var (mentionedUserIds, mentions, remoteMentions, splitDomainMapping) = await ResolveNoteMentionsAsync(note);

		var dbNote = new Note {
			Id     = IdHelpers.GenerateSlowflakeId(),
			Uri    = note.Id,
			Url    = note.Url?.Id, //FIXME: this doesn't seem to work yet
			Text   = note.MkContent ?? await mfmConverter.FromHtmlAsync(note.Content, mentions),
			UserId = user.Id,
			CreatedAt = note.PublishedAt?.ToUniversalTime() ??
			            throw GracefulException.UnprocessableEntity("Missing or invalid PublishedAt field"),
			UserHost   = user.Host,
			Visibility = note.GetVisibility(actor),
			Reply      = note.InReplyTo?.Id != null ? await ResolveNoteAsync(note.InReplyTo.Id) : null
		};

		if (dbNote.Text is { Length: > 100000 })
			throw GracefulException.UnprocessableEntity("Content cannot be longer than 100.000 characters");

		if (dbNote.Text is not null) {
			dbNote.Mentions             = mentionedUserIds;
			dbNote.MentionedRemoteUsers = remoteMentions;
			if (dbNote.Visibility == Note.NoteVisibility.Specified) {
				var visibleUserIds = (await note.GetRecipients(actor)
				                                .Select(async p => await userResolver.ResolveAsync(p))
				                                .AwaitAllNoConcurrencyAsync())
				                     .Select(p => p.Id)
				                     .Concat(mentionedUserIds).ToList();
				if (dbNote.ReplyUserId != null)
					visibleUserIds.Add(dbNote.ReplyUserId);

				dbNote.VisibleUserIds = visibleUserIds.Distinct().ToList();
			}

			dbNote.Text = await mentionsResolver.ResolveMentions(dbNote.Text, dbNote.UserHost, remoteMentions,
			                                                     splitDomainMapping);
		}

		user.NotesCount++;
		await db.Notes.AddAsync(dbNote);
		await db.SaveChangesAsync();
		logger.LogDebug("Note {id} created successfully", dbNote.Id);
		return dbNote;
	}

	private async Task<MentionQuad> ResolveNoteMentionsAsync(ASNote note) {
		var mentionTags = note.Tags?.OfType<ASMention>().Where(p => p.Href != null) ?? [];
		var users = await mentionTags
		                  .Select(async p => {
			                  try {
				                  return await userResolver.ResolveAsync(p.Href!.Id!);
			                  }
			                  catch {
				                  return null;
			                  }
		                  })
		                  .AwaitAllNoConcurrencyAsync();

		return ResolveNoteMentions(users.Where(p => p != null).Select(p => p!).ToList());
	}

	private async Task<MentionQuad> ResolveNoteMentionsAsync(string? text) {
		var users = text != null
			? await MfmParser.Parse(text)
			                 .SelectMany(p => p.Children.Append(p))
			                 .OfType<MfmMentionNode>()
			                 .DistinctBy(p => p.Acct)
			                 .Select(async p => {
				                 try {
					                 return await userResolver.ResolveAsync(p.Acct);
				                 }
				                 catch {
					                 return null;
				                 }
			                 })
			                 .AwaitAllNoConcurrencyAsync()
			: [];

		return ResolveNoteMentions(users.Where(p => p != null).Select(p => p!).ToList());
	}

	private MentionQuad ResolveNoteMentions(IReadOnlyCollection<User> users) {
		var userIds = users.Select(p => p.Id).Distinct().ToList();

		var remoteUsers = users.Where(p => p is { Host: not null, Uri: not null })
		                       .ToList();

		var localUsers = users.Where(p => p.Host is null)
		                      .ToList();

		var splitDomainMapping = remoteUsers.Where(p => new Uri(p.Uri!).Host != p.Host)
		                                    .DistinctBy(p => p.Host)
		                                    .ToDictionary(p => (p.UsernameLower, new Uri(p.Uri!).Host), p => p.Host!);

		var localMentions = localUsers.Select(p => new Note.MentionedUser {
			Host     = config.Value.AccountDomain,
			Username = p.Username,
			Uri      = $"https://{config.Value.WebDomain}/users/{p.Id}",
			Url      = $"https://{config.Value.WebDomain}/@{p.Username}"
		});

		var remoteMentions = remoteUsers.Select(p => new Note.MentionedUser {
			Host     = p.Host!,
			Uri      = p.Uri!,
			Username = p.Username,
			Url      = p.UserProfile?.Url
		}).ToList();

		var mentions = remoteMentions.Concat(localMentions).ToList();

		return (userIds, mentions, remoteMentions, splitDomainMapping);
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
		var fetchedNote = await fetchSvc.FetchNoteAsync(uri);
		if (fetchedNote?.AttributedTo is not [{ Id: not null } attrTo]) {
			logger.LogDebug("Invalid Note.AttributedTo, skipping");
			return null;
		}

		if (fetchedNote.Id != uri) {
			var res = await db.Notes.FirstOrDefaultAsync(p => p.Uri == fetchedNote.Id);
			if (res != null) return res;
		}

		//TODO: we don't need to fetch the actor every time, we can use userResolver here
		var actor = await fetchSvc.FetchActorAsync(attrTo.Id);

		try {
			return await ProcessNoteAsync(fetchedNote, actor);
		}
		catch (Exception e) {
			logger.LogDebug("Failed to create resolved note: {error}", e.Message);
			return null;
		}
	}
}