using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Serialization;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Types;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

using SplitDomainMapping = IReadOnlyDictionary<(string usernameLower, string webDomain), string>;

/// <summary>
///     Resolves mentions into their canonical form. This is required for handling split domain mentions correctly, as it
///     cannot be guaranteed that remote instances handle split domain users correctly.
/// </summary>
public class MentionsResolver(
	IOptions<Config.InstanceSection> config
)
{
	public string ResolveMentions(
		string mfm, string? host,
		List<Note.MentionedUser> mentionCache,
		SplitDomainMapping splitDomainMapping
	)
	{
		var nodes = MfmParser.Parse(mfm);
		nodes = ResolveMentions(nodes, host, mentionCache, splitDomainMapping);
		return MfmSerializer.Serialize(nodes);
	}

	public IEnumerable<MfmNode> ResolveMentions(
		IEnumerable<MfmNode> nodes, string? host,
		List<Note.MentionedUser> mentionCache,
		SplitDomainMapping splitDomainMapping
	)
	{
		var nodesList = nodes.ToList();

		// We need to call .ToList() on this so we can modify the collection in the loop
		foreach (var node in nodesList.ToList())
		{
			if (node is not MfmMentionNode mention)
			{
				node.Children = ResolveMentions(node.Children, host, mentionCache, splitDomainMapping);
				continue;
			}

			nodesList[nodesList.IndexOf(node)] = ResolveMention(mention, host, mentionCache, splitDomainMapping);
		}

		return nodesList;
	}

	private MfmInlineNode ResolveMention(
		MfmMentionNode node, string? host,
		IEnumerable<Note.MentionedUser> mentionCache,
		SplitDomainMapping splitDomainMapping
	)
	{
		// Fall back to object host, as localpart-only mentions are relative to the instance the note originated from
		node.Host ??= host ?? config.Value.AccountDomain;

		if (node.Host == config.Value.WebDomain)
			node.Host = config.Value.AccountDomain;

		if (node.Host != config.Value.AccountDomain &&
		    splitDomainMapping.TryGetValue((node.Username.ToLowerInvariant(), node.Host), out var value))
			node.Host = value;

		var resolvedUser =
			mentionCache.FirstOrDefault(p => p.Username.EqualsIgnoreCase(node.Username) && p.Host == node.Host);

		if (resolvedUser != null)
		{
			node.Username = resolvedUser.Username;
			node.Host     = resolvedUser.Host;
			node.Acct     = $"@{resolvedUser.Username}@{resolvedUser.Host}";

			return node;
		}

		return new MfmPlainNode { Children = [new MfmTextNode { Text = node.Acct }] };
	}
}