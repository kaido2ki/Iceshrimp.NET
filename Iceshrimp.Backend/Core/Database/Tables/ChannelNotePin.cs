﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("channel_note_pin")]
[Index(nameof(ChannelId))]
[Index(nameof(ChannelId), nameof(NoteId), IsUnique = true)]
public class ChannelNotePin
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the ChannelNotePin.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("channelId")]
	[StringLength(32)]
	public string ChannelId { get; set; } = null!;

	[Column("noteId")] [StringLength(32)] public string NoteId { get; set; } = null!;

	[ForeignKey(nameof(ChannelId))]
	[InverseProperty(nameof(Tables.Channel.ChannelNotePins))]
	public virtual Channel Channel { get; set; } = null!;

	[ForeignKey(nameof(NoteId))]
	[InverseProperty(nameof(Tables.Note.ChannelNotePins))]
	public virtual Note Note { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<ChannelNotePin>
	{
		public void Configure(EntityTypeBuilder<ChannelNotePin> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the ChannelNotePin.");

			entity.HasOne(d => d.Channel)
			      .WithMany(p => p.ChannelNotePins)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.ChannelNotePins)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}