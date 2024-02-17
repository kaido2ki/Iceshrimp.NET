using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Backend.Core.Middleware;

public class AuthorizedFetchMiddleware(
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
	IOptionsSnapshot<Config.SecuritySection> config,
	DatabaseContext db,
	ActivityPub.UserResolver userResolver,
	SystemUserService systemUserSvc,
	ActivityPub.FederationControlService fedCtrlSvc,
	ILogger<AuthorizedFetchMiddleware> logger
) : IMiddleware
{
	private static readonly JsonSerializerSettings JsonSerializerSettings =
		new() { DateParseHandling = DateParseHandling.None };

	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
	{
		var attribute = ctx.GetEndpoint()?.Metadata.GetMetadata<AuthorizedFetchAttribute>();

		if (attribute != null && config.Value.AuthorizedFetch)
		{
			var request = ctx.Request;

			//TODO: cache this somewhere
			var instanceActorUri = $"/users/{(await systemUserSvc.GetInstanceActorAsync()).Id}";
			if (ctx.Request.Path.Value == instanceActorUri)
			{
				await next(ctx);
				return;
			}

			if (!request.Headers.TryGetValue("signature", out var sigHeader))
				throw new GracefulException(HttpStatusCode.Unauthorized, "Request is missing the signature header");

			var sig = HttpSignature.Parse(sigHeader.ToString());

			// First, we check if we already have the key
			var key = await db.UserPublickeys.Include(p => p.User).FirstOrDefaultAsync(p => p.KeyId == sig.KeyId);

			// If we don't, we need to try to fetch it
			if (key == null)
			{
				try
				{
					var user = await userResolver.ResolveAsync(sig.KeyId);
					key = await db.UserPublickeys.Include(p => p.User).FirstOrDefaultAsync(p => p.User == user);
				}
				catch (Exception e)
				{
					if (e is GracefulException) throw;
					throw new GracefulException($"Failed to fetch key of signature user ({sig.KeyId}) - {e.Message}");
				}
			}

			// If we still don't have the key, something went wrong and we need to throw an exception
			if (key == null) throw new GracefulException($"Failed to fetch key of signature user ({sig.KeyId})");

			if (key.User.Host == null)
				throw new GracefulException("Remote user must have a host");

			// We want to check both the user host & the keyId host (as account & web domain might be different)
			if (await fedCtrlSvc.ShouldBlockAsync(key.User.Host, key.KeyId))
				throw GracefulException.Forbidden("Instance is blocked");

			List<string> headers = request.ContentLength > 0 || attribute.ForceBody
				? ["(request-target)", "digest", "host", "date"]
				: ["(request-target)", "host", "date"];

			var verified = await HttpSignature.VerifyAsync(ctx.Request, sig, headers, key.KeyPem);
			logger.LogDebug("HttpSignature.Verify returned {result} for key {keyId}", verified, sig.KeyId);

			if (!verified && request is { ContentType: not null, ContentLength: > 0 })
			{
				try
				{
					var contentType = new MediaTypeHeaderValue(request.ContentType);
					if (!ActivityPub.ActivityFetcherService.IsValidActivityContentType(contentType))
						throw new Exception("Request body is not an activity");

					var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
					ctx.Request.Body.Seek(0, SeekOrigin.Begin);
					var deserialized = JsonConvert.DeserializeObject<JObject?>(body);
					var expanded     = LdHelpers.Expand(deserialized);
					if (expanded == null)
						throw new Exception("Failed to expand ASObject");
					var obj = ASObject.Deserialize(expanded);
					if (obj == null)
						throw new Exception("Failed to deserialize ASObject");
					if (obj is not ASActivity activity)
						throw new Exception($"Job data is not an ASActivity - Type: {obj.Type}");
					if (activity.Actor == null)
						throw new Exception("Activity has no actor");
					key = null;
					key = await db.UserPublickeys
					              .Include(p => p.User)
					              .FirstOrDefaultAsync(p => p.User.Uri == activity.Actor.Id);

					if (key == null)
					{
						var user = await userResolver.ResolveAsync(activity.Actor.Id);
						key = await db.UserPublickeys
						              .Include(p => p.User)
						              .FirstOrDefaultAsync(p => p.User == user);

						if (key == null)
							throw new Exception($"Failed to fetch public key for user {activity.Actor.Id}");
					}

					// We need to re-run deserialize & expand with date time handling disabled for JSON-LD canonicalization to work correctly
					var rawDeserialized = JsonConvert.DeserializeObject<JObject?>(body, JsonSerializerSettings);
					var rawExpanded     = LdHelpers.Expand(rawDeserialized);
					if (rawExpanded == null)
						throw new Exception("Failed to expand activity for LD signature processing");
					verified = await LdSignature.VerifyAsync(expanded, rawExpanded, key.KeyPem, key.KeyId);
					logger.LogDebug("LdSignature.VerifyAsync returned {result} for actor {id}",
					                verified, activity.Actor.Id);
				}
				catch (Exception e)
				{
					logger.LogError("Error validating JSON-LD signature: {e}", e.ToString());
				}
			}

			if (!verified || key == null)
				throw new GracefulException(HttpStatusCode.Forbidden, "Request signature validation failed");

			//TODO: re-fetch key once if signature validation fails, to properly support key rotation (for both http and ld sigs)

			ctx.SetActor(key.User);
		}

		await next(ctx);
	}
}

public class AuthorizedFetchAttribute(bool forceBody = false) : Attribute
{
	public bool ForceBody { get; } = forceBody;
}

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
