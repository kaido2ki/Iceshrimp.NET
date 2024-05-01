using System.Text.RegularExpressions;
using AsyncKeyedLock;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Parsing;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

public class EmojiService(DatabaseContext db)
{
	private static readonly AsyncKeyedLocker<string> KeyedLocker = new(o =>
	{
		o.PoolSize        = 100;
		o.PoolInitialFill = 5;
	});

	private static readonly Regex CustomEmojiRegex = new(@"^:?([\w+-]+)(?:@\.)?:?$");

	private static readonly Regex RemoteCustomEmojiRegex =
		new(@"^:?([\w+-]+)@([a-zA-Z0-9._\-]+\.[a-zA-Z0-9._\-]+):?$");

	public async Task<List<Emoji>> ProcessEmojiAsync(List<ASEmoji>? emoji, string host)
	{
		emoji?.RemoveAll(p => p.Name == null);
		if (emoji is not { Count: > 0 }) return [];

		foreach (var emojo in emoji) emojo.Name = emojo.Name?.Trim(':');

		var resolved = await db.Emojis.Where(p => p.Host == host && emoji.Select(e => e.Name).Contains(p.Name))
		                       .ToListAsync();

		//TODO: handle updated emoji
		foreach (var emojo in emoji.Where(emojo => resolved.All(p => p.Name != emojo.Name)))
		{
			using (await KeyedLocker.LockAsync($"emoji:{host}:{emojo.Name}"))
			{
				var dbEmojo = await db.Emojis.FirstOrDefaultAsync(p => p.Host == host && p.Name == emojo.Name);
				if (dbEmojo == null)
				{
					dbEmojo = new Emoji
					{
						Id          = IdHelpers.GenerateSlowflakeId(),
						Host        = host,
						Name        = emojo.Name ?? throw new Exception("emojo.Name must not be null at this stage"),
						UpdatedAt   = DateTime.UtcNow,
						OriginalUrl = emojo.Image?.Url?.Link ?? throw new Exception("Emoji.Image has no url"),
						PublicUrl   = emojo.Image.Url.Link,
						Uri         = emojo.Id
					};
					await db.AddAsync(dbEmojo);
					await db.SaveChangesAsync();
				}

				resolved.Add(dbEmojo);
			}
		}

		return resolved;
	}

	public async Task<string> ResolveEmojiName(string name, string? host)
	{
		host = host?.ToPunycode();
		var match             = CustomEmojiRegex.Match(name);
		var remoteMatch       = RemoteCustomEmojiRegex.Match(name);
		var localMatchSuccess = !match.Success || match.Groups.Count != 2;
		if (localMatchSuccess && !remoteMatch.Success)
			throw GracefulException.BadRequest("Invalid emoji name");

		var hit = !remoteMatch.Success
			? await db.Emojis.FirstOrDefaultAsync(p => p.Host == host && p.Name == match.Groups[1].Value)
			: await db.Emojis.FirstOrDefaultAsync(p => p.Name == remoteMatch.Groups[1].Value &&
			                                           p.Host == remoteMatch.Groups[2].Value);

		if (hit == null)
			throw GracefulException.BadRequest("Unknown emoji");

		return hit.Host == null ? $":{hit.Name}:" : $":{hit.Name}@{hit.Host}:";
	}

	public async Task<Emoji?> ResolveEmoji(string fqn)
	{
		if (!fqn.StartsWith(':')) return null;
		var split = fqn.Trim(':').Split('@');
		var name  = split[0];
		var host  = split.Length > 1 ? split[1] : null;

		return await db.Emojis.FirstOrDefaultAsync(p => p.Host == host && p.Name == name);
	}

	public async Task<List<Emoji>> ResolveEmoji(IEnumerable<MfmNodeTypes.MfmNode> nodes)
	{
		var list = new List<MfmNodeTypes.MfmEmojiCodeNode>();
		ResolveChildren(nodes, ref list);
		return await db.Emojis.Where(p => p.Host == null && list.Select(i => i.Name).Contains(p.Name)).ToListAsync();
	}

	private static void ResolveChildren(
		IEnumerable<MfmNodeTypes.MfmNode> nodes, ref List<MfmNodeTypes.MfmEmojiCodeNode> list
	)
	{
		foreach (var node in nodes)
		{
			if (node is MfmNodeTypes.MfmEmojiCodeNode emojiNode) list.Add(emojiNode);
			list.AddRange(node.Children.OfType<MfmNodeTypes.MfmEmojiCodeNode>());
			ResolveChildren(node.Children, ref list);
		}
	}
}