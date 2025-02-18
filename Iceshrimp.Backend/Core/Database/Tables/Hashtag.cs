using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Iceshrimp.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("hashtag")]
[Index(nameof(Name), IsUnique = true)]
public class Hashtag : IIdentifiable
{
	[Column("name")] [StringLength(128)] public string Name { get; set; } = null!;

	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	private class EntityTypeConfiguration : IEntityTypeConfiguration<Hashtag>
	{
		public void Configure(EntityTypeBuilder<Hashtag> entity) { }
	}
}