using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Shared.Schemas.Web;
using Iceshrimp.Utils.DependencyInjection;

namespace Iceshrimp.Backend.Controllers.Web.Renderers;

public class ReportRenderer(UserRenderer userRenderer, NoteRenderer noteRenderer) : IScopedService
{
	private static ReportResponse Render(Report report, ReportRendererDto data)
	{
		var noteIds = report.Notes.Select(i => i.Id).ToArray();
		var ruleIds = report.Rules.Select(i => i.Id).ToArray();
		return new ReportResponse
		{
			Id         = report.Id,
			CreatedAt  = report.CreatedAt,
			Comment    = report.Comment,
			Forwarded  = report.Forwarded,
			Resolved   = report.Resolved,
			Assignee   = data.Users.FirstOrDefault(p => p.Id == report.AssigneeId),
			TargetUser = data.Users.First(p => p.Id == report.TargetUserId),
			Reporter   = data.Users.First(p => p.Id == report.ReporterId),
			Notes      = data.Notes.Where(p => noteIds.Contains(p.Id)).ToArray(),
			Rules      = data.Rules.Where(p => ruleIds.Contains(p.Id)).ToArray()
		};
	}

	public async Task<ReportResponse> RenderOneAsync(Report report)
	{
		return Render(report, await BuildDtoAsync(report));
	}

	public async Task<IEnumerable<ReportResponse>> RenderManyAsync(IEnumerable<Report> reports)
	{
		var arr  = reports.ToArray();
		var data = await BuildDtoAsync(arr);
		return arr.Select(p => Render(p, data));
	}

	private async Task<UserResponse[]> GetUsersAsync(IEnumerable<User> users)
		=> await userRenderer.RenderManyAsync(users).ToArrayAsync();

	private async Task<NoteResponse[]> GetNotesAsync(IEnumerable<Note> notes)
		=> await noteRenderer.RenderManyAsync(notes, null).ToArrayAsync();

	private RuleResponse[] GetRules(IEnumerable<Rule> rules) =>
		rules.Select(p => new RuleResponse { Id = p.Id, Text = p.Text, Description = p.Description }).ToArray();

	private async Task<ReportRendererDto> BuildDtoAsync(params Report[] reports)
	{
		var notes = await GetNotesAsync(reports.SelectMany(p => p.Notes));
		var users = notes.Select(p => p.User).DistinctBy(p => p.Id).ToList();
		var rules = GetRules(reports.SelectMany(p => p.Rules));

		var missingUsers = reports.Select(p => p.TargetUser)
		                          .Concat(reports.Select(p => p.Assignee))
		                          .Concat(reports.Select(p => p.Reporter))
		                          .NotNull()
		                          .DistinctBy(p => p.Id)
		                          .ExceptBy(users.Select(p => p.Id), p => p.Id);

		users.AddRange(await GetUsersAsync(missingUsers));

		return new ReportRendererDto { Users = users.ToArray(), Notes = notes, Rules = rules};
	}

	private class ReportRendererDto
	{
		public required UserResponse[] Users;
		public required NoteResponse[] Notes;
		public required RuleResponse[] Rules;
	}
}
