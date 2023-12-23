using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("blocking")]
[Index("BlockerId", Name = "IDX_0627125f1a8a42c9a1929edb55")]
[Index("BlockeeId", Name = "IDX_2cd4a2743a99671308f5417759")]
[Index("BlockerId", "BlockeeId", Name = "IDX_98a1bc5cb30dfd159de056549f", IsUnique = true)]
[Index("CreatedAt", Name = "IDX_b9a354f7941c1e779f3b33aea6")]
public class Blocking {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the Blocking.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The blockee user ID.
	/// </summary>
	[Column("blockeeId")]
	[StringLength(32)]
	public string BlockeeId { get; set; } = null!;

	/// <summary>
	///     The blocker user ID.
	/// </summary>
	[Column("blockerId")]
	[StringLength(32)]
	public string BlockerId { get; set; } = null!;

	[ForeignKey("BlockeeId")]
	[InverseProperty("BlockingBlockees")]
	public virtual User Blockee { get; set; } = null!;

	[ForeignKey("BlockerId")]
	[InverseProperty("BlockingBlockers")]
	public virtual User Blocker { get; set; } = null!;
}