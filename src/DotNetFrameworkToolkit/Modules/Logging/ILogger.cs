using System;

namespace DotNetFrameworkToolkit.Modules.Logging;

/// <summary>
/// Represents a function that takes two arguments and returns a result.
/// </summary>
/// <typeparam name="T1">The type of the first argument.</typeparam>
/// <typeparam name="T2">The type of the second argument.</typeparam>
/// <typeparam name="TResult">The type of the return value.</typeparam>
public delegate TResult Func<T1, T2, TResult>(T1 arg1, T2 arg2);

/// <summary>
/// Defines a contract for logging messages, exceptions, and contextual information
/// within an application. Implementations of this interface provide methods to write log entries,
/// check if a log level is enabled, and create logical operation scopes for grouping related log entries.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Writes a log entry.
    /// </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">Id of the event.</param>
    /// <param name="state">The entry to be written. Can be also an object.</param>
    /// <param name="exception">The exception related to this entry.</param>
    /// <param name="formatter">Function to create a <see cref="string"/> message of the <paramref name="state"/> and <paramref name="exception"/>.</param>
    /// <typeparam name="TState">The type of the object to be written.</typeparam>
    void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter);

    /// <summary>
    /// Checks if the given <paramref name="logLevel"/> is enabled.
    /// </summary>
    /// <param name="logLevel">Level to be checked.</param>
    /// <returns><see langword="true" /> if enabled.</returns>
    bool IsEnabled(LogLevel logLevel);

    /// <summary>
    /// Begins a logical operation scope.
    /// </summary>
    /// <param name="state">The identifier for the scope.</param>
    /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
    /// <returns>An <see cref="IDisposable"/> that ends the logical operation scope on dispose.</returns>
    IDisposable BeginScope<TState>(TState state) where TState : notnull;
}
