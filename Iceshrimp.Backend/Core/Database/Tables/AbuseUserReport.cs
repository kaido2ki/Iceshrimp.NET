﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("abuse_user_report")]
[Index(nameof(ReporterId))]
[Index(nameof(Resolved))]
[Index(nameof(TargetUserHost))]
[Index(nameof(TargetUserId))]
[Index(nameof(CreatedAt))]
[Index(nameof(ReporterHost))]
public class AbuseUserReport
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the AbuseUserReport.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("targetUserId")]
	[StringLength(32)]
	public string TargetUserId { get; set; } = null!;

	[Column("reporterId")]
	[StringLength(32)]
	public string ReporterId { get; set; } = null!;

	[Column("assigneeId")]
	[StringLength(32)]
	public string? AssigneeId { get; set; }

	[Column("resolved")] public bool Resolved { get; set; }

	[Column("comment")]
	[StringLength(2048)]
	public string Comment { get; set; } = null!;

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("targetUserHost")]
	[StringLength(512)]
	public string? TargetUserHost { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("reporterHost")]
	[StringLength(512)]
	public string? ReporterHost { get; set; }

	[Column("forwarded")] public bool Forwarded { get; set; }

	[ForeignKey(nameof(AssigneeId))]
	[InverseProperty(nameof(User.AbuseUserReportAssignees))]
	public virtual User? Assignee { get; set; }

	[ForeignKey(nameof(ReporterId))]
	[InverseProperty(nameof(User.AbuseUserReportReporters))]
	public virtual User Reporter { get; set; } = null!;

	[ForeignKey(nameof(TargetUserId))]
	[InverseProperty(nameof(User.AbuseUserReportTargetUsers))]
	public virtual User TargetUser { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<AbuseUserReport>
	{
		public void Configure(EntityTypeBuilder<AbuseUserReport> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the AbuseUserReport.");
			entity.Property(e => e.Forwarded).HasDefaultValue(false);
			entity.Property(e => e.ReporterHost).HasComment("[Denormalized]");
			entity.Property(e => e.Resolved).HasDefaultValue(false);
			entity.Property(e => e.TargetUserHost).HasComment("[Denormalized]");

			entity.HasOne(d => d.Assignee)
			      .WithMany(p => p.AbuseUserReportAssignees)
			      .OnDelete(DeleteBehavior.SetNull);

			entity.HasOne(d => d.Reporter)
			      .WithMany(p => p.AbuseUserReportReporters)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.TargetUser)
			      .WithMany(p => p.AbuseUserReportTargetUsers)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}