using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("abuse_user_report")]
[Index("ReporterId")]
[Index("Resolved")]
[Index("TargetUserHost")]
[Index("TargetUserId")]
[Index("CreatedAt")]
[Index("ReporterHost")]
public class AbuseUserReport {
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

	[ForeignKey("AssigneeId")]
	[InverseProperty(nameof(User.AbuseUserReportAssignees))]
	public virtual User? Assignee { get; set; }

	[ForeignKey("ReporterId")]
	[InverseProperty(nameof(User.AbuseUserReportReporters))]
	public virtual User Reporter { get; set; } = null!;

	[ForeignKey("TargetUserId")]
	[InverseProperty(nameof(User.AbuseUserReportTargetUsers))]
	public virtual User TargetUser { get; set; } = null!;
}