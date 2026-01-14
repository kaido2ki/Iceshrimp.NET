namespace Iceshrimp.Shared.Schemas.Web;

public class NoteUpdateRequest
{
    public required string Id { get; set; }

    public string? Text { get; set; }
    public string? Cw   { get; set; }
}
