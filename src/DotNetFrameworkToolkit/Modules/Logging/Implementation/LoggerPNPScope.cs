using Microsoft.Practices.Unity.Utility;
using System;
using System.Threading;
namespace DotNetFrameworkToolkit.Modules.Logging;

/// <summary>
/// A synchronous, thread-owned logging scope. It must be disposed on its creating thread;
/// </summary>
/// <remarks>
/// .Net Framework 2.0 scopes do not flow across asynchronous or worker-thread boundaries.
/// </remarks>
public class LoggerPNPScope : IDisposable
{
    /// <summary>Gets the enclosing scope.</summary>
    public LoggerPNPScope Parent { get; }

    internal object State { get; }
    internal bool IsDisposed { get; private set; }

    private readonly LoggerPNP _provider;
    private readonly int _threadId;

    /// <summary>
    /// Creates a scope for this thread.
    /// </summary>
    public LoggerPNPScope(LoggerPNP provider, object state)
    {
        Guard.ArgumentNotNull(provider, nameof(provider));

        this._provider = provider;

        State = state;
        _threadId = Thread.CurrentThread.ManagedThreadId;
        Parent = provider.CurrentScope;
        provider.CurrentScope = this;
    }

    /// <summary>
    /// Marks this scope complete without resurrecting already disposed parents.
    /// </summary>
    public void Dispose()
    {
        if (Thread.CurrentThread.ManagedThreadId != _threadId)
        {
            throw new InvalidOperationException("A logging scope must be disposed on its creating thread.");
        }

        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        LoggerPNPScope current = _provider.CurrentScope;

        while (current != null && current.IsDisposed)
        {
            current = current.Parent;
        }

        _provider.CurrentScope = current;
    }
}
