using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("clip")]
[Index("UserId")]
public class Clip {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the Clip.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The owner ID.
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	/// <summary>
	///     The name of the Clip.
	/// </summary>
	[Column("name")]
	[StringLength(128)]
	public string Name { get; set; } = null!;

	[Column("isPublic")] public bool IsPublic { get; set; }

	/// <summary>
	///     The description of the Clip.
	/// </summary>
	[Column("description")]
	[StringLength(2048)]
	public string? Description { get; set; }

	[InverseProperty(nameof(ClipNote.Clip))] public virtual ICollection<ClipNote> ClipNotes { get; set; } = new List<ClipNote>();

	[ForeignKey("UserId")]
	[InverseProperty(nameof(Tables.User.Clips))]
	public virtual User User { get; set; } = null!;
}