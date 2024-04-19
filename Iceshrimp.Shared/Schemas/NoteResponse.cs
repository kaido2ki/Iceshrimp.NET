using System.Text.Json.Serialization;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Shared.Schemas;

public class NoteResponse : NoteWithQuote, ICloneable
{
	[J("reply")]    public NoteBase?           Reply    { get; set; }
	[J("replyId")]  public string?             ReplyId  { get; set; }
	[J("renote")]   public NoteWithQuote?      Renote   { get; set; }
	[J("renoteId")] public string?             RenoteId { get; set; }
	[J("filtered")] public NoteFilteredSchema? Filtered { get; set; }

	// The properties below are only necessary for building a descendants tree
	[JI] public NoteResponse? Parent;

	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[J("descendants")]
	public List<NoteResponse>? Descendants { get; set; }

	public object Clone() => MemberwiseClone();
}

public class NoteWithQuote : NoteBase
{
	[J("quote")]   public NoteBase? Quote   { get; set; }
	[J("quoteId")] public string?   QuoteId { get; set; }
}

public class NoteBase
{
	[J("id")]          public required string                   Id          { get; set; }
	[J("createdAt")]   public required string                   CreatedAt   { get; set; }
	[J("text")]        public required string?                  Text        { get; set; }
	[J("cw")]          public required string?                  Cw          { get; set; }
	[J("visibility")]  public required string                   Visibility  { get; set; }
	[J("likes")]       public required int                      Likes       { get; set; }
	[J("renotes")]     public required int                      Renotes     { get; set; }
	[J("user")]        public required UserResponse             User        { get; set; }
	[J("attachments")] public required List<NoteAttachment>     Attachments { get; set; }
	[J("reactions")]   public required List<NoteReactionSchema> Reactions   { get; set; }
}

public class NoteAttachment
{
	[JI]                public required string  Id;
	[J("url")]          public required string  Url          { get; set; }
	[J("thumbnailUrl")] public required string  ThumbnailUrl { get; set; }
	[J("blurhash")]     public required string? Blurhash     { get; set; }
	[J("alt")]          public required string? AltText      { get; set; }
}

public class NoteReactionSchema
{
	[JI]           public required string  NoteId;
	[J("name")]    public required string  Name    { get; set; }
	[J("count")]   public required int     Count   { get; set; }
	[J("reacted")] public required bool    Reacted { get; set; }
	[J("url")]     public required string? Url     { get; set; }
}

public class NoteFilteredSchema
{
	[J("filterId")] public required long   Id      { get; set; }
	[J("keyword")]  public required string Keyword { get; set; }
	[J("drop")]     public required bool   Hide    { get; set; }
}