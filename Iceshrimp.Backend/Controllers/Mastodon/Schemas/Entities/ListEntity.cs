using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class ListEntity
{
	[J("id")]        public required string Id        { get; set; }
	[J("title")]     public required string Title     { get; set; }
	[J("exclusive")] public required bool   Exclusive { get; set; }

	[J("replies_policy")] public string RepliesPolicy => "followed"; //FIXME
}