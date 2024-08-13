namespace Iceshrimp.Frontend.Core.InMemoryLogger;

internal class InMemoryLoggerConfiguration
{
	public int      BufferSize { get; set; } = 100;
	public LogLevel LogLevel   { get; set; } = LogLevel.Information;
}