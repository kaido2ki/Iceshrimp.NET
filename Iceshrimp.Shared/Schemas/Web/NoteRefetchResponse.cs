namespace Iceshrimp.Shared.Schemas.Web;

public class NoteRefetchResponse
{
	public required NoteResponse Note   { get; set; }
	public required List<string> Errors { get; set; }
}