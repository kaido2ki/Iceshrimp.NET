using System.Net;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Federation.ActivityPub;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Middleware;

public class AuthorizedFetchMiddleware(RequestDelegate next) {
	public async Task InvokeAsync(HttpContext context, IOptionsSnapshot<Config.SecuritySection> config,
	                              DatabaseContext db, UserResolver userResolver,
	                              ILogger<AuthorizedFetchMiddleware> logger) {
		var endpoint  = context.Features.Get<IEndpointFeature>()?.Endpoint;
		var attribute = endpoint?.Metadata.GetMetadata<AuthorizedFetchAttribute>();

		if (attribute != null && config.Value.AuthorizedFetch) {
			var request = context.Request;
			if (!request.Headers.TryGetValue("signature", out var sigHeader))
				throw new CustomException(HttpStatusCode.Unauthorized, "Request is missing the signature header",
				                          logger);

			var sig = HttpSignature.Parse(sigHeader.ToString());

			// First, we check if we already have the key
			var key = await db.UserPublickeys.FirstOrDefaultAsync(p => p.KeyId == sig.KeyId);

			// If we don't, we need to try to fetch it
			if (key == null) {
				var user = await userResolver.Resolve(sig.KeyId);
				key = await db.UserPublickeys.FirstOrDefaultAsync(p => p.UserId == user.Id);
			}

			// If we still don't have the key, something went wrong and we need to throw an exception
			if (key == null) throw new CustomException("Failed to fetch key of signature user", logger);

			//TODO: re-fetch key once if signature validation fails, to properly support key rotation

			List<string> headers = request.Body.Length > 0 || attribute.ForceBody
				? ["(request-target)", "digest", "host", "date"]
				: ["(request-target)", "host", "date"];

			var verified = await HttpSignature.Verify(context.Request, sig, headers, key.KeyPem);
			logger.LogDebug("HttpSignature.Verify returned {result} for key {keyId}", verified, sig.KeyId);
			if (!verified)
				throw new CustomException(HttpStatusCode.Forbidden, "Request signature validation failed", logger);
		}

		await next(context);
	}
}

public class AuthorizedFetchAttribute(bool forceBody = false) : Attribute {
	public bool ForceBody { get; } = forceBody;
}