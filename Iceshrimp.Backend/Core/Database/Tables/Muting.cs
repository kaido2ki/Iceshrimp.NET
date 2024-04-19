using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("muting")]
[Index("MuterId", "MuteeId", IsUnique = true)]
[Index(nameof(MuterId))]
[Index(nameof(ExpiresAt))]
[Index(nameof(MuteeId))]
[Index(nameof(CreatedAt))]
public class Muting
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the Muting.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The mutee user ID.
	/// </summary>
	[Column("muteeId")]
	[StringLength(32)]
	public string MuteeId { get; set; } = null!;

	/// <summary>
	///     The muter user ID.
	/// </summary>
	[Column("muterId")]
	[StringLength(32)]
	public string MuterId { get; set; } = null!;

	[Column("expiresAt")] public DateTime? ExpiresAt { get; set; }

	[ForeignKey("MuteeId")]
	[InverseProperty(nameof(User.IncomingMutes))]
	public virtual User Mutee { get; set; } = null!;

	[ForeignKey("MuterId")]
	[InverseProperty(nameof(User.OutgoingMutes))]
	public virtual User Muter { get; set; } = null!;
}