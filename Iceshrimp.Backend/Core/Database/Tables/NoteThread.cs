using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("note_thread")]
public class NoteThread : IEntity
{
	[Column("id")]
	[StringLength(256)]
	public required string Id { get; set; }
	
	/// <summary>
	///     The last time this thread has been backfilled.
	/// </summary>
	[Column("backfilledAt")]
	public DateTime? BackfilledAt { get; set; }
	
	[InverseProperty(nameof(Note.Thread))]
	public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

	[InverseProperty(nameof(NoteThreadMuting.Thread))]
	public virtual ICollection<NoteThreadMuting> NoteThreadMutings { get; set; } = new List<NoteThreadMuting>();
}
