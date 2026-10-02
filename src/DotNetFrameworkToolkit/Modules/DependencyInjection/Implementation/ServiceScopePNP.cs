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
