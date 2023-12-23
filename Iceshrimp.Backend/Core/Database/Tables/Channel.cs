using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("channel")]
[Index("UsersCount", Name = "IDX_094b86cd36bb805d1aa1e8cc9a")]
[Index("NotesCount", Name = "IDX_0f58c11241e649d2a638a8de94")]
[Index("LastNotedAt", Name = "IDX_29ef80c6f13bcea998447fce43")]
[Index("CreatedAt", Name = "IDX_71cb7b435b7c0d4843317e7e16")]
[Index("UserId", Name = "IDX_823bae55bd81b3be6e05cff438")]
public class Channel {
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
	[InverseProperty("Channels")]
	public virtual DriveFile? Banner { get; set; }

	[InverseProperty("Followee")]
	public virtual ICollection<ChannelFollowing> ChannelFollowings { get; set; } = new List<ChannelFollowing>();

	[InverseProperty("Channel")]
	public virtual ICollection<ChannelNotePining> ChannelNotePinings { get; set; } = new List<ChannelNotePining>();

	[InverseProperty("Channel")] public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

	[ForeignKey("UserId")]
	[InverseProperty("Channels")]
	public virtual User? User { get; set; }
}