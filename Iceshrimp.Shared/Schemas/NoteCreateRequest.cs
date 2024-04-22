using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Shared.Schemas;

public class NoteCreateRequest
{
	[J("text")]       public required string         Text       { get; set; }
	[J("cw")]         public          string?        Cw         { get; set; }
	[J("replyId")]    public          string?        ReplyId    { get; set; }
	[J("renoteId")]   public          string?        RenoteId   { get; set; }
	[J("mediaIds")]   public          List<string>?  MediaIds   { get; set; }
	[J("visibility")] public required NoteVisibility Visibility { get; set; }
}