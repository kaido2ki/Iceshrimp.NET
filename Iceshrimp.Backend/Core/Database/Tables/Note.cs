using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using EntityFrameworkCore.Projectables;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("note")]
[Index(nameof(Uri), IsUnique = true)]
[Index(nameof(ReplyId))]
[Index(nameof(AttachedFileTypes))]
[Index(nameof(FileIds))]
[Index(nameof(RenoteId))]
[Index(nameof(Mentions))]
[Index(nameof(UserId))]
[Index(nameof(UserHost))]
[Index(nameof(VisibleUserIds))]
[Index(nameof(Tags))]
[Index(nameof(ThreadId))]
[Index(nameof(CreatedAt))]
[Index(nameof(ChannelId))]
[Index(nameof(CreatedAt), nameof(UserId))]
[Index(nameof(Id), nameof(UserHost))]
[Index(nameof(Url))]
[Index(nameof(UserId), nameof(Id))]
[Index(nameof(Visibility))]
[Index(nameof(ReplyUri))]
[Index(nameof(RenoteUri))]
public class Note : IEntity
{
	[PgName("note_visibility_enum")]
	public enum NoteVisibility
	{
		[PgName("public")]    Public    = 0,
		[PgName("home")]      Home      = 1,
		[PgName("followers")] Followers = 2,
		[PgName("specified")] Specified = 3
	}

	/// <summary>
	///     The created date of the Note.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The ID of reply target.
	/// </summary>
	[Column("replyId")]
	[StringLength(32)]
	public string? ReplyId { get; set; }

	/// <summary>
	///     The ID of renote target.
	/// </summary>
	[Column("renoteId")]
	[StringLength(32)]
	public string? RenoteId { get; set; }

	/// <summary>
	///     The URI of the reply target, if it couldn't be resolved at time of ingestion.
	/// </summary>
	[Column("replyUri")]
	[StringLength(512)]
	public string? ReplyUri { get; set; }

	/// <summary>
	///     The URI of the renote target, if it couldn't be resolved at time of ingestion.
	/// </summary>
	[Column("renoteUri")]
	[StringLength(512)]
	public string? RenoteUri { get; set; }

	[Column("text")] public string? Text { get; set; }

	[Column("name")] [StringLength(256)] public string? Name { get; set; }

	[Column("cw")] public string? Cw { get; set; }

	/// <summary>
	///     The ID of author.
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	[Column("visibility")] public NoteVisibility Visibility { get; set; }

	[NotMapped]
	[Projectable]
	[SuppressMessage("ReSharper", "MergeIntoLogicalPattern",
	                 Justification = "Projectable expression cannot contain patterns")]
	public bool VisibilityIsPublicOrHome => Visibility == NoteVisibility.Public || Visibility == NoteVisibility.Home;

	[Column("localOnly")] public bool LocalOnly { get; set; }

	[Column("renoteCount")] public short RenoteCount { get; set; }

	[Column("repliesCount")] public short RepliesCount { get; set; }

	[Column("likeCount")] public int LikeCount { get; set; }

	[Column("reactions", TypeName = "jsonb")]
	public Dictionary<string, long> Reactions { get; set; } = null!;

	/// <summary>
	///     The URI of a note. it will be null when the note is local.
	/// </summary>
	[Column("uri")]
	[StringLength(512)]
	public string? Uri { get; set; }

	[Column("score")] public int Score { get; set; }

	[Column("fileIds", TypeName = "character varying(32)[]")]
	public List<string> FileIds { get; set; } = [];

	[Column("attachedFileTypes", TypeName = "character varying(256)[]")]
	public List<string> AttachedFileTypes { get; set; } = [];

	[Column("visibleUserIds", TypeName = "character varying(32)[]")]
	public List<string> VisibleUserIds { get; set; } = [];

	[Column("mentions", TypeName = "character varying(32)[]")]
	public List<string> Mentions { get; set; } = [];

	[Column("mentionedRemoteUsers", TypeName = "jsonb")]
	public List<MentionedUser> MentionedRemoteUsers { get; set; } = [];

