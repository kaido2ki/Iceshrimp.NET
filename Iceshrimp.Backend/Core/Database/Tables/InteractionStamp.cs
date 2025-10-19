using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("interaction_stamp")]
[Index(nameof(NoteId), IsUnique = true)]
public class InteractionStamp
{
	[PgName("interaction_stamp_type")]
	public enum InteractionStampType
	{
		[PgName("quote")] Quote,
	}
	
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;
	
	[Column("type")]
	public InteractionStampType Type { get; set; }

	/// <summary>
	/// The note being interacted with
	/// </summary>
	[Column("targetNoteId")]
	[StringLength(32)]
	public string TargetNoteId { get; set; } = null!;
	
	/// <summary>
	/// The note doing the interaction (quote, reply, whatever)
	/// </summary>
	[Column("noteId")]
	[StringLength(32)]
	public string NoteId { get; set; } = null!;
	
	[ForeignKey(nameof(TargetNoteId))]
	public Note TargetNote { get; set; } = null!;
	
	[ForeignKey(nameof(NoteId))]
	public Note Note { get; set; } = null!;

	private class EntityTypeConfiguration : IEntityTypeConfiguration<InteractionStamp>
	{
		public void Configure(EntityTypeBuilder<InteractionStamp> entity)
		{
			entity.Property(e => e.TargetNoteId).HasComment("The note being interacted with");
			entity.Property(e => e.NoteId).HasComment("The note doing the interaction (quote, reply, whatever)");
            
            // Reverse of the filter from Note
            // TODO: name this filter when we update to EF 10
            // https://learn.microsoft.com/en-us/ef/core/querying/filters?tabs=ef10#using-multiple-query-filters
            entity.HasQueryFilter(e => e.Note.Published);
		}
	}
}