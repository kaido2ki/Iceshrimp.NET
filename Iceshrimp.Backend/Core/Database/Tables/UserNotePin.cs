using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_note_pin")]
[Index(nameof(UserId), nameof(NoteId), IsUnique = true)]
[Index(nameof(UserId))]
public class UserNotePin
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the UserNotePins.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("noteId")] [StringLength(32)] public string NoteId { get; set; } = null!;

	[ForeignKey(nameof(NoteId))]
	[InverseProperty(nameof(Tables.Note.UserNotePins))]
	public virtual Note Note { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.UserNotePins))]
	public virtual User User { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<UserNotePin>
	{
		public void Configure(EntityTypeBuilder<UserNotePin> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the UserNotePins.");

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.UserNotePins)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.UserNotePins)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}