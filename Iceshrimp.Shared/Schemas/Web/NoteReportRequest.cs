namespace Iceshrimp.Shared.Schemas.Web;

public class NoteReportRequest
{
    public required string Comment { get; set; }

    /// <summary>
    /// Rules that the user has violated
    /// </summary>
    public required List<string> RuleIds { get; set; }
}

public class UserReportRequest : NoteReportRequest
{
    public required List<string> NoteIds { get; set; }
}
