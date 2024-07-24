using System.Net;
using Iceshrimp.Backend.Core.Configuration;
using Microsoft.AspNetCore.Connections;

namespace Iceshrimp.Backend.Core.Extensions;

public static class WebHostExtensions
{
	public static void ConfigureKestrel(this IWebHostBuilder builder, IConfiguration configuration)
	{
		var workerConfig = configuration.GetSection("Worker").Get<Config.WorkerSection>();
		if (workerConfig?.WorkerType == Enums.WorkerType.QueueOnly)
		{
			builder.ConfigureKestrel(o => o.Listen(new NullEndPoint()))
			       .ConfigureServices(s => s.AddSingleton<IConnectionListenerFactory, NullListenerFactory>());

			return;
		}

		var config = configuration.GetSection("Instance").Get<Config.InstanceSection>() ??
		             throw new Exception("Failed to read Instance config section");

		if (config.ListenSocket == null) return;

		if (File.Exists(config.ListenSocket))
			File.Delete(config.ListenSocket);
		if (!Path.Exists(Path.GetDirectoryName(config.ListenSocket)))
			throw new Exception($"Failed to configure unix socket {config.ListenSocket}: Directory does not exist");

		builder.ConfigureKestrel(options => options.ListenUnixSocket(config.ListenSocket));
	}
}

public class NullEndPoint : EndPoint
{
	public override string ToString() => "<null>";
}

public class NullListenerFactory : IConnectionListenerFactory, IConnectionListenerFactorySelector
{
	public ValueTask<IConnectionListener> BindAsync(
		EndPoint endpoint, CancellationToken cancellationToken = new()
	)
	{
		if (endpoint is not NullEndPoint nep)
			throw new NotSupportedException($"{endpoint.GetType()} is not supported.");

		return ValueTask.FromResult<IConnectionListener>(new NullListener(nep));
	}

	public bool CanBind(EndPoint endpoint) => endpoint is NullEndPoint;
}

public class NullListener(NullEndPoint endpoint) : IConnectionListener
{
	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}

	public ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = new())
	{
		return ValueTask.FromResult<ConnectionContext?>(null);
	}

	public ValueTask UnbindAsync(CancellationToken cancellationToken = new())
	{
		return ValueTask.CompletedTask;
	}

	public EndPoint EndPoint => endpoint;
}