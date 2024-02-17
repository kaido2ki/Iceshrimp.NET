using Microsoft.AspNetCore.Mvc;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public abstract class AccountSchemas
{
	public class AccountStatusesRequest
	{
		[FromQuery(Name = "only_media")]      public bool    OnlyMedia      { get; set; } = false;
		[FromQuery(Name = "exclude_replies")] public bool    ExcludeReplies { get; set; } = false;
		[FromQuery(Name = "exclude_reblogs")] public bool    ExcludeRenotes { get; set; } = false;
		[FromQuery(Name = "pinned")]          public bool    Pinned         { get; set; } = false;
		[FromQuery(Name = "tagged")]          public string? Tagged         { get; set; }
	}
}