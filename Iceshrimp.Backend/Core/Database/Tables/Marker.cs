using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("marker")]
[Index(nameof(UserId))]
[PrimaryKey(nameof(UserId), nameof(Type))]
public class Marker
{
	[PgName("marker_type_enum")]
	public enum MarkerType
	{
		[PgName("home")]          Home,
		[PgName("notifications")] Notifications
	}

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("type")] [StringLength(32)] public MarkerType Type { get; set; }

	[Column("position")]
	[StringLength(32)]
	public string Position { get; set; } = null!;

	[ConcurrencyCheck] [Column("version")] public int Version { get; set; }

	[Column("lastUpdated")] public DateTime LastUpdatedAt { get; set; }

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.Markers))]
	public virtual User User { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<Marker>
	{
		public void Configure(EntityTypeBuilder<Marker> entity)
		{
			entity.Property(d => d.Version).HasDefaultValue(0);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.Markers)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}