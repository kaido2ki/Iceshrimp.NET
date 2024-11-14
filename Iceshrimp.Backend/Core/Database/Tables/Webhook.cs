using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("webhook")]
[Index(nameof(Active))]
[Index(nameof(On))]
[Index(nameof(UserId))]
public class Webhook
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the Antenna.
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
	///     The name of the Antenna.
	/// </summary>
	[Column("name")]
	[StringLength(128)]
	public string Name { get; set; } = null!;

	[Column("on", TypeName = "character varying(128)[]")]
	public List<string> On { get; set; } = null!;

	[Column("url")] [StringLength(1024)] public string Url { get; set; } = null!;

	[Column("secret")]
	[StringLength(1024)]
	public string Secret { get; set; } = null!;

	[Column("active")] public bool Active { get; set; }

	[Column("latestSentAt")] public DateTime? LatestSentAt { get; set; }

	[Column("latestStatus")] public int? LatestStatus { get; set; }

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.Webhooks))]
	public virtual User User { get; set; } = null!;

	private class EntityTypeConfiguration : IEntityTypeConfiguration<Webhook>
	{
		public void Configure(EntityTypeBuilder<Webhook> entity)
		{
			entity.Property(e => e.Active).HasDefaultValue(true);
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Antenna.");
			entity.Property(e => e.Name).HasComment("The name of the Antenna.");
			entity.Property(e => e.On).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.UserId).HasComment("The owner ID.");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.Webhooks)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}