using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Extensions;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public class InstanceInfoV2Response(
	Config config,
	string? instanceName,
	string? instanceDescription,
	string? adminContact
)
{
	[J("version")]    public string Version       => $"4.2.1 (compatible; Iceshrimp.NET/{config.Instance.Version})";
	[J("source_url")] public string SourceUrl     => Constants.RepositoryUrl;
	[J("domain")]     public string AccountDomain => config.Instance.AccountDomain;
	[J("title")]      public string InstanceName  => instanceName ?? config.Instance.AccountDomain;

	[J("description")]
	public string Description => instanceDescription?.Truncate(140) ??
	                             "This Iceshrimp.NET instance does not appear to have a description";

	[J("contact")]       public InstanceContact         Contact       => new(adminContact);
	[J("registrations")] public InstanceRegistrations   Registrations => new(config.Security);
	[J("configuration")] public InstanceConfigurationV2 Configuration => new(config.Instance);
	[J("api_versions")]  public InstanceApiVersions     ApiVersions   => new();

	[J("usage")] public required InstanceUsage Usage { get; set; }

	[J("rules")] public required List<RuleEntity> Rules { get; set; }

	[J("icon")] public required List<InstanceIcon> Icons { get; set; }
	
	[J("thumbnail")] public required InstanceThumbnail Thumbnail { get; set; }


	//TODO: add the rest
}

public class InstanceApiVersions
{
	// this is modeled after https://codeberg.org/fediverse-pl/maep/pulls/2, however since the extensions aren't submitted
	// there (yet?) we'll use our own namespace for it
	[J("net.iceshrimp.scheduled_boosts")] public ushort ScheduledBoosts { get; set; } = 1;
	[J("chuckya")]                        public ushort Chuckya         { get; set; } = 5;
}

public class InstanceConfigurationV2(Config.InstanceSection config)
{
	[J("accounts")]          public InstanceAccountsConfiguration        Accounts        => new(config.CharacterLimit);
	[J("statuses")]          public InstanceStatusesConfiguration        Statuses        => new(config.CharacterLimit);
	[J("media_attachments")] public InstanceMediaConfiguration           Media           => new();
	[J("polls")]             public InstancePollConfiguration            Polls           => new();
	[J("reactions")]         public InstanceReactionConfiguration        Reactions       => new();
	[J("gif_search")]        public InstanceGifSearchConfiguration       GifSearch       => new();
	[J("urls")]              public InstanceUrlsV2                       Urls            => new(config);
	[J("timelines_access")]  public InstanceTimelinesAccessConfiguration TimelinesAccess => new();
}

public class InstanceUrlsV2(Config.InstanceSection config)
{
	[J("streaming")] public string StreamingApi => $"wss://{config.WebDomain}";
}

public class InstanceRegistrations(Config.SecuritySection config)
{
	[J("enabled")]           public bool    Enabled          => config.Registrations > Enums.Registrations.Closed;
	[J("approval_required")] public bool    ApprovalRequired => config.Registrations < Enums.Registrations.Open;
	[J("message")]           public string? Message          => null;
	[J("url")]               public string? Url              => null;
}

public class InstanceUsage
{
	[J("users")] public required InstanceUsersUsage Users { get; set; }
}

public class InstanceUsersUsage
{
	[J("active_month")] public required long ActiveMonth { get; set; }
}

public class InstanceContact(string? adminContact)
{
	[J("email")] public string Email => adminContact ?? "unset@example.org";
}

public class InstanceExtendedDescription(string? description)
{
	[J("updated_at")] public string UpdatedAt => DateTime.Now.ToStringIso8601Like();

	[J("content")]
	public string Content => description ?? "This Iceshrimp.NET instance does not appear to have a description";
}

public class InstanceIcon(string src, int width, int height)
{
	[J("src")]  public string Url  => src;
	[J("size")] public string Size => $"{width}x{height}";
}

public class InstanceThumbnail(string url, string? blurhash)
{
	[J("url")]      public string  Url      => url;
	[J("blurhash")] public string? Blurhash => blurhash;
}

public class InstanceTimelinesAccessConfiguration
{
	[J("live_feeds")]          public InstanceTimelineAccessConfiguration LiveFeeds         => new("authenticated");
	[J("hashtag_feeds")]       public InstanceTimelineAccessConfiguration HashtagFeeds      => new("authenticated");
	[J("trending_link_feeds")] public InstanceTimelineAccessConfiguration TrendingLinkFeeds => new("disabled");
}

public class InstanceTimelineAccessConfiguration(string access)
{
	[J("local")]  public string InstanceTimelineAccessLocal  => access;
	[J("bubble")] public string InstanceTimelineAccessBubble => access;
	[J("remote")] public string InstanceTimelineAccessRemote => access;
}