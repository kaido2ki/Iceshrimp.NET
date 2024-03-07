using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Backend.Controllers.Schemas;

public class NoteResponse : NoteBase
{
	[J("reply")]  public NoteBase?   Reply  { get; set; }
	[J("renote")] public NoteRenote? Renote { get; set; }
	[J("quote")]  public NoteBase?   Quote  { get; set; }
}

public class NoteRenote : NoteBase
{
	[J("quote")] public NoteBase? Quote { get; set; }
}

public class NoteBase
{
	[J("id")]          public required string               Id          { get; set; }
	[J("text")]        public required string?              Text        { get; set; }
	[J("cw")]          public required string?              Cw          { get; set; }
	[J("visibility")]  public required string               Visibility  { get; set; }
	[J("user")]        public required UserResponse         User        { get; set; }
	[J("attachments")] public required List<NoteAttachment> Attachments { get; set; }
}

public class NoteAttachment
{
	[JI]                public required string  Id;
	[J("url")]          public required string  Url          { get; set; }
	[J("thumbnailUrl")] public required string  ThumbnailUrl { get; set; }
	[J("blurhash")]     public required string? Blurhash     { get; set; }
	[J("alt")]          public required string? AltText      { get; set; }
}