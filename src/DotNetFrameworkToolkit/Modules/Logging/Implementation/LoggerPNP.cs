using Microsoft.Practices.EnterpriseLibrary.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DotNetFrameworkToolkit.Modules.Logging;

/// <summary>
/// Log messages, exceptions, and contextual information
/// within an application. Provided methods to write log entries to configured sinks,
/// check if a log level is enabled, and create logical operation scopes for grouping related log entries.
/// </summary>
/// <summary>
/// This implementation uses the Patterns & Practices Enterprise Library.
/// </summary>
public class LoggerPNP : ILogger, IDisposable
{
    /// <summary>
    /// Gets the minimum <see cref="LogLevel"/> that will be logged by this logger.
    /// </summary>
    public LogLevel MinimumLevel { get; private set; }

    internal LoggerPNPScope CurrentScope { get; set; } // TODO: Not implemented yet.

    private readonly LogWriter _writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerPNP"/> class with the specified minimum log level and sinks.
    /// </summary>
    /// <param name="minimumLevel">The minimum <see cref="LogLevel"/> to log. Defaults to <see cref="LogLevel.Information"/>.</param>
    /// <param name="sinks">Optional trace listeners to receive log output. If none are provided, a <see cref="ConsoleTraceListener"/> is used.</param>
    public LoggerPNP(LogLevel minimumLevel = LogLevel.Information, params TraceListener[] sinks)
    {
        MinimumLevel = minimumLevel;

        _writer = ConfigureLogWriter(sinks);
    }

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return new LoggerPNPScope(this, state);
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return Convert.ToInt32(logLevel) >= Convert.ToInt32(MinimumLevel);
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        string formattedMessage;
        if (formatter == null)
        {
            formattedMessage = state.ToString();
        }
        else
        {
            formattedMessage = formatter.Invoke(state, exception);
        }

        _writer.Write(BuildLogEntry(logLevel, eventId, formattedMessage, exception));
    }

    /// <summary>
    /// Releases all resources used by the <see cref="LoggerPNP"/> instance.
    /// </summary>
    public void Dispose()
    {
        _writer.Dispose();
    }

    private static LogWriter ConfigureLogWriter(params TraceListener[] sinks)
    {
        // ref: http://web.archive.org/web/20210330115056/http://codebetter.com/davidhayden/2006/02/19/enterprise-library-2-0-logging-application-block/

        if (sinks == null || sinks.Length == 0)
        {
            sinks = [new ConsoleTraceListener()];
        }

        LogSource mainLogSource = new("MainLogSource", SourceLevels.All);
        mainLogSource.Listeners.Clear();
        mainLogSource.Listeners.AddRange(sinks);

        // Assigning a non-existent LogSource for Logging Application Block Special Sources we don’t care about.
        LogSource nonExistentLogSource = new("Empty");

        // All messages, of any category, get distributed to all TraceListeners in mainLogSource.
        IDictionary<string, LogSource> traceSources = new Dictionary<string, LogSource>();
        foreach (LogLevel logCategory in Enum.GetValues(typeof(LogLevel)))
        {
            traceSources.Add(logCategory.ToString(), mainLogSource);
        }

        string defaultCategory = LogLevel.Error.ToString();

        // No filters at this time.
        return new LogWriter([], traceSources, nonExistentLogSource, nonExistentLogSource, mainLogSource, defaultCategory, false, true);
    }

    private static LogEntry BuildLogEntry(LogLevel logLevel, EventId eventId, string message, Exception exception = null)
    {
        LogEntryException logEntry = new()
        {
            TimeStamp = DateTime.UtcNow,
            Message = message,
            Categories = [logLevel.ToString()],
            Severity = MapLogLevelToTraceEventType(logLevel),
            MachineName = Environment.MachineName,
            AppDomainName = AppDomain.CurrentDomain.FriendlyName,
            LogLevel = logLevel,
            Exception = exception
        };

        if (eventId != null)
        {
            logEntry.EventId = eventId.Id;
        }

        return logEntry;
    }

    private static TraceEventType MapLogLevelToTraceEventType(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace or LogLevel.Debug => TraceEventType.Verbose,
            LogLevel.Information => TraceEventType.Information,
            LogLevel.Warning => TraceEventType.Warning,
            LogLevel.Error => TraceEventType.Error,
            LogLevel.Critical => TraceEventType.Critical,
            LogLevel.None => TraceEventType.Verbose,
            _ => TraceEventType.Verbose,
        };
    }
}
