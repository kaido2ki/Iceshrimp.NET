using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Streaming;

public class StreamingRequestMessage
{
	[J("type")]   public required string  Type   { get; set; }
	[J("stream")] public required string  Stream { get; set; }
	[J("list")]   public          string? List   { get; set; }
	[J("tag")]    public          string? Tag    { get; set; }
}

public class StreamingUpdateMessage
{
	[J("stream")]  public required List<string> Stream  { get; set; }
	[J("event")]   public required string       Event   { get; set; }
	[J("payload")] public required string       Payload { get; set; }
}