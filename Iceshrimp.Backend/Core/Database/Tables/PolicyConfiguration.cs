using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("policy_configuration")]
public class PolicyConfiguration
{
	[Key] [Column("name")]               public string Name { get; set; } = null!;
	[Column("data", TypeName = "jsonb")] public string Data { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<PolicyConfiguration>
	{
		public void Configure(EntityTypeBuilder<PolicyConfiguration> entity)
		{
			entity.Property(e => e.Data).HasDefaultValueSql("'{}'::jsonb").HasComment("The plugin-specific data object");
		}
	}
}