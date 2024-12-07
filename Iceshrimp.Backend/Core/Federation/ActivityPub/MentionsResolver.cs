using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.MfmSharp;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

using SplitDomainMapping = IReadOnlyDictionary<(string usernameLower, string webDomain), string>;

/// <summary>
///     Resolves mentions into their canonical form. This is required for handling split domain mentions correctly, as it
///     cannot be guaranteed that remote instances handle split domain users correctly.
/// </summary>
public class MentionsResolver(IOptions<Config.InstanceSection> config) : ISingletonService
{
	public string ResolveMentions(
		string mfm, string? host,
		List<Note.MentionedUser> mentionCache,
		SplitDomainMapping splitDomainMapping
	)
	{
		var nodes = MfmParser.Parse(mfm);
		ResolveMentions(nodes.AsSpan(), host, mentionCache, splitDomainMapping);
		return nodes.Serialize();
	}

	public void ResolveMentions(
		Span<IMfmNode> nodes, string? host,
		List<Note.MentionedUser> mentionCache,
		SplitDomainMapping splitDomainMapping
	)
	{
		for (var i = 0; i < nodes.Length; i++)
		{
			var node = nodes[i];
			if (node is not MfmMentionNode mention)
			{
				ResolveMentions(node.Children, host, mentionCache, splitDomainMapping);
				continue;
			}

			nodes[i] = ResolveMention(mention, host, mentionCache, splitDomainMapping);
		}
	}

	[OverloadResolutionPriority(1)]
	private void ResolveMentions(
		Span<IMfmInlineNode> nodes, string? host,
		List<Note.MentionedUser> mentionCache,
		SplitDomainMapping splitDomainMapping
	)
	{
		for (var i = 0; i < nodes.Length; i++)
		{
			var node = nodes[i];
			if (node is not MfmMentionNode mention)
			{
				ResolveMentions(node.Children, host, mentionCache, splitDomainMapping);
				continue;
			}

			nodes[i] = ResolveMention(mention, host, mentionCache, splitDomainMapping);
		}
	}

	private IMfmInlineNode ResolveMention(
		MfmMentionNode node, string? host,
		IEnumerable<Note.MentionedUser> mentionCache,
		SplitDomainMapping splitDomainMapping
	)
	{
		// Fall back to object host, as localpart-only mentions are relative to the instance the note originated from
		var finalHost = node.Host ?? host ?? config.Value.AccountDomain;

		if (finalHost == config.Value.WebDomain)
			finalHost = config.Value.AccountDomain;

		if (finalHost != config.Value.AccountDomain
		    && splitDomainMapping.TryGetValue((node.User.ToLowerInvariant(), finalHost), out var value))
			finalHost = value;

		var resolvedUser =
			mentionCache.FirstOrDefault(p => p.Username.EqualsIgnoreCase(node.User) && p.Host == finalHost);

		if (resolvedUser != null)
			return new MfmMentionNode(resolvedUser.Username, resolvedUser.Host);

		return new MfmPlainNode($"@{node.Acct}");
	}
}
