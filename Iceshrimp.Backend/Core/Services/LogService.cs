using Iceshrimp.Backend.Core.Helpers;
using Serilog.Core;
using Serilog.Events;

namespace Iceshrimp.Backend.Core.Services;

public class LogService(ILogStorageProvider logStorage) : ILogEventSink
{
	public IQueryable<LogEntry> Logs => logStorage.Logs;

	public void Emit(LogEvent logEvent)
	{
		logStorage.Append(new LogEntry(logEvent));
	}
}

public interface ILogStorageProvider
{
	public IQueryable<LogEntry> Logs { get; }

	public void Append(LogEntry entry);
}

public class InMemoryLogStorageProvider : ILogStorageProvider
{
	private readonly WriteLockingList<LogEntry> _logs = [];

	//private readonly IReadOnlyDictionary<LogLevel, int> _maxEntries =
	//	Enum.GetValues<LogLevel>()
	//	    .ToDictionary(p => p, _ => 100)
	//	    .AsReadOnly();

	public void Append(LogEntry entry)
	{
		_logs.Add(entry);
		_logs.Trim(100);
		//_logs.Trim(_maxEntries[entry.Level]);
	}

	public IQueryable<LogEntry> Logs => _logs.AsQueryable().Reverse();
}

public class LogEntry(LogEvent logEvent)
{
	public DateTime      Timestamp => logEvent.Timestamp.LocalDateTime;
	public LogEventLevel Level     => logEvent.Level;
	public string        Category  => logEvent.Properties["SourceContext"].ToString().Trim('"');
	public string        Message   => logEvent.RenderMessage();
	public string        Template  => logEvent.MessageTemplate.Text;
}