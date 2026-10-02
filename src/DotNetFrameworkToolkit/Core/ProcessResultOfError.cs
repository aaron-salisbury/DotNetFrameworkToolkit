using System;
using System.Collections.Generic;

namespace DotNetFrameworkToolkit.Core;

/// <summary>
/// Represents a value or an expected, explicitly modeled error.
/// </summary>
/// <typeparam name="T">The successful value type.</typeparam>
/// <typeparam name="TError">An error enum whose default value is reserved for success.</typeparam>
/// <remarks>Unexpected exceptions should propagate; this type is not an exception container.</remarks>
[Serializable]
public class ProcessResult<T, TError> where TError : struct, Enum
{
    private readonly T _value;
    private readonly TError _error;

    /// <summary>Creates a successful result, including for null or false values.</summary>
    public ProcessResult(T value)
    {
        _value = value;
    }

    // Two typed arguments keep failure construction unambiguous when T and TError are identical.
    private ProcessResult(T value, TError error) : this(value)
    {
        if (EqualityComparer<TError>.Default.Equals(error, default))
        {
            throw new ArgumentOutOfRangeException(nameof(error), "The default error value is reserved for success.");
        }

        _error = error;
    }

    /// <summary>Reports whether the stored error is the zero/default success sentinel.</summary>
    public bool IsSuccessful => EqualityComparer<TError>.Default.Equals(_error, default);

    /// <summary>Gets the modeled error, or zero/default on success.</summary>
    public TError Error => _error;

    /// <summary>Gets the successful value, or the value type's default on failure.</summary>
    public T ValueOrDefault => _value;

    /// <summary>Gets the successful value.</summary>
    /// <exception cref="InvalidOperationException">The result is unsuccessful.</exception>
    public T Value
    {
        get
        {
            if (!IsSuccessful)
            {
                throw new InvalidOperationException($"The process result is unsuccessful with error '{_error}'.");
            }
            return _value;
        }
    }

    /// <summary>Returns the success flag and the value, or default on failure.</summary>
    public bool TryGet(out T value)
    {
        value = _value;
        return IsSuccessful;
    }

    /// <summary>Creates a successful result without interpreting the value as an error.</summary>
    public static ProcessResult<T, TError> Success(T value)
    {
        return new(value);
    }

    /// <summary>Creates a failure with any nonzero error, including an undefined enum value.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The error is zero/default.</exception>
    public static ProcessResult<T, TError> Failure(TError error)
    {
        return new(default(T), error);
    }

    /// <summary>Reports operation success; a null result converts to false.</summary>
    public static implicit operator bool(ProcessResult<T, TError> result)
    {
        return result != null && result.IsSuccessful;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return IsSuccessful ? $"Success({_value})" : $"Failure({_error})";
    }
}
