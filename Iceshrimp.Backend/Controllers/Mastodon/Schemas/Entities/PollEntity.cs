using Iceshrimp.Backend.Core.Database;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class PollEntity : IEntity
{
	[J("expires_at")]   public required string? ExpiresAt   { get; set; }
	[J("expired")]      public required bool    Expired     { get; set; }
	[J("multiple")]     public required bool    Multiple    { get; set; }
	[J("votes_count")]  public required int     VotesCount  { get; set; }
	[J("voters_count")] public required int?    VotersCount { get; set; }
	[J("voted")]        public required bool    Voted       { get; set; }
	[J("own_votes")]    public required int[]   OwnVotes    { get; set; }

	[J("options")] public required List<PollOptionEntity> Options { get; set; }
	[J("emojis")]  public          List<EmojiEntity>      Emoji   => []; //TODO
	[J("id")]      public required string                 Id      { get; set; }
}

public class PollOptionEntity
{
	[J("title")]       public required string Title      { get; set; }
	[J("votes_count")] public required int    VotesCount { get; set; }
}