using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("promo_note")]
[Index(nameof(UserId))]
public class PromoNote
{
	[Key]
	[Column("noteId")]
	[StringLength(32)]
	public string NoteId { get; set; } = null!;

	[Column("expiresAt")] public DateTime ExpiresAt { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	[ForeignKey(nameof(NoteId))]
	[InverseProperty(nameof(Tables.Note.PromoNote))]
	public virtual Note Note { get; set; } = null!;
}