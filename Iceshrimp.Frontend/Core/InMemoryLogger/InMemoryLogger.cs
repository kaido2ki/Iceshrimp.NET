using Microsoft.Extensions.Options;

namespace Iceshrimp.Frontend.Core.InMemoryLogger;

internal class InMemoryLogger (IOptions<InMemoryLoggerConfiguration> config, InMemoryLogService logService) : ILogger
{
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		logService.Add(formatter(state, exception));
	}
	
	public bool IsEnabled(LogLevel logLevel)
	{
		return config.Value.LogLevel.HasFlag(logLevel);
	}

	public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;
}