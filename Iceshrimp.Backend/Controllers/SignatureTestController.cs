using System.Net.Mime;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[Produces("application/json")]
[Route("/inbox")]
public class SignatureTestController(ILogger<SignatureTestController> logger, DatabaseContext db) : Controller {
	[HttpPost]
	[Consumes(MediaTypeNames.Application.Json)]
	public async Task<IActionResult> Inbox() {
		var sig      = new HttpSignature(Request, ["(request-target)", "digest", "host", "date"]);
		var key      = await db.UserPublickeys.SingleOrDefaultAsync(p => p.KeyId == sig.KeyId);
		var verified = key != null && sig.Verify(key.KeyPem);
		logger.LogInformation("sig.Verify returned {result} for key {keyId}", verified, sig.KeyId);
		return verified ? Ok() : StatusCode(StatusCodes.Status403Forbidden);
	}
}