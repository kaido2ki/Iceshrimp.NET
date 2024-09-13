using Iceshrimp.Frontend.Core.Miscellaneous;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Frontend.Core.InMemoryLogger;

internal class InMemoryLogService(IOptions<InMemoryLoggerConfiguration> configuration)
{
	private readonly LogBuffer _buffer = new(configuration.Value.BufferSize);

	public void Add(string logline)
	{
		_buffer.Add(logline);
	}

	public List<string> GetLogs()
	{
		return _buffer.AsList();
	}
}