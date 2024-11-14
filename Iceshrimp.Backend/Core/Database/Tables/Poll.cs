using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("poll")]
[Index(nameof(UserId))]
[Index(nameof(UserHost))]
public class Poll
{
	[Key]
	[Column("noteId")]
	[StringLength(32)]
	public string NoteId { get; set; } = null!;

	[Column("expiresAt")] public DateTime? ExpiresAt { get; set; }

	[Column("multiple")] public bool Multiple { get; set; }

	[Column("choices", TypeName = "character varying(256)[]")]
	public List<string> Choices { get; set; } = null!;

	[Column("votes")] public List<int> Votes { get; set; } = null!;

	[Column("votersCount")] public int? VotersCount { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("userHost")]
	[StringLength(512)]
	public string? UserHost { get; set; }

	[ForeignKey(nameof(NoteId))]
	[InverseProperty(nameof(Tables.Note.Poll))]
	public virtual Note Note { get; set; } = null!;

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("noteVisibility")]
	public Note.NoteVisibility NoteVisibility { get; set; }
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<Poll>
	{
		public void Configure(EntityTypeBuilder<Poll> entity)
		{
			entity.Property(e => e.Choices).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UserHost).HasComment("[Denormalized]");
			entity.Property(e => e.UserId).HasComment("[Denormalized]");
			entity.Property(e => e.NoteVisibility).HasComment("[Denormalized]");

			entity.HasOne(d => d.Note)
			      .WithOne(p => p.Poll)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}