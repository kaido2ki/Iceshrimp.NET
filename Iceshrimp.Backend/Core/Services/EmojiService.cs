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
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public partial class EmojiService(
	DatabaseContext db,
	DriveService driveSvc,
	SystemUserService sysUserSvc,
	IOptions<Config.InstanceSection> config
) : IScopedService
{
	private static readonly AsyncKeyedLocker<string> KeyedLocker = new(o =>
	{
		o.PoolSize        = 100;
		o.PoolInitialFill = 5;
	});

	public async Task<Emoji> CreateEmojiFromStream(
		Stream input, string fileName, string mimeType, List<string>? aliases = null,
		string? category = null
	)
	{
		var name     = fileName.Split(".")[0];
		var existing = await db.Emojis.AnyAsync(p => p.Host == null && p.Name == name);
		if (existing)
			throw GracefulException.Conflict("An emoji with that name already exists.");

		var user = await sysUserSvc.GetInstanceActorAsync();
		var request = new DriveFileCreationRequest
		{
			Filename    = fileName,
			MimeType    = mimeType,
			IsSensitive = false
		};
		var driveFile = await driveSvc.StoreFile(input, user, request, true);

		var id = IdHelpers.GenerateSnowflakeId();
		var emoji = new Emoji
		{
			Id          = id,
			Name        = name,
			Aliases     = aliases ?? [],
			Category    = category,
			UpdatedAt   = DateTime.UtcNow,
			OriginalUrl = driveFile.Url,
			PublicUrl   = driveFile.AccessUrl,
			Width       = driveFile.Properties.Width,
			Height      = driveFile.Properties.Height,
			Sensitive   = false
		};
		emoji.Uri = emoji.GetPublicUri(config.Value);

		await db.AddAsync(emoji);
		await db.SaveChangesAsync();

		return emoji;
	}

	public async Task<Emoji> CloneEmoji(Emoji existing)
	{
		var user = await sysUserSvc.GetInstanceActorAsync();
		var driveFile = await driveSvc.StoreFile(existing.OriginalUrl, user, false, forceStore: true,
		                                         skipImageProcessing: false) ??
		                throw new Exception("Error storing emoji file");

		var emoji = new Emoji
		{
			Id          = IdHelpers.GenerateSnowflakeId(),
			Name        = existing.Name,
			UpdatedAt   = DateTime.UtcNow,
			OriginalUrl = driveFile.Url,
			PublicUrl   = driveFile.AccessUrl,
			Width       = driveFile.Properties.Width,
			Height      = driveFile.Properties.Height,
			Sensitive   = existing.Sensitive
		};
		emoji.Uri = emoji.GetPublicUri(config.Value);

		await db.AddAsync(emoji);
		await db.SaveChangesAsync();

		return emoji;
	}

	public async Task DeleteEmoji(string id)
	{
		var emoji = await db.Emojis.FirstOrDefaultAsync(p => p.Host == null && p.Id == id);
		if (emoji == null) throw GracefulException.NotFound("Emoji not found");

		var driveFile = await db.DriveFiles.FirstOrDefaultAsync(p => p.Url == emoji.OriginalUrl);
		if (driveFile != null) await driveSvc.RemoveFile(driveFile.Id);

		db.Remove(emoji);
		await db.SaveChangesAsync();
	}

	public async Task<List<Emoji>> ProcessEmojiAsync(List<ASEmoji>? emoji, string host)
	{
		emoji?.RemoveAll(p => p.Name == null);
		if (emoji is not { Count: > 0 }) return [];

		foreach (var emojo in emoji) emojo.Name = emojo.Name?.Trim(':');
		host = host.ToPunycodeLower();

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
						Id          = IdHelpers.GenerateSnowflakeId(),
						Host        = host,
						Name        = emojo.Name ?? throw new Exception("emojo.Name must not be null at this stage"),
						UpdatedAt   = DateTime.UtcNow,
						OriginalUrl = emojo.Image?.Url?.Link ?? throw new Exception("Emoji.Image has no url"),
						PublicUrl   = emojo.Image.Url.Link,
						Uri         = emojo.Id,
						Sensitive   = false
					};
					await db.AddAsync(dbEmojo);
					await db.SaveChangesAsync();
				}

				resolved.Add(dbEmojo);
			}
		}

		return resolved;
	}

	// This is technically the unicode character 'heavy black heart', but misskey doesn't send the emoji version selector, so here we are.
	private const string MisskeyHeart         = "\u2764";
	private const string EmojiVersionSelector = "\ufe0f";

	public async Task<string> ResolveEmojiName(string name, string? host)
	{
		if (name == MisskeyHeart)
			return name + EmojiVersionSelector;
		if (EmojiHelpers.IsEmoji(name))
			return name;

		host = host?.ToPunycodeLower();
		var match             = CustomEmojiRegex.Match(name);
		var remoteMatch       = RemoteCustomEmojiRegex.Match(name);
		var localMatchSuccess = !match.Success || match.Groups.Count != 2;
		if (localMatchSuccess && !remoteMatch.Success)
			throw GracefulException.BadRequest("Invalid emoji name");

		// @formatter:off
		var hit = !remoteMatch.Success
			? await db.Emojis.FirstOrDefaultAsync(p => p.Host == host && p.Name == match.Groups[1].Value)
			: await db.Emojis.FirstOrDefaultAsync(p => p.Name == remoteMatch.Groups[1].Value &&
			                                           p.Host == remoteMatch.Groups[2].Value.ToPunycodeLower());
		// @formatter:on

		if (hit == null)
			throw GracefulException.BadRequest("Unknown emoji");

		return hit.Host == null ? $":{hit.Name}:" : $":{hit.Name}@{hit.Host}:";
	}

	public async Task<Emoji?> ResolveEmoji(string fqn)
	{
		if (!fqn.StartsWith(':')) return null;
		var split = fqn.Trim(':').Split('@');
		var name  = split[0];
		var host  = split.Length > 1 ? split[1].ToPunycodeLower() : null;

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

	public async Task<Emoji?> UpdateLocalEmoji(
		string id, string? name, List<string>? aliases, string? category, string? license, bool? sensitive
	)
	{
		var emoji = await db.Emojis.FirstOrDefaultAsync(p => p.Id == id);
		if (emoji == null) return null;
		if (emoji.Host != null) return null;

		emoji.UpdatedAt = DateTime.UtcNow;

		var existing = await db.Emojis.FirstOrDefaultAsync(p => p.Host == null && p.Name == name);

		if (name != null && existing == null && CustomEmojiRegex.IsMatch(name))
		{
			emoji.Name = name;
			emoji.Uri  = emoji.GetPublicUri(config.Value);
		}

		if (aliases != null) emoji.Aliases = aliases;

		// If category is provided but empty reset to null
		if (category != null) emoji.Category = string.IsNullOrEmpty(category) ? null : category;

		if (license != null) emoji.License = license;

		if (sensitive.HasValue) emoji.Sensitive = sensitive.Value;

		await db.SaveChangesAsync();

		return emoji;
	}

	public static bool IsCustomEmoji(string s) => CustomEmojiRegex.IsMatch(s) || RemoteCustomEmojiRegex.IsMatch(s);

	[GeneratedRegex(@"^:?([\w+-]+)(?:@\.)?:?$", RegexOptions.Compiled)]
	private static partial Regex CustomEmojiRegex { get; }

	[GeneratedRegex(@"^:?([\w+-]+)@([a-zA-Z0-9._\-]+\.[a-zA-Z0-9._\-]+):?$", RegexOptions.Compiled)]
	private static partial Regex RemoteCustomEmojiRegex { get; }
}
