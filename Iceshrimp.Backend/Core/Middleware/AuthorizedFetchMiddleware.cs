using System.Diagnostics.CodeAnalysis;
using System.Net;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static Iceshrimp.Backend.Core.Federation.ActivityPub.UserResolver;

namespace Iceshrimp.Backend.Core.Middleware;

public class AuthorizedFetchMiddleware(
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
	IOptionsSnapshot<Config.SecuritySection> config,
	DatabaseContext db,
	ActivityPub.UserResolver userResolver,
	UserService userSvc,
	SystemUserService systemUserSvc,
	ActivityPub.FederationControlService fedCtrlSvc,
	ILogger<AuthorizedFetchMiddleware> logger,
	IHostApplicationLifetime appLifetime
) : IMiddleware
{
	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
	{
		var attribute = ctx.GetEndpoint()?.Metadata.GetMetadata<AuthorizedFetchAttribute>();

		if (attribute != null && config.Value.AuthorizedFetch)
		{
			ctx.Response.Headers.CacheControl = "private, no-store";

			var request = ctx.Request;
			var ct      = appLifetime.ApplicationStopping;

			//TODO: cache this somewhere
			var instanceActorUri = $"/users/{(await systemUserSvc.GetInstanceActorAsync()).Id}";
			if (request.Path.Value == instanceActorUri)
			{
				await next(ctx);
				return;
			}

			UserPublickey? key      = null;
			var            verified = false;

			logger.LogTrace("Processing authorized fetch request for {path}", request.Path);

			try
			{
				if (!request.Headers.TryGetValue("signature", out var sigHeader))
					throw new GracefulException(HttpStatusCode.Unauthorized, "Request is missing the signature header");

				var sig = HttpSignature.Parse(sigHeader.ToString());

				if (await fedCtrlSvc.ShouldBlockAsync(sig.KeyId))
					throw new GracefulException(HttpStatusCode.Forbidden, "Forbidden", "Instance is blocked",
					                            suppressLog: true);

				// First, we check if we already have the key
				key = await db.UserPublickeys.Include(p => p.User)
				              .FirstOrDefaultAsync(p => p.KeyId == sig.KeyId, ct);

				// If we don't, we need to try to fetch it
				if (key == null)
				{
					try
					{
						var user = await userResolver.ResolveAsync(sig.KeyId, ResolveFlags.Uri).WaitAsync(ct);
						key = await db.UserPublickeys.Include(p => p.User)
						              .FirstOrDefaultAsync(p => p.User == user, ct);

						// If the key is still null here, we have a data consistency issue and need to update the key manually
						key ??= await userSvc.UpdateUserPublicKeyAsync(user).WaitAsync(ct);
					}
					catch (Exception e)
					{
						if (e is GracefulException) throw;
						throw new Exception($"Failed to fetch key of signature user ({sig.KeyId}) - {e.Message}");
					}
				}

				// If we still don't have the key, something went wrong and we need to throw an exception
				if (key == null) throw new Exception($"Failed to fetch key of signature user ({sig.KeyId})");

				if (key.User.IsLocalUser)
					throw new Exception("Remote user must have a host");

				// We want to check both the user host & the keyId host (as account & web domain might be different)
				if (await fedCtrlSvc.ShouldBlockAsync(key.User.Host, key.KeyId))
					throw new GracefulException(HttpStatusCode.Forbidden, "Forbidden", "Instance is blocked",
					                            suppressLog: true);

				List<string> headers = ["(request-target)", "host"];

				if (sig.Created != null && !sig.Headers.Contains("date"))
					headers.Add("(created)");
				else
					headers.Add("date");

				verified = await HttpSignature.VerifyAsync(request, sig, headers, key.KeyPem);
				logger.LogDebug("HttpSignature.Verify returned {result} for key {keyId}", verified, sig.KeyId);

				if (!verified)
				{
					logger.LogDebug("Refetching user key...");
					key      = await userSvc.UpdateUserPublicKeyAsync(key);
					verified = await HttpSignature.VerifyAsync(request, sig, headers, key.KeyPem);
					logger.LogDebug("HttpSignature.Verify returned {result} for key {keyId}", verified, sig.KeyId);
				}
			}
			catch (Exception e)
			{
				if (e is AuthFetchException afe) throw GracefulException.Accepted(afe.Message);
				if (e is GracefulException { SuppressLog: true }) throw;
				logger.LogDebug("Error validating HTTP signature: {error}", e.Message);
			}

			if (!verified || key == null)
				throw new GracefulException(HttpStatusCode.Forbidden, "Request signature validation failed");

			ctx.SetActor(key.User);
		}

		await next(ctx);
	}
}

public class AuthorizedFetchAttribute : Attribute;

public static partial class HttpContextExtensions
{
	private const string ActorKey = "auth-fetch-user";

	internal static void SetActor(this HttpContext ctx, User actor)
	{
		ctx.Items.Add(ActorKey, actor);
	}

	public static User? GetActor(this HttpContext ctx)
	{
		ctx.Items.TryGetValue(ActorKey, out var actor);
		return actor as User;
	}
}