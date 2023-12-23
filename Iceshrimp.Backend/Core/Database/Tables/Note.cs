using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("note")]
[Index("Uri", Name = "IDX_153536c67d05e9adb24e99fc2b", IsUnique = true)]
[Index("ReplyId", Name = "IDX_17cb3553c700a4985dff5a30ff")]
[Index("AttachedFileTypes", Name = "IDX_25dfc71b0369b003a4cd434d0b")]
[Index("FileIds", Name = "IDX_51c063b6a133a9cb87145450f5")]
[Index("RenoteId", Name = "IDX_52ccc804d7c69037d558bac4c9")]
[Index("Mentions", Name = "IDX_54ebcb6d27222913b908d56fd8")]
[Index("UserId", Name = "IDX_5b87d9d19127bd5d92026017a7")]
[Index("UserHost", Name = "IDX_7125a826ab192eb27e11d358a5")]
[Index("VisibleUserIds", Name = "IDX_796a8c03959361f97dc2be1d5c")]
[Index("Tags", Name = "IDX_88937d94d7443d9a99a76fa5c0")]
[Index("ThreadId", Name = "IDX_d4ebdef929896d6dc4a3c5bb48")]
[Index("CreatedAt", Name = "IDX_e7c0567f5261063592f022e9b5")]
[Index("ChannelId", Name = "IDX_f22169eb10657bded6d875ac8f")]
[Index("CreatedAt", "UserId", Name = "IDX_note_createdAt_userId")]
[Index("Id", "UserHost", Name = "IDX_note_id_userHost")]
[Index("Url", Name = "IDX_note_url")]
[Index("UserId", "Id", Name = "IDX_note_userId_id")]
public class Note {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

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

	[Column("text")] public string? Text { get; set; }

	[Column("name")] [StringLength(256)] public string? Name { get; set; }

	[Column("cw")] [StringLength(512)] public string? Cw { get; set; }

	/// <summary>
	///     The ID of author.
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	[Column("localOnly")] public bool LocalOnly { get; set; }

	[Column("renoteCount")] public short RenoteCount { get; set; }

	[Column("repliesCount")] public short RepliesCount { get; set; }

	[Column("reactions", TypeName = "jsonb")]
	public string Reactions { get; set; } = null!;

	/// <summary>
	///     The URI of a note. it will be null when the note is local.
	/// </summary>
	[Column("uri")]
	[StringLength(512)]
	public string? Uri { get; set; }

	[Column("score")] public int Score { get; set; }

	[Column("fileIds", TypeName = "character varying(32)[]")]
	public List<string> FileIds { get; set; } = null!;

	[Column("attachedFileTypes", TypeName = "character varying(256)[]")]
	public List<string> AttachedFileTypes { get; set; } = null!;

	[Column("visibleUserIds", TypeName = "character varying(32)[]")]
	public List<string> VisibleUserIds { get; set; } = null!;

	[Column("mentions", TypeName = "character varying(32)[]")]
	public List<string> Mentions { get; set; } = null!;

	[Column("mentionedRemoteUsers")] public string MentionedRemoteUsers { get; set; } = null!;

	[Column("emojis", TypeName = "character varying(128)[]")]
	public List<string> Emojis { get; set; } = null!;

	[Column("tags", TypeName = "character varying(128)[]")]
	public List<string> Tags { get; set; } = null!;

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
	public string? ThreadId { get; set; }

	/// <summary>
	///     The updated date of the Note.
	/// </summary>
	[Column("updatedAt")]
	public DateTime? UpdatedAt { get; set; }

	[ForeignKey("ChannelId")]
	[InverseProperty("Notes")]
	public virtual Channel? Channel { get; set; }

	[InverseProperty("Note")]
	public virtual ICollection<ChannelNotePining> ChannelNotePinings { get; set; } = new List<ChannelNotePining>();

	[InverseProperty("Note")] public virtual ICollection<ClipNote> ClipNotes { get; set; } = new List<ClipNote>();

	[InverseProperty("Note")] public virtual HtmlNoteCacheEntry? HtmlNoteCacheEntry { get; set; }

	[InverseProperty("Renote")] public virtual ICollection<Note> InverseRenote { get; set; } = new List<Note>();

	[InverseProperty("Reply")] public virtual ICollection<Note> InverseReply { get; set; } = new List<Note>();

	[InverseProperty("Note")] public virtual ICollection<NoteEdit> NoteEdits { get; set; } = new List<NoteEdit>();

	[InverseProperty("Note")]
	public virtual ICollection<NoteFavorite> NoteFavorites { get; set; } = new List<NoteFavorite>();

	[InverseProperty("Note")]
	public virtual ICollection<NoteReaction> NoteReactions { get; set; } = new List<NoteReaction>();

	[InverseProperty("Note")] public virtual ICollection<NoteUnread> NoteUnreads { get; set; } = new List<NoteUnread>();

	[InverseProperty("Note")]
	public virtual ICollection<NoteWatching> NoteWatchings { get; set; } = new List<NoteWatching>();

	[InverseProperty("Note")]
	public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

	[InverseProperty("Note")] public virtual Poll? Poll { get; set; }

	[InverseProperty("Note")] public virtual ICollection<PollVote> PollVotes { get; set; } = new List<PollVote>();

	[InverseProperty("Note")] public virtual PromoNote? PromoNote { get; set; }

	[InverseProperty("Note")] public virtual ICollection<PromoRead> PromoReads { get; set; } = new List<PromoRead>();

	[ForeignKey("RenoteId")]
	[InverseProperty("InverseRenote")]
	public virtual Note? Renote { get; set; }

	[ForeignKey("ReplyId")]
	[InverseProperty("InverseReply")]
	public virtual Note? Reply { get; set; }

	[ForeignKey("UserId")]
	[InverseProperty("Notes")]
	public virtual User User { get; set; } = null!;

	[InverseProperty("Note")]
	public virtual ICollection<UserNotePining> UserNotePinings { get; set; } = new List<UserNotePining>();
}