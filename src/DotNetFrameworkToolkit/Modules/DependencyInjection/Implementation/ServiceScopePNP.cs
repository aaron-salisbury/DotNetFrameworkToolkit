using Microsoft.Practices.Unity.Utility;
using System;

namespace DotNetFrameworkToolkit.Modules.DependencyInjection;

/// <inheritdoc/>
/// <remarks>
/// This implementation uses the Patterns &amp; Practices Enterprise Library.
/// </remarks>
public class ServiceScopePNP : IServiceScope, IDisposable
{
    private readonly ServiceProviderPNP _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceScopePNP"/> class.
    /// </summary>
    /// <param name="serviceProvider">The scoped service provider for this scope.</param>
    /// <remarks>
    /// This wrapper owns the supplied provider and delegates shutdown to it. Wrapping a root
    /// provider therefore disposes that root and its children, rather than creating a child lifetime.
    /// Prefer IServiceScopeFactory.CreateScope when a new child lifetime is required.
    /// </remarks>
    public ServiceScopePNP(ServiceProviderPNP serviceProvider)
    {
        Guard.ArgumentNotNull(serviceProvider, nameof(serviceProvider));

        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public IServiceProvider ServiceProvider => _serviceProvider;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}
