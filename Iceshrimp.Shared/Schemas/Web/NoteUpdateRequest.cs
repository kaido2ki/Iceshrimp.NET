namespace Iceshrimp.Shared.Schemas.Web;

public class NoteUpdateRequest
{
    public string?       Text     { get; set; }
    public string?       Cw       { get; set; }
    
    public List<string>? MediaIds { get; set; }
    
    public PollRequest?  Poll     { get; set; }
}
