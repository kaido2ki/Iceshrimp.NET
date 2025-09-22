using Iceshrimp.Shared.Helpers;

namespace Iceshrimp.Shared.Schemas.Web;

public class ReportResponse : IIdentifiable
{
	public required string         Id         { get; set; }
	public required DateTime       CreatedAt  { get; set; }
	public required UserResponse   TargetUser { get; set; }
	public required UserResponse   Reporter   { get; set; }
	public required UserResponse?  Assignee   { get; set; }
	public required NoteResponse[] Notes      { get; set; }
	public required RuleResponse[] Rules      { get; set; }
	public required bool           Resolved   { get; set; }
	public required bool           Forwarded  { get; set; }
	public required string         Comment    { get; set; }
}
