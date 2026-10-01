using System;
using System.Threading;
namespace DotNetFrameworkToolkit.Modules.Logging;
/// <summary>A synchronous, thread-owned logging scope. It must be disposed on its creating thread;
/// .NET 2.0 scopes do not flow across asynchronous or worker-thread boundaries.</summary>
public class LoggerPNPScope : IDisposable
{
    /// <summary>Gets the enclosing scope.</summary>
    public LoggerPNPScope Parent { get; }
    private readonly LoggerPNP provider;
    private readonly int threadId;
    internal object State { get; }
    internal bool IsDisposed { get; private set; }
    /// <summary>Creates a scope for this thread.</summary>
    public LoggerPNPScope(LoggerPNP provider, object state)
    {
        this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        State = state; threadId = Thread.CurrentThread.ManagedThreadId;
        Parent = provider.CurrentScope; provider.CurrentScope = this;
    }
    /// <summary>Marks this scope complete without resurrecting already disposed parents.</summary>
    public void Dispose()
    {
        if (Thread.CurrentThread.ManagedThreadId != threadId) throw new InvalidOperationException("A logging scope must be disposed on its creating thread.");
        if (IsDisposed) return;
        IsDisposed = true;
        LoggerPNPScope current = provider.CurrentScope;
        while (current != null && current.IsDisposed) current = current.Parent;
        provider.CurrentScope = current;
    }
}
