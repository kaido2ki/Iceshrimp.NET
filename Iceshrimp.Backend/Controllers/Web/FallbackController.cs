using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Iceshrimp.Backend.Controllers.Web;

[Produces(MediaTypeNames.Application.Json)]
public class FallbackController : ControllerBase
{
	[EnableCors("fallback")]
	[ProducesErrors(HttpStatusCode.NotImplemented)]
	public IActionResult FallbackAction()
	{
		throw new GracefulException(HttpStatusCode.NotImplemented, "This API method has not been implemented",
		                            Request.Path);
	}
}