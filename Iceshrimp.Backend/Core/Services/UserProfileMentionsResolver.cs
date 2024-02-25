using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Types;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class UserProfileMentionsResolver(ActivityPub.UserResolver userResolver, IOptions<Config.InstanceSection> config, ILogger<UserProfileMentionsResolver> logger)
{
	private int _recursionLimit = 10;

	public async Task<List<Note.MentionedUser>> ResolveMentions(UserProfile.Field[]? fields, string? bio, string? host)
	{
		if (fields is not { Length: > 0 } && bio == null) return [];
		var input = (fields ?? [])
		            .SelectMany<UserProfile.Field, string>(p => [p.Name, p.Value])
		            .Prepend(bio)
		            .Where(p => p != null)
		            .Cast<string>()
		            .ToList();

		var nodes        = input.SelectMany(p => MfmParser.Parse(p));
		var mentionNodes = EnumerateMentions(nodes);
		var users = await mentionNodes.DistinctBy(p => p.Acct)
		                              .Select(async p =>
		                              {
			                              try
			                              {
				                              return await userResolver.ResolveAsyncLimited(p.Username, p.Host ?? host,
						                               () =>
						                               {
							                               logger.LogDebug("Recursion limiter is at: {limit}", _recursionLimit);
							                               return _recursionLimit-- <= 0;
						                               });
			                              }
			                              catch
			                              {
				                              return null;
			                              }
		                              })
		                              .AwaitAllNoConcurrencyAsync();

		return users.Where(p => p != null)
		            .Cast<User>()
		            .DistinctBy(p => p.Id)
		            .Select(p => new Note.MentionedUser
		            {
			            Host     = p.Host,
			            Uri      = p.Uri ?? p.GetPublicUri(config.Value),
			            Url      = p.UserProfile?.Url,
			            Username = p.Username
		            })
		            .ToList();
	}

	[SuppressMessage("ReSharper", "ReturnTypeCanBeEnumerable.Local",
	                 Justification = "Roslyn inspection says this hurts performance")]
	private static List<MfmMentionNode> EnumerateMentions(IEnumerable<MfmNode> nodes)
	{
		var list = new List<MfmMentionNode>();

		foreach (var node in nodes)
		{
			if (node is MfmMentionNode mention)
				list.Add(mention);
			else
				list.AddRange(EnumerateMentions(node.Children));
		}

		return list;
	}
}