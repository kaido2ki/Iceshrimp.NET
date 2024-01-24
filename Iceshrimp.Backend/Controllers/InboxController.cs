using System.Data;
using System.Net.Mime;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[Route("/inbox")]
[Route("/users/{id}/inbox")]
[AuthorizedFetch(true)]
[Produces("application/json")]
[EnableRequestBuffering(1024 * 1024)]
public class InboxController(ILogger<InboxController> logger, DatabaseContext db) : Controller {
	[HttpPost]
	[Consumes(MediaTypeNames.Application.Json)]
	public async Task<IActionResult> Inbox([FromBody] JToken content) {
		if (!Request.Headers.TryGetValue("signature", out var sigHeader))
			throw new ConstraintException("Request is missing the signature header");

		var sig = HttpSignature.Parse(sigHeader.ToString());
		var key = await db.UserPublickeys.SingleOrDefaultAsync(p => p.KeyId == sig.KeyId);
		var verified = key != null &&
		               await HttpSignature.Verify(Request, sig, ["(request-target)", "digest", "host", "date"],
		                                          key.KeyPem);

		logger.LogDebug("HttpSignature.Verify returned {result} for key {keyId}", verified, sig.KeyId);
		return verified ? Ok() : StatusCode(StatusCodes.Status403Forbidden);
	}
}