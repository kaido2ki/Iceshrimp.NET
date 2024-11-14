using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("allowed_instance")]
public class AllowedInstance
{
	[Key]
	[Column("host")]
	[StringLength(256)]
	public string Host { get; set; } = null!;

	[Column("imported")] public bool IsImported { get; set; }

	private class EntityTypeConfiguration : IEntityTypeConfiguration<AllowedInstance>
	{
		public void Configure(EntityTypeBuilder<AllowedInstance> builder)
		{
			builder.Property(p => p.IsImported).HasDefaultValue(false);
		}
	}
}