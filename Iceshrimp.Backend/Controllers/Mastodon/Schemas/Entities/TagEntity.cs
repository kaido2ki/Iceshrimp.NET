using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class TagEntity
{
	[J("name")]      public required string Name      { get; set; }
	[J("url")]       public required string Url       { get; set; }
	[J("following")] public required bool   Following { get; set; }
	[J("history")]   public          object History   => new();
}