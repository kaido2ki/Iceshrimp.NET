using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("renote_muting")]
[Index("MuterId", "MuteeId", Name = "IDX_0d801c609cec4e9eb4b6b4490c", IsUnique = true)]
[Index("MuterId", Name = "IDX_7aa72a5fe76019bfe8e5e0e8b7")]
[Index("MuteeId", Name = "IDX_7eac97594bcac5ffcf2068089b")]
[Index("CreatedAt", Name = "IDX_d1259a2c2b7bb413ff449e8711")]
public class RenoteMuting {
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
	[InverseProperty("RenoteMutingMutees")]
	public virtual User Mutee { get; set; } = null!;

	[ForeignKey("MuterId")]
	[InverseProperty("RenoteMutingMuters")]
	public virtual User Muter { get; set; } = null!;
}