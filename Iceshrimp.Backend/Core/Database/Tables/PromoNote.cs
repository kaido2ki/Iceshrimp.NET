using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("promo_note")]
[Index("UserId", Name = "IDX_83f0862e9bae44af52ced7099e")]
public class PromoNote {
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

	[ForeignKey("NoteId")]
	[InverseProperty("PromoNote")]
	public virtual Note Note { get; set; } = null!;
}