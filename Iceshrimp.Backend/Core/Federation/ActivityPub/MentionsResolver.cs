using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Serialization;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

using SplitDomainMapping = IReadOnlyDictionary<(string usernameLower, string webDomain), string>;

/// <summary>
/// Resolves mentions into their canonical form. This is required for handling split domain mentions correctly, as it cannot be guaranteed that remote instances handle split domain users correctly.
/// </summary>
public class MentionsResolver(
	DatabaseContext db,
	IOptions<Config.InstanceSection> config,
	IDistributedCache cache
) {
	public async Task<string> ResolveMentions(
		string mfm, string? host,
		List<Note.MentionedUser> mentionCache,
		SplitDomainMapping splitDomainMapping
	) {
		var nodes = MfmParser.Parse(mfm);
		nodes = await ResolveMentions(nodes, host, mentionCache, splitDomainMapping);
		return MfmSerializer.Serialize(nodes);
	}

	public async Task<IEnumerable<MfmNode>> ResolveMentions(
		IEnumerable<MfmNode> nodes, string? host,
		List<Note.MentionedUser> mentionCache,
		SplitDomainMapping splitDomainMapping
	) {
		var nodesList = nodes.ToList();
		foreach (var mention in nodesList.SelectMany(p => p.Children.Append(p)).OfType<MfmMentionNode>())
			await ResolveMention(mention, host, mentionCache, splitDomainMapping);

		return nodesList;
	}

	private async Task ResolveMention(
		MfmMentionNode node, string? host,
		IEnumerable<Note.MentionedUser> mentionCache,
		SplitDomainMapping splitDomainMapping
	) {
		var finalHost = node.Host ?? host;

		if (finalHost == config.Value.AccountDomain || finalHost == config.Value.WebDomain)
			finalHost = null;
		if (finalHost != null &&
		    splitDomainMapping.TryGetValue((node.Username.ToLowerInvariant(), finalHost), out var value))
			finalHost = value;

		var resolvedUser =
			mentionCache.FirstOrDefault(p => string.Equals(p.Username, node.Username,
			                                               StringComparison.InvariantCultureIgnoreCase) &&
			                                 p.Host == finalHost);

		if (resolvedUser != null) {
			node.Username = resolvedUser.Username;
			node.Host     = resolvedUser.Host;
			node.Acct     = $"@{resolvedUser.Username}@{resolvedUser.Host}";
		}
		else {
			async Task<string> FetchLocalUserCapitalization() {
				var username = await db.Users.Where(p => p.UsernameLower == node.Username.ToLowerInvariant())
				                       .Select(p => p.Username)
				                       .FirstOrDefaultAsync();
				return username ?? node.Username;
			}

			node.Username = await cache.FetchAsync($"localUserNameCapitalization:{node.Username.ToLowerInvariant()}",
			                                       TimeSpan.FromHours(24), FetchLocalUserCapitalization);

			node.Host = config.Value.AccountDomain;
			node.Acct = $"@{node.Username}@{config.Value.AccountDomain}";
		}
	}
}