using System.Diagnostics.CodeAnalysis;
using System.Net;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Utils.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
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
	IHostApplicationLifetime appLifetime,
	MfmConverter mfmConverter
) : ConditionalMiddleware<AuthorizedFetchAttribute>, IMiddlewareService
{
	public static ServiceLifetime Lifetime => ServiceLifetime.Scoped;

	private static string? _instanceActorUri;
	private static string? _relayActorUri;

	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
	{
		// Ensure we're rendering HTML markup (AsyncLocal)
		mfmConverter.SupportsHtmlFormatting.Value = true;
		mfmConverter.SupportsInlineMedia.Value    = true;

		// Short-circuit instance & relay actor fetches
		_instanceActorUri ??= $"/users/{(await systemUserSvc.GetInstanceActorAsync()).Id}";
		_relayActorUri    ??= $"/users/{(await systemUserSvc.GetRelayActorAsync()).Id}";
		if (ctx.Request.Path.Value == _instanceActorUri || ctx.Request.Path.Value == _relayActorUri)
		{
			await next(ctx);
			return;
		}

		// Prevent authenticated responses from ending up in caches, and prevent unauthenticated responses
		// from being returned for authenticated requests
		if (ctx.Request.Headers.ContainsKey("Signature"))
			ctx.Response.Headers.CacheControl = "private, no-store";
		else
			ctx.Response.Headers.Append(HeaderNames.Vary, "Signature");

		var res = await ValidateSignatureAsync(ctx);

		// We want to reject blocked instances even if authorized fetch is disabled
		if (res.TryGetError(out var error) && error is InstanceBlockedException)
			throw error;

		if (res.TryGetResult(out var user) && user.Value != null)
		{
			ctx.SetActor(user.Value);
		}
		else if (config.Value.AuthorizedFetch)
		{
			if (error != null) throw error;
			throw new GracefulException(HttpStatusCode.Forbidden, "Request signature validation failed");
		}

		await next(ctx);
	}

	private async Task<Result<Optional<User>>> ValidateSignatureAsync(HttpContext ctx)
	{
		var request = ctx.Request;
		var ct      = appLifetime.ApplicationStopping;

		UserPublickey? key      = null;
		var            verified = false;

		logger.LogTrace("Processing authorized fetch request for {path}", request.Path);

		try
		{
			if (!request.Headers.TryGetValue("signature", out var sigHeader))
				return new GracefulException(HttpStatusCode.Unauthorized, "Request is missing the signature header");

			var sig = HttpSignature.Parse(sigHeader.ToString());

			if (await fedCtrlSvc.ShouldBlockAsync(sig.KeyId))
				return new InstanceBlockedException();

			// First, we check if we already have the key
			key = await db.UserPublickeys.Include(p => p.User).FirstOrDefaultAsync(p => p.KeyId == sig.KeyId, ct);

			// If we don't, we need to try to fetch it
			if (key == null)
			{
				try
				{
					var user = await userResolver.ResolveAsync(sig.KeyId, ResolveFlags.Uri).WaitAsync(ct);
					key = await db.UserPublickeys.Include(p => p.User).FirstOrDefaultAsync(p => p.User == user, ct);

					// If the key is still null here, we have a data consistency issue and need to update the key manually
					key ??= await userSvc.UpdateUserPublicKeyAsync(user).WaitAsync(ct);
				}
				catch (Exception e)
				{
					if (e is GracefulException) return e;
					return new Exception($"Failed to fetch key of signature user ({sig.KeyId}) - {e.Message}");
				}
			}

			if (key.User.IsSuspended)
				return GracefulException.Forbidden("User is suspended");
			if (key.User.IsLocalUser)
				return new Exception("Remote user must have a host");

			// We want to check both the user host & the keyId host (as account & web domain might be different)
			if (await fedCtrlSvc.ShouldBlockAsync(key.User.Host, key.KeyId))
				return new InstanceBlockedException();

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
			if (e is AuthFetchException afe) return GracefulException.Accepted(afe.Message);
			if (e is GracefulException { SuppressLog: true }) return e;
			logger.LogDebug("Error validating HTTP signature: {error}", e.Message);
		}

		if (!verified || key == null)
			return new Optional<User>(null);

		return new Optional<User>(key.User);
	}

	private class InstanceBlockedException() : GracefulException(HttpStatusCode.Forbidden, "Forbidden",
	                                                             "Instance is blocked", suppressLog: true);
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
