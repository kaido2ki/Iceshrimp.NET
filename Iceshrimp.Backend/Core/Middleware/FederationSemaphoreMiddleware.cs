using System.Net;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Helpers;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Middleware;

public class FederationSemaphoreMiddleware(
	IOptions<Config.PerformanceSection> config,
	IHostApplicationLifetime appLifetime
) : IMiddleware
{
	private readonly SemaphorePlus _semaphore = new(Math.Max(config.Value.FederationRequestHandlerConcurrency, 1));

	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
	{
		if (config.Value.FederationRequestHandlerConcurrency <= 0)
		{
			await next(ctx);
			return;
		}

		var attribute = ctx.GetEndpoint()?.Metadata.GetMetadata<FederationSemaphoreAttribute>();
		if (attribute == null)
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