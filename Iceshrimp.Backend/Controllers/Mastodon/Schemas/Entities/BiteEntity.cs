using Iceshrimp.Shared.Helpers;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class BiteEntity
{
    [J("id")]        public required string Id       { get; set; }
    [J("bite_back")] public required bool   BiteBack { get; set; }
}
