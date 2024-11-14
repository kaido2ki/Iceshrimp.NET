using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("moderation_log")]
[Index(nameof(UserId))]
public class ModerationLog
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the ModerationLog.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("type")] [StringLength(128)] public string Type { get; set; } = null!;

	//TODO: refactor this column (it's currently a Dictionary<string, any>, which is terrible) 
	[Column("info", TypeName = "jsonb")] public string Info { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.ModerationLogs))]
	public virtual User User { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<ModerationLog>
	{
		public void Configure(EntityTypeBuilder<ModerationLog> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the ModerationLog.");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.ModerationLogs)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}