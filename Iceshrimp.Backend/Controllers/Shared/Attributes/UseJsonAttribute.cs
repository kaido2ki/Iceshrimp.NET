using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Shared.Attributes;

public abstract class UseJsonAttribute : Attribute, IAsyncActionFilter
{
	public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		return next();
	}
}

public class UseNewtonsoftJsonAttribute : UseJsonAttribute;

internal class JsonInputMultiFormatter : TextInputFormatter
{
	public JsonInputMultiFormatter()
	{
		SupportedEncodings.Add(UTF8EncodingWithoutBOM);
		SupportedEncodings.Add(UTF16EncodingLittleEndian);
		SupportedMediaTypes.Add("text/json");
		SupportedMediaTypes.Add("application/json");
		SupportedMediaTypes.Add("application/*+json");
	}

	public override async Task<InputFormatterResult> ReadRequestBodyAsync(
		InputFormatterContext context, Encoding encoding
	)
	{
		var mvcOpt = context.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>().Value;
		var formatters = mvcOpt.InputFormatters;
		TextInputFormatter? formatter;

		var endpoint = context.HttpContext.GetEndpoint();
		if (endpoint?.Metadata.GetMetadata<UseNewtonsoftJsonAttribute>() != null)
			// We can't use OfType<NewtonsoftJsonInputFormatter> because NewtonsoftJsonPatchInputFormatter exists
			formatter = (NewtonsoftJsonInputFormatter?)formatters
				.FirstOrDefault(f => typeof(NewtonsoftJsonInputFormatter) == f.GetType());
		else
			// Default to System.Text.Json
			formatter = formatters.OfType<SystemTextJsonInputFormatter>().FirstOrDefault();

		if (formatter == null) throw new Exception("Failed to resolve formatter");

		var result = await formatter.ReadRequestBodyAsync(context, encoding);
		return result;
	}
}

internal class JsonOutputMultiFormatter : TextOutputFormatter
{
	public JsonOutputMultiFormatter()
	{
		SupportedEncodings.Add(Encoding.UTF8);
		SupportedEncodings.Add(Encoding.Unicode);
		SupportedMediaTypes.Add("text/json");
		SupportedMediaTypes.Add("application/json");
		SupportedMediaTypes.Add("application/*+json");
	}

	public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
	{
		var mvcOpt = context.HttpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>().Value;
		var formatters = mvcOpt.OutputFormatters;
		TextOutputFormatter? formatter;

		var endpoint = context.HttpContext.GetEndpoint();
		if (endpoint?.Metadata.GetMetadata<UseNewtonsoftJsonAttribute>() != null)
			formatter = formatters.OfType<NewtonsoftJsonOutputFormatter>().FirstOrDefault();
		else
			// Default to System.Text.Json
			formatter = formatters.OfType<SystemTextJsonOutputFormatter>().FirstOrDefault();

		if (formatter == null) throw new Exception("Failed to resolve formatter");

		await formatter.WriteResponseBodyAsync(context, selectedEncoding);
	}
}