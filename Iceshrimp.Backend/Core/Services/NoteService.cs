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
	DriveService driveSvc,
	NotificationService notificationSvc,
	EventService eventSvc,
	ActivityPub.ActivityRenderer activityRenderer,
	EmojiService emojiSvc,
	FollowupTaskService followupTaskSvc,
	ActivityPub.ObjectResolver objectResolver
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

		if (cw != null && string.IsNullOrWhiteSpace(cw))
			cw = null;

		if ((user.UserSettings?.PrivateMode ?? false) && visibility < Note.NoteVisibility.Followers)
			visibility = Note.NoteVisibility.Followers;

		var tags = ResolveHashtags(text);

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
			User                 = user,
			CreatedAt            = DateTime.UtcNow,
			UserHost             = null,
			Visibility           = visibility,
			FileIds              = attachments?.Select(p => p.Id).ToList() ?? [],
			AttachedFileTypes    = attachments?.Select(p => p.Type).ToList() ?? [],
			Mentions             = mentionedUserIds,
			VisibleUserIds       = visibility == Note.NoteVisibility.Specified ? mentionedUserIds : [],
			MentionedRemoteUsers = remoteMentions,
			ThreadId             = reply?.ThreadId ?? reply?.Id,
			Tags                 = tags
		};

		await UpdateNoteCountersAsync(note, true);

		await db.AddAsync(note);
		await db.SaveChangesAsync();
		eventSvc.RaiseNotePublished(this, note);
		await notificationSvc.GenerateMentionNotifications(note, mentionedLocalUserIds);
		await notificationSvc.GenerateReplyNotifications(note, mentionedLocalUserIds);
		await notificationSvc.GenerateRenoteNotification(note);

		if (user.Host != null)
		{
			_ = followupTaskSvc.ExecuteTask("UpdateInstanceNoteCounter", async provider =>
			{
				var bgDb          = provider.GetRequiredService<DatabaseContext>();
				var bgInstanceSvc = provider.GetRequiredService<InstanceService>();
				var dbInstance    = await bgInstanceSvc.GetUpdatedInstanceMetadataAsync(user);
				await bgDb.Instances.Where(p => p.Id == dbInstance.Id)
				          .ExecuteUpdateAsync(p => p.SetProperty(i => i.NotesCount, i => i.NotesCount + 1));
			});

			return note;
		}

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

	/// <remarks>
	/// This needs to be called before SaveChangesAsync on create & after on delete
	/// </remarks>
	private async Task UpdateNoteCountersAsync(Note note, bool create)
	{
		var diff = create ? 1 : -1;

		if (note is { Renote.Id: not null, IsPureRenote: true })
		{
			if (!db.Notes.Any(p => p.UserId == note.User.Id && p.RenoteId == note.Renote.Id && p.IsPureRenote))
			{
				await db.Notes.Where(p => p.Id == note.Renote.Id)
				        .ExecuteUpdateAsync(p => p.SetProperty(n => n.RenoteCount, n => n.RenoteCount + diff));
			}
		}
		else
		{
			await db.Users.Where(p => p.Id == note.User.Id)
			        .ExecuteUpdateAsync(p => p.SetProperty(u => u.NotesCount, u => u.NotesCount + diff));
		}

		if (note.Reply != null)
		{
			await db.Notes.Where(p => p.Id == note.Reply.Id)
			        .ExecuteUpdateAsync(p => p.SetProperty(n => n.RepliesCount, n => n.RepliesCount + diff));
		}
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
		note.Tags             = ResolveHashtags(text);

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
		db.Update(note.User);
		db.Remove(note);
		eventSvc.RaiseNoteDeleted(this, note);
		await db.SaveChangesAsync();
		await UpdateNoteCountersAsync(note, false);

		if (note.UserHost != null)
		{
			if (note.User.Uri != null)
			{
				_ = followupTaskSvc.ExecuteTask("UpdateInstanceNoteCounter", async provider =>
				{
					var bgDb          = provider.GetRequiredService<DatabaseContext>();
					var bgInstanceSvc = provider.GetRequiredService<InstanceService>();
					var dbInstance    = await bgInstanceSvc.GetUpdatedInstanceMetadataAsync(note.User);
					await bgDb.Instances.Where(p => p.Id == dbInstance.Id)
					          .ExecuteUpdateAsync(p => p.SetProperty(i => i.NotesCount, i => i.NotesCount - 1));
				});
			}

			return;
		}

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

		db.Remove(dbNote);
		eventSvc.RaiseNoteDeleted(this, dbNote);
		await db.SaveChangesAsync();
		await UpdateNoteCountersAsync(dbNote, false);

		// ReSharper disable once EntityFramework.NPlusOne.IncompleteDataUsage (same reason as above)
		if (dbNote.User.Uri != null && dbNote.UserHost != null)
		{
			_ = followupTaskSvc.ExecuteTask("UpdateInstanceNoteCounter", async provider =>
			{
				var bgDb          = provider.GetRequiredService<DatabaseContext>();
				var bgInstanceSvc = provider.GetRequiredService<InstanceService>();
				// ReSharper disable once EntityFramework.NPlusOne.IncompleteDataUsage (same reason as above)
				var dbInstance = await bgInstanceSvc.GetUpdatedInstanceMetadataAsync(dbNote.User);
				await bgDb.Instances.Where(p => p.Id == dbInstance.Id)
				          .ExecuteUpdateAsync(p => p.SetProperty(i => i.NotesCount, i => i.NotesCount - 1));
			});
		}
	}

	public async Task UndoAnnounceAsync(ASNote note, User actor)
	{
		var renote = await ResolveNoteAsync(note);
		if (renote == null) return;
		var notes = await db.Notes.IncludeCommonProperties()
		                    .Where(p => p.Renote == renote && p.User == actor && p.IsPureRenote)
		                    .ToListAsync();

		if (notes.Count == 0) return;
		db.RemoveRange(notes);
		await db.SaveChangesAsync();
		await db.Notes.Where(p => p.Id == note.Id)
		        .ExecuteUpdateAsync(p => p.SetProperty(n => n.RenoteCount, n => n.RenoteCount - 1));

		foreach (var hit in notes)
			eventSvc.RaiseNoteDeleted(this, hit);
	}

	public async Task<Note> ProcessNoteAsync(ASNote note, User actor, User? user = null)
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
		if (actor.Host == null)
			throw GracefulException.UnprocessableEntity("User.Host is null");
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
			Text       = note.MkContent ?? await MfmConverter.FromHtmlAsync(note.Content, mentions),
			Cw         = note.Summary,
			User       = actor,
			CreatedAt  = createdAt,
			UserHost   = actor.Host,
			Visibility = note.GetVisibility(actor),
			Reply      = note.InReplyTo?.Id != null ? await ResolveNoteAsync(note.InReplyTo.Id, user: user) : null,
			Renote     = quoteUrl != null ? await ResolveNoteAsync(quoteUrl, user: user) : null
		};

		if (dbNote.Renote?.IsPureRenote ?? false)
			throw GracefulException.UnprocessableEntity("Cannot renote or quote a pure renote");

		if (dbNote.Reply != null)
		{
			dbNote.ReplyUserId   = dbNote.Reply.UserId;
			dbNote.ReplyUserHost = dbNote.Reply.UserHost;
			dbNote.ThreadId      = dbNote.Reply.ThreadId ?? dbNote.Reply.ThreadId;
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
			dbNote.Tags = ResolveHashtags(dbNote.Text, note);
		}

		var sensitive = (note.Sensitive ?? false) || dbNote.Cw != null;
		var files     = await ProcessAttachmentsAsync(note.Attachments, actor, sensitive);
		if (files.Count != 0)
		{
			dbNote.FileIds           = files.Select(p => p.Id).ToList();
			dbNote.AttachedFileTypes = files.Select(p => p.Type).ToList();
		}

		var emoji = await emojiSvc.ProcessEmojiAsync(note.Tags?.OfType<ASEmoji>().ToList(), actor.Host);
		dbNote.Emojis = emoji.Select(p => p.Id).ToList();

		await UpdateNoteCountersAsync(dbNote, true);
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
		dbNote.Text           = note.MkContent ?? await MfmConverter.FromHtmlAsync(note.Content, mentions);
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
			dbNote.Tags = ResolveHashtags(dbNote.Text, note);
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

	private List<string> ResolveHashtags(string? text, ASNote? note = null)
	{
		List<string> tags = [];

		if (text != null)
		{
			tags = MfmParser.Parse(text)
			                .SelectMany(p => p.Children.Append(p))
			                .OfType<MfmHashtagNode>()
			                .Select(p => p.Hashtag.ToLowerInvariant())
			                .Select(p => p.Trim('#'))
			                .Distinct()
			                .ToList();
		}

		var extracted = note?.Tags?.OfType<ASHashtag>()
		                    .Select(p => p.Name?.ToLowerInvariant())
		                    .Where(p => p != null)
		                    .Cast<string>()
		                    .Select(p => p.Trim('#'))
		                    .Distinct()
		                    .ToList();

		if (extracted != null)
			tags.AddRange(extracted);

		if (tags.Count == 0) return [];

		tags = tags.Distinct().ToList();

		_ = followupTaskSvc.ExecuteTask("UpdateHashtagsTable", async provider =>
		{
			var bgDb     = provider.GetRequiredService<DatabaseContext>();
			var existing = await bgDb.Hashtags.Where(p => tags.Contains(p.Name)).Select(p => p.Name).ToListAsync();
			var dbTags = tags.Except(existing)
			                 .Select(p => new Hashtag { Id = IdHelpers.GenerateSlowflakeId(), Name = p });
			await bgDb.AddRangeAsync(dbTags);
			await bgDb.SaveChangesAsync();
		});

		return tags;
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
		                   .Take(10)
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
			var res = await db.Notes.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Uri == fetchedNote.Id);
			if (res != null) return res;
		}

		var actor = await userResolver.ResolveAsync(attrTo.Id);

		try
		{
			return await ProcessNoteAsync(fetchedNote, actor, user);
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
			await db.Notes.Where(p => p.Id == note.Id)
			        .ExecuteUpdateAsync(p => p.SetProperty(n => n.LikeCount, n => n.LikeCount + 1));

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

		await db.Notes.Where(p => p.Id == note.Id)
		        .ExecuteUpdateAsync(p => p.SetProperty(n => n.LikeCount, n => n.LikeCount - count));

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

	public async Task BookmarkNoteAsync(Note note, User user)
	{
		if (user.Host != null) throw new Exception("This method is only valid for local users");

		if (!await db.NoteBookmarks.AnyAsync(p => p.Note == note && p.User == user))
		{
			var bookmark = new NoteBookmark
			{
				Id = IdHelpers.GenerateSlowflakeId(), CreatedAt = DateTime.UtcNow, User = user, Note = note
			};

			await db.NoteBookmarks.AddAsync(bookmark);
			await db.SaveChangesAsync();
		}
	}

	public async Task UnbookmarkNoteAsync(Note note, User user)
	{
		if (user.Host != null) throw new Exception("This method is only valid for local users");

		await db.NoteBookmarks.Where(p => p.Note == note && p.User == user).ExecuteDeleteAsync();
	}

	public async Task PinNoteAsync(Note note, User user)
	{
		if (user.Host != null) throw new Exception("This method is only valid for local users");

		if (note.User != user)
			throw GracefulException.UnprocessableEntity("Validation failed: Someone else's post cannot be pinned");

		if (!await db.UserNotePins.AnyAsync(p => p.Note == note && p.User == user))
		{
			if (await db.UserNotePins.CountAsync(p => p.User == user) > 10)
				throw GracefulException.UnprocessableEntity("You cannot pin more than 10 notes at once.");

			var pin = new UserNotePin
			{
				Id = IdHelpers.GenerateSlowflakeId(), CreatedAt = DateTime.UtcNow, User = user, Note = note
			};

			await db.UserNotePins.AddAsync(pin);
			await db.SaveChangesAsync();

			var activity = activityRenderer.RenderUpdate(await userRenderer.RenderAsync(user));
			await deliverSvc.DeliverToFollowersAsync(activity, user, []);
		}
	}

	public async Task UnpinNoteAsync(Note note, User user)
	{
		if (user.Host != null) throw new Exception("This method is only valid for local users");

		var count = await db.UserNotePins.Where(p => p.Note == note && p.User == user).ExecuteDeleteAsync();
		if (count == 0) return;

		var activity = activityRenderer.RenderUpdate(await userRenderer.RenderAsync(user));
		await deliverSvc.DeliverToFollowersAsync(activity, user, []);
	}

	public async Task UpdatePinnedNotesAsync(ASActor actor, User user)
	{
		logger.LogDebug("Updating pinned notes for user {user}", user.Id);
		var collection = actor.Featured;
		if (collection == null) return;
		if (collection.IsUnresolved)
			collection = await objectResolver.ResolveObject(collection, force: true) as ASOrderedCollection;
		if (collection is not { Items: not null }) return;

		var items = await collection.Items.Take(10).Select(p => objectResolver.ResolveObject(p)).AwaitAllAsync();
		var notes = await items.OfType<ASNote>().Select(p => ResolveNoteAsync(p.Id, p)).AwaitAllNoConcurrencyAsync();
		var previousPins = await db.Users.Where(p => p.Id == user.Id)
		                           .Select(p => p.PinnedNotes.Select(i => i.Id))
		                           .FirstOrDefaultAsync() ??
		                   throw new Exception("existingPins must not be null at this stage");

		if (previousPins.SequenceEqual(notes.Where(p => p != null).Cast<Note>().Select(p => p.Id))) return;

		var pins = notes.Where(p => p != null)
		                .Cast<Note>()
		                .Select(p => new UserNotePin
		                {
			                Id        = IdHelpers.GenerateSlowflakeId(),
			                CreatedAt = DateTime.UtcNow,
			                Note      = p,
			                User      = user
		                });

		db.RemoveRange(await db.UserNotePins.Where(p => p.User == user).ToListAsync());
		await db.AddRangeAsync(pins);
		await db.SaveChangesAsync();
	}
}