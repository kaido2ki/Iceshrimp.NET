using Iceshrimp.Shared.Configuration;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;

public class PleromaInstanceExtensions
{
	[J("vapid_public_key")] public required string           VapidPublicKey { get; set; }
	[J("metadata")]         public required InstanceMetadata Metadata       { get; set; }
}

public class InstanceMetadata
{
	[J("post_formats")] public string[] PostFormats => ["text/plain", "text/x.misskeymarkdown"];

	[J("features")]
	public string[] Features =>
	[
		"pleroma_api",
		"akkoma_api",
		"mastodon_api",
		"mastodon_api_streaming",
		"polls",
		"quote_posting",
		"editing",
		"pleroma_emoji_reactions",
		"exposable_reactions",
		"custom_emoji_reactions",
		"pleroma:bites",
		"akkoma:machine_translation"
	];

	[J("fields_limits")] public FieldsLimits FieldsLimits => new();
}

public class FieldsLimits
{
	[J("max_fields")]        public int MaxFields       => Limits.MaxProfileFields;
	[J("max_remote_fields")] public int MaxRemoteFields => Limits.MaxProfileFields;
	[J("name_length")]       public int NameLength      => Limits.MaxProfileFieldNameLength;
	[J("value_length")]      public int ValueLength     => Limits.MaxProfileFieldValueLength;
}