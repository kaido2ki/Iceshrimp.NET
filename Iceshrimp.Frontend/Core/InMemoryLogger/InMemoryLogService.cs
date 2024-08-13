namespace Iceshrimp.Frontend.Core.InMemoryLogger;

internal class InMemoryLogService
{
	private List<string> LogBuffer { get; } = [];
	private int          _bufferCapacity = 100;

	public void Add(string logline)
	{
		if (LogBuffer.Count > _bufferCapacity - 1) LogBuffer.RemoveAt(0);
		LogBuffer.Add(logline);
	}

	public void ResizeBuffer(int newCapacity)
	{
		if (newCapacity > _bufferCapacity)
		{
			LogBuffer.RemoveRange(0, newCapacity - _bufferCapacity);
		}
		_bufferCapacity = newCapacity;
	}
	
	public IReadOnlyCollection<string> GetLogs()
	{
		return LogBuffer.AsReadOnly();
	}
}