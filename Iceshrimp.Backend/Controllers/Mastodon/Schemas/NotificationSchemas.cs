using Microsoft.AspNetCore.Mvc;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public abstract class NotificationSchemas
{
	public class GetNotificationsRequest
	{
		[FromQuery(Name = "types")]         public List<string>? Types        { get; set; }
		[FromQuery(Name = "exclude_types")] public List<string>? ExcludeTypes { get; set; }
		[FromQuery(Name = "account_id")]    public string?       AccountId    { get; set; }
	}
}