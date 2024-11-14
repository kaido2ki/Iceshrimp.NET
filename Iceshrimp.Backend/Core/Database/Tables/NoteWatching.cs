using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("note_watching")]
[Index(nameof(NoteId))]
[Index(nameof(CreatedAt))]
[Index(nameof(NoteUserId))]
[Index(nameof(UserId), nameof(NoteId), IsUnique = true)]
[Index(nameof(UserId))]
public class NoteWatching
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the NoteWatching.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The watcher ID.
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	/// <summary>
	///     The target Note ID.
	/// </summary>
	[Column("noteId")]
	[StringLength(32)]
	public string NoteId { get; set; } = null!;

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("noteUserId")]
	[StringLength(32)]
	public string NoteUserId { get; set; } = null!;

	[ForeignKey(nameof(NoteId))]
	[InverseProperty(nameof(Tables.Note.NoteWatchings))]
	public virtual Note Note { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.NoteWatchings))]
	public virtual User User { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<NoteWatching>
	{
		public void Configure(EntityTypeBuilder<NoteWatching> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the NoteWatching.");
			entity.Property(e => e.NoteId).HasComment("The target Note ID.");
			entity.Property(e => e.NoteUserId).HasComment("[Denormalized]");
			entity.Property(e => e.UserId).HasComment("The watcher ID.");

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.NoteWatchings)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.NoteWatchings)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}