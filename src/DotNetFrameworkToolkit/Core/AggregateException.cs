using Microsoft.Practices.Unity.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Text;

namespace DotNetFrameworkToolkit.Core;

/// <summary>
/// Represents one or more errors that occur during application execution.
/// </summary>
[Serializable]
public class AggregateException : Exception
{
    private const string InnerExceptionsSerializationName = "InnerExceptions";

    /// <summary>
    /// Gets a read-only collection of the <see cref="Exception"/> instances that caused the current exception.
    /// </summary>
    public ReadOnlyCollection<Exception> InnerExceptions
    {
        get;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateException"/> class.
    /// </summary>
    public AggregateException() : this("One or more errors occurred.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the exception.</param>
    public AggregateException(string message) : base(message)
    {
        this.InnerExceptions = new([]);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public AggregateException(string message, Exception innerException) : this(message, CreateSingleInnerExceptionList(innerException))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateException"/> class with references to the inner exceptions that are the cause of this exception.
    /// </summary>
    /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
    public AggregateException(IEnumerable<Exception> innerExceptions) : this("One or more errors occurred.", innerExceptions)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateException"/> class with references to the inner exceptions that are the cause of this exception.
    /// </summary>
    /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
    public AggregateException(params Exception[] innerExceptions) : this("One or more errors occurred.", (IEnumerable<Exception>)innerExceptions)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateException"/> class with a specified error message and references to the inner exceptions that are the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
    public AggregateException(string message, IEnumerable<Exception> innerExceptions) : this(message, ValidateAndCopyInnerExceptions(innerExceptions))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateException"/> class with a specified error message and references to the inner exceptions that are the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
    public AggregateException(string message, params Exception[] innerExceptions) : this(message, (IEnumerable<Exception>)innerExceptions)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="info"/> is <see langword="null"/>.</exception>
    /// <exception cref="SerializationException">Thrown when the inner exceptions payload is missing or invalid.</exception>
    protected AggregateException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        Exception[] serializedInnerExceptions = (Exception[])info.GetValue(InnerExceptionsSerializationName, typeof(Exception[]));
        List<Exception> innerExceptions = ValidateAndCopyInnerExceptions(serializedInnerExceptions);
        this.InnerExceptions = new(innerExceptions);
    }

    /// <summary>
    /// Sets the <see cref="SerializationInfo"/> object with information about the exception.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> object that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> object that contains contextual information about the source or destination.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="info"/> is <see langword="null"/>.</exception>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        Guard.ArgumentNotNull(info, nameof(info));

        base.GetObjectData(info, context);

        Exception[] innerExceptions = new Exception[this.InnerExceptions.Count];
        for (int i = 0; i < this.InnerExceptions.Count; i++)
        {
            innerExceptions[i] = this.InnerExceptions[i];
        }

        info.AddValue(InnerExceptionsSerializationName, innerExceptions, typeof(Exception[]));
    }

    private AggregateException(string message, List<Exception> innerExceptions)
    : base(message, GetFirstException(innerExceptions))
    {
        this.InnerExceptions = new(innerExceptions);
    }

    private static Exception GetFirstException(IList<Exception> exceptions)
    {
        if (exceptions.Count > 0)
        {
            return exceptions[0];
        }

        return null;
    }

    private static List<Exception> CreateSingleInnerExceptionList(Exception innerException)
    {
        Guard.ArgumentNotNull(innerException, nameof(innerException));

        return [innerException];
    }

    private static List<Exception> ValidateAndCopyInnerExceptions(IEnumerable<Exception> innerExceptions)
    {
        Guard.ArgumentNotNull(innerExceptions, nameof(innerExceptions));

        List<Exception> list = [];
        foreach (Exception ex in innerExceptions)
        {
            if (ex is null)
            {
                throw new ArgumentException("An element of innerExceptions is null.", nameof(innerExceptions));
            }

            list.Add(ex);
        }

        return list;
    }

    /// <summary>
    /// Returns a string that represents the current exception.
    /// </summary>
    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append(base.ToString());

        for (int i = 0; i < this.InnerExceptions.Count; ++i)
        {
            sb.AppendLine();
            sb.Append("---> (Inner Exception #");
            sb.Append(i);
            sb.Append(") ");
            sb.Append(this.InnerExceptions[i]);
            sb.Append("<---");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Flattens an <see cref="AggregateException"/> instances into a single, new instance.
    /// </summary>
    /// <returns>A new, flattened <see cref="AggregateException"/>.</returns>
    public AggregateException Flatten()
    {
        List<Exception> flattenedExceptions = new();
        Queue<AggregateException> exceptionsToFlatten = new();
        exceptionsToFlatten.Enqueue(this);

        while (exceptionsToFlatten.Count > 0)
        {
            AggregateException current = exceptionsToFlatten.Dequeue();
            foreach (var inner in current.InnerExceptions)
            {
                if (inner is AggregateException aggregate)
                {
                    exceptionsToFlatten.Enqueue(aggregate);
                }
                else
                {
                    flattenedExceptions.Add(inner);
                }
            }
        }

        return new AggregateException(this.Message, flattenedExceptions);
    }

    /// <summary>
    /// Invokes a handler on each <see cref="Exception"/> contained by this <see cref="AggregateException"/>.
    /// </summary>
    /// <param name="predicate">The predicate to execute for each exception.</param>
    public void Handle(Predicate<Exception> predicate)
    {
        Guard.ArgumentNotNull(predicate, nameof(predicate));

        List<Exception> unhandled = [];
        foreach (var inner in this.InnerExceptions)
        {
            if (!predicate(inner))
            {
                unhandled.Add(inner);
            }
        }

        if (unhandled.Count > 0)
        {
            throw new AggregateException(this.Message, unhandled);
        }
    }
}
