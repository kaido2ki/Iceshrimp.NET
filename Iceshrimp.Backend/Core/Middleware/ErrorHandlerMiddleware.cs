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
				if (verbosity > ExceptionVerbosity.Basic && ce.OverrideBasic)
					verbosity = ExceptionVerbosity.Basic;

				ctx.Response.StatusCode = (int)ce.StatusCode;
				await ctx.Response.WriteAsJsonAsync(new ErrorResponse {
					StatusCode = ctx.Response.StatusCode,
					Error      = verbosity >= ExceptionVerbosity.Basic ? ce.Error : ce.StatusCode.ToString(),
					Message    = verbosity >= ExceptionVerbosity.Basic ? ce.Message : null,
					Details    = verbosity == ExceptionVerbosity.Full ? ce.Details : null,
					Source     = verbosity == ExceptionVerbosity.Full ? type : null,
					RequestId  = ctx.TraceIdentifier
				});
				if (ce.Details != null)
					logger.LogDebug("Request {id} was rejected with {statusCode} {error}: {message} - {details}",
					                ctx.TraceIdentifier, (int)ce.StatusCode, ce.Error, ce.Message, ce.Details);
				else
					logger.LogDebug("Request {id} was rejected with {statusCode} {error}: {message}",
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

public class GracefulException(
	HttpStatusCode statusCode,
	string error,
	string message,
	string? details = null,
	bool overrideBasic = false)
	: Exception(message) {
	public readonly string?        Details       = details;
	public readonly string         Error         = error;
	public readonly bool           OverrideBasic = overrideBasic;
	public readonly HttpStatusCode StatusCode    = statusCode;

	public GracefulException(HttpStatusCode statusCode, string message, string? details = null) :
		this(statusCode, statusCode.ToString(), message, details) { }

	public GracefulException(string message, string? details = null) :
		this(HttpStatusCode.InternalServerError, HttpStatusCode.InternalServerError.ToString(), message, details) { }

	public static GracefulException UnprocessableEntity(string message, string? details = null) {
		return new GracefulException(HttpStatusCode.UnprocessableEntity, message, details);
	}

	public static GracefulException Forbidden(string message, string? details = null) {
		return new GracefulException(HttpStatusCode.Forbidden, message, details);
	}

	public static GracefulException Unauthorized(string message, string? details = null) {
		return new GracefulException(HttpStatusCode.Unauthorized, message, details);
	}

	public static GracefulException NotFound(string message, string? details = null) {
		return new GracefulException(HttpStatusCode.NotFound, message, details);
	}

	public static GracefulException BadRequest(string message, string? details = null) {
		return new GracefulException(HttpStatusCode.BadRequest, message, details);
	}

	public static GracefulException RecordNotFound() {
		return new GracefulException(HttpStatusCode.NotFound, "Record not found");
	}

	public static GracefulException MisdirectedRequest() {
		return new GracefulException(HttpStatusCode.MisdirectedRequest, HttpStatusCode.MisdirectedRequest.ToString(),
		                             "This server is not configured to respond to this request.", null, true);
	}
}

public enum ExceptionVerbosity {
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	None = 0,
	Basic = 1,
	Full  = 2
}