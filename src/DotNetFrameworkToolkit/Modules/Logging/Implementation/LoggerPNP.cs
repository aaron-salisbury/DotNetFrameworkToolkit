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
/// <remarks>
/// This implementation uses the Patterns & Practices Enterprise Library.
/// </remarks>
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

    #region Explicit Interface Implementation of Convenience Methods
    private static readonly Func<FormattedLogValues, Exception, string> _messageFormatter = MessageFormatter;

    //------------------------------------------DEBUG------------------------------------------//

    /// <inheritdoc/>
    public void LogDebug(EventId eventId, Exception exception, string message, params object[] args)
    {
        Log(LogLevel.Debug, eventId, exception, message, args);
    }

    /// <inheritdoc/>
    public void LogDebug(EventId eventId, string message, params object[] args)
    {
        Log(LogLevel.Debug, eventId, message, args);
    }

    /// <inheritdoc/>
    public void LogDebug(Exception exception, string message, params object[] args)
    {
        Log(LogLevel.Debug, exception, message, args);
    }

    /// <inheritdoc/>
    public void LogDebug(string message, params object[] args)
    {
        Log(LogLevel.Debug, message, args);
    }

    //------------------------------------------TRACE------------------------------------------//

    /// <inheritdoc/>
    public void LogTrace(EventId eventId, Exception exception, string message, params object[] args)
    {
        Log(LogLevel.Trace, eventId, exception, message, args);
    }

    /// <inheritdoc/>
    public void LogTrace(EventId eventId, string message, params object[] args)
    {
        Log(LogLevel.Trace, eventId, message, args);
    }

    /// <inheritdoc/>
    public void LogTrace(Exception exception, string message, params object[] args)
    {
        Log(LogLevel.Trace, exception, message, args);
    }

    /// <inheritdoc/>
    public void LogTrace(string message, params object[] args)
    {
        Log(LogLevel.Trace, message, args);
    }

    //------------------------------------------INFORMATION------------------------------------------//

    /// <inheritdoc/>
    public void LogInformation(EventId eventId, Exception exception, string message, params object[] args)
    {
        Log(LogLevel.Information, eventId, exception, message, args);
    }

    /// <inheritdoc/>
    public void LogInformation(EventId eventId, string message, params object[] args)
    {
        Log(LogLevel.Information, eventId, message, args);
    }

    /// <inheritdoc/>
    public void LogInformation(Exception exception, string message, params object[] args)
    {
        Log(LogLevel.Information, exception, message, args);
    }

    /// <inheritdoc/>
    public void LogInformation(string message, params object[] args)
    {
        Log(LogLevel.Information, message, args);
    }

    //------------------------------------------WARNING------------------------------------------//

    /// <inheritdoc/>
    public void LogWarning(EventId eventId, Exception exception, string message, params object[] args)
    {
        Log(LogLevel.Warning, eventId, exception, message, args);
    }

    /// <inheritdoc/>
    public void LogWarning(EventId eventId, string message, params object[] args)
    {
        Log(LogLevel.Warning, eventId, message, args);
    }

    /// <inheritdoc/>
    public void LogWarning(Exception exception, string message, params object[] args)
    {
        Log(LogLevel.Warning, exception, message, args);
    }

    /// <inheritdoc/>
    public void LogWarning(string message, params object[] args)
    {
        Log(LogLevel.Warning, message, args);
    }

    //------------------------------------------ERROR------------------------------------------//

    /// <inheritdoc/>
    public void LogError(EventId eventId, Exception exception, string message, params object[] args)
    {
        Log(LogLevel.Error, eventId, exception, message, args);
    }

    /// <inheritdoc/>
    public void LogError(EventId eventId, string message, params object[] args)
    {
        Log(LogLevel.Error, eventId, message, args);
    }

    /// <inheritdoc/>
    public void LogError(Exception exception, string message, params object[] args)
    {
        Log(LogLevel.Error, exception, message, args);
    }

    /// <inheritdoc/>
    public void LogError(string message, params object[] args)
    {
        Log(LogLevel.Error, message, args);
    }

    //------------------------------------------CRITICAL------------------------------------------//

    /// <inheritdoc/>
    public void LogCritical(EventId eventId, Exception exception, string message, params object[] args)
    {
        Log(LogLevel.Critical, eventId, exception, message, args);
    }

    /// <inheritdoc/>
    public void LogCritical(EventId eventId, string message, params object[] args)
    {
        Log(LogLevel.Critical, eventId, message, args);
    }

    /// <inheritdoc/>
    public void LogCritical(Exception exception, string message, params object[] args)
    {
        Log(LogLevel.Critical, exception, message, args);
    }

    /// <inheritdoc/>
    public void LogCritical(string message, params object[] args)
    {
        Log(LogLevel.Critical, message, args);
    }

    /// <inheritdoc/>
    public void Log(LogLevel logLevel, string message, params object[] args)
    {
        Log(logLevel, 0, null, message, args);
    }

    /// <inheritdoc/>
    public void Log(LogLevel logLevel, EventId eventId, string message, params object[] args)
    {
        Log(logLevel, eventId, null, message, args);
    }

    /// <inheritdoc/>
    public void Log(LogLevel logLevel, Exception exception, string message, params object[] args)
    {
        Log(logLevel, 0, exception, message, args);
    }

    /// <inheritdoc/>
    public void Log(LogLevel logLevel, EventId eventId, Exception exception, string message, params object[] args)
    {
        FormattedLogValues state = new(message, args);

        Log(logLevel, eventId, state, exception, _messageFormatter);
    }

    //------------------------------------------Scope------------------------------------------//

    /// <inheritdoc/>
    public IDisposable BeginScope(string messageFormat, params object[] args)
    {
        return BeginScope(new FormattedLogValues(messageFormat, args));
    }

    //------------------------------------------HELPERS------------------------------------------//

    private static string MessageFormatter(FormattedLogValues state, Exception error)
    {
        string formattedMessage = state.ToString();

        return formattedMessage;
    }
    #endregion
}
