using Microsoft.Extensions.Options;

namespace Iceshrimp.Frontend.Core.InMemoryLogger;

[ProviderAlias("InMemory")]
internal class InMemoryLoggerProvider(IOptions<InMemoryLoggerConfiguration> config, InMemoryLogService logService)
	: ILoggerProvider
{
	private InMemoryLogger _logger = new(config, logService);

	public void Dispose() { }

	public ILogger CreateLogger(string categoryName)
	{
		return _logger;
	}
}