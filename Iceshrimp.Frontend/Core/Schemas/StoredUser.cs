using System.Text.Json.Serialization;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Schemas;

public class StoredUser : UserResponse
{
	[JsonPropertyName("token")] public required string Token       { get; set; }
	public                                      bool   IsAdmin     { get; set; }
	public                                      bool   IsModerator { get; set; }
}

public class MinimalStoredUser {
	[JsonPropertyName("token")] public required string Token       { get; set; }
	public required                             string Id          { get; set; }
	public required                             string Username    { get; set; }
}