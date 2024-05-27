namespace Iceshrimp.Shared.Schemas;

public class NoteCreateRequest
{
	public required string         Text           { get; set; }
	public          string?        Cw             { get; set; }
	public          string?        Language       { get; set; }
	public          string?        ReplyId        { get; set; }
	public          string?        RenoteId       { get; set; }
	public          List<string>?  MediaIds       { get; set; }
	public required NoteVisibility Visibility     { get; set; }
	public          string?        IdempotencyKey { get; set; }
}