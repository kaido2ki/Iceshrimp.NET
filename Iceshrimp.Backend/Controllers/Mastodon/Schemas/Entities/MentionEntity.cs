using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Database.Tables;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class MentionEntity()
{
	[JI] public required string? Host;

	// internal properties that won't be serialized
	[JI] public required string Uri;

	[SetsRequiredMembers]
	public MentionEntity(User u, string webDomain) : this()
	{
		Id       = u.Id;
		Username = u.Username;
		Host     = u.Host;
		Acct     = u.Acct;
		Uri      = u.Uri ?? u.GetPublicUri(webDomain);
		Url = u.UserProfile != null
			? u.UserProfile.Url ?? u.Uri ?? u.GetPublicUrl(webDomain)
			: u.Uri ?? u.GetPublicUri(webDomain);
	}

	[J("id")]       public required string Id       { get; set; }
	[J("username")] public required string Username { get; set; }
	[J("acct")]     public required string Acct     { get; set; }
	[J("url")]      public required string Url      { get; set; }
}