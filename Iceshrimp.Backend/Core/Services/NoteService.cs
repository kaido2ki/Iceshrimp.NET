using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Types;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

using MentionQuintuple =
	(List<string> mentionedUserIds,
	List<string> mentionedLocalUserIds,
	List<Note.MentionedUser> mentions,
	List<Note.MentionedUser> remoteMentions,
	Dictionary<(string usernameLower, string webDomain), string> splitDomainMapping);

[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor",
                 Justification = "We need IOptionsSnapshot for config hot reload")]
public class NoteService(
	ILogger<NoteService> logger,
	DatabaseContext db,
	ActivityPub.UserResolver userResolver,
	IOptionsSnapshot<Config.InstanceSection> config,
	ActivityPub.ActivityFetcherService fetchSvc,
	ActivityPub.ActivityDeliverService deliverSvc,
	ActivityPub.NoteRenderer noteRenderer,
	ActivityPub.UserRenderer userRenderer,
	ActivityPub.MentionsResolver mentionsResolver,
	MfmConverter mfmConverter,
	DriveService driveSvc,
	NotificationService notificationSvc,
	EventService eventSvc,
	ActivityPub.ActivityRenderer activityRenderer
)
{
	private readonly List<string> _resolverHistory = [];
	private          int          _recursionLimit  = 100;

	public async Task<Note> CreateNoteAsync(
		User user, Note.NoteVisibility visibility, string? text = null, string? cw = null, Note? reply = null,
		Note? renote = null, IReadOnlyCollection<DriveFile>? attachments = null
	)
	{
		if (text?.Length > config.Value.CharacterLimit)
			throw GracefulException.BadRequest($"Text cannot be longer than {config.Value.CharacterLimit} characters");
		if (cw?.Length > config.Value.CharacterLimit)
			throw GracefulException
				.BadRequest($"Content warning cannot be longer than {config.Value.CharacterLimit} characters");
		if (text is { Length: > 100000 })
			throw GracefulException.BadRequest("Text cannot be longer than 100.000 characters");
		if (cw is { Length: > 100000 })
			throw GracefulException.BadRequest("Content warning cannot be longer than 100.000 characters");
		if (renote?.IsPureRenote ?? false)
			throw GracefulException.BadRequest("Cannot renote or quote a pure renote");

		var (mentionedUserIds, mentionedLocalUserIds, mentions, remoteMentions, splitDomainMapping) =
			await ResolveNoteMentionsAsync(text);

		if (text != null)
			text = mentionsResolver.ResolveMentions(text, null, mentions, splitDomainMapping);

		if (attachments != null && attachments.Any(p => p.UserId != user.Id))
			throw GracefulException.BadRequest("Refusing to create note with files belonging to someone else");

		var note = new Note
		{
			Id                   = IdHelpers.GenerateSlowflakeId(),
			Text                 = text,
			Cw                   = cw,
			Reply                = reply,
			ReplyUserId          = reply?.UserId,
			ReplyUserHost        = reply?.UserHost,
			Renote               = renote,
			RenoteUserId         = renote?.UserId,
			RenoteUserHost       = renote?.UserHost,
			UserId               = user.Id,
			CreatedAt            = DateTime.UtcNow,
			UserHost             = null,
			Visibility           = visibility,
			FileIds              = attachments?.Select(p => p.Id).ToList() ?? [],
			AttachedFileTypes    = attachments?.Select(p => p.Type).ToList() ?? [],
			Mentions             = mentionedUserIds,
			VisibleUserIds       = visibility == Note.NoteVisibility.Specified ? mentionedUserIds : [],
			MentionedRemoteUsers = remoteMentions
		};

		if (!note.IsPureRenote) user.NotesCount++;
		if (reply != null) reply.RepliesCount++;
		if (renote != null && !note.IsQuote)
			if (!db.Notes.Any(p => p.UserId == user.Id && p.RenoteId == renote.Id && p.IsPureRenote))
				renote.RenoteCount++;

		await db.AddAsync(note);
		await db.SaveChangesAsync();
		eventSvc.RaiseNotePublished(this, note);
		await notificationSvc.GenerateMentionNotifications(note, mentionedLocalUserIds);
		await notificationSvc.GenerateReplyNotifications(note, mentionedLocalUserIds);
		await notificationSvc.GenerateRenoteNotification(note);

		if (user.Host != null) return note;

		var actor = userRenderer.RenderLite(user);
		ASActivity activity = note is { IsPureRenote: true, Renote: not null }
			? ActivityPub.ActivityRenderer.RenderAnnounce(noteRenderer.RenderLite(note.Renote),
			                                              note.GetPublicUri(config.Value), actor, note.Visibility,
			                                              user.GetPublicUri(config.Value) + "/followers")
			: ActivityPub.ActivityRenderer.RenderCreate(await noteRenderer.RenderAsync(note, mentions), actor);

		List<string> additionalUserIds =
			note is { IsPureRenote: true, Renote: not null, Visibility: < Note.NoteVisibility.Followers }
				? [note.Renote.User.Id]
				: [];

		var recipients = await db.Users
		                         .Where(p => mentionedUserIds.Concat(additionalUserIds).Contains(p.Id))
		                         .Select(p => new User { Host = p.Host, Inbox = p.Inbox })
		                         .ToListAsync();

		if (note.Visibility == Note.NoteVisibility.Specified)
			await deliverSvc.DeliverToAsync(activity, user, recipients.ToArray());
		else
			await deliverSvc.DeliverToFollowersAsync(activity, user, recipients);

		return note;
	}

	public async Task<Note> UpdateNoteAsync(
		Note note, string? text = null, string? cw = null, IReadOnlyCollection<DriveFile>? attachments = null
	)
	{
		var noteEdit = new NoteEdit
		{
			Id        = IdHelpers.GenerateSlowflakeId(),
			UpdatedAt = DateTime.UtcNow,
			Note      = note,
			Text      = note.Text,
			Cw        = note.Cw,
			FileIds   = note.FileIds
		};

		if (text?.Length > config.Value.CharacterLimit)
			throw GracefulException.BadRequest($"Text cannot be longer than {config.Value.CharacterLimit} characters");
		if (cw?.Length > config.Value.CharacterLimit)
			throw GracefulException
				.BadRequest($"Content warning cannot be longer than {config.Value.CharacterLimit} characters");
		if (text is { Length: > 100000 })
			throw GracefulException.BadRequest("Text cannot be longer than 100.000 characters");
		if (cw is { Length: > 100000 })
			throw GracefulException.BadRequest("Content warning cannot be longer than 100.000 characters");
		if (attachments != null && attachments.Any(p => p.UserId != note.User.Id))
			throw GracefulException.BadRequest("Refusing to create note with files belonging to someone else");

		var previousMentionedLocalUserIds = await db.Users.Where(p => note.Mentions.Contains(p.Id) && p.Host == null)
		                                            .Select(p => p.Id)
		                                            .ToListAsync();

		var (mentionedUserIds, mentionedLocalUserIds, mentions, remoteMentions, splitDomainMapping) =
			await ResolveNoteMentionsAsync(text);
		if (text != null)
			text = mentionsResolver.ResolveMentions(text, null, mentions, splitDomainMapping);

		mentionedLocalUserIds = mentionedLocalUserIds.Except(previousMentionedLocalUserIds).ToList();
		note.Text             = text;


		if (text is not null)
		{
			note.Mentions             = mentionedUserIds;
			note.MentionedRemoteUsers = remoteMentions;
			if (note.Visibility == Note.NoteVisibility.Specified)
			{
				if (note.ReplyUserId != null)
					mentionedUserIds.Add(note.ReplyUserId);

				// We want to make sure not to revoke visibility
				note.VisibleUserIds = mentionedUserIds.Concat(note.VisibleUserIds).Distinct().ToList();
			}

			note.Text = mentionsResolver.ResolveMentions(text, note.UserHost, mentions, splitDomainMapping);
		}

		//TODO: handle updated alt text et al
		note.FileIds           = attachments?.Select(p => p.Id).ToList() ?? [];
		note.AttachedFileTypes = attachments?.Select(p => p.Type).ToList() ?? [];

		note.UpdatedAt = DateTime.UtcNow;

		db.Update(note);
		await db.AddAsync(noteEdit);
		await db.SaveChangesAsync();
		await notificationSvc.GenerateMentionNotifications(note, mentionedLocalUserIds);
		await notificationSvc.GenerateReplyNotifications(note, mentionedLocalUserIds);
		await notificationSvc.GenerateEditNotifications(note);
		eventSvc.RaiseNoteUpdated(this, note);

		var actor    = userRenderer.RenderLite(note.User);
		var obj      = await noteRenderer.RenderAsync(note, mentions);
		var activity = activityRenderer.RenderUpdate(obj, actor);

		var recipients = await db.Users.Where(p => mentionedUserIds.Contains(p.Id))
		                         .Select(p => new User { Host = p.Host, Inbox = p.Inbox })
		                         .ToListAsync();

		if (note.Visibility == Note.NoteVisibility.Specified)
			await deliverSvc.DeliverToAsync(activity, note.User, recipients.ToArray());
		else
			await deliverSvc.DeliverToFollowersAsync(activity, note.User, recipients);

		return note;
	}

	public async Task DeleteNoteAsync(Note note)
	{
		note.User.NotesCount--;
		db.Update(note.User);
		db.Remove(note);
		eventSvc.RaiseNoteDeleted(this, note);
		await db.SaveChangesAsync();

		if (note.UserHost != null)
			return;

		var recipients = await db.Users.Where(p => note.Mentions.Concat(note.VisibleUserIds).Distinct().Contains(p.Id))
		                         .Select(p => new User { Host = p.Host, Inbox = p.Inbox })
		                         .ToListAsync();

		var actor = userRenderer.RenderLite(note.User);
		ASActivity activity = note.IsPureRenote
			? activityRenderer.RenderUndo(actor,
			                              ActivityPub.ActivityRenderer
			                                         .RenderAnnounce(noteRenderer.RenderLite(note.Renote ?? throw new Exception("Refusing to undo renote without renote")),
			                                                         note.GetPublicUri(config.Value), actor,
			                                                         note.Visibility,
			                                                         note.User.GetPublicUri(config.Value) +
			                                                         "/followers"))
			: activityRenderer.RenderDelete(actor, new ASTombstone { Id = note.GetPublicUri(config.Value) });

		if (note.Visibility == Note.NoteVisibility.Specified)
			await deliverSvc.DeliverToAsync(activity, note.User, recipients.ToArray());
		else
			await deliverSvc.DeliverToFollowersAsync(activity, note.User, recipients);
	}

	public async Task DeleteNoteAsync(ASTombstone note, User actor)
	{
		// ReSharper disable once EntityFramework.NPlusOne.IncompleteDataQuery (it doesn't know about IncludeCommonProperties())
		var dbNote = await db.Notes.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Uri == note.Id);
		if (dbNote == null)
		{
			logger.LogDebug("Note '{id}' isn't known, skipping", note.Id);
			return;
		}

		// ReSharper disable once EntityFramework.NPlusOne.IncompleteDataUsage (same reason as above)
		if (dbNote.User != actor)
		{
			logger.LogDebug("Note '{id}' isn't owned by actor requesting its deletion, skipping", note.Id);
			return;
		}

		logger.LogDebug("Deleting note '{id}' owned by {userId}", note.Id, actor.Id);

		actor.NotesCount--;
		if (dbNote.IsPureRenote) dbNote.RenoteCount--;
		db.Remove(dbNote);
		eventSvc.RaiseNoteDeleted(this, dbNote);
		await db.SaveChangesAsync();
	}

	public async Task UndoAnnounceAsync(ASNote note, User actor)
	{
		var renote = await ResolveNoteAsync(note);
		if (renote == null) return;
		var notes = await db.Notes.IncludeCommonProperties()
		                    .Where(p => p.Renote == renote && p.User == actor && p.IsPureRenote)
		                    .ToListAsync();

		if (notes.Count == 0) return;
		renote.RenoteCount--;
		db.RemoveRange(notes);
		await db.SaveChangesAsync();

		foreach (var hit in notes)
			eventSvc.RaiseNoteDeleted(this, hit);
	}

	public async Task<Note> ProcessNoteAsync(ASNote note, User actor)
	{
		var dbHit = await db.Notes.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Uri == note.Id);

		if (dbHit != null)
		{
			logger.LogDebug("Note '{id}' already exists, skipping", note.Id);
			return dbHit;
		}

		if (_resolverHistory is [])
			_resolverHistory.Add(note.Id);
		logger.LogDebug("Creating note: {id}", note.Id);

		// Validate note
		if (note.AttributedTo is not { Count: 1 } || note.AttributedTo[0].Id != actor.Uri)
			throw GracefulException.UnprocessableEntity("User.Uri doesn't match Note.AttributedTo");
		if (actor.Uri == null)
			throw GracefulException.UnprocessableEntity("User.Uri is null");
		if (new Uri(note.Id).IdnHost != new Uri(actor.Uri).IdnHost)
			throw GracefulException.UnprocessableEntity("User.Uri host doesn't match Note.Id host");
		if (!note.Id.StartsWith("https://"))
			throw GracefulException.UnprocessableEntity("Note.Id schema is invalid");
		if (note.Url?.Link != null && !note.Url.Link.StartsWith("https://"))
			throw GracefulException.UnprocessableEntity("Note.Url schema is invalid");
		if (note.PublishedAt is null or { Year: < 2007 } || note.PublishedAt > DateTime.Now + TimeSpan.FromDays(3))
			throw GracefulException.UnprocessableEntity("Note.PublishedAt is nonsensical");
		if (actor.IsSuspended)
			throw GracefulException.Forbidden("User is suspended");

		//TODO: resolve emoji

		var (mentionedUserIds, mentionedLocalUserIds, mentions, remoteMentions, splitDomainMapping) =
			await ResolveNoteMentionsAsync(note);

		var createdAt = note.PublishedAt?.ToUniversalTime() ??
		                throw GracefulException.UnprocessableEntity("Missing or invalid PublishedAt field");

		var quoteUrl = note.MkQuote ?? note.QuoteUri ?? note.QuoteUrl;

		var dbNote = new Note
		{
			Id         = IdHelpers.GenerateSlowflakeId(createdAt),
			Uri        = note.Id,
			Url        = note.Url?.Id, //FIXME: this doesn't seem to work yet
			Text       = note.MkContent ?? await mfmConverter.FromHtmlAsync(note.Content, mentions),
			Cw         = note.Summary,
			UserId     = actor.Id,
			CreatedAt  = createdAt,
			UserHost   = actor.Host,
			Visibility = note.GetVisibility(actor),
			Reply      = note.InReplyTo?.Id != null ? await ResolveNoteAsync(note.InReplyTo.Id) : null,
			Renote     = quoteUrl != null ? await ResolveNoteAsync(quoteUrl) : null
		};

		if (dbNote.Renote?.IsPureRenote ?? false)
			throw GracefulException.UnprocessableEntity("Cannot renote or quote a pure renote");

		if (dbNote.Reply != null)
		{
			dbNote.ReplyUserId   = dbNote.Reply.UserId;
			dbNote.ReplyUserHost = dbNote.Reply.UserHost;
		}

		if (dbNote.Renote != null)
		{
			dbNote.RenoteUserId   = dbNote.Renote.UserId;
			dbNote.RenoteUserHost = dbNote.Renote.UserHost;
		}

		if (dbNote.Text is { Length: > 100000 })
			throw GracefulException.UnprocessableEntity("Content cannot be longer than 100.000 characters");
		if (dbNote.Cw is { Length: > 100000 })
			throw GracefulException.UnprocessableEntity("Summary cannot be longer than 100.000 characters");

		if (dbNote.Text is not null)
		{
			dbNote.Mentions             = mentionedUserIds;
			dbNote.MentionedRemoteUsers = remoteMentions;
			if (dbNote.Visibility == Note.NoteVisibility.Specified)
			{
				var visibleUserIds = (await note.GetRecipients(actor)
				                                .Select(userResolver.ResolveAsync)
				                                .AwaitAllNoConcurrencyAsync())
				                     .Select(p => p.Id)
				                     .Concat(mentionedUserIds)
				                     .ToList();
				if (dbNote.ReplyUserId != null)
					visibleUserIds.Add(dbNote.ReplyUserId);

				dbNote.VisibleUserIds = visibleUserIds.Distinct().ToList();
			}

			dbNote.Text = mentionsResolver.ResolveMentions(dbNote.Text, dbNote.UserHost, mentions, splitDomainMapping);
		}

		var sensitive = (note.Sensitive ?? false) || dbNote.Cw != null;
		var files     = await ProcessAttachmentsAsync(note.Attachments, actor, sensitive);
		if (files.Count != 0)
		{
			dbNote.FileIds           = files.Select(p => p.Id).ToList();
			dbNote.AttachedFileTypes = files.Select(p => p.Type).ToList();
		}

		actor.NotesCount++;
		if (dbNote.Reply != null) dbNote.Reply.RepliesCount++;
		await db.Notes.AddAsync(dbNote);
		await db.SaveChangesAsync();
		eventSvc.RaiseNotePublished(this, dbNote);
		await notificationSvc.GenerateMentionNotifications(dbNote, mentionedLocalUserIds);
		await notificationSvc.GenerateReplyNotifications(dbNote, mentionedLocalUserIds);
		await notificationSvc.GenerateRenoteNotification(dbNote);
		logger.LogDebug("Note {id} created successfully", dbNote.Id);
		return dbNote;
	}

	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataUsage",
	                 Justification = "Inspection doesn't understand IncludeCommonProperties()")]
	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataQuery", Justification = "See above")]
	public async Task<Note> ProcessNoteUpdateAsync(ASNote note, User actor)
	{
		var dbNote = await db.Notes.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Uri == note.Id);
		if (dbNote == null) return await ProcessNoteAsync(note, actor);

		if (dbNote.User != actor)
			throw GracefulException.UnprocessableEntity("Refusing to update note of user other than actor");
		if (dbNote.User.IsSuspended)
			throw GracefulException.Forbidden("User is suspended");

		var noteEdit = new NoteEdit
		{
			Id        = IdHelpers.GenerateSlowflakeId(),
			UpdatedAt = DateTime.UtcNow,
			Note      = dbNote,
			Text      = dbNote.Text,
			Cw        = dbNote.Cw,
			FileIds   = dbNote.FileIds
		};

		var previousMentionedLocalUserIds = await db.Users.Where(p => dbNote.Mentions.Contains(p.Id) && p.Host == null)
		                                            .Select(p => p.Id)
		                                            .ToListAsync();
		var (mentionedUserIds, mentionedLocalUserIds, mentions, remoteMentions, splitDomainMapping) =
			await ResolveNoteMentionsAsync(note);

		mentionedLocalUserIds = mentionedLocalUserIds.Except(previousMentionedLocalUserIds).ToList();
		dbNote.Text           = note.MkContent ?? await mfmConverter.FromHtmlAsync(note.Content, mentions);
		dbNote.Cw             = note.Summary;

		if (dbNote.Cw is { Length: > 100000 })
			throw GracefulException.UnprocessableEntity("Summary cannot be longer than 100.000 characters");

		if (dbNote.Text is { Length: > 100000 })
			throw GracefulException.UnprocessableEntity("Content cannot be longer than 100.000 characters");

		if (dbNote.Text is not null)
		{
			dbNote.Mentions             = mentionedUserIds;
			dbNote.MentionedRemoteUsers = remoteMentions;
			if (dbNote.Visibility == Note.NoteVisibility.Specified)
			{
				var visibleUserIds = (await note.GetRecipients(actor)
				                                .Select(userResolver.ResolveAsync)
				                                .AwaitAllNoConcurrencyAsync())
				                     .Select(p => p.Id)
				                     .Concat(mentionedUserIds)
				                     .ToList();
				if (dbNote.ReplyUserId != null)
					visibleUserIds.Add(dbNote.ReplyUserId);

				// We want to make sure not to revoke visibility
				dbNote.VisibleUserIds = visibleUserIds.Concat(dbNote.VisibleUserIds).Distinct().ToList();
			}

			dbNote.Text = mentionsResolver.ResolveMentions(dbNote.Text, dbNote.UserHost, mentions, splitDomainMapping);
		}

		//TODO: handle updated alt text et al
		var sensitive = (note.Sensitive ?? false) || dbNote.Cw != null;
		var files     = await ProcessAttachmentsAsync(note.Attachments, actor, sensitive);
		dbNote.FileIds           = files.Select(p => p.Id).ToList();
		dbNote.AttachedFileTypes = files.Select(p => p.Type).ToList();

		dbNote.UpdatedAt = DateTime.UtcNow;

		db.Update(dbNote);
		await db.AddAsync(noteEdit);
		await db.SaveChangesAsync();
		await notificationSvc.GenerateMentionNotifications(dbNote, mentionedLocalUserIds);
		await notificationSvc.GenerateReplyNotifications(dbNote, mentionedLocalUserIds);
		await notificationSvc.GenerateEditNotifications(dbNote);
		eventSvc.RaiseNoteUpdated(this, dbNote);
		return dbNote;
	}

	private async Task<MentionQuintuple> ResolveNoteMentionsAsync(ASNote note)
	{
		var mentionTags = note.Tags?.OfType<ASMention>().Where(p => p.Href != null) ?? [];
		var users = await mentionTags
		                  .Select(async p =>
		                  {
			                  try
			                  {
				                  return await userResolver.ResolveAsync(p.Href!.Id!);
			                  }
			                  catch
			                  {
				                  return null;
			                  }
		                  })
		                  .AwaitAllNoConcurrencyAsync();

		return ResolveNoteMentions(users.Where(p => p != null).Select(p => p!).ToList());
	}

	private async Task<MentionQuintuple> ResolveNoteMentionsAsync(string? text)
	{
		var users = text != null
			? await MfmParser.Parse(text)
			                 .SelectMany(p => p.Children.Append(p))
			                 .OfType<MfmMentionNode>()
			                 .DistinctBy(p => p.Acct)
			                 .Select(async p =>
			                 {
				                 try
				                 {
					                 return await userResolver.ResolveAsync(p.Acct);
				                 }
				                 catch
				                 {
					                 return null;
				                 }
			                 })
			                 .AwaitAllNoConcurrencyAsync()
			: [];

		return ResolveNoteMentions(users.Where(p => p != null).Select(p => p!).ToList());
	}

	private MentionQuintuple ResolveNoteMentions(IReadOnlyCollection<User> users)
	{
		var userIds      = users.Select(p => p.Id).Distinct().ToList();
		var localUserIds = users.Where(p => p.Host == null).Select(p => p.Id).Distinct().ToList();

		var remoteUsers = users.Where(p => p is { Host: not null, Uri: not null })
		                       .ToList();

		var localUsers = users.Where(p => p.Host is null)
		                      .ToList();

		var splitDomainMapping = remoteUsers.Where(p => new Uri(p.Uri!).Host != p.Host)
		                                    .DistinctBy(p => p.Host)
		                                    .ToDictionary(p => (p.UsernameLower, new Uri(p.Uri!).Host), p => p.Host!);

		var localMentions = localUsers.Select(p => new Note.MentionedUser
		{
			Host     = config.Value.AccountDomain,
			Username = p.Username,
			Uri      = p.GetPublicUri(config.Value),
			Url      = $"https://{config.Value.WebDomain}/@{p.Username}"
		});

		var remoteMentions = remoteUsers.Select(p => new Note.MentionedUser
		                                {
			                                Host     = p.Host!,
			                                Uri      = p.Uri!,
			                                Username = p.Username,
			                                Url      = p.UserProfile?.Url
		                                })
		                                .ToList();

		var mentions = remoteMentions.Concat(localMentions).ToList();

		return (userIds, localUserIds, mentions, remoteMentions, splitDomainMapping);
	}

	private async Task<List<DriveFile>> ProcessAttachmentsAsync(
		List<ASAttachment>? attachments, User user, bool sensitive
	)
	{
		if (attachments is not { Count: > 0 }) return [];
		var result = await attachments
		                   .OfType<ASDocument>()
		                   .Select(p => driveSvc.StoreFile(p.Url?.Id, user, p.Sensitive ?? sensitive, p.Description,
		                                                   p.MediaType))
		                   .AwaitAllNoConcurrencyAsync();

		return result.Where(p => p != null).Cast<DriveFile>().ToList();
	}

	public async Task<Note?> ResolveNoteAsync(string uri, ASNote? fetchedNote = null, User? user = null)
	{
		//TODO: is this enough to prevent DoS attacks?
		if (_recursionLimit-- <= 0)
			throw GracefulException.UnprocessableEntity("Refusing to resolve threads this long");
		if (_resolverHistory.Contains(uri))
			throw GracefulException.UnprocessableEntity("Refusing to resolve circular threads");
		_resolverHistory.Add(uri);

		var note = uri.StartsWith($"https://{config.Value.WebDomain}/notes/")
			? await db.Notes.IncludeCommonProperties()
			          .FirstOrDefaultAsync(p => p.Id ==
			                                    uri.Substring($"https://{config.Value.WebDomain}/notes/".Length))
			: await db.Notes.IncludeCommonProperties()
			          .FirstOrDefaultAsync(p => p.Uri == uri);

		if (note != null) return note;

		//TODO: should we fall back to a regular user's keypair if fetching with instance actor fails & a local user is following the actor?
		fetchedNote ??= user != null ? await fetchSvc.FetchNoteAsync(uri, user) : await fetchSvc.FetchNoteAsync(uri);

		if (fetchedNote == null)
		{
			logger.LogDebug("Failed to fetch note, skipping");
			return null;
		}

		if (fetchedNote.AttributedTo is not [{ Id: not null } attrTo])
		{
			logger.LogDebug("Invalid Note.AttributedTo, skipping");
			return null;
		}

		if (fetchedNote.Id != uri)
		{
			var res = await db.Notes.FirstOrDefaultAsync(p => p.Uri == fetchedNote.Id);
			if (res != null) return res;
		}

		var actor = await userResolver.ResolveAsync(attrTo.Id);

		try
		{
			return await ProcessNoteAsync(fetchedNote, actor);
		}
		catch (Exception e)
		{
			logger.LogDebug("Failed to create resolved note: {error}", e.Message);
			return null;
		}
	}

	public async Task<Note?> ResolveNoteAsync(ASNote note)
	{
		return await ResolveNoteAsync(note.Id, note);
	}

	public async Task LikeNoteAsync(Note note, User user)
	{
		if (!await db.NoteLikes.AnyAsync(p => p.Note == note && p.User == user))
		{
			var like = new NoteLike
			{
				Id = IdHelpers.GenerateSlowflakeId(), CreatedAt = DateTime.UtcNow, User = user, Note = note
			};

			await db.NoteLikes.AddAsync(like);
			await db.SaveChangesAsync();
			if (user.Host == null && note.UserHost != null)
			{
				var activity = activityRenderer.RenderLike(note, user);
				await deliverSvc.DeliverToFollowersAsync(activity, user, [note.User]);
			}

			eventSvc.RaiseNoteLiked(this, note, user);
			await notificationSvc.GenerateLikeNotification(note, user);
		}
	}

	public async Task UnlikeNoteAsync(Note note, User actor)
	{
		var count = await db.NoteLikes.Where(p => p.Note == note && p.User == actor).ExecuteDeleteAsync();
		if (count == 0) return;
		if (actor.Host == null && note.UserHost != null)
		{
			var activity = activityRenderer.RenderUndo(userRenderer.RenderLite(actor),
			                                           activityRenderer.RenderLike(note, actor));
			await deliverSvc.DeliverToFollowersAsync(activity, actor, [note.User]);
		}

		eventSvc.RaiseNoteUnliked(this, note, actor);
		await db.Notifications
		        .Where(p => p.Type == Notification.NotificationType.Like &&
		                    p.Notifiee == note.User &&
		                    p.Notifier == actor)
		        .ExecuteDeleteAsync();
	}

	public async Task LikeNoteAsync(ASNote note, User actor)
	{
		var dbNote = await ResolveNoteAsync(note) ?? throw new Exception("Cannot register like for unknown note");
		await LikeNoteAsync(dbNote, actor);
	}

	public async Task UnlikeNoteAsync(ASNote note, User actor)
	{
		var dbNote = await ResolveNoteAsync(note) ?? throw new Exception("Cannot unregister like for unknown note");
		await UnlikeNoteAsync(dbNote, actor);
	}
}