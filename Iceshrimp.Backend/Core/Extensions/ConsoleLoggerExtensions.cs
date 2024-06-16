using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Iceshrimp.Backend.Core.Extensions;

public static class ConsoleLoggerExtensions
{
	public static ILoggingBuilder AddCustomConsoleFormatter(this ILoggingBuilder builder)
	{
		if (Environment.GetEnvironmentVariable("INVOCATION_ID") is not null)
		{
			builder.AddConsole(options => options.FormatterName = "systemd-custom")
			       .AddConsoleFormatter<CustomSystemdConsoleFormatter, ConsoleFormatterOptions>();
		}
		else
		{
			builder.AddConsole(options => options.FormatterName = "custom")
			       .AddConsoleFormatter<CustomFormatter, ConsoleFormatterOptions>();
		}

		return builder;
	}
}

/*
 * This is a slightly modified version of Microsoft's SimpleConsoleFormatter.
 * Changes mainly concern handling of line breaks.
 * Adapted under MIT from https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Logging.Console/src/SimpleConsoleFormatter.cs
 */

#region Logger implementation

file static class TextWriterExtensions
{
	private const string DefaultForegroundColor = "\x1B[39m\x1B[22m"; // reset to default foreground color
	private const string DefaultBackgroundColor = "\x1B[49m";         // reset to the background color

	public static void WriteColoredMessage(
		this TextWriter textWriter, string message, ConsoleColor? background,
		ConsoleColor? foreground
	)
	{
		if (background.HasValue)
			textWriter.Write(GetBackgroundColorEscapeCode(background.Value));
		if (foreground.HasValue)
			textWriter.Write(GetForegroundColorEscapeCode(foreground.Value));

		textWriter.Write(message);

		if (foreground.HasValue)
			textWriter.Write(DefaultForegroundColor);
		if (background.HasValue)
			textWriter.Write(DefaultBackgroundColor);
	}

	private static string GetForegroundColorEscapeCode(ConsoleColor color)
	{
		return color switch
		{
			ConsoleColor.Black       => "\x1B[30m",
			ConsoleColor.DarkRed     => "\x1B[31m",
			ConsoleColor.DarkGreen   => "\x1B[32m",
			ConsoleColor.DarkYellow  => "\x1B[33m",
			ConsoleColor.DarkBlue    => "\x1B[34m",
			ConsoleColor.DarkMagenta => "\x1B[35m",
			ConsoleColor.DarkCyan    => "\x1B[36m",
			ConsoleColor.Gray        => "\x1B[37m",
			ConsoleColor.Red         => "\x1B[1m\x1B[31m",
			ConsoleColor.Green       => "\x1B[1m\x1B[32m",
			ConsoleColor.Yellow      => "\x1B[1m\x1B[33m",
			ConsoleColor.Blue        => "\x1B[1m\x1B[34m",
			ConsoleColor.Magenta     => "\x1B[1m\x1B[35m",
			ConsoleColor.Cyan        => "\x1B[1m\x1B[36m",
			ConsoleColor.White       => "\x1B[1m\x1B[37m",
			_                        => DefaultForegroundColor // default foreground color
		};
	}

	private static string GetBackgroundColorEscapeCode(ConsoleColor color)
	{
		return color switch
		{
			ConsoleColor.Black       => "\x1B[40m",
			ConsoleColor.DarkRed     => "\x1B[41m",
			ConsoleColor.DarkGreen   => "\x1B[42m",
			ConsoleColor.DarkYellow  => "\x1B[43m",
			ConsoleColor.DarkBlue    => "\x1B[44m",
			ConsoleColor.DarkMagenta => "\x1B[45m",
			ConsoleColor.DarkCyan    => "\x1B[46m",
			ConsoleColor.Gray        => "\x1B[47m",
			_                        => DefaultBackgroundColor // Use default background color
		};
	}
}

