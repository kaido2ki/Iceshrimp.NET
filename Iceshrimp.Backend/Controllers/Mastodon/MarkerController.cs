using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Route("/api/v1/markers")]
[Authenticate]
[EnableRateLimiting("sliding")]
[EnableCors("mastodon")]
[Produces(MediaTypeNames.Application.Json)]
public class MarkerController(DatabaseContext db) : ControllerBase
{
	[HttpGet]
	[Authorize("read:statuses")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<Dictionary<string, MarkerEntity>> GetMarkers([FromQuery(Name = "timeline")] List<string> types)
	{
		var user = HttpContext.GetUserOrFail();
		var markers = await db.Markers.Where(p => p.User == user && types.Select(DecodeType).Contains(p.Type))
		                      .ToListAsync();

		return markers.ToDictionary(p => EncodeType(p.Type),
		                            p => new MarkerEntity
		                            {
			                            Position  = p.Position,
			                            Version   = p.Version,
			                            UpdatedAt = p.LastUpdatedAt.ToStringIso8601Like()
		                            });
	}

	[HttpPost]
	[Authorize("write:statuses")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.Conflict)]
	public async Task<Dictionary<string, MarkerEntity>> SetMarkers(
		[FromHybrid] Dictionary<string, MarkerSchemas.MarkerPosition> request
	)
	{
		var user = HttpContext.GetUserOrFail();
		try
		{
			foreach (var item in request)
			{
				var type = DecodeType(item.Key);

				var marker = await db.Markers.FirstOrDefaultAsync(p => p.User == user && p.Type == type);
				if (marker == null)
				{
					marker = new Marker { User = user, Type = type };
					await db.AddAsync(marker);
				}
				else if (marker.Position != item.Value.LastReadId)
				{
					marker.Version++;
				}

				marker.LastUpdatedAt = DateTime.UtcNow;
				marker.Position      = item.Value.LastReadId;
			}

			await db.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException)
		{
			throw new GracefulException(HttpStatusCode.Conflict, "Conflict during update, please try again");
		}

		return await GetMarkers(request.Keys.ToList());
	}

	private static Marker.MarkerType DecodeType(string type) =>
		type switch
		{
			"home"          => Marker.MarkerType.Home,
			"notifications" => Marker.MarkerType.Notifications,
			_               => throw GracefulException.BadRequest($"Unknown marker type {type}")
		};

	private static string EncodeType(Marker.MarkerType type) =>
		type switch
		{
			Marker.MarkerType.Home          => "home",
			Marker.MarkerType.Notifications => "notifications",
			_                               => throw GracefulException.BadRequest($"Unknown marker type {type}")
		};
}