using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[ApiController]
[Tags("Mastodon")]
[EnableRateLimiting("sliding")]
[Produces("application/json")]
[Route("/api/v1")]
public class MastodonAuthController : Controller { }