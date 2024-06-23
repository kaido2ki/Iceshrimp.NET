using System.Text.RegularExpressions;
using AsyncKeyedLock;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Parsing;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

public partial class EmojiService(DatabaseContext db, DriveService driveSvc, SystemUserService sysUserSvc)
{
	private static readonly AsyncKeyedLocker<string> KeyedLocker = new(o =>
	{
		o.PoolSize        = 100;
		o.PoolInitialFill = 5;
	});

	private static readonly Regex CustomEmojiRegex = new(@"^:?([\w+-]+)(?:@\.)?:?$", RegexOptions.Compiled);

	private static readonly Regex RemoteCustomEmojiRegex =
		new(@"^:?([\w+-]+)@([a-zA-Z0-9._\-]+\.[a-zA-Z0-9._\-]+):?$", RegexOptions.Compiled);

	public async Task<Emoji> CreateEmojiFromStream(Stream input, string fileName, string mimeType, Config.InstanceSection config)
	{
		var user = await sysUserSvc.GetInstanceActorAsync();
		var request = new DriveFileCreationRequest
		{
			Filename    = fileName,
			MimeType    = mimeType,
			IsSensitive = false
		};
		var driveFile = await driveSvc.StoreFile(input, user, request);

		var name = fileName.Split(".")[0];
		
		var existing = await db.Emojis.FirstOrDefaultAsync(p => p.Host == null && p.Name == name);

		var id = IdHelpers.GenerateSlowflakeId();
		var emoji = new Emoji
		{
			Id          = id,
			Name        = existing == null && CustomEmojiRegex.IsMatch(name) ? name : id,
			UpdatedAt   = DateTime.UtcNow,
			OriginalUrl = driveFile.Url,
			PublicUrl   = driveFile.PublicUrl,
			Width       = driveFile.Properties.Width,
			Height      = driveFile.Properties.Height
		};
		emoji.Uri = emoji.GetPublicUri(config);

		await db.AddAsync(emoji);
		await db.SaveChangesAsync();

		return emoji;
	}

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
		if (EmojiRegex().IsMatch(name))
			return name;

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

	public async Task<Emoji?> UpdateLocalEmoji(string id, string? name, List<string>? aliases, string? category, string? license, Config.InstanceSection config)
	{
		var emoji = await db.Emojis.FirstOrDefaultAsync(p => p.Id == id);
		if (emoji == null) return null;
		if (emoji.Host != null) return null;

		emoji.UpdatedAt = DateTime.UtcNow;

		if (name != null && name.Length <= 128 && CustomEmojiRegex.IsMatch(name))
		{
			emoji.Name = name[..128];
			emoji.Uri  = emoji.GetPublicUri(config);
		}

		if (aliases != null) emoji.Aliases = aliases.Select(a => a[..128]).ToList();
		
		// If category is provided but empty reset to null
		if (category != null) emoji.Category = string.IsNullOrEmpty(category) ? null : category[..128];

		if (license != null) emoji.License = license[..1024];

		await db.SaveChangesAsync();

		return emoji;
	}

