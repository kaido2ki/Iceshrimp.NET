using System.Net;
using Iceshrimp.Backend.Controllers.Schemas;

namespace Iceshrimp.Backend.Core.Middleware;

public class ErrorHandlerMiddleware(ILoggerFactory loggerFactory) : IMiddleware {
	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next) {
		try {
			await next(ctx);
		}
		catch (Exception e) {
			ctx.Response.ContentType = "application/json";

			// Get the name of the class & function where the exception originated, falling back to this one
			var type = e.TargetSite?.DeclaringType?.FullName ?? typeof(ErrorHandlerMiddleware).FullName!;
			if (type.Contains('>'))
				type = type[..(type.IndexOf('>') + 1)];

			var logger = loggerFactory.CreateLogger(type);

			if (e is GracefulException ce) {
				ctx.Response.StatusCode = (int)ce.StatusCode;
				await ctx.Response.WriteAsJsonAsync(new ErrorResponse {
					StatusCode = ctx.Response.StatusCode,
					Error      = ce.Error,
					Message    = ce.Message,
					RequestId  = ctx.TraceIdentifier
				});
				logger.LogDebug("Request {id} was rejected with {statusCode} {error} due to: {message}",
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
				//TODO: use the overload that takes an exception instead of printing it ourselves
				logger.LogError("Request {id} encountered an unexpected error: {exception}", ctx.TraceIdentifier,
				                e.ToString());
			}
		}
	}
}

//TODO: Allow specifying differing messages for api response and server logs
//TODO: Make this configurable
public class GracefulException(HttpStatusCode statusCode, string error, string message) : Exception(message) {
	public readonly string         Error      = error;
	public readonly HttpStatusCode StatusCode = statusCode;

	public GracefulException(HttpStatusCode statusCode, string message) :
		this(statusCode, statusCode.ToString(), message) { }

	public GracefulException(string message) :
		this(HttpStatusCode.InternalServerError, HttpStatusCode.InternalServerError.ToString(), message) { }
}