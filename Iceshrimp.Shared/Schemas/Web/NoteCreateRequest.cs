namespace Iceshrimp.Shared.Schemas.Web;

public class NoteCreateRequest
{
	public required string         Text           { get; set; }
	public          string?        Cw             { get; set; }
	public          string?        ReplyId        { get; set; }
	public          string?        RenoteId       { get; set; }
	public          List<string>?  MediaIds       { get; set; }
	public required NoteVisibility Visibility     { get; set; }
	public          string?        IdempotencyKey { get; set; }
	public          PollRequest?   Poll           { get; set; }
}

public class PollRequest
{
	public required DateTime?    ExpiresAt { get; set; }
	public required bool         Multiple  { get; set; }
	public required List<string> Choices   { get; set; }
}