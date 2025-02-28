using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Iceshrimp.Backend.Core.Extensions;

public static class ConsoleLoggerExtensions
{
	public static ILoggingBuilder AddCustomConsoleFormatter(this ILoggingBuilder builder)
	{
		return builder.AddConsole(options => options.FormatterName = "custom")
		              .AddConsoleFormatter<CustomFormatter, ConsoleFormatterOptions>();
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
		if (!ConsoleUtils.EmitAnsiColorCodes)
		{
			background = null;
			foreground = null;
		}

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

			var enabled = !Console.IsOutputRedirected ||
			              Environment.GetEnvironmentVariable("INVOCATION_ID") is not null;

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

file sealed class CustomFormatter() : ConsoleFormatter("custom"), ISupportExternalScope
{
	private static readonly bool IsSystemd     = Environment.GetEnvironmentVariable("INVOCATION_ID") is not null;
	private static readonly bool LogTimestamps = Environment.GetEnvironmentVariable("LOG_TIMESTAMPS") is "1" or "true";

	private const string LoglevelPadding = ": ";

	// @formatter:off
	private static readonly string MessagePadding =
		new(' ', LogTimestamps ? DateTime.Now.ToDisplayStringTz().Length + 2 : 0 + GetLogLevelString(LogLevel.Information).Length + LoglevelPadding.Length);
	// @formatter:on

	private static readonly string NewLineWithMessagePadding = Environment.NewLine + MessagePadding;

	private IExternalScopeProvider? _scopeProvider;

	public override void Write<TState>(
		in LogEntry<TState> logEntry,
		IExternalScopeProvider? scopeProvider,
		TextWriter textWriter
	)
	{
		var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (logEntry.Exception == null && message == null) return;

		scopeProvider = _scopeProvider ?? scopeProvider;
		Dictionary<string, string> scopes = [];
		scopeProvider?.ForEachScope((p, _) =>
		                            {
			                            if (p is KeyValuePair<string, string> kvp)
				                            scopes.Add(kvp.Key, kvp.Value);
			                            else if (p is Tuple<string, string> tuple)
				                            scopes.Add(tuple.Item1, tuple.Item2);
			                            else if (p is (string key, string value))
				                            scopes.Add(key, value);
			                            else if (p is IEnumerable<KeyValuePair<string, object>> @enum)
				                            foreach (var item in @enum.Where(e => e.Value is string))
					                            scopes.Add(item.Key, (string)item.Value);
		                            },
		                            null as object);

		var scope = scopes.GetValueOrDefault("JobId") ??
		            scopes.GetValueOrDefault("RequestId");

		var logLevel       = logEntry.LogLevel;
		var logLevelColors = GetLogLevelConsoleColors(logLevel);
		var logLevelString = GetLogLevelString(logLevel);

		var prefix = IsSystemd ? GetSyslogSeverityIndicatorString(logEntry.LogLevel) : null;

		if (prefix != null) textWriter.Write(prefix);
		if (LogTimestamps)
		{
			textWriter.WriteColoredMessage(DateTime.Now.ToDisplayStringTz(), null, ConsoleColor.Green);
			textWriter.WriteColoredMessage("| ", null, ConsoleColor.Gray);
		}

		textWriter.WriteColoredMessage(logLevelString, logLevelColors.Background, logLevelColors.Foreground);

		CreateDefaultLogMessage(textWriter, logEntry, message, scope, prefix);
	}

	private static void CreateDefaultLogMessage<TState>(
		TextWriter textWriter, in LogEntry<TState> logEntry,
		string message, string? scope, string? prefix
	)
	{
		var exception  = logEntry.Exception;
		var singleLine = !message.Contains('\n') && exception == null;

		textWriter.Write(LoglevelPadding);
		textWriter.WriteColoredMessage("[", null, ConsoleColor.Gray);
		textWriter.WriteColoredMessage(scope ?? "core", null, ConsoleColor.Cyan);
		textWriter.WriteColoredMessage("] ", null, ConsoleColor.Gray);
		textWriter.WriteColoredMessage(logEntry.Category, null, ConsoleColor.Blue);

		if (singleLine) textWriter.WriteColoredMessage(" >", null, ConsoleColor.Gray);
		else
		{
			textWriter.WriteColoredMessage(":", null, ConsoleColor.Gray);
			textWriter.Write(Environment.NewLine);
		}

		WriteMessage(textWriter, message, singleLine, prefix);

		if (exception != null)
		{
			WriteMessage(textWriter, exception.ToString(), singleLine, prefix);
		}

		if (singleLine)
		{
			textWriter.Write(Environment.NewLine);
		}
	}

	private static void WriteMessage(TextWriter textWriter, string message, bool singleLine, string? prefix)
	{
		if (string.IsNullOrEmpty(message)) return;
		if (singleLine)
		{
			textWriter.Write(' ');
			WriteReplacing(textWriter, Environment.NewLine, " ", message);
		}
		else
		{
			textWriter.Write(prefix ?? "" + MessagePadding);
			if (prefix != null)
				WriteReplacing(textWriter, Environment.NewLine, Environment.NewLine + prefix + MessagePadding, message);
			else
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

	public void SetScopeProvider(IExternalScopeProvider scopeProvider)
	{
		_scopeProvider = scopeProvider;
	}
}

#endregion