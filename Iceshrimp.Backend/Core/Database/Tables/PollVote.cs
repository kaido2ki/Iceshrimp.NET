using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("poll_vote")]
[Index(nameof(CreatedAt))]
[Index(nameof(UserId), nameof(NoteId), nameof(Choice), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(NoteId))]
public class PollVote
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the PollVote.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("noteId")] [StringLength(32)] public string NoteId { get; set; } = null!;

	[Column("choice")] public int Choice { get; set; }

	[ForeignKey(nameof(NoteId))]
	[InverseProperty(nameof(Tables.Note.PollVotes))]
	public virtual Note Note { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.PollVotes))]
	public virtual User User { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<PollVote>
	{
		public void Configure(EntityTypeBuilder<PollVote> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the PollVote.");

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.PollVotes)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.PollVotes)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}