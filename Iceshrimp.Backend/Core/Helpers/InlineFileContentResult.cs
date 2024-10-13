using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Iceshrimp.Backend.Core.Helpers;

public class InlineFileContentResult(
	byte[] fileContents,
	string contentType
) : FileContentResult(fileContents, contentType)
{
	public InlineFileContentResult(
		byte[] fileContents, string contentType, string? fileDownloadName, bool enableRangeProcessing
	) : this(fileContents, contentType)
	{
		FileDownloadName      = fileDownloadName;
		EnableRangeProcessing = enableRangeProcessing;
	}

	public override Task ExecuteResultAsync(ActionContext context)
	{
		var contentDispositionHeader = new ContentDispositionHeaderValue("inline");
		contentDispositionHeader.SetHttpFileName(FileDownloadName);
		context.HttpContext.Response.Headers[HeaderNames.ContentDisposition] = contentDispositionHeader.ToString();

		FileDownloadName = null;
		return base.ExecuteResultAsync(context);
	}
}