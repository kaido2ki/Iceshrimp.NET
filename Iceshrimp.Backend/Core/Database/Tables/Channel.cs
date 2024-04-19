using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("channel")]
[Index(nameof(UsersCount))]
[Index(nameof(NotesCount))]
[Index(nameof(LastNotedAt))]
[Index(nameof(CreatedAt))]
[Index(nameof(UserId))]
public class Channel
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the Channel.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("lastNotedAt")] public DateTime? LastNotedAt { get; set; }

	/// <summary>
	///     The owner ID.
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string? UserId { get; set; }

	/// <summary>
	///     The name of the Channel.
	/// </summary>
	[Column("name")]
	[StringLength(128)]
	public string Name { get; set; } = null!;

	/// <summary>
	///     The description of the Channel.
	/// </summary>
	[Column("description")]
	[StringLength(2048)]
	public string? Description { get; set; }

	/// <summary>
	///     The ID of banner Channel.
	/// </summary>
	[Column("bannerId")]
	[StringLength(32)]
	public string? BannerId { get; set; }

	/// <summary>
	///     The count of notes.
	/// </summary>
	[Column("notesCount")]
	public int NotesCount { get; set; }

	/// <summary>
	///     The count of users.
	/// </summary>
	[Column("usersCount")]
	public int UsersCount { get; set; }

	[ForeignKey("BannerId")]
	[InverseProperty(nameof(DriveFile.Channels))]
	public virtual DriveFile? Banner { get; set; }

	[InverseProperty(nameof(ChannelFollowing.Followee))]
	public virtual ICollection<ChannelFollowing> ChannelFollowings { get; set; } = new List<ChannelFollowing>();

	[InverseProperty(nameof(ChannelNotePin.Channel))]
	public virtual ICollection<ChannelNotePin> ChannelNotePins { get; set; } = new List<ChannelNotePin>();

	[InverseProperty(nameof(Note.Channel))]
	public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

	[ForeignKey("UserId")]
	[InverseProperty(nameof(Tables.User.Channels))]
	public virtual User? User { get; set; }
}