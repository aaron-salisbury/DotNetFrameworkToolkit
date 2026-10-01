using System;
using System.Collections.Generic;

namespace DotNetFrameworkToolkit.Core;

/// <summary>Represents a value or an expected, explicitly modeled error.</summary>
/// <typeparam name="T">The successful value type.</typeparam>
/// <typeparam name="TError">An error enum whose default value is reserved for success.</typeparam>
/// <remarks>Unexpected exceptions should propagate; this type is not an exception container.</remarks>
[Serializable]
public class ProcessResult<T, TError> where TError : struct, Enum
{
    private readonly T _value;
    private readonly TError _error;

    public ProcessResult(T value)
    {
        _value = value;
    }

    private ProcessResult(TError error, bool failure)
    {
        if (EqualityComparer<TError>.Default.Equals(error, default))
        {
            throw new ArgumentOutOfRangeException(nameof(error), "The default error value is reserved for success.");
        }
        _error = error;
    }

    public bool IsSuccessful => EqualityComparer<TError>.Default.Equals(_error, default);
    public TError Error => _error;
    public T ValueOrDefault => _value;
    public T Value
    {
        get
        {
            if (!IsSuccessful) { throw new InvalidOperationException($"The process result is unsuccessful with error '{_error}'."); }
            return _value;
        }
    }
    public bool TryGet(out T value)
    {
        value = _value;
        return IsSuccessful;
    }
    public static ProcessResult<T, TError> Success(T value) => new(value);
    public static ProcessResult<T, TError> Failure(TError error) => new(error, true);
    public static implicit operator bool(ProcessResult<T, TError> result) => result != null && result.IsSuccessful;
    public override string ToString() => IsSuccessful ? $"Success({_value})" : $"Failure({_error})";
}
