using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Schemas;

public class NoteResponse : NoteBase {
	[J("reply")]  public NoteBase?   Reply  { get; set; }
	[J("renote")] public NoteRenote? Renote { get; set; }
	[J("quote")]  public NoteBase?   Quote  { get; set; }
}

public class NoteRenote : NoteBase {
	[J("quote")] public NoteBase? Quote { get; set; }
}

public class NoteBase {
	[J("id")]   public required string       Id   { get; set; }
	[J("text")] public required string?      Text { get; set; }
	[J("user")] public required UserResponse User { get; set; }
}