using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Attributes;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[Route("/inbox")]
[Route("/users/{id}/inbox")]
[AuthorizedFetch(true)]
[Produces("application/json")]
[UseNewtonsoftJson]
[EnableRequestBuffering(1024 * 1024)]
public class InboxController(ILogger<InboxController> logger) : Controller {
	[HttpPost]
	[Consumes(MediaTypeNames.Application.Json)]
	public IActionResult Inbox([FromBody] JToken content) {
		logger.LogDebug("{count}", content.Count());
		return Ok();
	}
}