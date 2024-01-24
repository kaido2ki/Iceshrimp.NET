using System.Net;
using Iceshrimp.Backend.Controllers.Schemas;

namespace Iceshrimp.Backend.Core.Middleware;

public class ErrorHandlerMiddleware(RequestDelegate next) {
	public async Task InvokeAsync(HttpContext ctx, ILogger<ErrorHandlerMiddleware> logger) {
		try {
			await next(ctx);
		}
		catch (Exception e) {
			ctx.Response.ContentType = "application/json";

			if (e is CustomException ce) {
				ctx.Response.StatusCode = (int)ce.StatusCode;
				await ctx.Response.WriteAsJsonAsync(new ErrorResponse {
					StatusCode = ctx.Response.StatusCode,
					Error      = ce.Error,
					Message    = ce.Message,
					RequestId  = ctx.TraceIdentifier
				});
				(ce.Logger ?? logger).LogDebug("Request {id} was rejected with {statusCode} {error} due to: {message}",
				                               ctx.TraceIdentifier, (int)ce.StatusCode, ce.Error, ce.Message);
			}
			else {
				ctx.Response.StatusCode = 500;
				await ctx.Response.WriteAsJsonAsync(new ErrorResponse {
					StatusCode = 500,
					Error      = "Internal Server Error",
					Message    = e.Message,
					RequestId  = ctx.TraceIdentifier
				});
				logger.LogError("Request {id} encountered an unexpected error: {exception}", ctx.TraceIdentifier,
				                e.ToString());
			}
		}
	}
}

//TODO: Find a better name for this class
public class CustomException(HttpStatusCode statusCode, string error, string message, ILogger? logger)
	: Exception(message) {
	public readonly string   Error  = error;
	public readonly ILogger? Logger = logger;

	public readonly HttpStatusCode StatusCode = statusCode;

	public CustomException(HttpStatusCode statusCode, string message, ILogger logger) :
		this(statusCode, statusCode.ToString(), message, logger) { }

	public CustomException(string message, ILogger logger) :
		this(HttpStatusCode.InternalServerError, HttpStatusCode.InternalServerError.ToString(), message, logger) { }

	[Obsolete("Please refactor this usage and specify the ILogger<CallingClass> constructor argument")]
	public CustomException(HttpStatusCode statusCode, string message) :
		this(statusCode, statusCode.ToString(), message, null) { }
}