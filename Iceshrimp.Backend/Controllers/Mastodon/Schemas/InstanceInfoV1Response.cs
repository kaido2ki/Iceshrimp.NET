using Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Extensions;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public class InstanceInfoV1Response(
	Config config,
	string? instanceName,
	string? instanceDescription,
	string? adminContact
)
{
	[J("stats")]   public required InstanceStats Stats { get; set; }
	[J("version")] public          string Version => $"4.2.1 (compatible; Iceshrimp.NET/{config.Instance.RawVersion})";

	[J("max_toot_chars")] public int    MaxNoteChars  => config.Instance.CharacterLimit;
	[J("uri")]            public string AccountDomain => config.Instance.AccountDomain;
	[J("title")]          public string InstanceName  => instanceName ?? config.Instance.AccountDomain;
	[J("email")]          public string Email         => adminContact ?? "unset@example.org";

	[J("short_description")]
	public string ShortDescription => instanceDescription?.Truncate(140) ??
	                                  "This Iceshrimp.NET instance does not appear to have a description";

	[J("description")]
	public string Description => instanceDescription ??
	                             "This Iceshrimp.NET instance does not appear to have a description";

	[J("registrations")]     public bool RegsOpen   => config.Security.Registrations == Enums.Registrations.Open;
	[J("invites_enabled")]   public bool RegsInvite => config.Security.Registrations == Enums.Registrations.Invite;
	[J("approval_required")] public bool RegsClosed => config.Security.Registrations == Enums.Registrations.Closed;

	[J("urls")]          public InstanceUrls            Urls          => new(config.Instance);
	[J("configuration")] public InstanceConfigurationV1 Configuration => new(config.Instance);

	[J("pleroma")] public required PleromaInstanceExtensions Pleroma { get; set; }

	//TODO: add the rest
}

public class InstanceUrls(Config.InstanceSection config)
{
	[J("streaming_api")] public string StreamingApi => $"wss://{config.WebDomain}";
}

public class InstanceStats(long userCount, long noteCount, long instanceCount)
{
	[J("user_count")]   public long UserCount     => userCount;
	[J("status_count")] public long NoteCount     => noteCount;
	[J("domain_count")] public long InstanceCount => instanceCount;
}

public class InstanceConfigurationV1(Config.InstanceSection config)
{
	[J("accounts")]          public InstanceAccountsConfiguration Accounts  => new();
	[J("statuses")]          public InstanceStatusesConfiguration Statuses  => new(config.CharacterLimit);
	[J("media_attachments")] public InstanceMediaConfiguration    Media     => new();
	[J("polls")]             public InstancePollConfiguration     Polls     => new();
	[J("reactions")]         public InstanceReactionConfiguration Reactions => new();
}

public class InstanceAccountsConfiguration
{
	[J("max_featured_tags")] public int MaxFeaturedTags => 20;
}

public class InstanceStatusesConfiguration(int maxNoteChars)
{
	[J("supported_mime_types")]        public List<string> SupportedMimeTypes  => ["text/x.misskeymarkdown"];
	[J("max_characters")]              public int          MaxNoteChars        => maxNoteChars;
	[J("max_media_attachments")]       public int          MaxMediaAttachments => 16;
	[J("characters_reserved_per_url")] public int          ReservedUrlChars    => 23;
}

public class InstanceMediaConfiguration
{
	[J("supported_mime_types")] public List<string> SupportedMimeTypes => Constants.BrowserSafeMimeTypes.ToList();
	[J("image_size_limit")]     public int          ImageSizeLimit     => 10485760;
	[J("image_matrix_limit")]   public int          ImageMatrixLimit   => 16777216;
	[J("video_size_limit")]     public int          VideoSizeLimit     => 41943040;
	[J("video_frame_limit")]    public int          VideoFrameLimit    => 60;
	[J("video_matrix_limit")]   public int          VideoMatrixLimit   => 2304000;
}

public class InstancePollConfiguration
{
	[J("allow_media")]               public bool AllowMedia        => true;
	[J("max_options")]               public int  MaxOptions        => 10;
	[J("max_characters_per_option")] public int  MaxCharsPerOption => 50;
	[J("min_expiration")]            public int  MinExpiration     => 50;
	[J("max_expiration")]            public int  MaxExpiration     => 2629746;
}

public class InstanceReactionConfiguration
{
	[J("max_reactions")] public int MaxOptions => 1;
}