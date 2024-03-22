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

	[J("usage")]   public required InstanceUsage   Usage   { get; set; }

	//TODO: add the rest
}

public class InstanceConfigurationV2(Config.InstanceSection config)
{
	[J("accounts")]          public InstanceAccountsConfiguration Accounts  => new();
	[J("statuses")]          public InstanceStatusesConfiguration Statuses  => new(config.CharacterLimit);
	[J("media_attachments")] public InstanceMediaConfiguration    Media     => new();
	[J("polls")]             public InstancePollConfiguration     Polls     => new();
	[J("reactions")]         public InstanceReactionConfiguration Reactions => new();
	[J("urls")]              public InstanceUrls                  Urls      => new(config);
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