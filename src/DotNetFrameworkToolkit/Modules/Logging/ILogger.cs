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

    #region Convenience Methods

    //------------------------------------------DEBUG------------------------------------------//

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogDebug(0, exception, "Error while processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogDebug(EventId eventId, Exception exception, string message, params object[] args);

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogDebug(0, "Processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogDebug(EventId eventId, string message, params object[] args);

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogDebug(exception, "Error while processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogDebug(Exception exception, string message, params object[] args);

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogDebug("Processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogDebug(string message, params object[] args);

    //------------------------------------------TRACE------------------------------------------//

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogTrace(0, exception, "Error while processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogTrace(EventId eventId, Exception exception, string message, params object[] args);

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogTrace(0, "Processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogTrace(EventId eventId, string message, params object[] args);

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogTrace(exception, "Error while processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogTrace(Exception exception, string message, params object[] args);

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogTrace("Processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogTrace(string message, params object[] args);

    //------------------------------------------INFORMATION------------------------------------------//

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogInformation(0, exception, "Error while processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogInformation(EventId eventId, Exception exception, string message, params object[] args);

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogInformation(0, "Processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogInformation(EventId eventId, string message, params object[] args);

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogInformation(exception, "Error while processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogInformation(Exception exception, string message, params object[] args);

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogInformation("Processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogInformation(string message, params object[] args);

    //------------------------------------------WARNING------------------------------------------//

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogWarning(0, exception, "Error while processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogWarning(EventId eventId, Exception exception, string message, params object[] args);

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogWarning(0, "Processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogWarning(EventId eventId, string message, params object[] args);

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogWarning(exception, "Error while processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogWarning(Exception exception, string message, params object[] args);

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogWarning("Processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogWarning(string message, params object[] args);

    //------------------------------------------ERROR------------------------------------------//

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogError(0, exception, "Error while processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogError(EventId eventId, Exception exception, string message, params object[] args);

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogError(0, "Processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogError(EventId eventId, string message, params object[] args);

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogError(exception, "Error while processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogError(Exception exception, string message, params object[] args);

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogError("Processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogError(string message, params object[] args);

    //------------------------------------------CRITICAL------------------------------------------//

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogCritical(0, exception, "Error while processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogCritical(EventId eventId, Exception exception, string message, params object[] args);

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogCritical(0, "Processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogCritical(EventId eventId, string message, params object[] args);

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogCritical(exception, "Error while processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogCritical(Exception exception, string message, params object[] args);

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogCritical("Processing request from {Address}", address)
    /// </code>
    /// </example>
    void LogCritical(string message, params object[] args);

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    void Log(LogLevel logLevel, string message, params object[] args);

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    void Log(LogLevel logLevel, EventId eventId, string message, params object[] args);

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    void Log(LogLevel logLevel, Exception exception, string message, params object[] args);

    /// <summary>
    /// Formats and writes a log message at the specified log level with an associated event ID and exception.
    /// </summary>
    /// <param name="logLevel">The severity level of the log entry.</param>
    /// <param name="eventId">The identifier for the log event.</param>
    /// <param name="exception">The exception to log, or <c>null</c> if none.</param>
    /// <param name="message">The format string of the log message. Supports message template formatting, e.g., <c>"User {User} failed to log in."</c>.</param>
    /// <param name="args">An object array containing zero or more objects to format.</param>
    void Log(LogLevel logLevel, EventId eventId, Exception exception, string message, params object[] args);

    //------------------------------------------Scope------------------------------------------//

    /// <summary>
    /// Formats the message and creates a scope.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to create the scope in.</param>
    /// <param name="messageFormat">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c>.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <returns>A disposable scope object. Can be null.</returns>
    /// <example>
    /// <code language="csharp">
    /// using(logger.BeginScope("Processing request from {Address}", address)) { }
    /// </code>
    /// </example>
    IDisposable BeginScope(string messageFormat, params object[] args);
    #endregion
}
