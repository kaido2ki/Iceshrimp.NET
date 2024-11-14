using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("note_reaction")]
[Index(nameof(CreatedAt))]
[Index(nameof(UserId))]
[Index(nameof(NoteId))]
[Index(nameof(UserId), nameof(NoteId), nameof(Reaction), IsUnique = true)]
public class NoteReaction
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the NoteReaction.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("noteId")] [StringLength(32)] public string NoteId { get; set; } = null!;

	[Column("reaction")]
	[StringLength(260)]
	public string Reaction { get; set; } = null!;

	[ForeignKey(nameof(NoteId))]
	[InverseProperty(nameof(Tables.Note.NoteReactions))]
	public virtual Note Note { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.NoteReactions))]
	public virtual User User { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<NoteReaction>
	{
		public void Configure(EntityTypeBuilder<NoteReaction> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the NoteReaction.");

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.NoteReactions)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.NoteReactions)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}