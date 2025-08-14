using Microsoft.Practices.EnterpriseLibrary.Logging;
using System;

namespace DotNetFrameworkToolkit.Modules.Logging;

/// <summary>
/// Represents a log entry that includes an associated exception and an explicit log level.
/// Inherits from <see cref="LogEntry"/> and is used to capture detailed error or exception information for logging purposes.
/// </summary>
public class LogEntryException : LogEntry
{
    /// <summary>
    /// Gets or sets the exception associated with this log entry.
    /// </summary>
    public Exception Exception { get; set; }

    /// <summary>
    /// Gets or sets the severity level of this log entry.
    /// </summary>
    public LogLevel LogLevel { get; set; }
}
