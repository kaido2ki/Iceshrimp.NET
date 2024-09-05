using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Shared.Attributes;

public class MaxRequestSizeIsMaxUploadSize : Attribute, IResourceFilter
{
	public void OnResourceExecuting(ResourceExecutingContext context)
	{
		var feature = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>() ??
		              throw new Exception("Failed to get IHttpMaxRequestBodySizeFeature");
		var config = context.HttpContext.RequestServices.GetRequiredService<IOptionsSnapshot<Config.StorageSection>>();
		feature.MaxRequestBodySize = config.Value.MaxUploadSizeBytes;

		if (context.HttpContext.Request.ContentLength > config.Value.MaxUploadSizeBytes)
			throw GracefulException.RequestEntityTooLarge("Attachment is too large.",
			                                              $"The media upload size limit is set to {config.Value.MaxUploadSizeBytes} bytes.");
	}

	public void OnResourceExecuted(ResourceExecutedContext context) { }
}