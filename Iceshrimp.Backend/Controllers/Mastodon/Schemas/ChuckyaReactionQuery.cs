using Microsoft.AspNetCore.Mvc;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public class ChuckyaReactionQuery : MastodonPaginationQuery
{
	[FromQuery(Name = "emoji")] public string? Emoji { get; set; }
}