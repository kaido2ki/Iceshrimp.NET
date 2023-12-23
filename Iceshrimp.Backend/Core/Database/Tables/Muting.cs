using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("muting")]
[Index("MuterId", "MuteeId", Name = "IDX_1eb9d9824a630321a29fd3b290", IsUnique = true)]
[Index("MuterId", Name = "IDX_93060675b4a79a577f31d260c6")]
[Index("ExpiresAt", Name = "IDX_c1fd1c3dfb0627aa36c253fd14")]
[Index("MuteeId", Name = "IDX_ec96b4fed9dae517e0dbbe0675")]
[Index("CreatedAt", Name = "IDX_f86d57fbca33c7a4e6897490cc")]
public class Muting {
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
	[InverseProperty("MutingMutees")]
	public virtual User Mutee { get; set; } = null!;

	[ForeignKey("MuterId")]
	[InverseProperty("MutingMuters")]
	public virtual User Muter { get; set; } = null!;
}