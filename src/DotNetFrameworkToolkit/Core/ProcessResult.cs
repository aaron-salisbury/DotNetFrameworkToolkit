using DotNetFrameworkToolkit.Modules.Logging;
using System;

namespace DotNetFrameworkToolkit.Core;

/// <summary>
/// Represents a result of an operation which can be the actual result or exception.
/// </summary>
/// <typeparam name="T">The type of the value stored in the Result.</typeparam>
/// <remarks>
/// Heavily inspired by the <see href="https://dotnet.github.io/dotNext/features/core/result.html">Result type</see> from .NEXT (dotNext).
/// </remarks>
[Serializable]
public class ProcessResult<T>
{
    private readonly T _value;
    private readonly Exception _exception;
    private readonly bool _hasError;

    /// <summary>
    /// Initializes a new successful result.
    /// </summary>
    /// <param name="value">The value to be stored as result.</param>
    public ProcessResult(T value)
    {
        _value = value;
        _exception = null;
        _hasError = false;
    }

    /// <summary>
    /// Initializes a new unsuccessful result.
    /// </summary>
    /// <param name="error">The exception representing error. Cannot be null.</param>
    public ProcessResult(Exception error)
    {
        _exception = error ?? throw new ArgumentNullException("error");
        _hasError = true;
        _value = default;
    }

    /// <summary>
    /// Extracts the actual result.
    /// </summary>
    /// <exception cref="Exception">This result is not successful.</exception>
    public T Value
    {
        get
        {
            Validate();
            return _value;
        }
    }

    /// <summary>
    /// Gets the value if present; otherwise return default value.
    /// </summary>
    public T ValueOrDefault
    {
        get { return _value; }
    }

    /// <summary>
    /// Gets exception associated with this result.
    /// </summary>
    public Exception Error
    {
        get { return _exception; }
    }

    /// <summary>
    /// Indicates that the result is successful.
    /// </summary>
    public bool IsSuccessful
    {
        get { return !_hasError; }
    }

    /// <summary>
    /// Returns a string that represents the current result, indicating success or failure and the associated value or error.
    /// </summary>
    public override string ToString()
    {
        return IsSuccessful ? "Success(" + _value + ")" : "Failure(" + _exception + ")";
    }

    /// <summary>
    /// Creates a successful <see cref="ProcessResult{T}"/> containing the specified value.
    /// </summary>
    public static ProcessResult<T> Success(T value)
    {
        return new ProcessResult<T>(value);
    }

    /// <summary>
    /// Creates a failed <see cref="ProcessResult{T}"/> containing the specified exception.
    /// </summary>
    public static ProcessResult<T> Failure(Exception error)
    {
        return new ProcessResult<T>(error);
    }

    /// <summary>
    /// Logs a failure message and exception using the specified logger and log level, then returns a failed <see cref="ProcessResult{T}"/>
    /// containing a new exception with the provided message and the original exception as its inner exception.
    /// </summary>
    /// <param name="message">The message to log and to use as the new exception's message.</param>
    /// <param name="error">The original exception to be wrapped and logged.</param>
    /// <param name="logLevel">The severity level at which to log the message.</param>
    /// <param name="logger">The logger to use for logging the failure.</param>
    /// <returns>
    /// A failed <see cref="ProcessResult{T}"/> containing a new exception with the specified message and the original exception as its inner exception.
    /// </returns>
    public static ProcessResult<T> LogAndForwardException(string message, Exception error, ILogger logger, LogLevel logLevel = LogLevel.Error)
    {
        logger.Log(logLevel, message);

        return Failure(new Exception(message, innerException: error));
    }

    /// <summary>
    /// Defines an implicit conversion from <see cref="ProcessResult{T}"/> to <see cref="bool"/>.
    /// </summary>
    public static implicit operator bool(ProcessResult<T> result)
    {
        return result.IsSuccessful;
    }

    /// <summary>
    /// Defines an explicit conversion from <see cref="ProcessResult{T}"/> to the underlying value of type <typeparamref name="T"/>.
    /// </summary>
    public static explicit operator T(ProcessResult<T> result)
    {
        return result.Value;
    }

    private void Validate()
    {
        if (_hasError)
        {
            throw _exception;
        }
    }
}
