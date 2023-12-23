using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("channel_note_pining")]
[Index("ChannelId", Name = "IDX_8125f950afd3093acb10d2db8a")]
[Index("ChannelId", "NoteId", Name = "IDX_f36fed37d6d4cdcc68c803cd9c", IsUnique = true)]
public class ChannelNotePining {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the ChannelNotePining.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("channelId")]
	[StringLength(32)]
	public string ChannelId { get; set; } = null!;

	[Column("noteId")] [StringLength(32)] public string NoteId { get; set; } = null!;

	[ForeignKey("ChannelId")]
	[InverseProperty("ChannelNotePinings")]
	public virtual Channel Channel { get; set; } = null!;

	[ForeignKey("NoteId")]
	[InverseProperty("ChannelNotePinings")]
	public virtual Note Note { get; set; } = null!;
}