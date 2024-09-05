namespace Iceshrimp.Frontend.Core.InMemoryLogger;

internal class InMemoryLoggerConfiguration
{
	public int      BufferSize { get; set; } = 1000;
	public LogLevel LogLevel   { get; set; } = LogLevel.Information;
}