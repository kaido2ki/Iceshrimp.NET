using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Iceshrimp.Backend.Core.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[Produces("application/json")]
[Route("/inbox")]
public class SignatureTestController : Controller {
	[HttpPost]
	[Consumes(MediaTypeNames.Application.Json)]
	public async Task<IActionResult> Inbox([FromServices] ILogger<SignatureTestController> logger,
	                                       [FromServices] DatabaseContext                  db) {
		var sig      = new HttpSignature(Request, ["(request-target)", "digest", "host", "date"]);
		var key      = await db.UserPublickeys.SingleOrDefaultAsync(p => p.KeyId == sig.KeyId);
		var verified = key != null && sig.Verify(key.KeyPem);
		logger.LogInformation("sig.Verify returned {result} for key {keyId}", verified, sig.KeyId);
		return verified ? Ok() : StatusCode(StatusCodes.Status403Forbidden);
	}
}