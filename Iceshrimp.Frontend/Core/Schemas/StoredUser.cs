using System.Text.Json.Serialization;
using Iceshrimp.Shared.Schemas;

namespace Iceshrimp.Frontend.Core.Schemas;

public class StoredUser : UserResponse
{
	[JsonPropertyName("token")] public required string Token   { get; set; }
	public                                      bool   IsAdmin { get; set; }
}