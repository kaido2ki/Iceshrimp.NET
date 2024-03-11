using Iceshrimp.Backend.Core.Database.Tables;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Schemas;

public class UserProfileResponse
{
	[J("id")]        public required string               Id        { get; set; }
	[J("birthday")]  public required string?              Birthday  { get; set; }
	[J("location")]  public required string?              Location  { get; set; }
	[J("fields")]    public required UserProfile.Field[]? Fields    { get; set; }
	[J("bio")]       public required string?              Bio       { get; set; }
	[J("followers")] public required int?                 Followers { get; set; }
	[J("following")] public required int?                 Following { get; set; }
}