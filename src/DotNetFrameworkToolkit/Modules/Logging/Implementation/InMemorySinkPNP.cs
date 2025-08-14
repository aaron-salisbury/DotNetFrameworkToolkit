using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging.Formatters;
using Microsoft.Practices.EnterpriseLibrary.Logging.TraceListeners;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DotNetFrameworkToolkit.Modules.Logging;

/// <summary>
/// A custom trace listener that stores log messages in memory for inspection or testing purposes.
/// Supports a configurable maximum number of log entries and emits an event when a log is written.
/// </summary>
[ConfigurationElementType(typeof(CustomTraceListenerData))]
public class InMemorySinkPNP : CustomTraceListener
{
    /// <summary>
    /// Occurs when a log event is emitted and added to the in-memory log collection.
    /// </summary>
    public event EventHandler<LogEmitEventArgs> LogEmitted;

    private readonly IList<string> _logs;
    /// <summary>
    /// Gets the collection of log messages currently stored in memory.
    /// </summary>
    public IList<string> Logs { get { return _logs; } }

    /// <summary>
    /// Gets or sets the maximum number of log messages to retain in memory.
    /// When the limit is reached, the oldest log entry is removed.
    /// </summary>
    public int MaxLogsCount { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemorySinkPNP"/> class.
    /// </summary>
    /// <param name="maxLogsCount">The maximum number of log messages to retain. Defaults to 1000.</param>
    /// <param name="formatter">The log formatter to use. If <c>null</c>, a default formatter is used.</param>
    public InMemorySinkPNP(int maxLogsCount = 1000, ILogFormatter formatter = null)
    {
        _logs = [];

        MaxLogsCount = maxLogsCount;
        Formatter = formatter ?? DefaultFormatter();
    }

    /// <summary>
    /// Writes a log message to the in-memory collection.
    /// </summary>
    /// <param name="message">The log message to write.</param>
    public override void Write(string message)
    {
        if (MaxLogsCount > 0 && Logs.Count >= MaxLogsCount)
        {
            Logs.RemoveAt(0);
        }

        Logs.Add(message);
    }

    /// <summary>
    /// Writes a log message followed by a line terminator to the in-memory collection.
    /// </summary>
    /// <param name="message">The log message to write.</param>
    public override void WriteLine(string message)
    {
        Write(message);
    }

    /// <summary>
    /// Processes trace data, formats it, stores it in memory, and raises the <see cref="LogEmitted"/> event.
    /// </summary>
    /// <param name="eventCache">The context information provided by the trace system.</param>
    /// <param name="source">The name of the source.</param>
    /// <param name="eventType">The type of event.</param>
    /// <param name="id">The event identifier.</param>
    /// <param name="data">The data to trace, typically a <see cref="LogEntry"/> or <see cref="LogEntryException"/>.</param>
    public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
    {
        DateTime timeStamp = DateTime.UtcNow;
        string message = data.ToString();
        LogLevel level = LogLevel.None;
        Exception exception = null;

        if (data is LogEntry logEntry)
        {
            timeStamp = logEntry.TimeStamp;
            level = MapTraceEventTypeToLogLevel(logEntry.Severity); // If not the LogEntryException subclass, then LogLevel wasn't explicitly saved.

            if (Formatter != null)
            {
                message = Formatter.Format(logEntry);
            }
        }

        if (data is LogEntryException logEntryException)
        {
            level = logEntryException.LogLevel;
            exception = logEntryException.Exception;
        }

        WriteLine(message);

        LogEmitted?.Invoke(this, new LogEmitEventArgs()
        {
            LogEvent = new()
            {
                TimeStamp = timeStamp,
                Message = message,
                Level = level,
                Exception = exception
            }
        });
    }

    private static ILogFormatter DefaultFormatter()
    {
        string textFormatterTemplate = "{timestamp(yyyy-MM-dd HH:mm:ss.ff)} UTC [{category}] {message}";

        return new TextFormatter(textFormatterTemplate);
    }

    private static LogLevel MapTraceEventTypeToLogLevel(TraceEventType trace)
    {
        return trace switch
        {
            TraceEventType.Verbose => LogLevel.Debug,
            TraceEventType.Information => LogLevel.Information,
            TraceEventType.Warning => LogLevel.Warning,
            TraceEventType.Error => LogLevel.Error,
            TraceEventType.Critical => LogLevel.Critical,
            _ => LogLevel.None,
        };
    }
}

/// <summary>
/// Provides data for the <see cref="InMemorySinkPNP.LogEmitted"/> event, containing the emitted <see cref="LogEvent"/>.
/// </summary>
public class LogEmitEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the <see cref="LogEvent"/> associated with the emitted log entry.
    /// </summary>
    public LogEvent LogEvent { get; set; }
}
