using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;

public class PleromaInstanceExtensions 
{
	[J("vapid_public_key")] public required string           VapidPublicKey { get; set; }
	[J("metadata")]         public required InstanceMetadata Metadata       { get; set; }
}

public class InstanceMetadata
{
	[J("post_formats")]  public string[] PostFormats => ["text/plain", "text/x.misskeymarkdown"];
	[J("fields_limits")] public FieldsLimits FieldsLimits => new();
}

// there doesn't seem to be any limits there, from briefly checking the code
public class FieldsLimits
{
	[J("max_fields")]        public int MaxFields       => int.MaxValue;
	[J("max_remote_fields")] public int MaxRemoteFields => int.MaxValue;
	[J("name_length")]       public int NameLength      => int.MaxValue;
	[J("value_length")]      public int ValueLength     => int.MaxValue;
}