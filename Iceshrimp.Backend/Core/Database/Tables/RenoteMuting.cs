using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("renote_muting")]
[Index("MuterId", "MuteeId", IsUnique = true)]
[Index("MuterId")]
[Index("MuteeId")]
[Index("CreatedAt")]
public class RenoteMuting
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

	[ForeignKey("MuteeId")]
	[InverseProperty(nameof(User.RenoteMutingMutees))]
	public virtual User Mutee { get; set; } = null!;

	[ForeignKey("MuterId")]
	[InverseProperty(nameof(User.RenoteMutingMuters))]
	public virtual User Muter { get; set; } = null!;
}