	[Column("emojis", TypeName = "character varying(128)[]")]
	public List<string> Emojis { get; set; } = [];

	[Column("tags", TypeName = "character varying(128)[]")]
	public List<string> Tags { get; set; } = [];

	[Column("hasPoll")] public bool HasPoll { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("userHost")]
	[StringLength(512)]
	public string? UserHost { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("replyUserId")]
	[StringLength(32)]
	public string? ReplyUserId { get; set; }

	/// <summary>
	///     Mastodon requires a slightly differently computed replyUserId field. To save processing time, we do this ahead of
	///     time.
	/// </summary>
	[Column("mastoReplyUserId")]
	[StringLength(32)]
	public string? MastoReplyUserId { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("replyUserHost")]
	[StringLength(512)]
	public string? ReplyUserHost { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("renoteUserId")]
	[StringLength(32)]
	public string? RenoteUserId { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("renoteUserHost")]
	[StringLength(512)]
	public string? RenoteUserHost { get; set; }

	/// <summary>
	///     The human readable url of a note. it will be null when the note is local.
	/// </summary>
	[Column("url")]
	[StringLength(512)]
	public string? Url { get; set; }

	/// <summary>
	///     The ID of source channel.
	/// </summary>
	[Column("channelId")]
	[StringLength(32)]
	public string? ChannelId { get; set; }

	[Column("threadId")]
	[StringLength(256)]
	public string ThreadId { get; set; } = null!;

	/// <summary>
	///     The updated date of the Note.
	/// </summary>
	[Column("updatedAt")]
	public DateTime? UpdatedAt { get; set; }

	/// <summary>
	///		ID of the ActivityStreams replies collection for this note, used to re-fetch replies.
	/// </summary>
	[Column("repliesCollection")]
	[StringLength(512)]
	public string? RepliesCollection { get; set; }

	[Column("repliesFetchedAt")]
	public DateTime? RepliesFetchedAt { get;set; }

	[Column("combinedAltText")]
	public string? CombinedAltText { get; set; }

	[ForeignKey(nameof(ChannelId))]
	[InverseProperty(nameof(Tables.Channel.Notes))]
	public virtual Channel? Channel { get; set; }

	[InverseProperty(nameof(ChannelNotePin.Note))]
	public virtual ICollection<ChannelNotePin> ChannelNotePins { get; set; } = new List<ChannelNotePin>();

	[InverseProperty(nameof(ClipNote.Note))]
	public virtual ICollection<ClipNote> ClipNotes { get; set; } = new List<ClipNote>();

	[InverseProperty(nameof(Renote))] public virtual ICollection<Note> InverseRenote { get; set; } = new List<Note>();

	[InverseProperty(nameof(Reply))] public virtual ICollection<Note> InverseReply { get; set; } = new List<Note>();

	[InverseProperty(nameof(NoteEdit.Note))]
	public virtual ICollection<NoteEdit> NoteEdits { get; set; } = new List<NoteEdit>();

	[InverseProperty(nameof(NoteBookmark.Note))]
	public virtual ICollection<NoteBookmark> NoteBookmarks { get; set; } = new List<NoteBookmark>();

	[InverseProperty(nameof(NoteReaction.Note))]
	public virtual ICollection<NoteReaction> NoteReactions { get; set; } = new List<NoteReaction>();

	[InverseProperty(nameof(NoteLike.Note))]
	public virtual ICollection<NoteLike> NoteLikes { get; set; } = new List<NoteLike>();

	[InverseProperty(nameof(NoteUnread.Note))]
	public virtual ICollection<NoteUnread> NoteUnreads { get; set; } = new List<NoteUnread>();

	[InverseProperty(nameof(NoteWatching.Note))]
	public virtual ICollection<NoteWatching> NoteWatchings { get; set; } = new List<NoteWatching>();

	[InverseProperty(nameof(Notification.Note))]
	public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

	[InverseProperty(nameof(Tables.Poll.Note))]
	public virtual Poll? Poll { get; set; }

