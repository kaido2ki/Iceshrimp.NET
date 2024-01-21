using System.Data;
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
		if (!Request.Headers.TryGetValue("signature", out var sigHeader))
			throw new ConstraintException("Signature string is missing the signature header");

		var sig = HttpSignature.Parse(sigHeader.ToString());
		var key = await db.UserPublickeys.SingleOrDefaultAsync(p => p.KeyId == sig.KeyId);
		var verified = key != null &&
		               await HttpSignature.Verify(Request, sig, ["(request-target)", "digest", "host", "date"],
		                                          key.KeyPem);
		
		logger.LogDebug("HttpSignature.Verify returned {result} for key {keyId}", verified, sig.KeyId);
		return verified ? Ok() : StatusCode(StatusCodes.Status403Forbidden);
	}
}