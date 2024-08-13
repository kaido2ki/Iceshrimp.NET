using Iceshrimp.Frontend.Core.Services;

namespace Iceshrimp.Frontend.Core.InMemoryLogger;

internal class InMemoryLogger (Func<InMemoryLoggerConfiguration> getCurrentConfig, InMemoryLogService logService) : ILogger
{
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		logService.Add(formatter(state, exception));
	}
	
	public bool IsEnabled(LogLevel logLevel)
	{
		return getCurrentConfig().LogLevel.HasFlag(logLevel);
	}

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;
}