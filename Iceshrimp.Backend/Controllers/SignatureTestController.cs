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
	public async Task<IActionResult> Inbox() {
		var sig = new HttpSignature(Request, ["(request-target)", "digest", "host", "date"]);
	
		//TODO: fetch key from db (duh)
		
		const string key = """
		                   -----BEGIN PUBLIC KEY-----
		                   MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAtCFuaufkSCpsDZ2twSrH
		                   GAFcJTGQ7ZspaFekVM7gBP1GQ/jfjwO3qT9fMgbsCuQXNTIw0U9zlsTIPB91yNPw
		                   w5UpbqQ3dnpnYnwXg1BsqfX7EOLR1Dlnw6dk+5yeginJsNno15SRQ7CDqbEXj7Nc
		                   lhNOGgU+LaXHhN59Paye3sfsvUHu4fmTp/rALWGPl/Rvx7RVRcR76CcTfTaHPYdb
		                   OQAtqPJBfWgHpPLAUjRypzZoN/ExMgiCbFuxI7UFNNXxU3te8GNZaaob8bSwyUB6
		                   Xuq7Rw+Me3eYiDxrYHQ99ZytsgoHBNVrVh/X7wIl0AlpjyWeGug3uIUjXR0twuGj
		                   wwIDAQAB
		                   -----END PUBLIC KEY-----
		                   """;
		
		return Ok(new ErrorResponse {
			StatusCode = 200,
			Error = "null",
			Message = sig.Verify(key).ToString()
		});
	}
}