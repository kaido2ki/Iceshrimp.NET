using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Iceshrimp.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("note_like")]
[Index(nameof(UserId))]
[Index(nameof(NoteId))]
[Index(nameof(UserId), nameof(NoteId), IsUnique = true)]
public class NoteLike : IIdentifiable
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("createdAt")] public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("noteId")] [StringLength(32)] public string NoteId { get; set; } = null!;

	[ForeignKey(nameof(NoteId))]
	[InverseProperty(nameof(Tables.Note.NoteLikes))]
	public virtual Note Note { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.NoteLikes))]
	public virtual User User { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<NoteLike>
	{
		public void Configure(EntityTypeBuilder<NoteLike> entity)
		{
			entity.HasOne(d => d.Note)
			      .WithMany(p => p.NoteLikes)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.NoteLikes)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}