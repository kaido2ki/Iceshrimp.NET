using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("note_unread")]
[Index(nameof(IsMentioned))]
[Index(nameof(NoteUserId))]
[Index(nameof(UserId))]
[Index(nameof(NoteChannelId))]
[Index(nameof(IsSpecified))]
[Index(nameof(UserId), nameof(NoteId), IsUnique = true)]
[Index(nameof(NoteId))]
public class NoteUnread
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("noteId")] [StringLength(32)] public string NoteId { get; set; } = null!;

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("noteUserId")]
	[StringLength(32)]
	public string NoteUserId { get; set; } = null!;

	[Column("isSpecified")] public bool IsSpecified { get; set; }

	[Column("isMentioned")] public bool IsMentioned { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("noteChannelId")]
	[StringLength(32)]
	public string? NoteChannelId { get; set; }

	[ForeignKey(nameof(NoteId))]
	[InverseProperty(nameof(Tables.Note.NoteUnreads))]
	public virtual Note Note { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.NoteUnreads))]
	public virtual User User { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<NoteUnread>
	{
		public void Configure(EntityTypeBuilder<NoteUnread> entity)
		{
			entity.Property(e => e.NoteChannelId).HasComment("[Denormalized]");
			entity.Property(e => e.NoteUserId).HasComment("[Denormalized]");

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.NoteUnreads)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.NoteUnreads)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}