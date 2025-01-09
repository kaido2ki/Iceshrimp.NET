using System.Net;
using AsyncKeyedLock;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Federation.WebFinger;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class UserResolver(
	ILogger<UserResolver> logger,
	UserService userSvc,
	WebFingerService webFingerSvc,
	FollowupTaskService followupTaskSvc,
	ActivityFetcherService fetchSvc,
	IOptions<Config.InstanceSection> config,
	DatabaseContext db
) : IScopedService
{
	private static readonly AsyncKeyedLocker<string> KeyedLocker = new(o =>
	{
		o.PoolSize        = 100;
		o.PoolInitialFill = 5;
	});

	/*
	 * The full web finger algorithm:
	 *
	 * 1. WebFinger(query), find the rel=self type=application/activity+json entry, that's apUri
	 * 1.1 Failing this, fetch the actor
	 * 1.1.1 If the actor uri differs from the query, recurse (once!) with the actor uri as the new query
	 * 1.1.2 Otherwise, Perform reverse discovery for apUri
	 * 2. WebFinger(apUri), find the first acct: URI in [subject] + aliases, that's candidateAcctUri
	 * 2.1 If no such URI exists, attempt to fetch the actor & assemble candidateAcctUri from preferredUsername and apUri host
	 * 2.2 If WebFinger returns null:
	 * 2.2.1 If already in reverse discovery, abort
	 * 2.2.2 Otherwise, perform reverse discovery for apUri
	 * 3. WebFinger(candidateAcctUri), validate it also points to apUri. If so, you have finalAcct
	 * 3.1 If no such URI exists, attempt to fetch the actor & assemble finalAcct from preferredUsername and apUri host
	 *
	 * Avoid repeating WebFinger's with same URI for performance, optimize away validation checks when the domain matches.
	 * Skip step 2 when performing reverse discovery & apUri matches the actor uri.
	 */

	private async Task<(string Acct, string Uri)> WebFingerAsync(
		string query, bool recurse = true, string? actorUri = null,
		Dictionary<string, WebFingerResponse>? responses = null
	)
	{
		if (actorUri == null)
			logger.LogDebug("Running WebFinger for query '{query}'", query);
		else
			logger.LogDebug("Performing WebFinger reverse discovery for query '{query}' and uri '{uri}'",
			                query, actorUri);

		responses ??= [];

		ASActor? actor = null;

		var fingerRes = await webFingerSvc.ResolveAsync(query);
		if (fingerRes == null)
		{
			if (recurse && query.StartsWith("https://"))
			{
				logger.LogDebug("WebFinger returned null, fetching actor as fallback");
				try
				{
					actor = await fetchSvc.FetchActorAsync(query);
					if (query != actor.Id)
					{
						logger.LogDebug("Actor ID differs from query, retrying...");
						return await WebFingerAsync(actor.Id, false);
					}

					logger.LogDebug("Actor ID matches query, performing reverse discovery...");
					actor.NormalizeAndValidate(query);
					var domain   = new Uri(actor.Id).Host;
					var username = actor.Username!;
					return await WebFingerAsync(actor.WebfingerAddress ?? $"acct:{username}@{domain}", false, actor.Id);
				}
				catch (Exception e)
				{
					logger.LogDebug("Failed to fetch actor {uri}: {e}", query, e.Message);
				}
			}

			throw new GracefulException($"Failed to WebFinger '{query}'");
		}

		responses.Add(query, fingerRes);

		var apUri = fingerRes.Links
		                     .FirstOrDefault(p => p is { Rel: "self", Type: Constants.APMime or Constants.ASMime })
		                     ?.Href;

		if (apUri == null)
			throw new GracefulException($"WebFinger response for '{query}' didn't contain a candidate apUri");
		var candidateAcctUri = GetAcctUri(fingerRes);
		if (candidateAcctUri == null)
		{
			if (Uri.TryCreate(apUri, UriKind.Absolute, out var parsedApUri))
			{
				// @formatter:off
				logger.LogDebug("WebFinger response for {apUri} didn't contain any acct uris, fetching actor as fallback", apUri);

				if (actor != null && actor.Id != apUri)
					actor = await fetchSvc.FetchActorAsync(apUri);
				else
					actor ??= await fetchSvc.FetchActorAsync(apUri);

				if (actor.Username is null)
					throw new GracefulException($"WebFinger response for '{apUri}' didn't contain any acct uris & actor doesn't have a preferredUsername");
				if (apUri != actor.Id)
					throw new GracefulException("WebFinger fallback fallback failed: actor uri mismatch");
				
				actor.NormalizeAndValidate(apUri);
				candidateAcctUri = $"acct:{actor.Username}@{parsedApUri.Host}";
				// @formatter:on
			}
			else
			{
				throw new Exception($"WebFinger response for '{apUri}' didn't contain any acct uris");
			}
		}

		var queryHost   = WebFingerService.ParseQuery(query).domain;
		var subjectHost = WebFingerService.ParseQuery(candidateAcctUri).domain;
		var apUriHost   = new Uri(apUri).Host;
		if (subjectHost == apUriHost && subjectHost == queryHost)
			return (candidateAcctUri, apUri);

		// We need to skip this step when performing reverse discovery & the uris match
		if (apUri != actorUri)
		{
			if (actorUri != null) throw new Exception("Reverse discovery failed: uri mismatch");
			fingerRes = responses.GetValueOrDefault(apUri);
			if (fingerRes == null)
			{
				logger.LogDebug("AP uri didn't match query, re-running WebFinger for '{apUri}'", apUri);
				fingerRes = await webFingerSvc.ResolveAsync(apUri);
				if (fingerRes == null)
				{
					logger.LogDebug("Failed to validate apUri, falling back to reverse discovery");
					try
					{
						if (actor != null && actor.Id != apUri)
							actor = await fetchSvc.FetchActorAsync(apUri);
						else
							actor ??= await fetchSvc.FetchActorAsync(apUri);
						if (apUri != actor.Id)
							throw new Exception("Reverse discovery fallback failed: uri mismatch");

						logger.LogDebug("Actor ID matches apUri, performing reverse discovery...");
						actor.NormalizeAndValidate(apUri);
						var domain   = new Uri(actor.Id).Host;
						var username = actor.Username!;
						return await WebFingerAsync(actor.WebfingerAddress ?? $"acct:{username}@{domain}", false,
						                            apUri, responses);
					}
					catch (Exception e)
					{
						logger.LogDebug("Failed to fetch actor {uri}: {e}", query, e.Message);
						throw new GracefulException($"Failed to WebFinger '{query}'");
					}
				}

				responses.Add(apUri, fingerRes);
			}
		}

		var acctUri = GetAcctUri(fingerRes);

		if (acctUri == null)
		{
			if (Uri.TryCreate(apUri, UriKind.Absolute, out var parsedApUri))
			{
				// @formatter:off
				logger.LogDebug("WebFinger response for {apUri} didn't contain any acct uris, fetching actor as fallback", apUri);
				
				if (actor != null && actor.Id != apUri)
					actor = await fetchSvc.FetchActorAsync(apUri);
				else
					actor ??= await fetchSvc.FetchActorAsync(apUri);

				if (actor.Username is null)
					throw new GracefulException($"WebFinger response for '{apUri}' didn't contain any acct uris & actor doesn't have a preferredUsername");
				if (actor.Id != apUri)
					throw new GracefulException("WebFinger fallback fallback failed: actor uri mismatch");

				actor.NormalizeAndValidate(apUri);
				acctUri = $"acct:{actor.Username}@{parsedApUri.Host}";
				// @formatter:on
			}
			else
			{
				throw new Exception($"WebFinger response for '{acctUri}' didn't contain any acct uris");
			}
		}

		if (WebFingerService.ParseQuery(acctUri).domain == apUriHost)
			return (acctUri, apUri);

		fingerRes = responses.GetValueOrDefault(acctUri);
		if (fingerRes == null)
		{
			logger.LogDebug("Acct uri didn't match query, re-running WebFinger for '{acctUri}'", acctUri);

			fingerRes = await webFingerSvc.ResolveAsync(acctUri);

			if (fingerRes == null) throw new Exception($"Failed to WebFinger '{acctUri}'");
			responses.Add(acctUri, fingerRes);
		}

		var finalUri = fingerRes.Links.FirstOrDefault(p => p is { Rel: "self", Type: "application/activity+json" })
		                        ?.Href
		               ?? throw new Exception("Final AP URI was null");

		var finalAcct = GetAcctUri(fingerRes);

		if (finalAcct == null)
		{
			if (actor?.Id != finalUri)
				actor = await fetchSvc.FetchActorAsync(finalUri);

			// @formatter:off
			if (actor.Username is null)
				throw new GracefulException($"WebFinger response for '{finalUri}' didn't contain any acct uris & actor doesn't have a preferredUsername");
			if (actor.Id != finalUri)
				throw new GracefulException("WebFinger fallback fallback failed: actor uri mismatch");

			actor.NormalizeAndValidate(apUri);
			finalAcct = $"acct:{actor.Username}@{new Uri(finalUri).Host}";
			if (finalAcct != acctUri)
				throw new GracefulException("Failed too build fallback acct: finalAcct doesn't match acctUri");
			// @formatter:on
		}

		if (apUri != finalUri)
		{
			logger.LogDebug("WebFinger: finalUri doesn't match apUri, setting acct host to apUri host: {apUri}", apUri);
			var split = finalAcct.Split('@');
			if (split.Length != 2)
				throw new Exception($"Failed to finalize WebFinger for '{apUri}': invalid acct '{finalAcct}'");
			split[1]  = new Uri(apUri).Host;
			finalAcct = string.Join('@', split);
		}

		return (finalAcct, finalUri);
	}

	public static string? GetAcctUri(WebFingerResponse fingerRes)
	{
		var acct = (fingerRes.Aliases ?? [])
		           .Prepend(fingerRes.Subject)
		           .FirstOrDefault(p => p.StartsWith("acct:"));
		if (acct != null) return acct;

		// AodeRelay doesn't prefix its actor's subject with acct, so we have to fall back to guessing here
		acct = (fingerRes.Aliases ?? [])
		       .Prepend(fingerRes.Subject)
		       .FirstOrDefault(p => !p.Contains(':') && !p.Contains(' ') && p.Split("@").Length == 2);
		return acct is not null ? $"acct:{acct}" : acct;
	}

	public static string GetQuery(string username, string? host) => host != null
		? $"acct:{username}@{host}"
		: $"acct:{username}";

	[Flags]
	public enum ResolveFlags
	{
		/// <summary>
		/// No flags. Do not use.
		/// </summary>
		None = 0,

		/// <summary>
		/// Whether to allow http/s: queries.
		/// </summary>
		Uri = 1,

		/// <summary>
		/// Whether to allow acct: queries.
		/// </summary>
		Acct = 2,

		/// <summary>
		/// Whether to match user urls in addition to user uris. Does nothing if the Uri flag is not set.
		/// </summary>
		MatchUrl = 4,

		/// <summary>
		/// Whether to enforce that the returned object has the same URL as the query. Does nothing if the Uri flag is not set.
		/// </summary>
		EnforceUri = 8,

		/// <summary>
		/// Return early if no matching user was found in the database
		/// </summary>
		OnlyExisting = 16
	}

	public const ResolveFlags EnforceUriFlags = ResolveFlags.Uri | ResolveFlags.EnforceUri;
	public const ResolveFlags SearchFlags     = ResolveFlags.Uri | ResolveFlags.MatchUrl | ResolveFlags.Acct;

	/// <summary>
	/// Resolves a local or remote user.
	/// </summary>
	/// <param name="query">The query to resolve. Can be an acct (with acct: or @ prefix), </param>
	/// <param name="flags">The options to use for the resolve process.</param>
	/// <returns>The user in question.</returns>
	private async Task<Result<User>> ResolveInternalAsync(string query, ResolveFlags flags)
	{
		// Before we begin, validate method parameters & canonicalize query
		if (flags == 0)
			throw new Exception("ResolveFlags.None is not valid for this method");
		if (query.Contains(' '))
			return GracefulException.BadRequest($"Invalid query: {query}");
		if (query.StartsWith('@'))
			query = $"acct:{query[1..]}";
		if (!Uri.TryCreate(query, UriKind.Absolute, out var parsedQuery))
			query = $"acct:{query}";
		if (parsedQuery == null && !Uri.TryCreate(query, UriKind.Absolute, out parsedQuery))
			return GracefulException.BadRequest($"Invalid query: {query}");
		if (parsedQuery.Scheme is not "https" and not "acct")
			return GracefulException.BadRequest("Invalid query scheme");
		if (parsedQuery.AbsolutePath.StartsWith("/notes/"))
			return GracefulException.BadRequest("Refusing to resolve local note URL as user");
		if (parsedQuery.Scheme is "acct" && !flags.HasFlag(ResolveFlags.Acct))
			return GracefulException.BadRequest("Refusing to resolve acct: resource without ResolveFlags.Acct");
		if (parsedQuery.Scheme is "https" && !flags.HasFlag(ResolveFlags.Uri))
			return GracefulException.BadRequest("Refusing to resolve http(s): uri without !allowUri");
		if (parsedQuery.Fragment is { Length: > 0 })
			parsedQuery = new Uri(parsedQuery.GetLeftPart(UriPartial.Query));

		// Update query after canonicalization for use below
		query = parsedQuery.AbsoluteUri;

		// First, let's see if we already know the user
		var user = await userSvc.GetUserFromQueryAsync(parsedQuery, flags.HasFlag(ResolveFlags.MatchUrl));
		if (user != null) return user;

		// If query still matches the web domain, we can return early
		if (parsedQuery.Host == config.Value.WebDomain)
			throw GracefulException.NotFound("No match found");

		if (flags.HasFlag(ResolveFlags.OnlyExisting))
			return GracefulException.NotFound("No match found & OnlyExisting flag is set");

		// We don't, so we need to run WebFinger
		var (acct, uri) = await WebFingerAsync(query);

		// Check the database again with the new data
		if (uri != query)
			user = await userSvc.GetUserFromQueryAsync(new Uri(uri), allowUrl: false);

		// If there's still no match, try looking it up by acct returned by WebFinger
		if (user == null && acct != query)
		{
			user = await userSvc.GetUserFromQueryAsync(new Uri(acct), allowUrl: false);
			if (user != null && user.Uri != uri && !flags.HasFlag(ResolveFlags.Acct))
				return GracefulException.BadRequest($"User with acct {acct} is known, but Acct flag is not set."
				                                    + $"This likely means that the domain was reused without"
				                                    + $"preserving user data, and a user that previously existed"
				                                    + $"was recreated with a new URI. To fix this, have your instance"
				                                    + $"administrator delete the old user from the database.");
		}

		// @formatter:off
		if (flags.HasFlag(ResolveFlags.EnforceUri) && user != null && new Uri(user.GetUriOrPublicUri(config.Value)) != new Uri(query))
			return GracefulException.BadRequest("Refusing to return user with mismatching uri with EnforceUri flag");
		// @formatter:on

		if (user != null)
			return user;

		if (flags.HasFlag(ResolveFlags.EnforceUri) && new Uri(uri) != new Uri(query))
			return GracefulException.BadRequest("Refusing to create user with mismatching uri with EnforceUri flag");

		// All checks passed & we still don't know the user, so we pass the job on to userSvc, which will create it
		using (await KeyedLocker.LockAsync(uri))
		{
			return await userSvc.CreateUserAsync(uri, acct);
		}
	}

	/// <inheritdoc cref="ResolveInternalAsync"/>
	public async Task<User> ResolveAsync(string query, ResolveFlags flags)
	{
		return await ResolveInternalAsync(query, flags) switch
		{
			Result<User>.Success result  => result.Result,
			Result<User>.Failure failure => throw failure.Error,
			_                            => throw new ArgumentOutOfRangeException()
		};
	}

	/// <inheritdoc cref="ResolveInternalAsync"/>
	public async Task<User?> ResolveOrNullAsync(string query, ResolveFlags flags)
	{
		string errorMessage;
		try
		{
			var res = await ResolveInternalAsync(query, flags);
			if (res.TryGetResult(out var result)) return result;
			if (!res.TryGetError(out var error)) throw new Exception("Return value neither result nor error");
			if (error is GracefulException { StatusCode: HttpStatusCode.NotFound }) return null;
			errorMessage = error.Message;
		}
		catch (GracefulException ge) when (ge.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}
		catch (Exception e)
		{
			errorMessage = e.Message;
		}

		logger.LogDebug("Failed to resolve user: {error}", errorMessage);
		return null;
	}

	public async Task<User> GetUpdatedUserAsync(User user)
	{
		if (!user.NeedsUpdate) return user;

		// Prevent multiple background tasks from being started
		user.LastFetchedAt = DateTime.UtcNow;
		await db.Users.Where(p => p == user)
		        .ExecuteUpdateAsync(p => p.SetProperty(i => i.LastFetchedAt, _ => user.LastFetchedAt));

		var success = false;

		try
		{
			var task = followupTaskSvc.ExecuteTaskAsync("UpdateUserAsync", async provider =>
			{
				// Get a fresh UserService instance in a new scope
				var bgUserSvc = provider.GetRequiredService<UserService>();

				// Use the id overload so it doesn't attempt to insert in the main thread's DbContext
				await bgUserSvc.UpdateUserAsync(user.Id);
				success = true;
			});

			// Return early, but continue execution in background
			await task.WaitAsync(TimeSpan.FromMilliseconds(1500));
		}
		catch (Exception e)
		{
			if (e is TimeoutException)
				logger.LogDebug("UpdateUserAsync timed out for user {user}", user.Uri);
			else if (e is not AuthFetchException { Message: "The remote user no longer exists." })
				logger.LogError("UpdateUserAsync for user {user} failed with {error}", user.Uri, e.Message);
		}

		if (success)
			await db.ReloadEntityRecursivelyAsync(user);

		return user;
	}
}
