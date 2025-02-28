using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Iceshrimp.Backend.Core.Federation.ActivityPub.UserResolver;

namespace Iceshrimp.Backend.Core.Middleware;

public class InboxValidationMiddleware(
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
	IOptionsSnapshot<Config.SecuritySection> config,
	DatabaseContext db,
	ActivityPub.UserResolver userResolver,
	UserService userSvc,
	ActivityPub.FederationControlService fedCtrlSvc,
	ILogger<InboxValidationMiddleware> logger,
	IHostApplicationLifetime appLifetime
) : ConditionalMiddleware<InboxValidationAttribute>, IMiddlewareService
{
	public static ServiceLifetime Lifetime => ServiceLifetime.Scoped;

	private static readonly JsonSerializerSettings JsonSerializerSettings =
		new() { DateParseHandling = DateParseHandling.None };

	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
	{
		var request = ctx.Request;
		var ct      = appLifetime.ApplicationStopping;

		if (request is not { ContentType: not null, ContentLength: > 0 })
			throw GracefulException.UnprocessableEntity("Inbox request must have a body");

		HttpSignature.HttpSignatureHeader? sig = null;

		if (request.Headers.TryGetValue("signature", out var sigHeader))
		{
			try
			{
				sig = HttpSignature.Parse(sigHeader.ToString());
				if (await fedCtrlSvc.ShouldBlockAsync(sig.KeyId))
					throw new GracefulException(HttpStatusCode.Forbidden, "Forbidden", "Instance is blocked",
					                            suppressLog: true);
			}
			catch (Exception e)
			{
				if (e is GracefulException { SuppressLog: true }) throw;
			}
		}

		var body = await new StreamReader(request.Body).ReadToEndAsync(ct);
		request.Body.Seek(0, SeekOrigin.Begin);

		JToken parsed;
		try
		{
			parsed = JToken.Parse(body);
		}
		catch (Exception e)
		{
			logger.LogDebug("Failed to parse ASObject ({error}), skipping", e.Message);
			return;
		}

		JArray? expanded;
		try
		{
			expanded = LdHelpers.Expand(parsed);
			if (expanded == null) throw new Exception("Failed to expand ASObject");
		}
		catch (Exception e)
		{
			logger.LogDebug("Failed to expand ASObject ({error}), skipping", e.Message);
			return;
		}

		ASObject? obj;
		try
		{
			obj = ASObject.Deserialize(expanded);
			if (obj == null) throw new Exception("Failed to deserialize ASObject");
		}
		catch (Exception e)
		{
			throw GracefulException
				.UnprocessableEntity($"Failed to deserialize request body as ASObject: {e.Message}");
		}

		if (obj is not ASActivity activity)
			throw new GracefulException(HttpStatusCode.UnprocessableEntity,
			                            "Request body is not an ASActivity", $"Type: {obj.Type}");

		UserPublickey? key      = null;
		var            verified = false;

		try
		{
			if (sig == null)
				throw new GracefulException(HttpStatusCode.Unauthorized, "Request is missing the signature header");

			// First, we check if we already have the key
			key = await db.UserPublickeys.Include(p => p.User)
			              .FirstOrDefaultAsync(p => p.KeyId == sig.KeyId, ct);

			// If we don't, we need to try to fetch it
			if (key == null)
			{
				try
				{
					var flags = activity is ASDelete
						? ResolveFlags.Uri | ResolveFlags.OnlyExisting
						: ResolveFlags.Uri;

					var user = await userResolver.ResolveOrNullAsync(sig.KeyId, flags).WaitAsync(ct);
					if (user == null) throw AuthFetchException.NotFound("Delete activity actor is unknown");
					key = await db.UserPublickeys.Include(p => p.User)
					              .FirstOrDefaultAsync(p => p.User == user, ct);

					// If the key is still null here, we have a data consistency issue and need to update the key manually
					key ??= await userSvc.UpdateUserPublicKeyAsync(user).WaitAsync(ct);
				}
				catch (Exception e)
				{
					if (e is GracefulException) throw;
					throw new
						GracefulException($"Failed to fetch key of signature user ({sig.KeyId}) - {e.Message}");
				}
			}

			// If we still don't have the key, something went wrong and we need to throw an exception
			if (key == null) throw new GracefulException($"Failed to fetch key of signature user ({sig.KeyId})");

			if (key.User.IsLocalUser)
				throw new Exception("Remote user must have a host");

			// We want to check both the user host & the keyId host (as account & web domain might be different)
			if (await fedCtrlSvc.ShouldBlockAsync(key.User.Host, key.KeyId))
				throw new GracefulException(HttpStatusCode.Forbidden, "Forbidden", "Instance is blocked",
				                            suppressLog: true);

			List<string> headers = ["(request-target)", "digest", "host"];

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

		if (
			(!verified || (key?.User.Uri != null && activity.Actor?.Id != key.User.Uri)) &&
			(activity is ASDelete || config.Value.AcceptLdSignatures)
		)
		{
			if (activity is ASDelete)
				logger.LogDebug("Activity is ASDelete & actor uri is not matching, trying LD signature next...");
			else
				logger.LogDebug("Trying LD signature next...");
			try
			{
				var contentType = new MediaTypeHeaderValue(request.ContentType);
				if (!ActivityPub.ActivityFetcherService.IsValidActivityContentType(contentType))
					throw new Exception("Request body is not an activity");

				if (activity.Actor == null)
					throw new Exception("Activity has no actor");
				if (await fedCtrlSvc.ShouldBlockAsync(new Uri(activity.Actor.Id).Host))
					throw new GracefulException(HttpStatusCode.Forbidden, "Forbidden", "Instance is blocked",
					                            suppressLog: true);
				key = null;
				key = await db.UserPublickeys
				              .Include(p => p.User)
				              .FirstOrDefaultAsync(p => p.User.Uri == activity.Actor.Id, ct);

				if (key == null)
				{
					var flags = activity is ASDelete
						? ResolveFlags.Uri | ResolveFlags.OnlyExisting
						: ResolveFlags.Uri;

					var user = await userResolver
					                 .ResolveOrNullAsync(activity.Actor.Id, flags)
					                 .WaitAsync(ct);
					if (user == null) throw AuthFetchException.NotFound("Delete activity actor is unknown");
					key = await db.UserPublickeys
					              .Include(p => p.User)
					              .FirstOrDefaultAsync(p => p.User == user, ct);

					if (key == null)
						throw new Exception($"Failed to fetch public key for user {activity.Actor.Id}");
				}

				if (await fedCtrlSvc.ShouldBlockAsync(key.User.Host, new Uri(key.KeyId).Host))
					throw new GracefulException(HttpStatusCode.Forbidden, "Forbidden", "Instance is blocked",
					                            suppressLog: true);

				// We need to re-run deserialize & expand with date time handling disabled for JSON-LD canonicalization to work correctly
				var rawDeserialized = JsonConvert.DeserializeObject<JObject?>(body, JsonSerializerSettings);
				var rawExpanded     = LdHelpers.Expand(rawDeserialized);
				if (rawExpanded == null)
					throw new Exception("Failed to expand activity for LD signature processing");
				verified = await LdSignature.VerifyAsync(expanded, rawExpanded, key.KeyPem, key.KeyId);
				logger.LogDebug("LdSignature.VerifyAsync returned {result} for actor {id}",
				                verified, activity.Actor.Id);
				if (!verified)
				{
					logger.LogDebug("Refetching user key...");
					key      = await userSvc.UpdateUserPublicKeyAsync(key);
					verified = await LdSignature.VerifyAsync(expanded, rawExpanded, key.KeyPem, key.KeyId);
					logger.LogDebug("LdSignature.VerifyAsync returned {result} for actor {id}",
					                verified, activity.Actor.Id);
				}
			}
			catch (Exception e)
			{
				if (e is AuthFetchException afe) throw GracefulException.Accepted(afe.Message);
				if (e is GracefulException { SuppressLog: true }) throw;
				logger.LogError("Error validating JSON-LD signature: {error}", e.Message);
			}
		}

		if (!verified || key == null)
			throw new GracefulException(HttpStatusCode.Forbidden, "Request signature validation failed");

		ctx.SetActor(key.User);

		await next(ctx);
	}
}

public class InboxValidationAttribute : Attribute;