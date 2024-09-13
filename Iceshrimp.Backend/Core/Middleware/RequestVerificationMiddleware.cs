using Iceshrimp.Backend.Core.Configuration;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Middleware;

public class RequestVerificationMiddleware(
	IOptions<Config.InstanceSection> config,
	IHostEnvironment environment,
	ILogger<RequestVerificationMiddleware> logger
) : IMiddleware
{
	private readonly bool _isDevelopment = environment.IsDevelopment();

	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
	{
		if (!IsValid(ctx.Request))
			throw GracefulException.MisdirectedRequest();

		await next(ctx);
	}

	public bool IsValid(HttpRequest rq)
	{
		if (rq.Host.Host == config.Value.WebDomain) return true;
		if (config.Value.AdditionalDomainsArray.Contains(rq.Host.Host)) return true;
		if (rq.Host.Host == config.Value.AccountDomain && rq.Path.StartsWithSegments("/.well-known"))
		{
			if (rq.Path == "/.well-known/webfinger") return true;
			if (rq.Path == "/.well-known/host-meta") return true;
			if (rq.Path == "/.well-known/nodeinfo") return true;
		}

		if (_isDevelopment) return true;

		if (rq.Host.Host == config.Value.AccountDomain)
			logger.LogWarning("Received invalid request for account domain ({host}). The request path '{path}' is only valid for the web domain. Please ensure your reverse proxy is configured correctly.",
			                  rq.Host.Host, rq.Path);
		else
			logger.LogWarning("Received invalid request for host '{host}', please ensure your reverse proxy is configured correctly.",
			                  rq.Host.Host);

		return false;
	}
}