namespace Iceshrimp.Shared.Schemas.Web;

public class NoteReportRequest
{
	public required string       Comment { get; set; }
	public required List<string> RuleIds { get; set; }
}

public class UserReportRequest : NoteReportRequest
{
	public required List<string> NoteIds { get; set; }
}
