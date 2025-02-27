using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Iceshrimp.Backend.Controllers.Shared.Attributes;

public class NoRequestSizeLimitAttribute : Attribute, IFormOptionsMetadata, IResourceFilter
{
	public void OnResourceExecuting(ResourceExecutingContext context)
	{
		var feature = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>() ??
		              throw new Exception("Failed to get IHttpMaxRequestBodySizeFeature");
		feature.MaxRequestBodySize = long.MaxValue;
	}

	public void OnResourceExecuted(ResourceExecutedContext context) { }

	public bool? BufferBody                   => null;
	public int?  MemoryBufferThreshold        => null;
	public long? BufferBodyLengthLimit        => long.MaxValue;
	public int?  ValueCountLimit              => null;
	public int?  KeyLengthLimit               => null;
	public int?  ValueLengthLimit             => null;
	public int?  MultipartBoundaryLengthLimit => null;
	public int?  MultipartHeadersCountLimit   => null;
	public int?  MultipartHeadersLengthLimit  => null;
	public long? MultipartBodyLengthLimit     => long.MaxValue;
}