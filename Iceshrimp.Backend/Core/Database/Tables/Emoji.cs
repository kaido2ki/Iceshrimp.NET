using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("emoji")]
[Index("Name", "Host", IsUnique = true)]
[Index("Host")]
[Index("Name")]
public class Emoji
{
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
	public List<string> Aliases { get; set; } = [];

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