using System;

namespace DotNetFrameworkToolkit.Modules.Logging;

/// <summary>
/// Represents a logical operation scope for logging, allowing log entries to be grouped and correlated.
/// Scopes can be nested, and disposing a scope restores the previous parent scope.
/// </summary>
public class LoggerPNPScope : IDisposable
{
    /// <summary>
    /// Gets or sets the parent scope in the scope stack.
    /// </summary>
    public LoggerPNPScope Parent { get; set; }

    private readonly LoggerPNP _provider;
    private readonly object _state;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerPNPScope"/> class and sets it as the current scope for the logger.
    /// </summary>
    /// <param name="provider">The <see cref="LoggerPNP"/> instance that manages this scope.</param>
    /// <param name="state">The state or context object associated with this scope.</param>
    public LoggerPNPScope(LoggerPNP provider, object state)
    {
        _state = state;

        _provider = provider;
        Parent = provider.CurrentScope;
        _provider.CurrentScope = this;
    }

    /// <summary>
    /// Disposes the current scope, restoring the previous parent scope in the logger.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            _provider.CurrentScope = Parent;
        }
    }
}
