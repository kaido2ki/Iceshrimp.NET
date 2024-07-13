using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("plugin_store")]
[Index(nameof(Id))]
public class PluginStoreEntry
{
	[Key] [Column("id")]                 public Guid   Id   { get; set; }
	[Column("name")]                     public string Name { get; set; } = null!;
	[Column("data", TypeName = "jsonb")] public string Data { get; set; } = null!;
}

public class PluginStoreEntityTypeConfiguration : IEntityTypeConfiguration<PluginStoreEntry>
{
	public void Configure(EntityTypeBuilder<PluginStoreEntry> entity)
	{
		entity.Property(e => e.Data).HasDefaultValueSql("'{}'::jsonb").HasComment("The plugin-specific data object");
	}
}