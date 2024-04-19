using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("blocking")]
[Index(nameof(BlockerId))]
[Index(nameof(BlockeeId))]
[Index("BlockerId", "BlockeeId", IsUnique = true)]
[Index(nameof(CreatedAt))]
public class Blocking
{
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

	[ForeignKey(nameof(BlockeeId))]
	[InverseProperty(nameof(User.IncomingBlocks))]
	public virtual User Blockee { get; set; } = null!;

	[ForeignKey(nameof(BlockerId))]
	[InverseProperty(nameof(User.OutgoingBlocks))]
	public virtual User Blocker { get; set; } = null!;
}