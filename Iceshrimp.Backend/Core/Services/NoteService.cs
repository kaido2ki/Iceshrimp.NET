using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing;
using static Iceshrimp.Parsing.MfmNodeTypes;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Queues;
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
	ActivityPub.ObjectResolver objectResolver,
	QueueService queueSvc,
	PollService pollSvc
)
{
	private const    int          DefaultRecursionLimit = 100;
	private readonly List<string> _resolverHistory      = [];
	private          int          _recursionLimit       = DefaultRecursionLimit;

	public async Task<Note> CreateNoteAsync(
		User user, Note.NoteVisibility visibility, string? text = null, string? cw = null, Note? reply = null,
		Note? renote = null, IReadOnlyCollection<DriveFile>? attachments = null, Poll? poll = null,
		bool localOnly = false
	)
	{
		if ((text?.Length ?? 0) + (cw?.Length ?? 0) > config.Value.CharacterLimit)
			throw GracefulException
				.BadRequest($"Text & content warning cannot exceed {config.Value.CharacterLimit} characters in total");
		if (text is { Length: > 100000 })
			throw GracefulException.BadRequest("Text cannot be longer than 100.000 characters");
		if (cw is { Length: > 100000 })
			throw GracefulException.BadRequest("Content warning cannot be longer than 100.000 characters");
		if (renote?.IsPureRenote ?? false)
			throw GracefulException.BadRequest("Cannot renote or quote a pure renote");
		if (reply?.IsPureRenote ?? false)
			throw GracefulException.BadRequest("Cannot reply to a pure renote");
		if (poll is { Choices.Count: < 2 })
			throw GracefulException.BadRequest("Polls must have at least two options");

		if (renote is { Visibility: > Note.NoteVisibility.Home } &&
		    renote.User != user &&
		    text == null &&
		    poll == null &&
		    attachments is not { Count: > 0 })
		{
			throw GracefulException.BadRequest("You're not allowed to renote this note");
		}

		if (renote != null && renote.User != user)
		{
			if (await db.Blockings.AnyAsync(p => p.Blockee == user && p.Blocker == renote.User))
				throw GracefulException.Forbidden("You are not allowed to interact with this user");
		}

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
			Text                 = text?.Trim(),
			Cw                   = cw?.Trim(),
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
			Tags                 = tags,
			LocalOnly            = localOnly
		};

		if (poll != null)
		{
			poll.Note           = note;
			poll.UserId         = note.User.Id;
			poll.UserHost       = note.UserHost;
			poll.Votes          = poll.Choices.Select(_ => 0).ToList();
			poll.NoteVisibility = note.Visibility;
			await db.AddAsync(poll);
			note.HasPoll = true;
			await EnqueuePollExpiryTask(poll);
		}

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

		if (localOnly) return note;

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
		                         .Select(p => new User { Id = p.Id })
		                         .ToListAsync();

		if (note.Visibility == Note.NoteVisibility.Specified)
			await deliverSvc.DeliverToAsync(activity, user, recipients.ToArray());
		else
			await deliverSvc.DeliverToFollowersAsync(activity, user, recipients);

		return note;
	}

	/// <remarks>
	///     This needs to be called before SaveChangesAsync on create & after on delete
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
		Note note, string? text = null, string? cw = null, IReadOnlyCollection<DriveFile>? attachments = null,
		Poll? poll = null
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
		if (cw != null && string.IsNullOrWhiteSpace(cw))
			cw = null;

		mentionedLocalUserIds = mentionedLocalUserIds.Except(previousMentionedLocalUserIds).ToList();
		note.Text             = text?.Trim();
		note.Cw               = cw?.Trim();
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

		if (poll != null)
		{
			if (note.Poll != null)
			{
				if (note.Poll.ExpiresAt != poll.ExpiresAt)
				{
					note.Poll.ExpiresAt = poll.ExpiresAt;
					await EnqueuePollExpiryTask(note.Poll);
				}

				if (!note.Poll.Choices.SequenceEqual(poll.Choices) || note.Poll.Multiple != poll.Multiple)
				{
					await db.PollVotes.Where(p => p.Note == note).ExecuteDeleteAsync();
					note.Poll.Choices  = poll.Choices;
					note.Poll.Votes    = poll.Choices.Select(_ => 0).ToList();
					note.Poll.Multiple = poll.Multiple;
					db.Update(note.Poll);
				}
			}
			else
			{
				poll.Note           = note;
				poll.UserId         = note.User.Id;
				poll.UserHost       = note.UserHost;
				poll.Votes          = poll.Choices.Select(_ => 0).ToList();
				poll.NoteVisibility = note.Visibility;
				await db.AddAsync(poll);
				await EnqueuePollExpiryTask(poll);
			}

			note.HasPoll = true;
		}

		db.Update(note);
		await db.AddAsync(noteEdit);
		await db.SaveChangesAsync();
		await notificationSvc.GenerateMentionNotifications(note, mentionedLocalUserIds);
		await notificationSvc.GenerateReplyNotifications(note, mentionedLocalUserIds);
		await notificationSvc.GenerateEditNotifications(note);
		eventSvc.RaiseNoteUpdated(this, note);

		if (note.LocalOnly) return note;

		var actor    = userRenderer.RenderLite(note.User);
		var obj      = await noteRenderer.RenderAsync(note, mentions);
		var activity = ActivityPub.ActivityRenderer.RenderUpdate(obj, actor);

		var recipients = await db.Users.Where(p => mentionedUserIds.Contains(p.Id))
		                         .Select(p => new User { Id = p.Id })
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
		                         .Select(p => new User { Id = p.Id })
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

	public async Task<Note?> ProcessNoteAsync(ASNote note, User actor, User? user = null)
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
		if (actor.IsSuspended)
			throw GracefulException.Forbidden("User is suspended");

		var reply = note.InReplyTo?.Id != null ? await ResolveNoteAsync(note.InReplyTo.Id, user: user) : null;

		if (reply is { HasPoll: true } && note.Name != null)
		{
			if (reply.UserHost != null)
				throw GracefulException.UnprocessableEntity("Poll vote not destined for this instance");

			var poll = await db.Polls.FirstOrDefaultAsync(p => p.Note == reply) ??
			           throw GracefulException.UnprocessableEntity("Poll does not exist");

			if (poll.Choices.All(p => p != note.Name))
				throw GracefulException.UnprocessableEntity("Unknown poll option");

			var existingVotes = await db.PollVotes.Where(p => p.User == actor && p.Note == reply).ToListAsync();
			if (existingVotes.Any(p => poll.Choices[p.Choice] == note.Name))
				throw GracefulException.UnprocessableEntity("Actor has already voted for this option");
			if (!poll.Multiple && existingVotes.Count != 0)
				throw GracefulException.UnprocessableEntity("Actor has already voted in this poll");

			var vote = new PollVote
			{
				Id        = IdHelpers.GenerateSlowflakeId(),
				CreatedAt = DateTime.UtcNow,
				User      = actor,
				Note      = reply,
				Choice    = poll.Choices.IndexOf(note.Name)
			};
			await db.AddAsync(vote);
			await db.SaveChangesAsync();
			await pollSvc.RegisterPollVote(vote, poll, reply);

			return null;
		}

		if (note.PublishedAt is null or { Year: < 2007 } || note.PublishedAt > DateTime.Now + TimeSpan.FromDays(3))
			throw GracefulException.UnprocessableEntity("Note.PublishedAt is nonsensical");

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
			Reply      = reply,
			Renote     = quoteUrl != null ? await ResolveNoteAsync(quoteUrl, user: user) : null
		};

		if (dbNote.Renote?.IsPureRenote ?? false)
			throw GracefulException.UnprocessableEntity("Cannot renote or quote a pure renote");
		if (dbNote.Reply?.IsPureRenote ?? false)
			throw GracefulException.UnprocessableEntity("Cannot reply to a pure renote");

		if (dbNote.Renote != null && dbNote.Renote.User != user)
		{
			if (await db.Blockings.AnyAsync(p => p.Blockee == user && p.Blocker == dbNote.Renote.User))
				throw GracefulException.Forbidden("You are not allowed to interact with this user");
		}

		if (note is ASQuestion question)
		{
			if (question is { AnyOf: not null, OneOf: not null })
				throw GracefulException.UnprocessableEntity("Polls cannot have both anyOf and oneOf set");

			var choices = (question.AnyOf ?? question.OneOf)?.Where(p => p.Name != null).ToList() ??
			              throw GracefulException.UnprocessableEntity("Polls must have either anyOf or oneOf set");

			if (choices.Count == 0)
				throw GracefulException.UnprocessableEntity("Poll must have at least one option");

			var poll = new Poll
			{
				Note           = dbNote,
				UserId         = dbNote.User.Id,
				UserHost       = dbNote.UserHost,
				ExpiresAt      = question.EndTime ?? question.Closed,
				Multiple       = question.AnyOf != null,
				Choices        = choices.Select(p => p.Name).Cast<string>().ToList(),
				NoteVisibility = dbNote.Visibility,
				Votes          = choices.Select(p => (int?)p.Replies?.TotalItems ?? 0).ToList()
			};

			await db.AddAsync(poll);
			dbNote.HasPoll = true;
			await EnqueuePollExpiryTask(poll);
		}

		if (dbNote.Reply != null)
		{
			dbNote.ReplyUserId   = dbNote.Reply.UserId;
			dbNote.ReplyUserHost = dbNote.Reply.UserHost;
			dbNote.ThreadId      = dbNote.Reply.ThreadId ?? dbNote.Reply.Id;
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
	public async Task<Note?> ProcessNoteUpdateAsync(ASNote note, User actor)
	{
		var dbNote = await db.Notes.IncludeCommonProperties()
		                     .Include(p => p.Poll)
		                     .FirstOrDefaultAsync(p => p.Uri == note.Id);

		if (dbNote == null) return await ProcessNoteAsync(note, actor);

		if (dbNote.User != actor)
			throw GracefulException.UnprocessableEntity("Refusing to update note of user other than actor");
		if (dbNote.User.IsSuspended)
			throw GracefulException.Forbidden("User is suspended");

		if (dbNote.UpdatedAt != null && dbNote.UpdatedAt > (note.UpdatedAt ?? DateTime.UtcNow))
			throw GracefulException.UnprocessableEntity("Note update is older than last known version");

		var noteEdit = new NoteEdit
		{
			Id        = IdHelpers.GenerateSlowflakeId(),
			UpdatedAt = note.UpdatedAt ?? DateTime.UtcNow,
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
			if (!dbNote.Mentions.SequenceEqual(mentionedUserIds))
				dbNote.Mentions = mentionedUserIds;
			if (!dbNote.MentionedRemoteUsers.SequenceEqual(remoteMentions))
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
				var missing = visibleUserIds.Except(dbNote.VisibleUserIds).ToList();
				if (missing.Count != 0)
					dbNote.VisibleUserIds.AddRange(missing);
			}

			dbNote.Text = mentionsResolver.ResolveMentions(dbNote.Text, dbNote.UserHost, mentions, splitDomainMapping);
			dbNote.Tags = ResolveHashtags(dbNote.Text, note);
		}

		var isPollEdited = false;

		if (note is ASQuestion question)
		{
			if (question is { AnyOf: not null, OneOf: not null })
				throw GracefulException.UnprocessableEntity("Polls cannot have both anyOf and oneOf set");

			var choices = (question.AnyOf ?? question.OneOf)?.Where(p => p.Name != null).ToList() ??
			              throw GracefulException.UnprocessableEntity("Polls must have either anyOf or oneOf set");

			if (choices.Count == 0)
				throw GracefulException.UnprocessableEntity("Poll must have at least one option");

			if (dbNote.Poll != null)
			{
				if (dbNote.Poll.ExpiresAt != (question.EndTime ?? question.Closed))
				{
					dbNote.Poll.ExpiresAt = question.EndTime ?? question.Closed;
					if (dbNote.Poll.ExpiresAt != null)
						await EnqueuePollExpiryTask(dbNote.Poll);
				}

				if (!dbNote.Poll.Choices.SequenceEqual(choices.Select(p => p.Name)) ||
				    dbNote.Poll.Multiple != (question.AnyOf != null))
				{
					isPollEdited = true;

					await db.PollVotes.Where(p => p.Note == dbNote).ExecuteDeleteAsync();
					dbNote.Poll.Choices  = choices.Select(p => p.Name).Cast<string>().ToList();
					dbNote.Poll.Votes    = choices.Select(p => (int?)p.Replies?.TotalItems ?? 0).ToList();
					dbNote.Poll.Multiple = question.AnyOf != null;
					db.Update(dbNote.Poll);
				}
				else
				{
					dbNote.Poll.Votes = choices.Select(p => (int?)p.Replies?.TotalItems ?? 0).ToList();
					db.Update(dbNote.Poll);
				}
			}
			else
			{
				isPollEdited = true;

				var poll = new Poll
				{
					Note           = dbNote,
					UserId         = dbNote.User.Id,
					UserHost       = dbNote.UserHost,
					ExpiresAt      = question.EndTime ?? question.Closed,
					Multiple       = question.AnyOf != null,
					Choices        = choices.Select(p => p.Name).Cast<string>().ToList(),
					NoteVisibility = dbNote.Visibility,
					Votes          = choices.Select(p => (int?)p.Replies?.TotalItems ?? 0).ToList()
				};

				await db.AddAsync(poll);
				await EnqueuePollExpiryTask(poll);
			}

			dbNote.HasPoll = true;
		}
		else
		{
			if (dbNote.HasPoll)
			{
				dbNote.HasPoll = false;
			}

			if (dbNote.Poll != null)
			{
				db.Remove(dbNote.Poll);
				dbNote.Poll = null;
			}
		}

		//TODO: handle updated alt text et al
		var sensitive = (note.Sensitive ?? false) || dbNote.Cw != null;
		var files     = await ProcessAttachmentsAsync(note.Attachments, actor, sensitive);
		dbNote.FileIds           = files.Select(p => p.Id).ToList();
		dbNote.AttachedFileTypes = files.Select(p => p.Type).ToList();

		if (note is not ASQuestion || isPollEdited || db.Entry(dbNote).State != EntityState.Unchanged)
		{
			dbNote.UpdatedAt = DateTime.UtcNow;
			await db.AddAsync(noteEdit);
		}

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
			await bgDb.UpsertRange(dbTags).On(p => p.Name).NoUpdate().RunAsync();
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

	public async Task<Note?> ResolveNoteAsync(
		string uri, ASNote? fetchedNote = null, User? user = null, bool clearHistory = false
	)
	{
		if (clearHistory)
		{
			_resolverHistory.Clear();
			_recursionLimit = DefaultRecursionLimit;
		}

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

	public async Task<bool> LikeNoteAsync(Note note, User user)
	{
		if (note.IsPureRenote)
			throw GracefulException.BadRequest("Cannot like a pure renote");

		if (!await db.NoteLikes.AnyAsync(p => p.Note == note && p.User == user))
		{
			if (await db.Blockings.AnyAsync(p => p.Blockee == user && p.Blocker == note.User))
				throw GracefulException.Forbidden("You are not allowed to interact with this user");

			var like = new NoteLike
			{
				Id        = IdHelpers.GenerateSlowflakeId(),
				CreatedAt = DateTime.UtcNow,
				User      = user,
				Note      = note
			};

			await db.NoteLikes.AddAsync(like);
			await db.SaveChangesAsync();
			await db.Notes.Where(p => p.Id == note.Id)
			        .ExecuteUpdateAsync(p => p.SetProperty(n => n.LikeCount, n => n.LikeCount + 1));

			if (user.Host == null && note.UserHost != null)
			{
				var activity = activityRenderer.RenderLike(like);
				await deliverSvc.DeliverToConditionalAsync(activity, user, note);
			}

			eventSvc.RaiseNoteLiked(this, note, user);
			await notificationSvc.GenerateLikeNotification(note, user);
			return true;
		}

		return false;
	}

	public async Task<bool> UnlikeNoteAsync(Note note, User user)
	{
		var like = await db.NoteLikes.Where(p => p.Note == note && p.User == user).FirstOrDefaultAsync();
		if (like == null) return false;
		db.Remove(like);
		await db.SaveChangesAsync();

		await db.Notes.Where(p => p.Id == note.Id)
		        .ExecuteUpdateAsync(p => p.SetProperty(n => n.LikeCount, n => n.LikeCount - 1));

		if (user.Host == null && note.UserHost != null)
		{
			var activity =
				activityRenderer.RenderUndo(userRenderer.RenderLite(user), activityRenderer.RenderLike(like));
			await deliverSvc.DeliverToConditionalAsync(activity, user, note);
		}

		eventSvc.RaiseNoteUnliked(this, note, user);
		await db.Notifications
		        .Where(p => p.Type == Notification.NotificationType.Like &&
		                    p.Notifiee == note.User &&
		                    p.Notifier == user)
		        .ExecuteDeleteAsync();

		return true;
	}

	public async Task<Note> LikeNoteAsync(ASNote note, User actor)
	{
		var dbNote = await ResolveNoteAsync(note) ?? throw new Exception("Cannot register like for unknown note");
		await LikeNoteAsync(dbNote, actor);
		return dbNote;
	}

	public async Task<Note> UnlikeNoteAsync(ASNote note, User user)
	{
		var dbNote = await ResolveNoteAsync(note) ?? throw new Exception("Cannot unregister like for unknown note");
		await UnlikeNoteAsync(dbNote, user);
		return dbNote;
	}

	public async Task BookmarkNoteAsync(Note note, User user)
	{
		if (user.Host != null) throw new Exception("This method is only valid for local users");

		if (note.IsPureRenote)
			throw GracefulException.BadRequest("Cannot bookmark a pure renote");

		if (!await db.NoteBookmarks.AnyAsync(p => p.Note == note && p.User == user))
		{
			var bookmark = new NoteBookmark
			{
				Id        = IdHelpers.GenerateSlowflakeId(),
				CreatedAt = DateTime.UtcNow,
				User      = user,
				Note      = note
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

		if (note.IsPureRenote)
			throw GracefulException.BadRequest("Cannot pin a pure renote");
		if (note.User != user)
			throw GracefulException.UnprocessableEntity("Validation failed: Someone else's post cannot be pinned");

		if (!await db.UserNotePins.AnyAsync(p => p.Note == note && p.User == user))
		{
			if (await db.UserNotePins.CountAsync(p => p.User == user) > 10)
				throw GracefulException.UnprocessableEntity("You cannot pin more than 10 notes at once.");

			var pin = new UserNotePin
			{
				Id        = IdHelpers.GenerateSlowflakeId(),
				CreatedAt = DateTime.UtcNow,
				User      = user,
				Note      = note
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

		var items = await collection.Items.Take(10)
		                            .Select(p => objectResolver.ResolveObject(p))
		                            .AwaitAllNoConcurrencyAsync();
		var notes = await items.OfType<ASNote>()
		                       .Select(p => ResolveNoteAsync(p.Id, p, null, true))
		                       .AwaitAllNoConcurrencyAsync();
		var previousPins = await db.Users.Where(p => p.Id == user.Id)
		                           .Select(p => p.PinnedNotes.Select(i => i.Id))
		                           .FirstOrDefaultAsync() ?? [];

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

	private async Task EnqueuePollExpiryTask(Poll poll)
	{
		if (!poll.ExpiresAt.HasValue) return;
		var job = new PollExpiryJobData { NoteId = poll.Note.Id };
		await queueSvc.BackgroundTaskQueue.ScheduleAsync(job, poll.ExpiresAt.Value);
	}

	public async Task<(string name, bool success)> ReactToNoteAsync(Note note, User user, string name)
	{
		if (note.IsPureRenote)
			throw GracefulException.BadRequest("Cannot react to a pure renote");

		name = await emojiSvc.ResolveEmojiName(name, user.Host);
		if (await db.NoteReactions.AnyAsync(p => p.Note == note && p.User == user && p.Reaction == name))
			return (name, false);

		if (await db.Blockings.AnyAsync(p => p.Blockee == user && p.Blocker == note.User))
			throw GracefulException.Forbidden("You are not allowed to interact with this user");

		var reaction = new NoteReaction
		{
			Id        = IdHelpers.GenerateSlowflakeId(),
			CreatedAt = DateTime.UtcNow,
			Note      = note,
			User      = user,
			Reaction  = name
		};

		await db.AddAsync(reaction);
		await db.SaveChangesAsync();
		eventSvc.RaiseNoteReacted(this, reaction);
		await notificationSvc.GenerateReactionNotification(reaction);

		await db.Database
		        .ExecuteSqlAsync($"""UPDATE "note" SET "reactions" = jsonb_set("reactions", ARRAY[{name}], (COALESCE("reactions"->>{name}, '0')::int + 1)::text::jsonb) WHERE "id" = {note.Id}""");

		if (user.Host == null)
		{
			var emoji    = await emojiSvc.ResolveEmoji(reaction.Reaction);
			var activity = activityRenderer.RenderReact(reaction, emoji);
			await deliverSvc.DeliverToConditionalAsync(activity, user, note);
		}

		return (name, true);
	}

	public async Task ReactToNoteAsync(ASNote note, User actor, string name)
	{
		var dbNote = await ResolveNoteAsync(note.Id, note.VerifiedFetch ? note : null);
		if (dbNote == null)
			throw GracefulException.UnprocessableEntity("Failed to resolve reaction target");

		await ReactToNoteAsync(dbNote, actor, name);
	}

	public async Task<(string name, bool success)> RemoveReactionFromNoteAsync(Note note, User user, string name)
	{
		name = await emojiSvc.ResolveEmojiName(name, user.Host);

		var reaction =
			await db.NoteReactions.FirstOrDefaultAsync(p => p.Note == note && p.User == user && p.Reaction == name);

		if (reaction == null) return (name, false);
		db.Remove(reaction);
		await db.SaveChangesAsync();
		eventSvc.RaiseNoteUnreacted(this, reaction);

		await db.Database
		        .ExecuteSqlAsync($"""UPDATE "note" SET "reactions" = jsonb_set("reactions", ARRAY[{name}], (COALESCE("reactions"->>{name}, '1')::int - 1)::text::jsonb) WHERE "id" = {note.Id}""");

		if (user.Host == null)
		{
			var actor    = userRenderer.RenderLite(user);
			var emoji    = await emojiSvc.ResolveEmoji(reaction.Reaction);
			var activity = activityRenderer.RenderUndo(actor, activityRenderer.RenderReact(reaction, emoji));
			await deliverSvc.DeliverToConditionalAsync(activity, user, note);
		}

		if (note.User.Host == null && note.User != user)
		{
			await db.Notifications
			        .Where(p => p.Note == note &&
			                    p.Notifier == user &&
			                    p.Type == Notification.NotificationType.Reaction)
			        .ExecuteDeleteAsync();
		}

		return (name, true);
	}

	public async Task RemoveReactionFromNoteAsync(ASNote note, User actor, string name)
	{
		var dbNote = await ResolveNoteAsync(note.Id, note.VerifiedFetch ? note : null);
		if (dbNote == null) return;
		await RemoveReactionFromNoteAsync(dbNote, actor, name);
	}
}