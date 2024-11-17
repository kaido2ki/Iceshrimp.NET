using System.Net;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Middleware;

public class FederationSemaphoreMiddleware(
	IOptions<Config.PerformanceSection> config,
	IHostApplicationLifetime appLifetime
) : ConditionalMiddleware<FederationSemaphoreAttribute>, IMiddlewareService
{
	public static ServiceLifetime Lifetime => ServiceLifetime.Singleton;

	private readonly SemaphorePlus _semaphore = new(Math.Max(config.Value.FederationRequestHandlerConcurrency, 1));

	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
	{
		if (config.Value.FederationRequestHandlerConcurrency <= 0)
		{
			await next(ctx);
			return;
		}

		try
		{
			var cts = CancellationTokenSource
				.CreateLinkedTokenSource(ctx.RequestAborted, appLifetime.ApplicationStopping);
			await _semaphore.WaitAsync(cts.Token);
		}
		catch (OperationCanceledException)
		{
			throw new GracefulException(HttpStatusCode.ServiceUnavailable, "Service temporarily unavailable",
			                            "Please try again later", suppressLog: true);
		}

		try
		{
			await next(ctx);
		}
		finally
		{
			_semaphore.Release();
		}
	}
}

public class FederationSemaphoreAttribute : Attribute;