	[InverseProperty(nameof(PollVote.Note))]
	public virtual ICollection<PollVote> PollVotes { get; set; } = new List<PollVote>();

	[InverseProperty(nameof(Tables.PromoNote.Note))]
	public virtual PromoNote? PromoNote { get; set; }

	[InverseProperty(nameof(PromoRead.Note))]
	public virtual ICollection<PromoRead> PromoReads { get; set; } = new List<PromoRead>();

	[ForeignKey(nameof(RenoteId))]
	[InverseProperty(nameof(InverseRenote))]
	public virtual Note? Renote { get; set; }

	[ForeignKey(nameof(ReplyId))]
	[InverseProperty(nameof(InverseReply))]
	public virtual Note? Reply { get; set; }
	
	[ForeignKey(nameof(ThreadId))]
	[InverseProperty(nameof(NoteThread.Notes))]
	public virtual NoteThread Thread { get; set; } = null!;

	[Projectable]
	public string RawAttachments
		=> InternalRawAttachments(Id);

	[NotMapped] [Projectable] public bool IsPureRenote => (RenoteId != null || Renote != null) && !IsQuote;

	[NotMapped]
	[Projectable]
	public bool IsQuote => (RenoteId != null || Renote != null) &&
	                       (Text != null || Cw != null || HasPoll || FileIds.Count > 0);

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.Notes))]
	public virtual User User { get; set; } = null!;

	[InverseProperty(nameof(UserNotePin.Note))]
	public virtual ICollection<UserNotePin> UserNotePins { get; set; } = new List<UserNotePin>();

	[NotMapped] public bool? PrecomputedIsReplyVisible  { get; private set; } = false;
	[NotMapped] public bool? PrecomputedIsRenoteVisible { get; private set; } = false;

	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	public static string InternalRawAttachments(string id)
		=> throw new NotSupportedException();

	[Projectable]
	public bool TextContainsCaseInsensitive(string str) =>
		Text != null && EF.Functions.ILike(Text, "%" + EfHelpers.EscapeLikeQuery(str) + "%", @"\");

	[Projectable]
	[SuppressMessage("ReSharper", "MergeIntoPattern", Justification = "Projectable chain must not contain patterns")]
	public bool IsVisibleFor(User? user) =>
		(VisibilityIsPublicOrHome && (!LocalOnly || (user != null && user.IsLocalUser))) ||
		(user != null && CheckComplexVisibility(user));

	[Projectable]
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Projectable chain must to be public")]
	public bool CheckComplexVisibility(User user) => User == user ||
	                                                 VisibleUserIds.Contains(user.Id) ||
	                                                 Mentions.Contains(user.Id) ||
	                                                 (Visibility == NoteVisibility.Followers &&
	                                                  (User.IsFollowedBy(user) || ReplyUserId == user.Id));

	public bool IsVisibleFor(User? user, IEnumerable<string> followingIds) =>
		VisibilityIsPublicOrHome || (user != null && CheckComplexVisibility(user, followingIds));

	private bool CheckComplexVisibility(User user, IEnumerable<string> followingIds)
		=> User.Id == user.Id ||
		   VisibleUserIds.Contains(user.Id) ||
		   Mentions.Contains(user.Id) ||
		   (Visibility == NoteVisibility.Followers &&
		    (followingIds.Contains(User.Id) || ReplyUserId == user.Id));

	public Note WithPrecomputedVisibilities(bool reply, bool renote, bool renoteRenote)
	{
		if (Reply != null)
			PrecomputedIsReplyVisible = reply;
		if (Renote != null)
			PrecomputedIsRenoteVisible = renote;
		if (Renote?.Renote != null)
			Renote.PrecomputedIsRenoteVisible = renoteRenote;

		return this;
	}

	[Projectable]
	[SuppressMessage("ReSharper", "MergeIntoPattern", Justification = "Projectables")]
	[SuppressMessage("ReSharper", "MergeSequentialChecks", Justification = "Projectables")]
	public Note WithPrecomputedVisibilities(User user)
		=> WithPrecomputedVisibilities(Reply != null && Reply.IsVisibleFor(user),
		                               Renote != null && Renote.IsVisibleFor(user),
		                               Renote != null && Renote.Renote != null && Renote.Renote.IsVisibleFor(user));

	public string GetPublicUri(Config.InstanceSection config) => UserHost == null
		? $"https://{config.WebDomain}/notes/{Id}"
		: throw new Exception("Cannot access PublicUri for remote note");

	public string? GetPublicUriOrNull(Config.InstanceSection config) => UserHost == null
		? $"https://{config.WebDomain}/notes/{Id}"
		: null;

	public class MentionedUser
	{
		[J("uri")]      public required string  Uri      { get; set; }
		[J("url")]      public          string? Url      { get; set; }
		[J("username")] public required string  Username { get; set; }
		[J("host")]     public required string? Host     { get; set; }
	}
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<Note>
	{
		public void Configure(EntityTypeBuilder<Note> entity)
		{
			entity.HasIndex(e => e.Mentions, "GIN_note_mentions").HasMethod("gin");
			entity.HasIndex(e => e.Tags, "GIN_note_tags").HasMethod("gin");
			entity.HasIndex(e => e.VisibleUserIds, "GIN_note_visibleUserIds").HasMethod("gin");
			entity.HasIndex(e => e.Text, "GIN_TRGM_note_text")
			      .HasMethod("gin")
			      .HasOperators("gin_trgm_ops");
			entity.HasIndex(e => e.Cw, "GIN_TRGM_note_cw")
			      .HasMethod("gin")
			      .HasOperators("gin_trgm_ops");
			entity.HasIndex(e => e.CombinedAltText, "GIN_TRGM_note_combined_alt_text")
			      .HasMethod("gin")
			      .HasOperators("gin_trgm_ops");

			entity.Property(e => e.AttachedFileTypes).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.ChannelId).HasComment("The ID of source channel.");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Note.");
			entity.Property(e => e.Emojis).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.FileIds).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.HasPoll).HasDefaultValue(false);
			entity.Property(e => e.LikeCount).HasDefaultValue(0);
			entity.Property(e => e.LocalOnly).HasDefaultValue(false);
			entity.Property(e => e.MentionedRemoteUsers).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.Mentions).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.Reactions).HasDefaultValueSql("'{}'::jsonb");
			entity.Property(e => e.RenoteCount).HasDefaultValue((short)0);
			entity.Property(e => e.RenoteId).HasComment("The ID of renote target.");
			entity.Property(e => e.RenoteUserHost).HasComment("[Denormalized]");
			entity.Property(e => e.RenoteUserId).HasComment("[Denormalized]");
			entity.Property(e => e.RepliesCount).HasDefaultValue((short)0);
			entity.Property(e => e.ReplyId).HasComment("The ID of reply target.");
			entity.Property(e => e.ReplyUserHost).HasComment("[Denormalized]");
			entity.Property(e => e.ReplyUserId).HasComment("[Denormalized]");
			entity.Property(e => e.Score).HasDefaultValue(0);
			entity.Property(e => e.Tags).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UpdatedAt).HasComment("The updated date of the Note.");
			entity.Property(e => e.Uri).HasComment("The URI of a note. it will be null when the note is local.");
			entity.Property(e => e.Url)
			      .HasComment("The human readable url of a note. it will be null when the note is local.");
			entity.Property(e => e.UserHost).HasComment("[Denormalized]");
			entity.Property(e => e.UserId).HasComment("The ID of author.");
			entity.Property(e => e.VisibleUserIds).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.ReplyUri)
			      .HasComment("The URI of the reply target, if it couldn't be resolved at time of ingestion.");
			entity.Property(e => e.RenoteUri)
			      .HasComment("The URI of the renote target, if it couldn't be resolved at time of ingestion.");

			entity.HasOne(d => d.Channel)
			      .WithMany(p => p.Notes)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Renote)
			      .WithMany(p => p.InverseRenote)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Reply)
			      .WithMany(p => p.InverseReply)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.Notes)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}