file static class ConsoleUtils
{
	private static volatile int _sEmitAnsiColorCodes = -1;

	public static bool EmitAnsiColorCodes
	{
		get
		{
			var emitAnsiColorCodes = _sEmitAnsiColorCodes;
			if (emitAnsiColorCodes != -1)
			{
				return Convert.ToBoolean(emitAnsiColorCodes);
			}

			var enabled = !Console.IsOutputRedirected;

			if (enabled)
			{
				enabled = Environment.GetEnvironmentVariable("NO_COLOR") is null;
			}
			else
			{
				var envVar = Environment.GetEnvironmentVariable("FORCE_COLOR") ??
				             Environment.GetEnvironmentVariable("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION");
				enabled = envVar is not null &&
				          (envVar == "1" || envVar.Equals("true", StringComparison.OrdinalIgnoreCase));
			}

			_sEmitAnsiColorCodes = Convert.ToInt32(enabled);
			return enabled;
		}
	}
}

file sealed class CustomFormatter() : ConsoleFormatter("custom")
{
	private const string LoglevelPadding = ": ";

	private static readonly string MessagePadding =
		new(' ', GetLogLevelString(LogLevel.Information).Length + LoglevelPadding.Length);

	private static readonly string NewLineWithMessagePadding = Environment.NewLine + MessagePadding;

	public override void Write<TState>(
		in LogEntry<TState> logEntry,
		IExternalScopeProvider? scopeProvider,
		TextWriter textWriter
	)
	{
		var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (logEntry.Exception == null && message == null)
		{
			return;
		}

		var logLevel       = logEntry.LogLevel;
		var logLevelColors = GetLogLevelConsoleColors(logLevel);
		var logLevelString = GetLogLevelString(logLevel);

		textWriter.WriteColoredMessage(logLevelString, logLevelColors.Background, logLevelColors.Foreground);

		CreateDefaultLogMessage(textWriter, logEntry, message);
	}

	private static void CreateDefaultLogMessage<TState>(
		TextWriter textWriter, in LogEntry<TState> logEntry,
		string message
	)
	{
		var eventId    = logEntry.EventId.Id;
		var exception  = logEntry.Exception;
		var singleLine = !message.Contains('\n') && exception == null;

		textWriter.Write(LoglevelPadding);
		textWriter.Write(logEntry.Category);
		textWriter.Write('[');

		Span<char> span = stackalloc char[10];
		if (eventId.TryFormat(span, out var charsWritten))
			textWriter.Write(span[..charsWritten]);
		else
			textWriter.Write(eventId.ToString());

		textWriter.Write(']');
		if (!singleLine) textWriter.Write(Environment.NewLine);

		WriteMessage(textWriter, message, singleLine);

		if (exception != null)
		{
			WriteMessage(textWriter, exception.ToString(), singleLine);
		}

		if (singleLine)
		{
			textWriter.Write(Environment.NewLine);
		}
	}

	private static void WriteMessage(TextWriter textWriter, string message, bool singleLine)
	{
		if (string.IsNullOrEmpty(message)) return;
		if (singleLine)
		{
			textWriter.Write(' ');
			WriteReplacing(textWriter, Environment.NewLine, " ", message);
		}
		else
		{
			textWriter.Write(MessagePadding);
			WriteReplacing(textWriter, Environment.NewLine, NewLineWithMessagePadding, message);
			textWriter.Write(Environment.NewLine);
		}

		return;

		static void WriteReplacing(TextWriter writer, string oldValue, string newValue, string message)
		{
			var newMessage = message.Replace(oldValue, newValue);
			writer.Write(newMessage);
		}
	}

	private static string GetLogLevelString(LogLevel logLevel)
	{
		return logLevel switch
		{
			LogLevel.Trace       => "trce",
			LogLevel.Debug       => "dbug",
			LogLevel.Information => "info",
			LogLevel.Warning     => "warn",
			LogLevel.Error       => "fail",
			LogLevel.Critical    => "crit",
			LogLevel.None        => throw new ArgumentOutOfRangeException(nameof(logLevel)),
			_                    => throw new ArgumentOutOfRangeException(nameof(logLevel))
		};
	}

	private static ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
	{
		if (!ConsoleUtils.EmitAnsiColorCodes)
		{
			return new ConsoleColors(null, null);
		}

		return logLevel switch
		{
			LogLevel.Trace       => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
			LogLevel.Debug       => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
			LogLevel.Information => new ConsoleColors(ConsoleColor.DarkGreen, ConsoleColor.Black),
			LogLevel.Warning     => new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black),
			LogLevel.Error       => new ConsoleColors(ConsoleColor.Black, ConsoleColor.DarkRed),
			LogLevel.Critical    => new ConsoleColors(ConsoleColor.White, ConsoleColor.DarkRed),
			_                    => new ConsoleColors(null, null)
		};
	}

	private readonly struct ConsoleColors(ConsoleColor? foreground, ConsoleColor? background)
	{
		public ConsoleColor? Foreground { get; } = foreground;
		public ConsoleColor? Background { get; } = background;
	}
}

