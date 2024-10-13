using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Iceshrimp.Backend.Core.Helpers;

public class InlineFileStreamResult(Stream fileStream, string contentType) : FileStreamResult(fileStream, contentType)
{
	public InlineFileStreamResult(
		Stream fileStream, string contentType, string? fileDownloadName, bool enableRangeProcessing
	) : this(fileStream, contentType)
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