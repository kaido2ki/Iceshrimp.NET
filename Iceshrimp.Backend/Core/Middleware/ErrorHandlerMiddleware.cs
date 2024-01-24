using System.Diagnostics.CodeAnalysis;
using System.Net;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Middleware;

//TODO: If we make this a scoped service instead of a singleton, we can use IOptionsSnapshot
public class ErrorHandlerMiddleware(IOptions<Config.SecuritySection> options, ILoggerFactory loggerFactory)
	: IMiddleware {
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

			var logger    = loggerFactory.CreateLogger(type);
			var verbosity = options.Value.ExceptionVerbosity;

			if (e is GracefulException ce) {
				ctx.Response.StatusCode = (int)ce.StatusCode;
				await ctx.Response.WriteAsJsonAsync(new ErrorResponse {
					StatusCode = ctx.Response.StatusCode,
					Error      = verbosity >= ExceptionVerbosity.Basic ? ce.Error : ce.StatusCode.ToString(),
					Message    = verbosity >= ExceptionVerbosity.Basic ? ce.Message : null,
					Details    = verbosity == ExceptionVerbosity.Full ? ce.Details : null,
					Source     = verbosity == ExceptionVerbosity.Full ? type : null,
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
					Message    = verbosity >= ExceptionVerbosity.Basic ? e.Message : null,
					Source     = verbosity == ExceptionVerbosity.Full ? type : null,
					RequestId  = ctx.TraceIdentifier
				});
				//TODO: use the overload that takes an exception instead of printing it ourselves
				logger.LogError("Request {id} encountered an unexpected error: {exception}", ctx.TraceIdentifier,
				                e.ToString());
			}
		}
	}
}

public class GracefulException(HttpStatusCode statusCode, string error, string message) : Exception(message) {
	public readonly string?        Details    = null; //TODO: implement this
	public readonly string         Error      = error;
	public readonly HttpStatusCode StatusCode = statusCode;

	public GracefulException(HttpStatusCode statusCode, string message) :
		this(statusCode, statusCode.ToString(), message) { }

	public GracefulException(string message) :
		this(HttpStatusCode.InternalServerError, HttpStatusCode.InternalServerError.ToString(), message) { }
}

public enum ExceptionVerbosity {
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	None = 0,
	Basic = 1,
	Full  = 2
}