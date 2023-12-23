using Iceshrimp.Backend.Controllers.Schemas;
using Microsoft.AspNetCore.Mvc;

namespace Iceshrimp.Backend.Controllers;

[Produces("application/json")]
public class FallbackController : Controller {
	[ProducesResponseType(StatusCodes.Status501NotImplemented, Type = typeof(ErrorResponse))]
	public IActionResult FallbackAction() {
		return StatusCode(501, new ErrorResponse {
			StatusCode = 501,
			Error      = "Not implemented",
			Message    = "This API method has not been implemented"
		});
	}
}