	// Generated for Unicode 15.1 by https://iceshrimp.dev/iceshrimp/UnicodeEmojiRegex
	[GeneratedRegex(@"\uD83C[\uDDE6-\uDDFF]\uD83C[\uDDE6-\uDDFF]|\uD83C[\uDC04\uDCCF\uDD70\uDD71\uDD7E\uDD7F\uDD8E\uDD91-\uDD9A\uDDE6-\uDDFF\uDE01\uDE02\uDE1A\uDE2F\uDE32-\uDE3A\uDE50\uDE51\uDF00-\uDF21\uDF24-\uDF93\uDF96\uDF97\uDF99-\uDF9B\uDF9E-\uDFF0\uDFF3-\uDFF5\uDFF7-\uDFFF]|\uD83D[\uDC00-\uDCFD\uDCFF-\uDD3D\uDD49-\uDD4E\uDD50-\uDD67\uDD6F\uDD70\uDD73-\uDD7A\uDD87\uDD8A-\uDD8D\uDD90\uDD95\uDD96\uDDA4\uDDA5\uDDA8\uDDB1\uDDB2\uDDBC\uDDC2-\uDDC4\uDDD1-\uDDD3\uDDDC-\uDDDE\uDDE1\uDDE3\uDDE8\uDDEF\uDDF3\uDDFA-\uDE4F\uDE80-\uDEC5\uDECB-\uDED2\uDED5-\uDED7\uDEDC-\uDEE5\uDEE9\uDEEB\uDEEC\uDEF0\uDEF3-\uDEFC\uDFE0-\uDFEB\uDFF0]|\uD83E[\uDD0C-\uDD3A\uDD3C-\uDD45\uDD47-\uDDFF\uDE70-\uDE7C\uDE80-\uDE88\uDE90-\uDEBD\uDEBF-\uDEC5\uDECE-\uDEDB\uDEE0-\uDEE8\uDEF0-\uDEF8]|[\#\*0-9\u00A9\u00AE\u203C\u2049\u2122\u2139\u2194-\u2199\u21A9\u21AA\u231A\u231B\u2328\u23CF\u23E9-\u23F3\u23F8-\u23FA\u24C2\u25AA\u25AB\u25B6\u25C0\u25FB-\u25FE\u2600-\u2604\u260E\u2611\u2614\u2615\u2618\u261D\u2620\u2622\u2623\u2626\u262A\u262E\u262F\u2638-\u263A\u2640\u2642\u2648-\u2653\u265F\u2660\u2663\u2665\u2666\u2668\u267B\u267E\u267F\u2692-\u2697\u2699\u269B\u269C\u26A0\u26A1\u26A7\u26AA\u26AB\u26B0\u26B1\u26BD\u26BE\u26C4\u26C5\u26C8\u26CE\u26CF\u26D1\u26D3\u26D4\u26E9\u26EA\u26F0-\u26F5\u26F7-\u26FA\u26FD\u2702\u2705\u2708-\u270D\u270F\u2712\u2714\u2716\u271D\u2721\u2728\u2733\u2734\u2744\u2747\u274C\u274E\u2753-\u2755\u2757\u2763\u2764\u2795-\u2797\u27A1\u27B0\u27BF\u2934\u2935\u2B05-\u2B07\u2B1B\u2B1C\u2B50\u2B55\u3030\u303D\u3297\u3299](?:\uD83C[\uDFFB-\uDFFF]|\uFE0F\u20E3?|(?:\uDB40[\uDC20-\uDC7E])+\uDB40\uDC7F)?(?:\u200D\uD83C[\uDC04\uDCCF\uDD70\uDD71\uDD7E\uDD7F\uDD8E\uDD91-\uDD9A\uDDE6-\uDDFF\uDE01\uDE02\uDE1A\uDE2F\uDE32-\uDE3A\uDE50\uDE51\uDF00-\uDF21\uDF24-\uDF93\uDF96\uDF97\uDF99-\uDF9B\uDF9E-\uDFF0\uDFF3-\uDFF5\uDFF7-\uDFFF]|\uD83D[\uDC00-\uDCFD\uDCFF-\uDD3D\uDD49-\uDD4E\uDD50-\uDD67\uDD6F\uDD70\uDD73-\uDD7A\uDD87\uDD8A-\uDD8D\uDD90\uDD95\uDD96\uDDA4\uDDA5\uDDA8\uDDB1\uDDB2\uDDBC\uDDC2-\uDDC4\uDDD1-\uDDD3\uDDDC-\uDDDE\uDDE1\uDDE3\uDDE8\uDDEF\uDDF3\uDDFA-\uDE4F\uDE80-\uDEC5\uDECB-\uDED2\uDED5-\uDED7\uDEDC-\uDEE5\uDEE9\uDEEB\uDEEC\uDEF0\uDEF3-\uDEFC\uDFE0-\uDFEB\uDFF0]|\uD83E[\uDD0C-\uDD3A\uDD3C-\uDD45\uDD47-\uDDFF\uDE70-\uDE7C\uDE80-\uDE88\uDE90-\uDEBD\uDEBF-\uDEC5\uDECE-\uDEDB\uDEE0-\uDEE8\uDEF0-\uDEF8]|[\#\*0-9\u00A9\u00AE\u203C\u2049\u2122\u2139\u2194-\u2199\u21A9\u21AA\u231A\u231B\u2328\u23CF\u23E9-\u23F3\u23F8-\u23FA\u24C2\u25AA\u25AB\u25B6\u25C0\u25FB-\u25FE\u2600-\u2604\u260E\u2611\u2614\u2615\u2618\u261D\u2620\u2622\u2623\u2626\u262A\u262E\u262F\u2638-\u263A\u2640\u2642\u2648-\u2653\u265F\u2660\u2663\u2665\u2666\u2668\u267B\u267E\u267F\u2692-\u2697\u2699\u269B\u269C\u26A0\u26A1\u26A7\u26AA\u26AB\u26B0\u26B1\u26BD\u26BE\u26C4\u26C5\u26C8\u26CE\u26CF\u26D1\u26D3\u26D4\u26E9\u26EA\u26F0-\u26F5\u26F7-\u26FA\u26FD\u2702\u2705\u2708-\u270D\u270F\u2712\u2714\u2716\u271D\u2721\u2728\u2733\u2734\u2744\u2747\u274C\u274E\u2753-\u2755\u2757\u2763\u2764\u2795-\u2797\u27A1\u27B0\u27BF\u2934\u2935\u2B05-\u2B07\u2B1B\u2B1C\u2B50\u2B55\u3030\u303D\u3297\u3299](?:\uD83C[\uDFFB-\uDFFF]|\uFE0F\u20E3?|(?:\uDB40[\uDC20-\uDC7E])+\uDB40\uDC7F)?)*")]
	private static partial Regex EmojiRegex();
}