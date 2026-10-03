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
/// This implementation uses the Patterns &amp; Practices Enterprise Library.
/// Scopes belong to their creating thread and must be disposed there, even after logger shutdown.
/// Enabled writes and scope creation reject shutdown; disabled writes remain no-ops.
/// Formatting failures propagate before any entry is written. Do not dispose this logger
/// from its formatter or a sink callback.
/// </remarks>
public class LoggerPNP : ILogger, IDisposable
{
    private readonly LogWriter _writer;

    /// <summary>
    /// Gets the minimum <see cref="LogLevel"/> that will be logged by this logger.
    /// </summary>
    public LogLevel MinimumLevel
    {
        get; private set;
    }

    [ThreadStatic] private static Dictionary<LoggerPNP, LoggerPNPScope> scopes;
    private readonly Core.OperationLifetime lifetime = new();
    internal LoggerPNPScope CurrentScope
    {
        get
        {
            if (scopes != null && scopes.TryGetValue(this, out LoggerPNPScope scope))
            {
                return scope;
            }
            return null;
        }
        set
        {
            scopes ??= [];

            if (value == null)
            {
                scopes.Remove(this);
            }
            else
            {
                scopes[this] = value;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerPNP"/> class with the specified minimum log level and sinks.
    /// </summary>
    /// <param name="minimumLevel">The minimum <see cref="LogLevel"/> to log. Defaults to <see cref="LogLevel.Information"/>.</param>
    /// <param name="sinks">Optional trace listeners to receive log output. If none are provided, a <see cref="ConsoleTraceListener"/> is used.</param>
    public LoggerPNP(LogLevel minimumLevel = LogLevel.Information, params TraceListener[] sinks)
    {
        if (!Enum.IsDefined(typeof(LogLevel), minimumLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(minimumLevel));
        }

        MinimumLevel = minimumLevel;

        _writer = ConfigureLogWriter(sinks);
    }

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return new LoggerPNPScope(this, state);
    }

    // Direct construction and BeginScope share the same atomic lifetime admission.
    internal IDisposable EnterScopeOperation()
    {
        return lifetime.Enter();
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= MinimumLevel && logLevel >= LogLevel.Trace && logLevel < LogLevel.None;
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        using (lifetime.Enter())
        {
            string formattedMessage;
            if (formatter == null)
            {
                formattedMessage = state == null ? string.Empty : state.ToString();
            }
            else
            {
                formattedMessage = formatter.Invoke(state, exception);
            }

            LogEntry entry = BuildLogEntry(logLevel, eventId, formattedMessage, exception);
            List<LoggerPNPScope> chain = new();

            for (LoggerPNPScope scope = CurrentScope; scope != null; scope = scope.Parent)
            {
                if (!scope.IsDisposed)
                {
                    chain.Add(scope);
                }
            }

            List<string> scopeMessages = new();
            for (int i = chain.Count - 1; i >= 0; i--)
            {
                AddProperties(entry, chain[i].State);
                scopeMessages.Add(chain[i].State == null
                    ? string.Empty
                    : chain[i].State.ToString());
            }

            if (scopeMessages.Count != 0)
            {
                entry.ExtendedProperties["Scopes"] = scopeMessages.ToArray();
            }

            AddProperties(entry, state);
            _writer.Write(entry);
        }
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        lifetime.Dispose(() =>
        {
            try
            {
                _writer.Dispose();
            }
            finally
            {
                CurrentScope = null;
            }
        });
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
            Message = exception == null ? message : message + Environment.NewLine + exception.ToString(),
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
            if (eventId.Name != null)
            {
                logEntry.ExtendedProperties["EventName"] = eventId.Name;
            }
        }

        return logEntry;
    }

    private static void AddProperties(LogEntry entry, object state)
    {
        if (state is FormattedLogValues formatted)
        {
            foreach (KeyValuePair<string, object> property in formatted.Properties)
            {
                entry.ExtendedProperties[property.Key] = property.Value;
            }
        }
        else if (state is LoggerState loggerState)
        {
            if (loggerState.SinglePropertyName != null)
            {
                entry.ExtendedProperties[loggerState.SinglePropertyName] = loggerState.SinglePropertyValue;
            }
            if (loggerState.PropertyValuesByNames != null)
            {
                foreach (KeyValuePair<string, object> property in loggerState.PropertyValuesByNames)
                {
                    entry.ExtendedProperties[property.Key] = property.Value;
                }
            }
        }
        else if (state is IEnumerable<KeyValuePair<string, object>> properties)
        {
            foreach (KeyValuePair<string, object> property in properties)
            {
                entry.ExtendedProperties[property.Key] = property.Value;
            }
        }
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
        if (!IsEnabled(logLevel))
        {
            return;
        }

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
        string formattedMessage = state == null ? string.Empty : state.ToString();

        return formattedMessage;
    }
    #endregion
}
