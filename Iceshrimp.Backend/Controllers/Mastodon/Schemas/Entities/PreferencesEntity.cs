using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class PreferencesEntity
{
	[J("posting:default:visibility")] public required string  PostingDefaultVisibility { get; set; }
	[J("posting:default:sensitive")]  public required bool    PostingDefaultSensitive  { get; set; }
	[J("posting:default:language")]   public          string? PostingDefaultLanguage   => null;
	[J("reading:expand:media")]       public required string  ReadingExpandMedia       { get; set; }
	[J("reading:expand:spoilers")]    public required bool    ReadingExpandSpoilers    { get; set; }
}
