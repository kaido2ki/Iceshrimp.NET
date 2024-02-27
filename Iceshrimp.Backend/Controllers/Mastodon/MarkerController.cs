using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
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
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MarkerSchemas.MarkerResponse))]
	public async Task<IActionResult> GetMarkers([FromQuery(Name = "timeline")] List<string> types)
	{
		var user = HttpContext.GetUserOrFail();
		var markers = await db.Markers.Where(p => p.User == user && types.Select(DecodeType).Contains(p.Type))
		                      .ToListAsync();

		var res           = new MarkerSchemas.MarkerResponse();
		var home          = markers.FirstOrDefault(p => p.Type == Marker.MarkerType.Home);
		var notifications = markers.FirstOrDefault(p => p.Type == Marker.MarkerType.Notifications);
		if (home != null)
		{
			res.Home = new MarkerEntity
			{
				Position  = home.Position,
				Version   = home.Version,
				UpdatedAt = home.LastUpdatedAt.ToStringIso8601Like()
			};
		}

		if (notifications != null)
		{
			res.Notifications = new MarkerEntity
			{
				Position  = notifications.Position,
				Version   = notifications.Version,
				UpdatedAt = notifications.LastUpdatedAt.ToStringIso8601Like()
			};
		}


		return Ok(res);
	}

	[HttpPost]
	[Authorize("write:statuses")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MarkerSchemas.MarkerResponse))]
	public async Task<IActionResult> SetMarkers([FromHybrid] MarkerSchemas.MarkerRequest request)
	{
		var user  = HttpContext.GetUserOrFail();
		var types = new List<Marker.MarkerType>();
		try
		{
			if (request.Home != null)
			{
				types.Add(Marker.MarkerType.Home);
				var marker = await db.Markers.FirstOrDefaultAsync(p => p.User == user &&
				                                                       p.Type == Marker.MarkerType.Home);
				if (marker == null)
				{
					marker = new Marker { User = user, Type = Marker.MarkerType.Home };
					await db.AddAsync(marker);
				}
				else if (marker.Position != request.Home.LastReadId)
				{
					marker.Version++;
				}

				marker.LastUpdatedAt = DateTime.UtcNow;
				marker.Position      = request.Home.LastReadId;
			}

			if (request.Notifications != null)
			{
				types.Add(Marker.MarkerType.Notifications);

				var marker = await db.Markers.FirstOrDefaultAsync(p => p.User == user &&
				                                                       p.Type == Marker.MarkerType.Notifications);
				if (marker == null)
				{
					marker = new Marker { User = user, Type = Marker.MarkerType.Notifications };
					await db.AddAsync(marker);
				}
				else if (marker.Position != request.Notifications.LastReadId)
				{
					marker.Version++;
				}

				marker.LastUpdatedAt = DateTime.UtcNow;
				marker.Position      = request.Notifications.LastReadId;
			}

			await db.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException)
		{
			throw new GracefulException(HttpStatusCode.Conflict, "Conflict during update, please try again");
		}

		return await GetMarkers(types.Select(EncodeType).ToList());
	}

	public Marker.MarkerType DecodeType(string type) =>
		type switch
		{
			"home"          => Marker.MarkerType.Home,
			"notifications" => Marker.MarkerType.Notifications,
			_               => throw GracefulException.BadRequest($"Unknown marker type {type}")
		};

	public string EncodeType(Marker.MarkerType type) =>
		type switch
		{
			Marker.MarkerType.Home          => "home",
			Marker.MarkerType.Notifications => "notifications",
			_                               => throw GracefulException.BadRequest($"Unknown marker type {type}")
		};
}