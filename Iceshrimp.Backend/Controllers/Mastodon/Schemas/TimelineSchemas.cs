using Microsoft.AspNetCore.Mvc;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public abstract class TimelineSchemas
{
	public class PublicTimelineRequest
	{
		[FromQuery(Name = "local")]      public bool OnlyLocal  { get; set; } = false;
		[FromQuery(Name = "remote")]     public bool OnlyRemote { get; set; } = false;
		[FromQuery(Name = "only_media")] public bool OnlyMedia  { get; set; } = false;
	}

	public class HashtagTimelineRequest : PublicTimelineRequest
	{
		[FromQuery(Name = "any")]  public List<string> Any  { get; set; } = [];
		[FromQuery(Name = "all")]  public List<string> All  { get; set; } = [];
		[FromQuery(Name = "none")] public List<string> None { get; set; } = [];
	}
}