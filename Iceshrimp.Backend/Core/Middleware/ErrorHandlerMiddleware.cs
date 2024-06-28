using System.Diagnostics.CodeAnalysis;
using System.Net;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Shared.Schemas;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Middleware;

public class ErrorHandlerMiddleware(
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
	IOptionsSnapshot<Config.SecuritySection> options,
	ILoggerFactory loggerFactory
)
	: IMiddleware
{
	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
	{
		try
		{
			await next(ctx);
		}
		catch (Exception e)
		{
			// Get the name of the class & function where the exception originated, falling back to this one
			var type = e.TargetSite?.DeclaringType?.FullName ?? typeof(ErrorHandlerMiddleware).FullName!;
			if (type.Contains('>'))
				type = type[..(type.IndexOf('>') + 1)];

			var logger    = loggerFactory.CreateLogger(type);
			var verbosity = options.Value.ExceptionVerbosity;

			if (ctx.Response.HasStarted)
			{
				if (e is GracefulException earlyCe)
				{
					var level = earlyCe.SuppressLog ? LogLevel.Trace : LogLevel.Debug;
					if (earlyCe.Details != null)
					{
						logger.Log(level, "Request {id} was rejected with {statusCode} {error}: {message} - {details}",
						           ctx.TraceIdentifier, (int)earlyCe.StatusCode, earlyCe.Error, earlyCe.Message,
						           earlyCe.Details);
					}
					else
					{
						logger.Log(level, "Request {id} was rejected with {statusCode} {error}: {message}",
						           ctx.TraceIdentifier, (int)earlyCe.StatusCode, earlyCe.Error, earlyCe.Message);
					}
				}
				else
				{
					logger.LogError("Request {id} encountered an unexpected error: {exception}", ctx.TraceIdentifier,
					                e.ToString());
				}

				return;
			}

			ctx.Response.ContentType = "application/json";

			var isMastodon = ctx.GetEndpoint()?.Metadata.GetMetadata<MastodonApiControllerAttribute>() != null;

			if (e is GracefulException ce)
			{
				if (ce.StatusCode == HttpStatusCode.Accepted)
				{
					ctx.Response.StatusCode = (int)ce.StatusCode;
					await ctx.Response.CompleteAsync();
					return;
				}

				if (verbosity > ExceptionVerbosity.Basic && ce.OverrideBasic)
					verbosity = ExceptionVerbosity.Basic;

				ctx.Response.StatusCode        = (int)ce.StatusCode;
				ctx.Response.Headers.RequestId = ctx.TraceIdentifier;

				if (isMastodon)
					await ctx.Response.WriteAsJsonAsync(new MastodonErrorResponse
					{
						Error = verbosity >= ExceptionVerbosity.Basic
							? ce.Message
							: ce.StatusCode.ToString(),
						Description = verbosity >= ExceptionVerbosity.Basic
							? ce.Details
							: null
					});
				else
					await ctx.Response.WriteAsJsonAsync(new ErrorResponse
					{
						StatusCode = ctx.Response.StatusCode,
						Error =
							verbosity >= ExceptionVerbosity.Basic
								? ce.Error
								: ce.StatusCode.ToString(),
						Message =
							verbosity >= ExceptionVerbosity.Basic
								? ce.Message
								: null,
						Details =
							verbosity == ExceptionVerbosity.Full
								? ce.Details
								: null,
						Source = verbosity == ExceptionVerbosity.Full
							? type
							: null,
						RequestId = ctx.TraceIdentifier
					});

				var level = ce.SuppressLog ? LogLevel.Trace : LogLevel.Debug;

				if (ce.Details != null)
					logger.Log(level,
					           "Request {id} was rejected by {source} with {statusCode} {error}: {message} - {details}",
					           ctx.TraceIdentifier, type, (int)ce.StatusCode, ce.Error, ce.Message, ce.Details);
				else
					logger.Log(level, "Request {id} was rejected by {source} with {statusCode} {error}: {message}",
					           ctx.TraceIdentifier, type, (int)ce.StatusCode, ce.Error, ce.Message);
			}
			else
			{
				ctx.Response.StatusCode        = 500;
				ctx.Response.Headers.RequestId = ctx.TraceIdentifier;
				await ctx.Response.WriteAsJsonAsync(new ErrorResponse
				{
					StatusCode = 500,
					Error      = "Internal Server Error",
					Message =
						verbosity >= ExceptionVerbosity.Basic
							? e.Message
							: null,
					Source    = verbosity == ExceptionVerbosity.Full ? type : null,
					RequestId = ctx.TraceIdentifier
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
	bool suppressLog = false,
	bool overrideBasic = false
) : Exception(message)
{
	public readonly string?        Details       = details;
	public readonly string         Error         = error;
	public readonly bool           OverrideBasic = overrideBasic;
	public readonly HttpStatusCode StatusCode    = statusCode;
	public readonly bool           SuppressLog   = suppressLog;

	public GracefulException(HttpStatusCode statusCode, string message, string? details = null) :
		this(statusCode, statusCode.ToString(), message, details) { }

	public GracefulException(string message, string? details = null) :
		this(HttpStatusCode.InternalServerError, HttpStatusCode.InternalServerError.ToString(), message, details) { }

	public static GracefulException UnprocessableEntity(string message, string? details = null) =>
		new(HttpStatusCode.UnprocessableEntity, message, details);

	public static GracefulException Forbidden(string message, string? details = null) =>
		new(HttpStatusCode.Forbidden, message, details);

	public static GracefulException Unauthorized(string message, string? details = null) =>
		new(HttpStatusCode.Unauthorized, message, details);

	public static GracefulException NotFound(string message, string? details = null) =>
		new(HttpStatusCode.NotFound, message, details);

	public static GracefulException BadRequest(string message, string? details = null) =>
		new(HttpStatusCode.BadRequest, message, details);

	public static GracefulException RequestTimeout(string message, string? details = null) =>
		new(HttpStatusCode.RequestTimeout, message, details);

	public static GracefulException RecordNotFound() => new(HttpStatusCode.NotFound, "Record not found");

	public static GracefulException MisdirectedRequest() =>
		new(HttpStatusCode.MisdirectedRequest, HttpStatusCode.MisdirectedRequest.ToString(),
		    "This server is not configured to respond to this request.", null, true, true);

	/// <summary>
	///     This is intended for cases where no error occured, but the request needs to be aborted early (e.g. WebFinger
	///     returning 410 Gone)
	/// </summary>
	public static GracefulException Accepted(string message) =>
		new(HttpStatusCode.Accepted, HttpStatusCode.Accepted.ToString(), message, suppressLog: true);
}

public class AuthFetchException(HttpStatusCode statusCode, string message, string? details = null)
	: GracefulException(statusCode, message, details)
{
	public static AuthFetchException NotFound(string message) =>
		new(HttpStatusCode.NotFound, HttpStatusCode.NotFound.ToString(), message);
}

public class LocalFetchException(string uri)
	: GracefulException(HttpStatusCode.UnprocessableEntity, "Refusing to fetch activity from local domain")
{
	public string Uri => uri;
}

public class InstanceBlockedException(string uri, string? host = null)
	: GracefulException(HttpStatusCode.UnprocessableEntity, "Instance is blocked")
{
	public string? Host
	{
		get
		{
			if (host != null) return host;
			return System.Uri.TryCreate(uri, UriKind.Absolute, out var res) ? res.Host : null;
		}
	}

	public string Uri => uri;
}

public enum ExceptionVerbosity
{
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	None = 0,
	Basic = 1,
	Full  = 2
}