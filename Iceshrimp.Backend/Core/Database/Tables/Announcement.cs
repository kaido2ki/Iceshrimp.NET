﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.Projectables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("announcement")]
[Index(nameof(CreatedAt))]
public class Announcement
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the Announcement.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("text")] [StringLength(8192)] public string Text { get; set; } = null!;

	[Column("title")] [StringLength(256)] public string Title { get; set; } = null!;

	[Column("imageUrl")]
	[StringLength(1024)]
	public string? ImageUrl { get; set; }

	/// <summary>
	///     The updated date of the Announcement.
	/// </summary>
	[Column("updatedAt")]
	public DateTime? UpdatedAt { get; set; }

	[Column("showPopup")] public bool ShowPopup { get; set; }

	[Column("isGoodNews")] public bool IsGoodNews { get; set; }

	[InverseProperty(nameof(AnnouncementRead.Announcement))]
	public virtual ICollection<AnnouncementRead> AnnouncementReads { get; set; } = new List<AnnouncementRead>();

	[NotMapped] [Projectable] public virtual IEnumerable<User> ReadBy => AnnouncementReads.Select(p => p.User);

	[Projectable]
	public bool IsReadBy(User user) => ReadBy.Contains(user);
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<Announcement>
	{
		public void Configure(EntityTypeBuilder<Announcement> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Announcement.");
			entity.Property(e => e.IsGoodNews).HasDefaultValue(false);
			entity.Property(e => e.ShowPopup).HasDefaultValue(false);
			entity.Property(e => e.UpdatedAt).HasComment("The updated date of the Announcement.");
		}
	}
}