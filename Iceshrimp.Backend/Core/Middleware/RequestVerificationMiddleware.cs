using Iceshrimp.Backend.Core.Configuration;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Middleware;

public class RequestVerificationMiddleware(IOptions<Config.InstanceSection> config, IHostEnvironment environment)
	: IMiddleware {
	private readonly bool _isDevelopment = environment.IsDevelopment();

	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next) {
		if (!IsValid(ctx.Request) && !_isDevelopment)
			throw GracefulException.MisdirectedRequest();

		await next(ctx);
	}

	public bool IsValid(HttpRequest rq) {
		if (rq.Host.Host == config.Value.WebDomain) return true;
		if (rq.Host.Host == config.Value.AccountDomain && rq.Path.StartsWithSegments("/.well-known")) {
			if (rq.Path == "/.well-known/webfinger") return true;
			if (rq.Path == "/.well-known/host-meta") return true;
			if (rq.Path == "/.well-known/nodeinfo") return true;
		}

		return false;
	}
}