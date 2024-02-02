using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("registry_item")]
[Index("Domain")]
[Index("Scope")]
[Index("UserId")]
public class RegistryItem {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the RegistryItem.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The updated date of the RegistryItem.
	/// </summary>
	[Column("updatedAt")]
	public DateTime UpdatedAt { get; set; }

	/// <summary>
	///     The owner ID.
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	/// <summary>
	///     The key of the RegistryItem.
	/// </summary>
	[Column("key")]
	[StringLength(1024)]
	public string Key { get; set; } = null!;

	[Column("scope", TypeName = "character varying(1024)[]")]
	public List<string> Scope { get; set; } = null!;

	[Column("domain")] [StringLength(512)] public string? Domain { get; set; }

	/// <summary>
	///     The value of the RegistryItem.
	/// </summary>
	[Column("value", TypeName = "jsonb")]
	public string? Value { get; set; }

	[ForeignKey("UserId")]
	[InverseProperty(nameof(Tables.User.RegistryItems))]
	public virtual User User { get; set; } = null!;
}