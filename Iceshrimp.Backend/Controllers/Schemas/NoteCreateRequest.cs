using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Schemas;

public class NoteCreateRequest
{
	[J("text")]    public required string  Text    { get; set; }
	[J("cw")]      public          string? Cw      { get; set; }
	[J("replyId")] public          string? ReplyId { get; set; }
}