namespace Iceshrimp.Frontend.Core.InMemoryLogger;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

internal static class InMemoryLoggerExtension
{
	public static void AddInMemoryLogger(
		this ILoggingBuilder builder)
	{
		builder.AddConfiguration();
		builder.Services.TryAddSingleton<InMemoryLogService>();
		builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, InMemoryLoggerProvider>());
		LoggerProviderOptions.RegisterProviderOptions<InMemoryLoggerConfiguration, InMemoryLoggerProvider>(builder.Services);
	}
	
	public static void AddInMemoryLogger(
		this ILoggingBuilder builder,
		Action<InMemoryLoggerConfiguration> configure
	)
	{
		builder.Services.TryAddSingleton<InMemoryLogService>();
		builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, InMemoryLoggerProvider>());
		builder.Services.Configure(configure);
	}
}

	