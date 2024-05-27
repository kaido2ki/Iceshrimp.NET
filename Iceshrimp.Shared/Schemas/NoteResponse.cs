using System.Text.Json.Serialization;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Shared.Schemas;

public class NoteResponse : NoteWithQuote, ICloneable
{
	public NoteBase?           Reply             { get; set; }
	public string?             ReplyId           { get; set; }
	public bool                ReplyInaccessible { get; set; }
	public NoteWithQuote?      Renote            { get; set; }
	public string?             RenoteId          { get; set; }
	public NoteFilteredSchema? Filtered          { get; set; }

	// The properties below are only necessary for building a descendants tree
	[JI] public NoteResponse? Parent;

	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<NoteResponse>? Descendants { get; set; }

	public object Clone() => MemberwiseClone();
}

public class NoteWithQuote : NoteBase
{
	public NoteBase? Quote             { get; set; }
	public string?   QuoteId           { get; set; }
	public bool?     QuoteInaccessible { get; set; }
}

public class NoteBase
{
	public required string                   Id          { get; set; }
	public required string                   CreatedAt   { get; set; }
	public required string                   Uri         { get; set; }
	public required string                   Url         { get; set; }
	public required string?                  Text        { get; set; }
	public required string?                  Cw          { get; set; }
	public required string?                  Language    { get; set; }
	public required NoteVisibility           Visibility  { get; set; }
	public required bool                     Liked       { get; set; }
	public required int                      Likes       { get; set; }
	public required int                      Renotes     { get; set; }
	public required int                      Replies     { get; set; }
	public required UserResponse             User        { get; set; }
	public required List<NoteAttachment>     Attachments { get; set; }
	public required List<NoteReactionSchema> Reactions   { get; set; }
}

public class NoteAttachment
{
	[JI] public required string  Id;
	public required      string  Url          { get; set; }
	public required      string  ThumbnailUrl { get; set; }
	public required      string  ContentType  { get; set; }
	public required      bool    IsSensitive  { get; set; }
	public required      string? Blurhash     { get; set; }
	public required      string? AltText      { get; set; }
}

public class NoteReactionSchema
{
	[JI] public required string  NoteId;
	public required      string  Name    { get; set; }
	public required      int     Count   { get; set; }
	public required      bool    Reacted { get; set; }
	public required      string? Url     { get; set; }
}

public class NoteFilteredSchema
{
	public required long   Id      { get; set; }
	public required string Keyword { get; set; }
	public required bool   Hide    { get; set; }
}

public enum NoteVisibility
{
	Public    = 0,
	Home      = 1,
	Followers = 2,
	Specified = 3
}