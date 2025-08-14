using System;

namespace DotNetFrameworkToolkit.Modules.Logging;

/// <summary>
/// Represents a single log event, including its timestamp, message, severity level, and any associated exception.
/// This class is used to encapsulate all relevant information for a logging operation within the application.
/// </summary>
public class LogEvent
{
    /// <summary>
    /// Gets or sets the date and time when the log event occurred.
    /// </summary>
    public DateTime TimeStamp { get; set; }

    /// <summary>
    /// Gets or sets the log message describing the event.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets the severity level of the log event.
    /// </summary>
    public LogLevel Level { get; set; }

    /// <summary>
    /// Gets or sets the exception associated with the log event, if any.
    /// </summary>
    public Exception Exception { get; set; }
}
