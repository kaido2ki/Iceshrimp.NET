using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Utils.DependencyInjection;

namespace Iceshrimp.Backend.Core.Services;

public class ReportService(
	ActivityPub.ActivityDeliverService deliverSvc,
	ActivityPub.ActivityRenderer activityRenderer,
	SystemUserService sysUserSvc,
	NotificationService notificationSvc,
	DatabaseContext db
) : IScopedService
{
	public async Task ForwardReportAsync(Report report, string? comment)
	{
		if (report.Forwarded)
			return;
		if (report.TargetUser.IsLocalUser)
			throw new Exception("Refusing to forward report to local instance");

		var actor    = await sysUserSvc.GetInstanceActorAsync();
		var activity = activityRenderer.RenderFlag(actor, report.TargetUser, report.Notes, comment ?? report.Comment);

		var inbox = report.TargetUser.SharedInbox
		            ?? report.TargetUser.Inbox
		            ?? throw new Exception("Target user does not have inbox");

		await deliverSvc.DeliverToAsync(activity, actor, inbox);
		report.Forwarded = true;
		await db.SaveChangesAsync();
	}

	public async Task<Report> CreateReportAsync(
		User reporter, User target, IEnumerable<Note> notes, IEnumerable<Rule> rules, string comment
	)
	{
		var report = new Report
		{
			Id             = IdHelpers.GenerateSnowflakeId(),
			CreatedAt      = DateTime.UtcNow,
			TargetUser     = target,
			TargetUserHost = target.Host,
			Reporter       = reporter,
			ReporterHost   = reporter.Host,
			Notes          = notes.ToArray(),
			Rules          = rules.ToArray(),
			Comment        = comment
		};

		db.Add(report);
		await db.SaveChangesAsync();
		await db.ReloadEntityRecursivelyAsync(report);

		await notificationSvc.GenerateReportNotificationsAsync(report);
		return report;
	}
}
