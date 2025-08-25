using System;

namespace DotNetFrameworkToolkit.Modules.Logging;

/// <summary>
/// ILogger extension methods for common scenarios.
/// </summary>
public static class LoggerExtensions
{
    private static readonly Func<FormattedLogValues, Exception, string> _messageFormatter = MessageFormatter;

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
    public static void LogDebug(ILogger logger, EventId eventId, Exception exception, string message, params object[] args)
    {
        Log(logger, LogLevel.Debug, eventId, exception, message, args);
    }

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
    public static void LogDebug(ILogger logger, EventId eventId, string message, params object[] args)
    {
        Log(logger, LogLevel.Debug, eventId, message, args);
    }

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
    public static void LogDebug(ILogger logger, Exception exception, string message, params object[] args)
    {
        Log(logger, LogLevel.Debug, exception, message, args);
    }

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
    public static void LogDebug(ILogger logger, string message, params object[] args)
    {
        Log(logger, LogLevel.Debug, message, args);
    }

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
    public static void LogTrace(ILogger logger, EventId eventId, Exception exception, string message, params object[] args)
    {
        Log(logger, LogLevel.Trace, eventId, exception, message, args);
    }

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
    public static void LogTrace(ILogger logger, EventId eventId, string message, params object[] args)
    {
        Log(logger, LogLevel.Trace, eventId, message, args);
    }

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
    public static void LogTrace(ILogger logger, Exception exception, string message, params object[] args)
    {
        Log(logger, LogLevel.Trace, exception, message, args);
    }

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
    public static void LogTrace(ILogger logger, string message, params object[] args)
    {
        Log(logger, LogLevel.Trace, message, args);
    }

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
    public static void LogInformation(ILogger logger, EventId eventId, Exception exception, string message, params object[] args)
    {
        Log(logger, LogLevel.Information, eventId, exception, message, args);
    }

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
    public static void LogInformation(ILogger logger, EventId eventId, string message, params object[] args)
    {
        Log(logger, LogLevel.Information, eventId, message, args);
    }

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
    public static void LogInformation(ILogger logger, Exception exception, string message, params object[] args)
    {
        Log(logger, LogLevel.Information, exception, message, args);
    }

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
    public static void LogInformation(ILogger logger, string message, params object[] args)
    {
        Log(logger, LogLevel.Information, message, args);
    }

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
    public static void LogWarning(ILogger logger, EventId eventId, Exception exception, string message, params object[] args)
    {
        Log(logger, LogLevel.Warning, eventId, exception, message, args);
    }

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
    public static void LogWarning(ILogger logger, EventId eventId, string message, params object[] args)
    {
        Log(logger, LogLevel.Warning, eventId, message, args);
    }

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
    public static void LogWarning(ILogger logger, Exception exception, string message, params object[] args)
    {
        Log(logger, LogLevel.Warning, exception, message, args);
    }

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
    public static void LogWarning(ILogger logger, string message, params object[] args)
    {
        Log(logger, LogLevel.Warning, message, args);
    }

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
    public static void LogError(ILogger logger, EventId eventId, Exception exception, string message, params object[] args)
    {
        Log(logger, LogLevel.Error, eventId, exception, message, args);
    }

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
    public static void LogError(ILogger logger, EventId eventId, string message, params object[] args)
    {
        Log(logger, LogLevel.Error, eventId, message, args);
    }

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
    public static void LogError(ILogger logger, Exception exception, string message, params object[] args)
    {
        Log(logger, LogLevel.Error, exception, message, args);
    }

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
    public static void LogError(ILogger logger, string message, params object[] args)
    {
        Log(logger, LogLevel.Error, message, args);
    }

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
    public static void LogCritical(ILogger logger, EventId eventId, Exception exception, string message, params object[] args)
    {
        Log(logger, LogLevel.Critical, eventId, exception, message, args);
    }

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
    public static void LogCritical(ILogger logger, EventId eventId, string message, params object[] args)
    {
        Log(logger, LogLevel.Critical, eventId, message, args);
    }

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
    public static void LogCritical(ILogger logger, Exception exception, string message, params object[] args)
    {
        Log(logger, LogLevel.Critical, exception, message, args);
    }

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
    public static void LogCritical(ILogger logger, string message, params object[] args)
    {
        Log(logger, LogLevel.Critical, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void Log(ILogger logger, LogLevel logLevel, string message, params object[] args)
    {
        Log(logger, logLevel, 0, null, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void Log(ILogger logger, LogLevel logLevel, EventId eventId, string message, params object[] args)
    {
        Log(logger, logLevel, eventId, null, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void Log(ILogger logger, LogLevel logLevel, Exception exception, string message, params object[] args)
    {
        Log(logger, logLevel, 0, exception, message, args);
    }

    public static void Log(ILogger logger, LogLevel logLevel, EventId eventId, Exception exception, string message, params object[] args)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        FormattedLogValues state = new(message, args);

        logger.Log(logLevel, eventId, state, exception, _messageFormatter);
    }

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
    public static IDisposable BeginScope(ILogger logger, string messageFormat, params object[] args)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        return logger.BeginScope(new FormattedLogValues(messageFormat, args));
    }

    //------------------------------------------HELPERS------------------------------------------//

    private static string MessageFormatter(FormattedLogValues state, Exception error)
    {
        string formattedMessage = state.ToString();

        return formattedMessage;
    }
}
