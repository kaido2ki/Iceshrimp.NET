using System.Security.Cryptography;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

public class SystemUserService(ILogger<SystemUserService> logger, DatabaseContext db, CacheService cache)
{
	public async Task<User> GetInstanceActorAsync()
	{
		return await GetOrCreateSystemUserAsync("instance.actor");
	}

	public async Task<User> GetRelayActorAsync()
	{
		return await GetOrCreateSystemUserAsync("relay.actor");
	}

	public async Task<(User user, UserKeypair keypair)> GetInstanceActorWithKeypairAsync()
	{
		return await GetOrCreateSystemUserAndKeypairAsync("instance.actor");
	}

	public async Task<(User user, UserKeypair keypair)> GetRelayActorWithKeypairAsync()
	{
		return await GetOrCreateSystemUserAndKeypairAsync("relay.actor");
	}

	private async Task<(User user, UserKeypair keypair)> GetOrCreateSystemUserAndKeypairAsync(string username)
	{
		var user    = await GetOrCreateSystemUserAsync(username);
		var keypair = await db.UserKeypairs.FirstAsync(p => p.User == user); //TODO: cache this in postgres as well

		return (user, keypair);
	}

	private async Task<User> GetOrCreateSystemUserAsync(string username)
	{
		return await cache.FetchAsync($"systemUser:{username}", TimeSpan.FromHours(24), async () =>
		{
			logger.LogTrace("GetOrCreateSystemUser delegate method called for user {username}", username);
			return await db.Users.FirstOrDefaultAsync(p => p.UsernameLower == username.ToLowerInvariant() &&
			                                               p.IsLocalUser) ??
			       await CreateSystemUserAsync(username);
		});
	}

	private async Task<User> CreateSystemUserAsync(string username)
	{
		if (await db.Users.AnyAsync(p => p.UsernameLower == username.ToLowerInvariant() && p.IsLocalUser))
			throw new GracefulException($"System user {username} already exists");

		var keypair = RSA.Create(4096);
		var user = new User
		{
			Id            = IdHelpers.GenerateSlowflakeId(),
			CreatedAt     = DateTime.UtcNow,
			Username      = username,
			UsernameLower = username.ToLowerInvariant(),
			Host          = null,
			IsAdmin       = false,
			IsLocked      = true,
			IsExplorable  = false,
			IsBot         = true
		};

		var userKeypair = new UserKeypair
		{
			UserId     = user.Id,
			PrivateKey = keypair.ExportPkcs8PrivateKeyPem(),
			PublicKey  = keypair.ExportSubjectPublicKeyInfoPem()
		};

		var userProfile = new UserProfile
		{
			UserId             = user.Id,
			AutoAcceptFollowed = false,
			Password           = null
		};

		var usedUsername = new UsedUsername { CreatedAt = DateTime.UtcNow, Username = username.ToLowerInvariant() };

		await db.AddRangeAsync(user, userKeypair, userProfile, usedUsername);
		await db.SaveChangesAsync();

		return user;
	}
}