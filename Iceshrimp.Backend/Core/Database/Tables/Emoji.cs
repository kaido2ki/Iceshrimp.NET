using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("emoji")]
[Index("Name", "Host", Name = "IDX_4f4d35e1256c84ae3d1f0eab10", IsUnique = true)]
[Index("Host", Name = "IDX_5900e907bb46516ddf2871327c")]
[Index("Name", Name = "IDX_b37dafc86e9af007e3295c2781")]
public class Emoji {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("updatedAt")] public DateTime? UpdatedAt { get; set; }

	[Column("name")] [StringLength(128)] public string Name { get; set; } = null!;

	[Column("host")] [StringLength(512)] public string? Host { get; set; }

	[Column("originalUrl")]
	[StringLength(512)]
	public string OriginalUrl { get; set; } = null!;

	[Column("uri")] [StringLength(512)] public string? Uri { get; set; }

	[Column("type")] [StringLength(64)] public string? Type { get; set; }

	[Column("aliases", TypeName = "character varying(128)[]")]
	public List<string> Aliases { get; set; } = null!;

	[Column("category")]
	[StringLength(128)]
	public string? Category { get; set; }

	[Column("publicUrl")]
	[StringLength(512)]
	public string PublicUrl { get; set; } = null!;

	[Column("license")]
	[StringLength(1024)]
	public string? License { get; set; }

	/// <summary>
	///     Image width
	/// </summary>
	[Column("width")]
	public int? Width { get; set; }

	/// <summary>
	///     Image height
	/// </summary>
	[Column("height")]
	public int? Height { get; set; }
}