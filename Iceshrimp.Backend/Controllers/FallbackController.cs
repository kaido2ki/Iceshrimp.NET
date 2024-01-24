using System.Net;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace Iceshrimp.Backend.Controllers;

[Produces("application/json")]
public class FallbackController(ILogger<FallbackController> logger) : Controller {
	[ProducesResponseType(StatusCodes.Status501NotImplemented, Type = typeof(ErrorResponse))]
	public IActionResult FallbackAction() {
		throw new CustomException(HttpStatusCode.NotImplemented, "This API method has not been implemented");
	}
}