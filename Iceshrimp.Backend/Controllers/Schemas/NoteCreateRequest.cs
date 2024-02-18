using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Schemas;

public class NoteCreateRequest
{
	[J("text")]    public required string  Text;
	[J("cw")]      public          string? Cw;
	[J("replyId")] public          string? ReplyId;
}