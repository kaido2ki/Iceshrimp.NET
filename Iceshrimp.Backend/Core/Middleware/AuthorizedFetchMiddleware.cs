using System.Net;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Federation.ActivityPub;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Middleware;

public class AuthorizedFetchMiddleware(
	IOptionsSnapshot<Config.SecuritySection> config,
	DatabaseContext db,
	UserResolver userResolver,
	ILogger<AuthorizedFetchMiddleware> logger) : IMiddleware {
	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next) {
		var endpoint  = ctx.Features.Get<IEndpointFeature>()?.Endpoint;
		var attribute = endpoint?.Metadata.GetMetadata<AuthorizedFetchAttribute>();

		if (attribute != null && config.Value.AuthorizedFetch) {
			var request = ctx.Request;
			if (!request.Headers.TryGetValue("signature", out var sigHeader))
				throw new GracefulException(HttpStatusCode.Unauthorized, "Request is missing the signature header");

			var sig = HttpSignature.Parse(sigHeader.ToString());

			// First, we check if we already have the key
			var key = await db.UserPublickeys.FirstOrDefaultAsync(p => p.KeyId == sig.KeyId);

			// If we don't, we need to try to fetch it
			if (key == null) {
				var user = await userResolver.Resolve(sig.KeyId);
				key = await db.UserPublickeys.FirstOrDefaultAsync(p => p.UserId == user.Id);
			}

			// If we still don't have the key, something went wrong and we need to throw an exception
			if (key == null) throw new GracefulException("Failed to fetch key of signature user");

			List<string> headers = request.ContentLength > 0 || attribute.ForceBody
				? ["(request-target)", "digest", "host", "date"]
				: ["(request-target)", "host", "date"];

			var verified = await HttpSignature.Verify(ctx.Request, sig, headers, key.KeyPem);
			logger.LogDebug("HttpSignature.Verify returned {result} for key {keyId}", verified, sig.KeyId);
			if (!verified)
				throw new GracefulException(HttpStatusCode.Forbidden, "Request signature validation failed");

			//TODO: re-fetch key once if signature validation fails, to properly support key rotation
			//TODO: Check for LD signature as well
		}

		await next(ctx);
	}
}

public class AuthorizedFetchAttribute(bool forceBody = false) : Attribute {
	public bool ForceBody { get; } = forceBody;
}