file sealed class CustomSystemdConsoleFormatter() : ConsoleFormatter("systemd-custom")
{
	private static readonly string MessagePadding            = new(' ', 6);
	private static readonly string NewLineWithMessagePadding = Environment.NewLine + MessagePadding;

	public override void Write<TState>(
		in LogEntry<TState> logEntry,
		IExternalScopeProvider? scopeProvider,
		TextWriter textWriter
	)
	{
		var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (logEntry.Exception == null && message == null) return;
		var logLevel             = logEntry.LogLevel;
		var category             = logEntry.Category;
		var id                   = logEntry.EventId.Id;
		var exception            = logEntry.Exception;
		var singleLine           = !message.Contains('\n') && exception == null;
		var syslogSeverityString = GetSyslogSeverityString(logLevel);
		textWriter.Write(syslogSeverityString);

		textWriter.Write(category);
		textWriter.Write('[');
		textWriter.Write(id);
		textWriter.Write(']');
		if (!singleLine) textWriter.Write(Environment.NewLine);

		if (!string.IsNullOrEmpty(message))
			WriteMessage(textWriter, message, logLevel, singleLine);

		if (exception != null)
			WriteMessage(textWriter, exception.ToString(), logLevel, singleLine);
	}

	private static void WriteMessage(TextWriter textWriter, string message, LogLevel logLevel, bool singleLine)
	{
		if (string.IsNullOrEmpty(message)) return;
		if (singleLine)
		{
			textWriter.Write(' ');
			WriteReplacing(textWriter, Environment.NewLine, " ", message);
		}
		else
		{
			textWriter.Write(MessagePadding);
			WriteReplacing(textWriter, Environment.NewLine,
			               Environment.NewLine + GetSyslogSeverityIndicatorString(logLevel) + MessagePadding, message);
		}

		textWriter.Write(Environment.NewLine);
		return;

		static void WriteReplacing(TextWriter writer, string oldValue, string newValue, string message)
		{
			var newMessage = message.Replace(oldValue, newValue);
			writer.Write(newMessage);
		}
	}

	private static string GetSyslogSeverityString(LogLevel logLevel)
	{
		return logLevel switch
		{
			LogLevel.Trace       => "<7>trce: ",
			LogLevel.Debug       => "<7>dbug: ",
			LogLevel.Information => "<6>info: ",
			LogLevel.Warning     => "<4>warn: ",
			LogLevel.Error       => "<3>fail: ",
			LogLevel.Critical    => "<2>crit: ",
			_                    => throw new ArgumentOutOfRangeException(nameof(logLevel))
		};
	}

	private static string GetSyslogSeverityIndicatorString(LogLevel logLevel)
	{
		return logLevel switch
		{
			LogLevel.Trace       => "<7>",
			LogLevel.Debug       => "<7>",
			LogLevel.Information => "<6>",
			LogLevel.Warning     => "<4>",
			LogLevel.Error       => "<3>",
			LogLevel.Critical    => "<2>",
			_                    => throw new ArgumentOutOfRangeException(nameof(logLevel))
		};
	}
}

#endregion