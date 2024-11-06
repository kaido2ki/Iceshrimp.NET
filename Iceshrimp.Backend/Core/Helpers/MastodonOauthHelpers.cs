namespace Iceshrimp.Backend.Core.Helpers;

public static class MastodonOauthHelpers
{
	private static readonly List<string> ReadScopes =
	[
		"read:accounts",
		"read:blocks",
		"read:bookmarks",
		"read:favourites",
		"read:filters",
		"read:follows",
		"read:lists",
		"read:mutes",
		"read:notifications",
		"read:search",
		"read:statuses"
	];

	private static readonly List<string> AdminReadScopes =
	[
		"admin:read:accounts",
		"admin:read:reports",
		"admin:read:domain_allows",
		"admin:read:domain_blocks",
		"admin:read:ip_blocks",
		"admin:read:email_domain_blocks",
		"admin:read:canonical_email_blocks"
	];

	private static readonly List<string> WriteScopes =
	[
		"write:accounts",
		"write:blocks",
		"write:bookmarks",
		"write:conversations",
		"write:favourites",
		"write:filters",
		"write:follows",
		"write:lists",
		"write:media",
		"write:mutes",
		"write:notifications",
		"write:reports",
		"write:statuses",
		"write:bites"
	];

	private static readonly List<string> AdminWriteScopes =
	[
		"admin:write:accounts",
		"admin:write:reports",
		"admin:write:domain_allows",
		"admin:write:domain_blocks",
		"admin:write:ip_blocks",
		"admin:write:email_domain_blocks",
		"admin:write:canonical_email_blocks"
	];

	private static readonly List<string> FollowScopes =
	[
		"read:follows", "read:blocks", "read:mutes", "write:follows", "write:blocks", "write:mutes"
	];

	private static readonly List<string> AdminScopeGroups = ["admin:read", "admin:write"];
	private static readonly List<string> ScopeGroups = ["read", "write", "follow", "push", "admin"];

	private static readonly List<string> ForbiddenSchemes = ["javascript", "file", "data", "mailto", "tel"];

	public static IEnumerable<string> ExpandScopes(IEnumerable<string> scopes)
	{
		var res = new List<string>();
		foreach (var scope in scopes)
			if (scope == "read")
				res.AddRange(ReadScopes);
			else if (scope == "write")
				res.AddRange(WriteScopes);
			else if (scope == "follow")
				res.AddRange(FollowScopes);
			else if (scope == "admin")
				res.AddRange(ExpandScopes(AdminScopeGroups));
			else if (scope == "admin:read")
				res.AddRange(AdminReadScopes);
			else if (scope == "admin:write")
				res.AddRange(AdminWriteScopes);
			else
				res.Add(scope);

		return res.Distinct();
	}

	public static bool ValidateScopes(List<string> scopes)
	{
		if (scopes.Distinct().Count() < scopes.Count) return false;

		var validScopes = ScopeGroups.Concat(ReadScopes).Concat(WriteScopes).Concat(FollowScopes);
		return !scopes.Except(validScopes).Any();
	}

	public static bool ValidateRedirectUri(string uri)
	{
		if (uri == "urn:ietf:wg:oauth:2.0:oob") return true;
		return Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && !ForbiddenSchemes.Contains(parsed.Scheme);
	}
}
