using System.Net;
using System.Net.Mime;
using Iceshrimp.Shared.Schemas;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Iceshrimp.Backend.Controllers;

[Produces(MediaTypeNames.Application.Json)]
public class FallbackController : ControllerBase
{
	[EnableCors("fallback")]
	[ProducesResponseType(StatusCodes.Status501NotImplemented, Type = typeof(ErrorResponse))]
	public IActionResult FallbackAction()
	{
		throw new GracefulException(HttpStatusCode.NotImplemented,
		                            "This API method has not been implemented", Request.Path);
	}
}