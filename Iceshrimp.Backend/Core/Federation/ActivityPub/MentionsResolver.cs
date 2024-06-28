using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.FSharp.Collections;
using static Iceshrimp.Parsing.MfmNodeTypes;

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
				node.Children =
					ListModule.OfSeq(ResolveMentions(node.Children, host, mentionCache, splitDomainMapping));
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
		var finalHost = node.Host?.Value ?? host ?? config.Value.AccountDomain;

		if (finalHost == config.Value.WebDomain)
			finalHost = config.Value.AccountDomain;

		if (finalHost != config.Value.AccountDomain &&
		    splitDomainMapping.TryGetValue((node.Username.ToLowerInvariant(), finalHost), out var value))
			finalHost = value;

		var resolvedUser =
			mentionCache.FirstOrDefault(p => p.Username.EqualsIgnoreCase(node.Username) && p.Host == finalHost);

		if (resolvedUser != null)
		{
			return new MfmMentionNode($"@{resolvedUser.Username}@{resolvedUser.Host}",
			                          resolvedUser.Username, resolvedUser.Host);
		}

		return new MfmPlainNode($"@{node.Acct}");
	}
}