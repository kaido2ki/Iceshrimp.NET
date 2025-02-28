﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Iceshrimp.Backend.Core.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("emoji")]
[Index(nameof(Name), nameof(Host), IsUnique = true)]
[Index(nameof(Host))]
[Index(nameof(Name))]
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

	[Column("sensitive")]
	public bool Sensitive { get; set; }

	public string GetPublicUri(Config.InstanceSection config) => Host == null
		? $"https://{config.WebDomain}/emoji/{Name}"
		: throw new Exception("Cannot access PublicUri for remote emoji");

	public string? GetPublicUriOrNull(Config.InstanceSection config) => Host == null
		? $"https://{config.WebDomain}/emoji/{Name}"
		: null;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<Emoji>
	{
		public void Configure(EntityTypeBuilder<Emoji> entity)
		{
			entity.Property(e => e.Aliases).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.Height).HasComment("Image height");
			entity.Property(e => e.PublicUrl).HasDefaultValueSql("''::character varying");
			entity.Property(e => e.Width).HasComment("Image width");
		}
	}
}