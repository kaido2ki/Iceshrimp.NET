using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("marker")]
[Index("UserId")]
[Index("UserId", "Type", IsUnique = true)]
[PrimaryKey("UserId", "Type")]
public class Marker
{
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	[Column("type")]
	[StringLength(32)]
	public MarkerType Type { get; set; }

	[Column("position")]
	[StringLength(32)]
	public string Position { get; set; } = null!;

	[ConcurrencyCheck] [Column("version")] public int Version { get; set; }

	[Column("lastUpdated")] public DateTime LastUpdatedAt { get; set; }

	[ForeignKey("UserId")]
	[InverseProperty(nameof(Tables.User.Markers))]
	public virtual User User { get; set; } = null!;

	[PgName("marker_type_enum")]
	public enum MarkerType
	{
		[PgName("home")]          Home,
		[PgName("notifications")] Notifications
	}
}