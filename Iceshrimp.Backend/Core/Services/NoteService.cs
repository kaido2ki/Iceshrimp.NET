using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;
using AsyncKeyedLock;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Queues;
using Iceshrimp.MfmSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static Iceshrimp.Backend.Core.Federation.ActivityPub.UserResolver;

namespace Iceshrimp.Backend.Core.Services;

public class NoteService(
	ILogger<NoteService> logger,
	DatabaseContext db,
	ActivityPub.UserResolver userResolver,
	IOptionsSnapshot<Config.InstanceSection> config,
	IOptionsSnapshot<Config.BackfillSection> backfillConfig,
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
	PollService pollSvc,
	ActivityPub.FederationControlService fedCtrlSvc,
	PolicyService policySvc
) : IScopedService
{
	private const int DefaultRecursionLimit = 100;

	private static readonly AsyncKeyedLocker<string> KeyedLocker = new(o =>
	{
		o.PoolSize        = 100;
		o.PoolInitialFill = 5;
	});

	private readonly List<string> _resolverHistory = [];
	private          int          _recursionLimit  = DefaultRecursionLimit;
	internal         int          NotesFetched => DefaultRecursionLimit - _recursionLimit;

	public class NoteCreationData
	{
		public required User                            User;
		public required Note.NoteVisibility             Visibility;
		public          string?                         Text;
		public          IMfmNode[]?                     ParsedText;
		public          string?                         Cw;
		public          Note?                           Reply;
		public          Note?                           Renote;
		public          IReadOnlyCollection<DriveFile>? Attachments;
		public          Poll?                           Poll;
		public          bool                            LocalOnly;
		public          string?                         Uri;
		public          string?                         Url;
		public          List<string>?                   Emoji;
		public          NoteMentionData?                ResolvedMentions;
		public          DateTime?                       CreatedAt;
		public          ASNote?                         ASNote;
		public          string?                         ReplyUri;
		public          string?                         RenoteUri;
	}

	public class NoteUpdateData
	{
		public required Note                            Note;
		public          string?                         Text;
		public          IMfmNode[]?                     ParsedText;
		public          string?                         Cw;
		public          IReadOnlyCollection<DriveFile>? Attachments;
		public          Poll?                           Poll;
		public          DateTime?                       UpdatedAt;
		public          NoteMentionData?                ResolvedMentions;
		public          ASNote?                         ASNote;
		public          List<string>?                   Emoji;
	}

	public record struct NoteMentionData(
		List<string> MentionedUserIds,
		List<string> MentionedLocalUserIds,
		List<Note.MentionedUser> Mentions,
		List<Note.MentionedUser> RemoteMentions,
		Dictionary<(string usernameLower, string webDomain), string> SplitDomainMapping
	);

	public async Task<Note> CreateNoteAsync(NoteCreationData data)
	{
		logger.LogDebug("Creating note for user {id}", data.User.Id);

		await policySvc.InitializeAsync();

		// @formatter:off
		if (data.User.IsRemoteUser && policySvc.ShouldReject(data, out var policy))
			throw GracefulException.UnprocessableEntity($"Note was rejected by {policy.Name}");
		if (data.User.IsLocalUser && (data.Text?.Length ?? 0) + (data.Cw?.Length ?? 0) > config.Value.CharacterLimit)
			throw GracefulException.UnprocessableEntity($"Text & content warning cannot exceed {config.Value.CharacterLimit} characters in total");
		if (data.User.IsSystemUser)
			throw GracefulException.BadRequest("System users cannot create notes");
		if (data.Text is { Length: > 100000 })
			throw GracefulException.UnprocessableEntity("Text cannot be longer than 100.000 characters");
		if (data.Cw is { Length: > 100000 })
			throw GracefulException.UnprocessableEntity("Content warning cannot be longer than 100.000 characters");
		if (data.Renote?.IsPureRenote ?? false)
			throw GracefulException.UnprocessableEntity("Cannot renote or quote a pure renote");
		if (data.Reply?.IsPureRenote ?? false)
			throw GracefulException.UnprocessableEntity("Cannot reply to a pure renote");
		if (data.User.IsSuspended)
			throw GracefulException.Forbidden("User is suspended");
		if (data.Attachments != null && data.Attachments.Any(p => p.UserId != data.User.Id))
			throw GracefulException.UnprocessableEntity("Refusing to create note with files belonging to someone else");
		// @formatter:on

		data.Poll?.Choices.RemoveAll(string.IsNullOrWhiteSpace);
		if (data.Poll is { Choices.Count: < 2 })
			throw GracefulException.UnprocessableEntity("Polls must have at least two options");

		data.ParsedText = data.Text != null ? MfmParser.Parse(data.Text.ReplaceLineEndings("\n")) : null;
		policySvc.CallRewriteHooks(data, IRewritePolicy.HookLocationEnum.PreLogic);

		if (!data.LocalOnly && (data.Renote is { LocalOnly: true } || data.Reply is { LocalOnly: true }))
			data.LocalOnly = true;

		if (data.Renote != null)
		{
			var pureRenote = data.Text == null && data.Poll == null && data.Attachments is not { Count: > 0 };

			if (data.Renote.Visibility > Note.NoteVisibility.Followers)
			{
				var target = pureRenote ? "renote" : "quote";
				throw GracefulException.UnprocessableEntity($"You're not allowed to {target} this note");
			}

			if (data.Renote.User != data.User)
			{
				if (pureRenote && data.Renote.Visibility > Note.NoteVisibility.Home)
					throw GracefulException.UnprocessableEntity("You're not allowed to renote this note");
				if (await db.Blockings.AnyAsync(p => p.Blockee == data.User && p.Blocker == data.Renote.User))
					throw GracefulException.Forbidden($"You are not allowed to interact with @{data.Renote.User.Acct}");
			}

			if (pureRenote && data.Renote.Visibility > data.Visibility)
				data.Visibility = data.Renote.Visibility;
		}

		var (mentionedUserIds, mentionedLocalUserIds, mentions, remoteMentions, splitDomainMapping) =
			data.ResolvedMentions ?? await ResolveNoteMentionsAsync(data.ParsedText);

		// ReSharper disable once EntityFramework.UnsupportedServerSideFunctionCall
		if (mentionedUserIds.Count > 0)
		{
			var blockAcct = await db.Users
			                        .Where(p => mentionedUserIds.Contains(p.Id) && p.ProhibitInteractionWith(data.User))
			                        .Select(p => p.Acct)
			                        .FirstOrDefaultAsync();
			if (blockAcct != null)
				throw GracefulException.Forbidden($"You're not allowed to interact with @{blockAcct}");
		}

		if (data.Text != null && string.IsNullOrWhiteSpace(data.Text))
		{
			data.Text       = null;
			data.ParsedText = null;
		}
		else if (data.Text != null)
		{
			mentionsResolver.ResolveMentions(data.ParsedText, data.User.Host, mentions, splitDomainMapping);
		}

		data.Cw = data.Cw?.ReplaceLineEndings("\n").Trim();
		if (data.Cw != null && string.IsNullOrWhiteSpace(data.Cw))
			data.Cw = null;

		if ((data.User.UserSettings?.PrivateMode ?? false) && data.Visibility < Note.NoteVisibility.Followers)
			data.Visibility = Note.NoteVisibility.Followers;

		// Enforce UserSettings.AlwaysMarkSensitive, if configured
		if (
			(data.User.UserSettings?.AlwaysMarkSensitive ?? false)
			&& (data.Attachments?.Any(p => !p.IsSensitive) ?? false)
		)
		{
			foreach (var driveFile in data.Attachments.Where(p => !p.IsSensitive)) driveFile.IsSensitive = true;
			await db.DriveFiles.Where(p => data.Attachments.Select(a => a.Id).Contains(p.Id) && !p.IsSensitive)
			        .ExecuteUpdateAsync(p => p.SetProperty(i => i.IsSensitive, _ => true));
		}

		var tags = ResolveHashtags(data.ParsedText, data.ASNote);
		if (tags.Count > 0 && data.Text != null && data.ASNote != null)
		{
			// @formatter:off
			var match = data.ASNote.Tags?.OfType<ASHashtag>().Where(p => p.Name != null && p.Href != null) ?? [];
			//TODO: refactor this to use the nodes object instead of matching on text
			data.Text = match.Aggregate(data.Text, (current, tag) => current.Replace($"[#{tag.Name!.TrimStart('#')}]({tag.Href})", $"#{tag.Name!.TrimStart('#')}")
			                                                      .Replace($"#[{tag.Name!.TrimStart('#')}]({tag.Href})", $"#{tag.Name!.TrimStart('#')}"));
			// @formatter:on
		}

		var mastoReplyUserId = data.Reply?.UserId != data.User.Id
			? data.Reply?.UserId
			: data.Reply.MastoReplyUserId ?? data.Reply.ReplyUserId ?? data.Reply.UserId;

		if (data.Emoji == null && data.User.IsLocalUser && data.ParsedText != null)
		{
			data.Emoji = (await emojiSvc.ResolveEmojiAsync(data.ParsedText)).Select(p => p.Id).ToList();
		}

		List<string> visibleUserIds = [];
		if (data.Visibility == Note.NoteVisibility.Specified)
		{
			if (data.ASNote != null)
			{
				// @formatter:off
				var recipients = data.ASNote.GetRecipients(data.User);
				if (recipients.Count > 100)
					throw GracefulException.UnprocessableEntity("Refusing to process note with more than 100 recipients");
				// @formatter:on

				visibleUserIds = (await recipients
				                        .Select(p => userResolver.ResolveOrNullAsync(p, EnforceUriFlags))
				                        .AwaitAllNoConcurrencyAsync())
				                 .NotNull()
				                 .Select(p => p.Id)
				                 .Concat(mentionedUserIds)
				                 .Append(data.Reply?.UserId)
				                 .OfType<string>()
				                 .Distinct()
				                 .ToList();
			}
			else
			{
				visibleUserIds = mentionedUserIds;
			}
		}

		var combinedAltText = data.Attachments?.Select(p => p.Comment).Where(c => c != null);
		policySvc.CallRewriteHooks(data, IRewritePolicy.HookLocationEnum.PostLogic);

		var noteId   = IdHelpers.GenerateSnowflakeId(data.CreatedAt);
		var threadId = data.Reply?.ThreadId ?? noteId;

		var context   = data.ASNote?.Context;
		var contextId = context?.Id;

		var thread = contextId != null
			? await db.NoteThreads.Where(t => t.Uri == contextId || t.Id == threadId).FirstOrDefaultAsync()
			: await db.NoteThreads.Where(t => t.Id == threadId).FirstOrDefaultAsync();

		var   contextOwner      = data.User.IsLocalUser ? data.User : null;
		bool? contextResolvable = data.User.IsLocalUser ? null : false;

		if (thread == null && context != null)
		{
			try
			{
				if (await objectResolver.ResolveObjectAsync(context) is ASCollection maybeContext)
				{
					context           = maybeContext;
					contextResolvable = true;

					var owner = context.AttributedTo?.FirstOrDefault();
					if (owner?.Id != null
					    && Uri.TryCreate(owner.Id, UriKind.Absolute, out var ownerUri)
					    && Uri.TryCreate(contextId, UriKind.Absolute, out var contextUri)
					    && ownerUri.Host == contextUri.Host)
						contextOwner = await userResolver.ResolveOrNullAsync(owner.Id, ResolveFlags.Uri);
				}
			}
			catch
			{
				/*
				 * some instance software such as the Pleroma family expose a context that isn't resolvable, which is permitted by spec.
				 * in that case we still use it for threading but mark it as unresolvable.
				 */
			}
		}

		thread ??= new NoteThread
		{
			Id           = threadId,
			Uri          = contextId,
			User         = contextOwner,
			IsResolvable = contextResolvable,
		};

		var note = new Note
		{
			Id                   = noteId,
			Uri                  = data.Uri,
			Url                  = data.Url,
			Text                 = data.ParsedText?.Serialize(),
			Cw                   = data.Cw,
			Reply                = data.Reply,
			ReplyUserId          = data.Reply?.UserId,
			MastoReplyUserId     = mastoReplyUserId,
			ReplyUserHost        = data.Reply?.UserHost,
			Renote               = data.Renote,
			RenoteUserId         = data.Renote?.UserId,
			RenoteUserHost       = data.Renote?.UserHost,
			User                 = data.User,
			CreatedAt            = data.CreatedAt ?? DateTime.UtcNow,
			UserHost             = data.User.Host,
			Visibility           = data.Visibility,
			FileIds              = data.Attachments?.Select(p => p.Id).ToList() ?? [],
			AttachedFileTypes    = data.Attachments?.Select(p => p.Type).ToList() ?? [],
			Mentions             = mentionedUserIds,
			VisibleUserIds       = visibleUserIds,
			MentionedRemoteUsers = remoteMentions,
			Thread               = thread,
			Tags                 = tags,
			LocalOnly            = data.LocalOnly,
			Emojis               = data.Emoji ?? [],
			ReplyUri             = data.ReplyUri,
			RenoteUri            = data.RenoteUri,
			RepliesCollection    = data.ASNote?.Replies?.Id,
			CombinedAltText      = combinedAltText != null ? string.Join(' ', combinedAltText) : null
		};

		if (data.Poll != null)
		{
			data.Poll.Note           =   note;
			data.Poll.UserId         =   note.User.Id;
			data.Poll.UserHost       =   note.UserHost;
			data.Poll.NoteVisibility =   note.Visibility;
			data.Poll.VotersCount    ??= note.UserHost == null ? 0 : null;

			if (data.Poll.Votes == null! || data.Poll.Votes.Count != data.Poll.Choices.Count)
				data.Poll.Votes = data.Poll.Choices.Select(_ => 0).ToList();

			await db.AddAsync(data.Poll);
			note.HasPoll = true;
			await EnqueuePollExpiryTaskAsync(data.Poll);
		}

		logger.LogDebug("Inserting created note {noteId} for user {userId} into the database", note.Id, data.User.Id);

		await UpdateNoteCountersAsync(note, true);
		await db.AddAsync(note);
		await db.SaveChangesAsync();
		eventSvc.RaiseNotePublished(this, note);
		await notificationSvc.GenerateMentionNotificationsAsync(note, mentionedLocalUserIds);
		await notificationSvc.GenerateReplyNotificationsAsync(note, mentionedLocalUserIds);
		await notificationSvc.GenerateRenoteNotificationAsync(note);

		logger.LogDebug("Note {id} created successfully", note.Id);

		if (data.Uri != null || data.Url != null)
		{
			_ = followupTaskSvc.ExecuteTaskAsync("ResolvePendingReplyRenoteTargets", async provider =>
			{
				var bgDb  = provider.GetRequiredService<DatabaseContext>();
				var count = 0;

				if (data.Uri != null)
				{
					count +=
						await bgDb.Notes.Where(p => p.ReplyUri == data.Uri)
						          .ExecuteUpdateAsync(p => p.SetProperty(i => i.ReplyUri, _ => null)
						                                    .SetProperty(i => i.ReplyId, _ => note.Id)
						                                    .SetProperty(i => i.ReplyUserId, _ => note.UserId)
						                                    .SetProperty(i => i.ReplyUserHost, _ => note.UserHost)
						                                    .SetProperty(i => i.MastoReplyUserId,
						                                                 i => i.UserId != data.User.Id
							                                                 ? i.UserId
							                                                 : mastoReplyUserId));

					count +=
						await bgDb.Notes.Where(p => p.RenoteUri == data.Uri)
						          .ExecuteUpdateAsync(p => p.SetProperty(i => i.RenoteUri, _ => null)
						                                    .SetProperty(i => i.RenoteId, _ => note.Id)
						                                    .SetProperty(i => i.RenoteUserId, _ => note.UserId)
						                                    .SetProperty(i => i.RenoteUserHost, _ => note.UserHost));
				}

				if (data.Url != null)
				{
					count +=
						await bgDb.Notes.Where(p => p.ReplyUri == data.Url)
						          .ExecuteUpdateAsync(p => p.SetProperty(i => i.ReplyUri, _ => null)
						                                    .SetProperty(i => i.ReplyId, _ => note.Id)
						                                    .SetProperty(i => i.ReplyUserId, _ => note.UserId)
						                                    .SetProperty(i => i.ReplyUserHost, _ => note.UserHost)
						                                    .SetProperty(i => i.MastoReplyUserId,
						                                                 i => i.UserId != data.User.Id
							                                                 ? i.UserId
							                                                 : mastoReplyUserId));
					count +=
						await bgDb.Notes.Where(p => p.RenoteUri == data.Url)
						          .ExecuteUpdateAsync(p => p.SetProperty(i => i.RenoteUri, _ => null)
						                                    .SetProperty(i => i.RenoteId, _ => note.Id)
						                                    .SetProperty(i => i.RenoteUserId, _ => note.UserId)
						                                    .SetProperty(i => i.RenoteUserHost, _ => note.UserHost));
				}

				if (count > 0)
				{
					await bgDb.Notes.Where(p => p.Id == note.Id)
					          .ExecuteUpdateAsync(p => p.SetProperty(i => i.RepliesCount,
					                                                 i => bgDb.Notes.Count(n => n.ReplyId == i.Id))
					                                    .SetProperty(i => i.RenoteCount,
					                                                 i => bgDb.Notes.Count(n => n.RenoteId == i.Id
						                                                 && n.IsPureRenote)));
				}
			});
		}

		if (data.User.IsRemoteUser)
		{
			_ = followupTaskSvc.ExecuteTaskAsync("UpdateInstanceNoteCounter", async provider =>
			{
				var bgDb          = provider.GetRequiredService<DatabaseContext>();
				var bgInstanceSvc = provider.GetRequiredService<InstanceService>();
				var dbInstance    = await bgInstanceSvc.GetUpdatedInstanceMetadataAsync(data.User);
				await bgDb.Instances.Where(p => p.Id == dbInstance.Id)
				          .ExecuteUpdateAsync(p => p.SetProperty(i => i.NotesCount, i => i.NotesCount + 1));
			});

			return note;
		}

		if (data.LocalOnly) return note;

		var actor = userRenderer.RenderLite(data.User);
		ASActivity activity = note is { IsPureRenote: true, Renote: not null }
			? ActivityPub.ActivityRenderer.RenderAnnounce(note.Renote.User == note.User
				                                              ? await noteRenderer.RenderAsync(note.Renote)
				                                              : noteRenderer.RenderLite(note.Renote),
			                                              note.GetPublicUri(config.Value), actor,
			                                              note.Visibility,
			                                              data.User.GetPublicUri(config.Value) + "/followers")
			: ActivityPub.ActivityRenderer.RenderCreate(await noteRenderer.RenderAsync(note, mentions), actor);

		List<string> additionalUserIds =
			note is { IsPureRenote: true, Renote: not null, Visibility: < Note.NoteVisibility.Followers }
				? [note.Renote.User.Id]
				: [];

		if (note.Reply?.ReplyUserId is { } replyUserId)
			additionalUserIds.Add(replyUserId);

		var recipientIds = mentionedUserIds.Concat(additionalUserIds);
		await deliverSvc.DeliverToConditionalAsync(activity, note.User, note, recipientIds);

		return note;
	}

	/// <remarks>
	///     This needs to be called before SaveChangesAsync on create, and afterwards on delete
	/// </remarks>
	private async Task UpdateNoteCountersAsync(Note note, bool create)
	{
		var diff = create ? 1 : -1;

		if (note is { Renote.Id: not null, IsPureRenote: true })
		{
			if (!await db.Notes.AnyAsync(p => p.UserId == note.User.Id
			                                  && p.RenoteId == note.Renote.Id
			                                  && p.IsPureRenote))
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

	private static List<string> GetInlineMediaUrls(Span<IMfmNode> mfm)
	{
		List<string> urls = [];

		foreach (var node in mfm)
		{
			if (node is MfmFnNode { Name: "media" } fn)
			{
				var urlNode = fn.Children.FirstOrDefault();
				if (urlNode is MfmUrlNode url) urls.Add(url.Url);
			}

			urls.AddRange(GetInlineMediaUrls(node.Children));
		}

		return urls;
	}

	[OverloadResolutionPriority(1)]
	private static List<string> GetInlineMediaUrls(Span<IMfmInlineNode> mfm)
	{
		List<string> urls = [];

		foreach (var node in mfm)
		{
			if (node is MfmFnNode { Name: "media" } fn)
			{
				var urlNode = fn.Children.FirstOrDefault();
				if (urlNode is MfmUrlNode url) urls.Add(url.Url);
			}

			urls.AddRange(GetInlineMediaUrls(node.Children));
		}

		return urls;
	}

	public async Task<Note> UpdateNoteAsync(NoteUpdateData data)
	{
		logger.LogDebug("Processing note update for note {id}", data.Note.Id);

		var note = data.Note;
		if (note.User.IsLocalUser && (data.Text?.Length ?? 0) + (data.Cw?.Length ?? 0) > config.Value.CharacterLimit)
			throw GracefulException
				.UnprocessableEntity($"Text & content warning cannot exceed {config.Value.CharacterLimit} characters in total");
		if (data.Text is { Length: > 100000 })
			throw GracefulException.UnprocessableEntity("Text cannot be longer than 100.000 characters");
		if (data.Cw is { Length: > 100000 })
			throw GracefulException.UnprocessableEntity("Content warning cannot be longer than 100.000 characters");
		if (data.Attachments != null && data.Attachments.Any(p => p.UserId != note.User.Id))
			throw GracefulException.UnprocessableEntity("Refusing to create note with files belonging to someone else");

		var noteEdit = new NoteEdit
		{
			Id        = IdHelpers.GenerateSnowflakeId(),
			UpdatedAt = data.UpdatedAt ?? DateTime.UtcNow,
			Note      = note,
			Text      = note.Text,
			Cw        = note.Cw,
			FileIds   = note.FileIds
		};

		data.ParsedText = data.Text != null ? MfmParser.Parse(data.Text.ReplaceLineEndings("\n")) : null;
		policySvc.CallRewriteHooks(data, IRewritePolicy.HookLocationEnum.PreLogic);

		var previousMentionedLocalUserIds = await db.Users.Where(p => note.Mentions.Contains(p.Id) && p.IsLocalUser)
		                                            .Select(p => p.Id)
		                                            .ToListAsync();

		var previousMentionedUserIds = await db.Users.Where(p => note.Mentions.Contains(p.Id))
		                                       .Select(p => p.Id)
		                                       .ToListAsync();

		var (mentionedUserIds, mentionedLocalUserIds, mentions, remoteMentions, splitDomainMapping) =
			data.ResolvedMentions ?? await ResolveNoteMentionsAsync(data.ParsedText);

		if (data.Text != null && string.IsNullOrWhiteSpace(data.Text))
		{
			data.Text       = null;
			data.ParsedText = null;
		}
		else if (data.Text != null)
		{
			mentionsResolver.ResolveMentions(data.ParsedText, note.User.Host, mentions, splitDomainMapping);
		}

		data.Cw = data.Cw?.ReplaceLineEndings("\n").Trim();
		if (data.Cw != null && string.IsNullOrWhiteSpace(data.Cw))
			data.Cw = null;

		// ReSharper disable EntityFramework.UnsupportedServerSideFunctionCall
		if (mentionedUserIds.Except(previousMentionedUserIds).Any())
		{
			var blockAcct = await db.Users
			                        .Where(p => mentionedUserIds.Except(previousMentionedUserIds).Contains(p.Id)
			                                    && (p.IsBlocking(note.User) || p.IsBlockedBy(note.User)))
			                        .Select(p => p.Acct)
			                        .FirstOrDefaultAsync();
			if (blockAcct != null)
				throw GracefulException.Forbidden($"You're not allowed to interact with @{blockAcct}");
		}
		// ReSharper restore EntityFramework.UnsupportedServerSideFunctionCall

		mentionedLocalUserIds = mentionedLocalUserIds.Except(previousMentionedLocalUserIds).ToList();
		note.Tags             = ResolveHashtags(data.ParsedText, data.ASNote);

		if (note.User.IsLocalUser && data.ParsedText != null)
			data.Emoji = (await emojiSvc.ResolveEmojiAsync(data.ParsedText)).Select(p => p.Id).ToList();
		if (data.Emoji != null && !note.Emojis.IsEquivalent(data.Emoji))
			note.Emojis = data.Emoji;
		else if (data.Emoji == null && note.Emojis.Count != 0)
			note.Emojis = [];

		if (data.Text is not null)
		{
			if (!note.Mentions.IsEquivalent(mentionedUserIds))
				note.Mentions = mentionedUserIds;
			if (!note.MentionedRemoteUsers.Select(p => p.Uri).IsEquivalent(remoteMentions.Select(p => p.Uri)))
				note.MentionedRemoteUsers = remoteMentions;

			if (note.Visibility == Note.NoteVisibility.Specified)
			{
				var visibleUserIds = mentionedUserIds.ToList();
				if (data.ASNote != null)
				{
					// @formatter:off
					var recipients = data.ASNote.GetRecipients(note.User);
					if (recipients.Count > 100)
						throw GracefulException.UnprocessableEntity("Refusing to process note update with more than 100 recipients");
					// @formatter:on

					visibleUserIds = (await recipients
					                        .Select(p => userResolver.ResolveOrNullAsync(p, EnforceUriFlags))
					                        .AwaitAllNoConcurrencyAsync())
					                 .NotNull()
					                 .Select(p => p.Id)
					                 .Concat(visibleUserIds)
					                 .ToList();
				}

				if (note.ReplyUserId != null)
					visibleUserIds.Add(note.ReplyUserId);

				// We want to make sure not to revoke visibility
				var missing = visibleUserIds.Except(note.VisibleUserIds).ToList();
				if (missing.Count != 0)
					note.VisibleUserIds.AddRange(missing);
			}

			mentionsResolver.ResolveMentions(data.ParsedText, note.UserHost, mentions, splitDomainMapping);
		}

		var attachments     = data.Attachments?.ToList() ?? [];
		var inlineMediaUrls = data.ParsedText != null ? GetInlineMediaUrls(data.ParsedText) : [];
		var newMediaUrls    = data.Attachments?.Select(p => p.Url) ?? [];
		var missingUrls     = inlineMediaUrls.Except(newMediaUrls).ToArray();

		if (missingUrls.Length > 0)
		{
			var missingAttachments = await db.DriveFiles
			                                 .Where(p => missingUrls.Contains(p.PublicUrl ?? p.Url)
			                                             && p.UserId == note.UserId)
			                                 .ToArrayAsync();

			attachments.AddRange(missingAttachments);
		}

		//TODO: handle updated alt text et al
		var fileIds = attachments.Select(p => p.Id).ToList();
		if (!note.FileIds.IsEquivalent(fileIds))
		{
			note.FileIds           = fileIds;
			note.AttachedFileTypes = attachments.Select(p => p.Type).ToList();

			var combinedAltText = attachments.Select(p => p.Comment).Where(c => c != null);
			note.CombinedAltText = string.Join(' ', combinedAltText);
		}

		note.Text = data.Text = data.ParsedText?.Serialize();
		note.Cw   = data.Cw;

		var isPollEdited = false;

		var poll = data.Poll;
		if (poll != null)
		{
			poll.Choices.RemoveAll(string.IsNullOrWhiteSpace);
			if (poll.Choices.Count < 2)
				throw GracefulException.UnprocessableEntity("Polls must have at least two options");

			if (note.Poll != null)
			{
				if (note.Poll.ExpiresAt != poll.ExpiresAt)
				{
					note.Poll.ExpiresAt = poll.ExpiresAt;
					await EnqueuePollExpiryTaskAsync(note.Poll);
				}

				if (
					!note.Poll.Choices.SequenceEqual(poll.Choices)
					|| note.Poll.Multiple != poll.Multiple
					|| note.Text != noteEdit.Text
				)
				{
					isPollEdited = true;

					await db.PollVotes.Where(p => p.Note == note).ExecuteDeleteAsync();
					note.Poll.Choices  = poll.Choices;
					note.Poll.Multiple = poll.Multiple;
					note.Poll.Votes = poll.Votes == null! || poll.Votes.Count != poll.Choices.Count
						? poll.Choices.Select(_ => 0).ToList()
						: poll.Votes;
					note.Poll.VotersCount =
						poll.VotersCount
						?? (note.Poll.VotersCount == null
							? null
							: Math.Max(note.Poll.VotersCount.Value, note.Poll.Votes.Sum()));
				}
				else if (poll.Votes.Count == poll.Choices.Count)
				{
					note.Poll.Votes = poll.Votes;
					note.Poll.VotersCount =
						poll.VotersCount
						?? (note.Poll.VotersCount == null
							? null
							: Math.Max(note.Poll.VotersCount.Value, note.Poll.Votes.Sum()));
				}
			}
			else
			{
				isPollEdited = true;

				poll.Note           = note;
				poll.UserId         = note.User.Id;
				poll.UserHost       = note.UserHost;
				poll.NoteVisibility = note.Visibility;
				if (poll.Votes == null! || poll.Votes.Count != poll.Choices.Count)
					poll.Votes = poll.Choices.Select(_ => 0).ToList();

				await db.AddAsync(poll);
				await EnqueuePollExpiryTaskAsync(poll);
			}

			note.HasPoll = true;
		}
		else
		{
			if (note.HasPoll)
				note.HasPoll = false;

			if (note.Poll != null)
			{
				db.Remove(note.Poll);
				note.Poll = null;
			}
		}

		var isEdit = data.ASNote is not ASQuestion
		             || poll == null
		             || isPollEdited
		             || db.Entry(note).State != EntityState.Unchanged;

		if (isEdit)
		{
			note.UpdatedAt = data.UpdatedAt ?? DateTime.UtcNow;
			await db.AddAsync(noteEdit);
		}

		if (data.ASNote != null)
			note.RepliesCollection = data.ASNote.Replies?.Id;

		policySvc.CallRewriteHooks(data, IRewritePolicy.HookLocationEnum.PostLogic);
		await db.SaveChangesAsync();
		eventSvc.RaiseNoteUpdated(this, note);

		if (!isEdit) return note;

		await notificationSvc.GenerateMentionNotificationsAsync(note, mentionedLocalUserIds);
		await notificationSvc.GenerateEditNotificationsAsync(note);

		if (note.LocalOnly || note.User.IsRemoteUser) return note;

		var actor    = userRenderer.RenderLite(note.User);
		var obj      = await noteRenderer.RenderAsync(note, mentions);
		var activity = ActivityPub.ActivityRenderer.RenderUpdate(obj, actor);

		List<string> additionalUserIds =
			note is { IsPureRenote: true, Renote: not null, Visibility: < Note.NoteVisibility.Followers }
				? [note.Renote.User.Id]
				: [];

		if (note.Reply?.ReplyUserId is { } replyUserId)
			additionalUserIds.Add(replyUserId);

		var recipientIds = mentionedUserIds.Concat(additionalUserIds);

		await deliverSvc.DeliverToConditionalAsync(activity, note.User, note, recipientIds);
		return note;
	}

	public async Task DeleteNoteAsync(Note note)
	{
		logger.LogDebug("Deleting note '{id}' owned by {userId}", note.Id, note.User.Id);

		db.Remove(note);
		eventSvc.RaiseNoteDeleted(this, note);
		await db.SaveChangesAsync();
		await UpdateNoteCountersAsync(note, false);

		if (note.User.IsRemoteUser)
		{
			if (note.User.Uri != null)
			{
				_ = followupTaskSvc.ExecuteTaskAsync("UpdateInstanceNoteCounter", async provider =>
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
		// @formatter:off
		ASActivity activity = note.IsPureRenote
			? activityRenderer.RenderUndo(actor, ActivityPub.ActivityRenderer.RenderAnnounce(
			                               noteRenderer.RenderLite(note.Renote ?? throw new Exception("Refusing to undo renote without renote")),
			                               note.GetPublicUri(config.Value), actor, note.Visibility,
			                               note.User.GetPublicUri(config.Value) + "/followers"))
			: ActivityPub.ActivityRenderer.RenderDelete(actor, new ASTombstone { Id = note.GetPublicUri(config.Value) });
		// @formatter:on

		if (note.Visibility == Note.NoteVisibility.Specified)
			await deliverSvc.DeliverToAsync(activity, note.User, recipients);
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

		await DeleteNoteAsync(dbNote);
	}

	public async Task DeleteNoteAsync(ASNote note, User actor)
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

		await DeleteNoteAsync(dbNote);
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

	public static ValueTask<IDisposable> GetNoteProcessLockAsync(string uri) => KeyedLocker.LockAsync(uri);

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
		if (note.Url?.Link != null && new Uri(note.Id).IdnHost != new Uri(note.Url.Link).IdnHost)
			note.Url = null;
		if (actor.IsSuspended)
			throw GracefulException.Forbidden("User is suspended");
		if (await fedCtrlSvc.ShouldBlockAsync(note.Id, actor.Host))
			throw GracefulException.UnprocessableEntity("Refusing to create note for user on blocked instance");

		policySvc.CallRewriteHooks(note, actor, IRewritePolicy.HookLocationEnum.PreLogic);

		var   replyUri = note.InReplyTo?.Id;
		var   reply    = replyUri != null ? await ResolveNoteAsync(replyUri, user: user) : null;
		Poll? poll     = null;

		if (reply is { HasPoll: true } && note.Name != null)
		{
			if (reply.UserHost != null)
				throw GracefulException.UnprocessableEntity("Poll vote not destined for this instance");

			poll = await db.Polls.FirstOrDefaultAsync(p => p.Note == reply)
			       ?? throw GracefulException.UnprocessableEntity("Poll does not exist");

			if (poll.Choices.All(p => p != note.Name))
				throw GracefulException.UnprocessableEntity("Unknown poll option");

			var existingVotes = await db.PollVotes.Where(p => p.User == actor && p.Note == reply).ToListAsync();
			if (existingVotes.Any(p => poll.Choices[p.Choice] == note.Name))
				throw GracefulException.UnprocessableEntity("Actor has already voted for this option");
			if (!poll.Multiple && existingVotes.Count != 0)
				throw GracefulException.UnprocessableEntity("Actor has already voted in this poll");

			var vote = new PollVote
			{
				Id        = IdHelpers.GenerateSnowflakeId(),
				CreatedAt = DateTime.UtcNow,
				User      = actor,
				Note      = reply,
				Choice    = poll.Choices.IndexOf(note.Name)
			};
			await db.AddAsync(vote);
			await db.SaveChangesAsync();
			await pollSvc.RegisterPollVoteAsync(vote, poll, reply);

			return null;
		}

		if (note.PublishedAt is null or { Year: < 2007 } || note.PublishedAt > DateTime.Now + TimeSpan.FromDays(3))
			throw GracefulException.UnprocessableEntity("Note.PublishedAt is nonsensical");

		if (replyUri != null)
		{
			if (reply == null && note.Name != null)
				throw GracefulException.UnprocessableEntity("Refusing to ingest poll vote for unknown note");
			if (reply != null)
				replyUri = null;
		}

		var mentionData = await ResolveNoteMentionsAsync(note);
		var createdAt = note.PublishedAt?.ToUniversalTime()
		                ?? throw GracefulException.UnprocessableEntity("Missing or invalid PublishedAt field");

		var quoteUrl = note.MkQuote
		               ?? note.QuoteUri
		               ?? note.QuoteUrl
		               ?? note.Tags?.OfType<ASTagRel>()
		                      .Where(p => p.MediaType is Constants.APMime or Constants.ASMime)
		                      .Where(p => p.Rel is $"{Constants.MisskeyNs}#_misskey_quote"
		                                           or $"{Constants.FedibirdNs}#quoteUri"
		                                           or $"{Constants.ActivityStreamsNs}#quoteUrl")
		                      .Select(p => p.Link)
		                      .FirstOrDefault();

		var renote     = quoteUrl != null ? await ResolveNoteAsync(quoteUrl, user: user) : null;
		var renoteUri  = renote == null ? quoteUrl : null;
		var visibility = note.GetVisibility(actor);

		var                   text            = note.MkContent;
		List<MfmInlineMedia>? htmlInlineMedia = null;

		if (text == null)
		{
			(text, htmlInlineMedia) = await MfmConverter.FromHtmlAsync(note.Content, mentionData.Mentions);
		}

		var cw  = note.Summary;
		var url = note.Url?.Link;
		var uri = note.Id;

		if (note is ASQuestion question)
		{
			if (question is { AnyOf: not null, OneOf: not null })
				throw GracefulException.UnprocessableEntity("Polls cannot have both anyOf and oneOf set");

			var choices = (question.AnyOf ?? question.OneOf)?.Where(p => p.Name != null).ToList()
			              ?? throw GracefulException.UnprocessableEntity("Polls must have either anyOf or oneOf set");

			if (choices.Count == 0)
				throw GracefulException.UnprocessableEntity("Poll must have at least one option");

			poll = new Poll
			{
				ExpiresAt   = question.EndTime ?? question.Closed,
				Multiple    = question.AnyOf != null,
				Choices     = choices.Select(p => p.Name).Cast<string>().ToList(),
				Votes       = choices.Select(p => (int?)p.Replies?.TotalItems ?? 0).ToList(),
				VotersCount = question.VotersCount
			};
		}

		var sensitive = (note.Sensitive ?? false) || !string.IsNullOrWhiteSpace(cw);
		var files     = await ProcessAttachmentsAsync(note.Attachments, htmlInlineMedia, actor, sensitive);
		var emoji = (await emojiSvc.ProcessEmojiAsync(note.Tags?.OfType<ASEmoji>().ToList(), actor.Host))
		            .Select(p => p.Id)
		            .ToList();

		policySvc.CallRewriteHooks(note, actor, IRewritePolicy.HookLocationEnum.PostLogic);

		return await CreateNoteAsync(new NoteCreationData
		{
			User             = actor,
			Visibility       = visibility,
			Text             = text,
			Cw               = cw,
			Reply            = reply,
			Renote           = renote,
			Attachments      = files,
			Poll             = poll,
			LocalOnly        = false,
			Uri              = uri,
			Url              = url,
			Emoji            = emoji,
			ResolvedMentions = mentionData,
			CreatedAt        = createdAt,
			ASNote           = note,
			ReplyUri         = replyUri,
			RenoteUri        = renoteUri
		});
	}

	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataUsage",
	                 Justification = "Inspection doesn't understand IncludeCommonProperties()")]
	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataQuery", Justification = "See above")]
	public async Task<Note?> ProcessNoteUpdateAsync(ASNote note, User actor, User? user = null)
	{
		var dbNote = await db.Notes.IncludeCommonProperties()
		                     .Include(p => p.Poll)
		                     .FirstOrDefaultAsync(p => p.Uri == note.Id);

		if (dbNote == null) return await ProcessNoteAsync(note, actor, user);

		logger.LogDebug("Processing note update {id} for note {noteId}", note.Id, dbNote.Id);

		var updatedAt = note.UpdatedAt ?? DateTime.UtcNow;

		if (dbNote.User != actor)
			throw GracefulException.UnprocessableEntity("Refusing to update note of user other than actor");
		if (dbNote.User.IsSuspended)
			throw GracefulException.Forbidden("User is suspended");
		if (dbNote.UpdatedAt != null && dbNote.UpdatedAt > updatedAt)
			throw GracefulException.UnprocessableEntity("Note update is older than last known version");
		if (updatedAt.Year < 2007 || updatedAt > DateTime.Now + TimeSpan.FromDays(3))
			throw GracefulException.UnprocessableEntity("updatedAt is nonsensical");
		if (actor.Host == null)
			throw GracefulException.UnprocessableEntity("User.Host is null");

		var mentionData = await ResolveNoteMentionsAsync(note);

		var                   text            = note.MkContent;
		List<MfmInlineMedia>? htmlInlineMedia = null;

		if (text == null)
			(text, htmlInlineMedia) = await MfmConverter.FromHtmlAsync(note.Content, mentionData.Mentions);

		var cw = note.Summary;

		Poll? poll = null;

		if (note is ASQuestion question)
		{
			if (question is { AnyOf: not null, OneOf: not null })
				throw GracefulException.UnprocessableEntity("Polls cannot have both anyOf and oneOf set");

			var choices = (question.AnyOf ?? question.OneOf)?.Where(p => p.Name != null).ToList()
			              ?? throw GracefulException.UnprocessableEntity("Polls must have either anyOf or oneOf set");

			if (choices.Count == 0)
				throw GracefulException.UnprocessableEntity("Poll must have at least one option");
			if (question.VotersCount is < 0)
				throw GracefulException.UnprocessableEntity("Voters count must not be negative");

			poll = new Poll
			{
				ExpiresAt   = question.EndTime ?? question.Closed,
				Multiple    = question.AnyOf != null,
				Choices     = choices.Select(p => p.Name).Cast<string>().ToList(),
				Votes       = choices.Select(p => (int?)p.Replies?.TotalItems ?? 0).ToList(),
				VotersCount = question.VotersCount
			};
		}

		var sensitive = (note.Sensitive ?? false) || !string.IsNullOrWhiteSpace(cw);
		var files     = await ProcessAttachmentsAsync(note.Attachments, htmlInlineMedia, actor, sensitive, false);
		var emoji = (await emojiSvc.ProcessEmojiAsync(note.Tags?.OfType<ASEmoji>().ToList(), actor.Host))
		            .Select(p => p.Id)
		            .ToList();

		return await UpdateNoteAsync(new NoteUpdateData
		{
			Note             = dbNote,
			Text             = text,
			Cw               = cw,
			Attachments      = files,
			Poll             = poll,
			UpdatedAt        = updatedAt,
			ResolvedMentions = mentionData,
			ASNote           = note,
			Emoji            = emoji
		});
	}

	private async Task<NoteMentionData> ResolveNoteMentionsAsync(ASNote note)
	{
		var mentionTags = note.Tags?.OfType<ASMention>().Where(p => p.Href?.Id != null).ToArray() ?? [];
		if (mentionTags.Length > 100)
			throw GracefulException.UnprocessableEntity("Refusing to process note with more than 100 mentions");

		var users = await mentionTags
		                  .Select(p => userResolver.ResolveOrNullAsync(p.Href!.Id!, EnforceUriFlags))
		                  .AwaitAllNoConcurrencyAsync();

		return ResolveNoteMentions(users.NotNull().ToList());
	}

	private async Task<NoteMentionData> ResolveNoteMentionsAsync(IMfmNode[]? parsedText)
	{
		if (parsedText == null)
			return ResolveNoteMentions([]);

		var mentions = parsedText
		               .SelectMany(p => p.Children.Append(p))
		               .OfType<MfmMentionNode>()
		               .DistinctBy(p => p.Acct)
		               .ToArray();

		if (mentions.Length > 100)
			throw GracefulException.UnprocessableEntity("Refusing to process note with more than 100 mentions");

		var users = await mentions.Select(p => userResolver.ResolveOrNullAsync($"acct:{p.Acct}", ResolveFlags.Acct))
		                          .AwaitAllNoConcurrencyAsync();

		return ResolveNoteMentions(users.NotNull().ToList());
	}

	private List<string> ResolveHashtags(IMfmNode[]? parsedText, ASNote? note = null)
	{
		List<string> tags = [];

		if (parsedText != null)
		{
			tags = parsedText
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
		                    .ToArray();

		if (extracted != null)
			tags.AddRange(extracted);

		if (tags.Count == 0) return [];

		tags = tags.Distinct().ToList();

		_ = followupTaskSvc.ExecuteTaskAsync("UpdateHashtagsTable", async provider =>
		{
			var bgDb     = provider.GetRequiredService<DatabaseContext>();
			var existing = await bgDb.Hashtags.Where(p => tags.Contains(p.Name)).Select(p => p.Name).ToListAsync();
			var dbTags = tags.Except(existing)
			                 .Select(p => new Hashtag { Id = IdHelpers.GenerateSnowflakeId(), Name = p });
			await bgDb.UpsertRange(dbTags).On(p => p.Name).NoUpdate().RunAsync();
		});

		return tags;
	}

	private NoteMentionData ResolveNoteMentions(IReadOnlyCollection<User> users)
	{
		var userIds      = users.Select(p => p.Id).Distinct().ToList();
		var localUserIds = users.Where(p => p.IsLocalUser).Select(p => p.Id).Distinct().ToList();

		var remoteUsers = users.Where(p => p is { IsRemoteUser: true, Uri: not null })
		                       .ToList();

		var localUsers = users.Where(p => p.IsLocalUser)
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

		return new NoteMentionData(userIds, localUserIds, mentions, remoteMentions, splitDomainMapping);
	}

	private async Task<List<DriveFile>> ProcessAttachmentsAsync(
		List<ASAttachment>? attachments, List<MfmInlineMedia>? htmlInlineMedia, User user, bool sensitive,
		bool logExisting = true
	)
	{
		var allAttachments = attachments?.OfType<ASDocument>().Take(10).ToList() ?? [];

		if (htmlInlineMedia != null)
		{
			var inlineUrls     = htmlInlineMedia.Select(p => p.Src);
			var unattachedUrls = inlineUrls.Except(allAttachments.Select(a => a.Url?.Id)).ToArray();

			allAttachments.AddRange(htmlInlineMedia.Where(p => unattachedUrls.Contains(p.Src))
			                                       .DistinctBy(p => p.Src)
			                                       .Select(p => new ASDocument
			                                       {
				                                       Url = new ASLink(p.Src), Description = p.Alt,
			                                       }));
		}

		if (allAttachments is not { Count: > 0 }) return [];
		var result = await allAttachments
		                   .Select(p => driveSvc.StoreFileAsync(p.Url?.Id, user, p.Sensitive ?? sensitive,
		                                                        p.Description, p.MediaType, logExisting))
		                   .AwaitAllNoConcurrencyAsync();

		return result.Where(p => p != null).Cast<DriveFile>().ToList();
	}

	public async Task<Note?> ResolveNoteAsync(
		string uri, ASNote? fetchedNote = null, User? user = null, bool clearHistory = false, bool forceRefresh = false
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

		var parsedUri = new Uri(uri, UriKind.Absolute);

		if (parsedUri.Host == config.Value.WebDomain && parsedUri.AbsolutePath.StartsWith("/notes/"))
		{
			var id = parsedUri.AbsolutePath["/notes/".Length..];
			return await db.Notes.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id);
		}

		if (parsedUri.Host == config.Value.WebDomain)
			return null;

		var note = await db.Notes.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Uri == uri);

		if (note != null && !forceRefresh) return note;

		if (!fetchedNote?.VerifiedFetch ?? false)
			fetchedNote = null;

		try
		{
			fetchedNote ??=
				user != null ? await fetchSvc.FetchNoteAsync(uri, user) : await fetchSvc.FetchNoteAsync(uri);
		}
		catch (LocalFetchException e) when (
			Uri.TryCreate(e.Uri, UriKind.Absolute, out var parsed)
			&& parsed.Host == config.Value.WebDomain
			&& parsed.AbsolutePath.StartsWith("/notes/")
		)
		{
			var id = parsed.AbsolutePath["/notes/".Length..];
			return await db.Notes.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id);
		}
		catch (AuthFetchException e) when (e.StatusCode == HttpStatusCode.NotFound)
		{
			logger.LogDebug("Failed to fetch note, skipping: {error}", e.Message);
			return null;
		}
		catch (Exception e)
		{
			logger.LogDebug("Failed to fetch note, skipping: {error}", e);
			return null;
		}

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
			if (res != null && !forceRefresh) return res;
		}

		var actor = await userResolver.ResolveAsync(attrTo.Id, EnforceUriFlags);

		using (await KeyedLocker.LockAsync(fetchedNote.Id))
		{
			try
			{
				return forceRefresh
					? await ProcessNoteUpdateAsync(fetchedNote, actor, user)
					: await ProcessNoteAsync(fetchedNote, actor, user);
			}
			catch (Exception e)
			{
				logger.LogDebug("Failed to create resolved note: {error}", e.Message);
				return null;
			}
		}
	}

	public async Task<Note?> ResolveNoteAsync(ASNote note)
	{
		return await ResolveNoteAsync(note.Id, note);
	}

	public async Task EnqueueBackfillTaskAsync(Note note, User? user)
	{
		var cfg = backfillConfig.Value.Replies;

		// return immediately if backfilling is not enabled
		if (!cfg.Enabled) return;

		// don't try to schedule a backfill for local notes
		if (note.UserHost == null) return;

		// don't try to schedule a backfill when we're actively backfilling the thread
		if (BackfillQueue.KeyedLocker.IsInUse(note.ThreadId)) return;

		var updatedRows = await db.NoteThreads
		                          .Where(t => t.Id == note.ThreadId
		                                      && t.Notes.Count < BackfillQueue.MaxRepliesPerThread
		                                      && (t.BackfilledAt == null
		                                          || t.BackfilledAt <= DateTime.UtcNow - cfg.RefreshAfterTimeSpan))
		                          .ExecuteUpdateAsync(p => p.SetProperty(t => t.BackfilledAt, DateTime.UtcNow));

		// only queue if the thread's backfill timestamp got updated. if it didn't, it means the cooldown is still in effect
		// (or the thread doesn't exist, which shouldn't be possible)
		if (updatedRows <= 0) return;

		var jobData = new BackfillJobData
		{
			ThreadId = note.ThreadId, AuthenticatedUserId = cfg.FetchAsUser ? user?.Id : null
		};

		await queueSvc.BackfillQueue.EnqueueAsync(jobData, mutex: $"backfill:{note.ThreadId}");
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
				Id        = IdHelpers.GenerateSnowflakeId(),
				CreatedAt = DateTime.UtcNow,
				User      = user,
				Note      = note
			};

			await db.NoteLikes.AddAsync(like);
			await db.SaveChangesAsync();
			await db.Notes.Where(p => p.Id == note.Id)
			        .ExecuteUpdateAsync(p => p.SetProperty(n => n.LikeCount, n => n.LikeCount + 1));

			if (user.IsLocalUser && note.UserHost != null)
			{
				var activity = activityRenderer.RenderLike(like);
				await deliverSvc.DeliverToConditionalAsync(activity, user, note);
			}

			eventSvc.RaiseNoteLiked(this, note, user);
			await notificationSvc.GenerateLikeNotificationAsync(note, user);
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

		if (user.IsLocalUser && note.UserHost != null)
		{
			var activity =
				activityRenderer.RenderUndo(userRenderer.RenderLite(user), activityRenderer.RenderLike(like));
			await deliverSvc.DeliverToConditionalAsync(activity, user, note);
		}

		eventSvc.RaiseNoteUnliked(this, note, user);
		await db.Notifications
		        .Where(p => p.Type == Notification.NotificationType.Like
		                    && p.Notifiee == note.User
		                    && p.Notifier == user)
		        .ExecuteDeleteAsync();

		return true;
	}

	public async Task<Note?> RenoteNoteAsync(Note note, User user, Note.NoteVisibility? visibility = null)
	{
		visibility ??= user.UserSettings?.DefaultRenoteVisibility ?? Note.NoteVisibility.Public;
		if (visibility == Note.NoteVisibility.Specified)
			throw GracefulException.BadRequest("Renote visibility must be one of: public, unlisted, private");
		if (note.IsPureRenote)
			throw GracefulException.BadRequest("Cannot renote a pure renote");

		if (!await db.Notes.AnyAsync(p => p.Renote == note && p.IsPureRenote && p.User == user))
			return await CreateNoteAsync(new NoteCreationData
			{
				User       = user,
				Visibility = visibility.Value,
				Renote     = note
			});

		return null;
	}

	public async Task<int> UnrenoteNoteAsync(Note note, User user)
	{
		var renotes = await db.Notes.Where(p => p.Renote == note && p.IsPureRenote && p.User == user).ToListAsync();
		if (renotes.Count == 0) return 0;

		foreach (var renote in renotes)
			await DeleteNoteAsync(renote);

		return renotes.Count;
	}

	public async Task LikeNoteAsync(ASNote note, User actor)
	{
		var dbNote = await ResolveNoteAsync(note)
		             ?? throw GracefulException.UnprocessableEntity("Cannot register like for unknown note");

		await LikeNoteAsync(dbNote, actor);
	}

	public async Task UnlikeNoteAsync(ASNote note, User user)
	{
		var dbNote = await ResolveNoteAsync(note)
		             ?? throw GracefulException.UnprocessableEntity("Cannot unregister like for unknown note");

		await UnlikeNoteAsync(dbNote, user);
	}

	public async Task BookmarkNoteAsync(Note note, User user)
	{
		if (user.IsRemoteUser) throw new Exception("This method is only valid for local users");

		if (note.IsPureRenote)
			throw GracefulException.BadRequest("Cannot bookmark a pure renote");

		if (!await db.NoteBookmarks.AnyAsync(p => p.Note == note && p.User == user))
		{
			var bookmark = new NoteBookmark
			{
				Id        = IdHelpers.GenerateSnowflakeId(),
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
		if (user.IsRemoteUser) throw new Exception("This method is only valid for local users");

		await db.NoteBookmarks.Where(p => p.Note == note && p.User == user).ExecuteDeleteAsync();
	}

	public async Task PinNoteAsync(Note note, User user)
	{
		if (user.IsRemoteUser) throw new Exception("This method is only valid for local users");

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
				Id        = IdHelpers.GenerateSnowflakeId(),
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
		if (user.IsRemoteUser) throw new Exception("This method is only valid for local users");

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
			collection = await objectResolver.ResolveObjectAsync(collection, force: true) as ASOrderedCollection;
		if (collection is not { Items: not null }) return;

		// ReSharper disable once EntityFramework.UnsupportedServerSideFunctionCall
		var followingUser = await db.Users.FirstOrDefaultAsync(p => p.IsFollowing(user));
		var notes = await objectResolver.IterateCollection(collection)
		                                .Take(10)
		                                .Where(p => p.Id != null)
		                                .Select(p => ResolveNoteAsync(p.Id!, null, followingUser, true))
		                                .AwaitAllNoConcurrencyAsync();

		var previousPins = await db.Users.Where(p => p.Id == user.Id)
		                           .Select(p => p.PinnedNotes.Select(i => i.Id))
		                           .FirstOrDefaultAsync()
		                   ?? [];

		if (previousPins.SequenceEqual(notes.Where(p => p != null).Cast<Note>().Select(p => p.Id))) return;

		if (notes.OfType<Note>().Any(p => p.User != user))
			throw GracefulException
				.UnprocessableEntity("Refusing to ingest pinned notes attributed to different actor");

		var pins = notes.OfType<Note>()
		                .Select(p => new UserNotePin
		                {
			                Id        = IdHelpers.GenerateSnowflakeId(),
			                CreatedAt = DateTime.UtcNow,
			                Note      = p,
			                User      = user
		                });

		db.RemoveRange(await db.UserNotePins.Where(p => p.User == user).ToListAsync());
		await db.AddRangeAsync(pins);
		await db.SaveChangesAsync();
	}

	private async Task EnqueuePollExpiryTaskAsync(Poll poll)
	{
		// Skip polls without expiry date
		if (!poll.ExpiresAt.HasValue) return;
		// Skip polls with expiry date more than 1 year in the future (to prevent excessive accumulation of delayed jobs)
		if (poll.ExpiresAt > DateTime.UtcNow + TimeSpan.FromDays(367)) return;

		var job = new PollExpiryJobData { NoteId = poll.Note.Id };
		await queueSvc.BackgroundTaskQueue.ScheduleAsync(job, poll.ExpiresAt.Value);
	}

	public async Task<(string name, bool success)> ReactToNoteAsync(Note note, User user, string name)
	{
		if (note.IsPureRenote)
			throw GracefulException.BadRequest("Cannot react to a pure renote");

		name = await emojiSvc.ResolveEmojiNameAsync(name, user.Host);
		if (await db.NoteReactions.AnyAsync(p => p.Note == note && p.User == user && p.Reaction == name))
			return (name, false);

		if (await db.Blockings.AnyAsync(p => p.Blockee == user && p.Blocker == note.User))
			throw GracefulException.Forbidden("You are not allowed to interact with this user");

		var reaction = new NoteReaction
		{
			Id        = IdHelpers.GenerateSnowflakeId(),
			CreatedAt = DateTime.UtcNow,
			Note      = note,
			User      = user,
			Reaction  = name
		};

		await db.AddAsync(reaction);
		await db.SaveChangesAsync();
		eventSvc.RaiseNoteReacted(this, reaction);
		await notificationSvc.GenerateReactionNotificationAsync(reaction);

		// @formatter:off
		await db.Database.ExecuteSqlAsync($"""UPDATE "note" SET "reactions" = jsonb_set("reactions", ARRAY[{name}], (COALESCE("reactions"->>{name}, '0')::int + 1)::text::jsonb) WHERE "id" = {note.Id}""");
		// @formatter:on

		if (user.IsLocalUser)
		{
			var emoji    = await emojiSvc.ResolveEmojiAsync(reaction.Reaction);
			var activity = activityRenderer.RenderReact(reaction, emoji);
			await deliverSvc.DeliverToConditionalAsync(activity, user, note);
		}

		return (name, true);
	}

	public async Task ReactToNoteAsync(ASNote note, User actor, string name)
	{
		var dbNote = await ResolveNoteAsync(note.Id, note);
		if (dbNote == null)
			throw GracefulException.UnprocessableEntity("Failed to resolve reaction target");

		await ReactToNoteAsync(dbNote, actor, name);
	}

	public async Task<(string name, bool success)> RemoveReactionFromNoteAsync(Note note, User user, string name)
	{
		name = await emojiSvc.ResolveEmojiNameAsync(name, user.Host);

		var reaction =
			await db.NoteReactions.FirstOrDefaultAsync(p => p.Note == note && p.User == user && p.Reaction == name);

		if (reaction == null) return (name, false);
		db.Remove(reaction);
		await db.SaveChangesAsync();
		eventSvc.RaiseNoteUnreacted(this, reaction);

		await db.Database
		        .ExecuteSqlAsync($"""UPDATE "note" SET "reactions" = jsonb_set("reactions", ARRAY[{name}], (COALESCE("reactions"->>{name}, '1')::int - 1)::text::jsonb) WHERE "id" = {note.Id}""");

		if (user.IsLocalUser)
		{
			var actor    = userRenderer.RenderLite(user);
			var emoji    = await emojiSvc.ResolveEmojiAsync(reaction.Reaction);
			var activity = activityRenderer.RenderUndo(actor, activityRenderer.RenderReact(reaction, emoji));
			await deliverSvc.DeliverToConditionalAsync(activity, user, note);
		}

		if (note.User.IsLocalUser && note.User != user)
		{
			await db.Notifications
			        .Where(p => p.Note == note
			                    && p.Notifier == user
			                    && p.Type == Notification.NotificationType.Reaction)
			        .ExecuteDeleteAsync();
		}

		return (name, true);
	}

	public async Task RemoveReactionFromNoteAsync(ASNote note, User actor, string name)
	{
		var dbNote = await ResolveNoteAsync(note.Id, note);
		if (dbNote == null) return;
		await RemoveReactionFromNoteAsync(dbNote, actor, name);
	}
}
