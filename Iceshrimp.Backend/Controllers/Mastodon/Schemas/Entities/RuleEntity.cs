using Iceshrimp.Backend.Core.Database;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class RuleEntity : IEntity
{
    [J("id")]   public required string  Id   { get; set; }
    [J("text")] public required string  Text { get; set; }
    [J("hint")] public required string? Hint { get; set; }
}
