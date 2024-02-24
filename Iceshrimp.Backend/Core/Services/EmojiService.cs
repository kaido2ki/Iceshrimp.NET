using AsyncKeyedLock;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

public class EmojiService(DatabaseContext db)
{
	private static readonly AsyncKeyedLocker<string> KeyedLocker = new(o =>
	{
		o.PoolSize        = 100;
		o.PoolInitialFill = 5;
	});

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
}