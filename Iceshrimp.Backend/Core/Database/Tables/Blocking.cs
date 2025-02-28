﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("blocking")]
[Index(nameof(BlockerId))]
[Index(nameof(BlockeeId))]
[Index(nameof(BlockerId), nameof(BlockeeId), IsUnique = true)]
[Index(nameof(CreatedAt))]
public class Blocking
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the Blocking.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The blockee user ID.
	/// </summary>
	[Column("blockeeId")]
	[StringLength(32)]
	public string BlockeeId { get; set; } = null!;

	/// <summary>
	///     The blocker user ID.
	/// </summary>
	[Column("blockerId")]
	[StringLength(32)]
	public string BlockerId { get; set; } = null!;

	[ForeignKey(nameof(BlockeeId))]
	[InverseProperty(nameof(User.IncomingBlocks))]
	public virtual User Blockee { get; set; } = null!;

	[ForeignKey(nameof(BlockerId))]
	[InverseProperty(nameof(User.OutgoingBlocks))]
	public virtual User Blocker { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<Blocking>
	{
		public void Configure(EntityTypeBuilder<Blocking> entity)
		{
			entity.Property(e => e.BlockeeId).HasComment("The blockee user ID.");
			entity.Property(e => e.BlockerId).HasComment("The blocker user ID.");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Blocking.");

			entity.HasOne(d => d.Blockee)
			      .WithMany(p => p.IncomingBlocks)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Blocker)
			      .WithMany(p => p.OutgoingBlocks)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}