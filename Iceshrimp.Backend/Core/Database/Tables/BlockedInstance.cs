using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("blocked_instance")]
public class BlockedInstance
{
	[Key]
	[Column("host")]
	[StringLength(256)]
	public string Host { get; set; } = null!;

	[Column("reason")]
	[StringLength(1024)]
	public string? Reason { get; set; }

	[Column("imported")] public bool IsImported { get; set; }
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<BlockedInstance>
	{
		public void Configure(EntityTypeBuilder<BlockedInstance> builder)
		{
			builder.Property(p => p.IsImported).HasDefaultValue(false);
		}
	}
}