using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Route("/api/v1/push/subscription")]
[Authenticate]
[Authorize("push")]
[EnableRateLimiting("sliding")]
[EnableCors("mastodon")]
[Produces(MediaTypeNames.Application.Json)]
public class PushController(DatabaseContext db, MetaService meta) : ControllerBase
{
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PushSchemas.PushSubscription))]
	public async Task<IActionResult> RegisterSubscription([FromHybrid] PushSchemas.RegisterPushRequest request)
	{
		var token = HttpContext.GetOauthToken();
		if (token == null) throw GracefulException.Unauthorized("The access token is invalid");
		var pushSubscription = await db.PushSubscriptions.FirstOrDefaultAsync(p => p.OauthToken == token);
		if (pushSubscription == null)
		{
			pushSubscription = new PushSubscription
			{
				Id         = IdHelpers.GenerateSlowflakeId(),
				CreatedAt  = DateTime.UtcNow,
				Endpoint   = request.Subscription.Endpoint,
				User       = token.User,
				OauthToken = token,
				PublicKey  = request.Subscription.Keys.PublicKey,
				AuthSecret = request.Subscription.Keys.AuthSecret,
				Types      = GetTypes(request.Data.Alerts),
				Policy     = GetPolicy(request.Data.Policy)
			};

			await db.AddAsync(pushSubscription);
			await db.SaveChangesAsync();
		}

		var res = await RenderSubscription(pushSubscription);

		return Ok(res);
	}

	[HttpPut]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PushSchemas.PushSubscription))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> EditSubscription([FromHybrid] PushSchemas.EditPushRequest request)
	{
		var token = HttpContext.GetOauthToken();
		if (token == null) throw GracefulException.Unauthorized("The access token is invalid");
		var pushSubscription = await db.PushSubscriptions.FirstOrDefaultAsync(p => p.OauthToken == token) ??
		                       throw GracefulException.RecordNotFound();

		pushSubscription.Types  = GetTypes(request.Data.Alerts);
		pushSubscription.Policy = GetPolicy(request.Data.Policy);
		await db.SaveChangesAsync();

		var res = await RenderSubscription(pushSubscription);

		return Ok(res);
	}

	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PushSchemas.PushSubscription))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetSubscription()
	{
		var token = HttpContext.GetOauthToken();
		if (token == null) throw GracefulException.Unauthorized("The access token is invalid");
		var pushSubscription = await db.PushSubscriptions.FirstOrDefaultAsync(p => p.OauthToken == token) ??
		                       throw GracefulException.RecordNotFound();

		var res = await RenderSubscription(pushSubscription);

		return Ok(res);
	}

	[HttpDelete]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
	public async Task<IActionResult> DeleteSubscription()
	{
		var token = HttpContext.GetOauthToken();
		if (token == null) throw GracefulException.Unauthorized("The access token is invalid");
		await db.PushSubscriptions.Where(p => p.OauthToken == token).ExecuteDeleteAsync();

		return Ok(new object());
	}

	private static PushSubscription.PushPolicy GetPolicy(string policy)
	{
		return policy switch
		{
			"all"      => PushSubscription.PushPolicy.All,
			"follower" => PushSubscription.PushPolicy.Follower,
			"followed" => PushSubscription.PushPolicy.Followed,
			"none"     => PushSubscription.PushPolicy.None,
			_          => throw GracefulException.BadRequest("Unknown push policy")
		};
	}

	private static string GetPolicyString(PushSubscription.PushPolicy policy)
	{
		return policy switch
		{
			PushSubscription.PushPolicy.All      => "all",
			PushSubscription.PushPolicy.Follower => "follower",
			PushSubscription.PushPolicy.Followed => "followed",
			PushSubscription.PushPolicy.None     => "none",
			_                                    => throw GracefulException.BadRequest("Unknown push policy")
		};
	}

	private static List<string> GetTypes(PushSchemas.Alerts alerts)
	{
		List<string> types = [];

		if (alerts.Favourite)
			types.Add("favourite");
		if (alerts.Follow)
			types.Add("follow");
		if (alerts.Mention)
			types.Add("mention");
		if (alerts.Poll)
			types.Add("poll");
		if (alerts.Reblog)
			types.Add("reblog");
		if (alerts.Status)
			types.Add("status");
		if (alerts.Update)
			types.Add("update");
		if (alerts.FollowRequest)
			types.Add("follow_request");

		return types;
	}

	private async Task<PushSchemas.PushSubscription> RenderSubscription(PushSubscription sub)
	{
		return new PushSchemas.PushSubscription
		{
			Id        = sub.Id,
			Endpoint  = sub.Endpoint,
			ServerKey = await meta.GetVapidPublicKey() ?? throw new Exception("Failed to fetch VAPID key"),
			Policy    = GetPolicyString(sub.Policy),
			Alerts = new PushSchemas.Alerts
			{
				Favourite     = sub.Types.Contains("favourite"),
				Follow        = sub.Types.Contains("follow"),
				Mention       = sub.Types.Contains("mention"),
				Poll          = sub.Types.Contains("poll"),
				Reblog        = sub.Types.Contains("reblog"),
				Status        = sub.Types.Contains("status"),
				Update        = sub.Types.Contains("update"),
				FollowRequest = sub.Types.Contains("follow_request")
			},
		};
	}
}