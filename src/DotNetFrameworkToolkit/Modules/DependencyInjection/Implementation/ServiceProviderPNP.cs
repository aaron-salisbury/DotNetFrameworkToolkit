using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Utility;
using System;
using System.Collections.Generic;

namespace DotNetFrameworkToolkit.Modules.DependencyInjection;

/// <summary>
/// An <see cref="IServiceProvider"/> implementation that uses a Unity container to resolve services.
/// </summary>
/// <remarks>
/// This implementation uses the Patterns & Practices Enterprise Library.
/// </remarks>
public class ServiceProviderPNP : IServiceProvider, IDisposable
{
    private readonly IUnityContainer _unityProvider;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceProviderPNP"/> class and registers itself as an <see cref="IServiceProvider"/> in the Unity container.
    /// </summary>
    /// <param name="services">The Unity container to use for service resolution.</param>
    public ServiceProviderPNP(IUnityContainer services) : this(services, [])
    {
    }

    internal ServiceProviderPNP(IUnityContainer services, IEnumerable<ServiceDescriptor> scopedServiceDescriptors)
    {
        Guard.ArgumentNotNull(services, nameof(services));
        Guard.ArgumentNotNull(scopedServiceDescriptors, nameof(scopedServiceDescriptors));

        List<ServiceDescriptor> scopedDescriptors = [.. scopedServiceDescriptors];

        services.RegisterInstance<IServiceProvider>(this);
        services.RegisterInstance<IServiceScopeFactory>(new ServiceScopeFactoryPNP(services, scopedDescriptors));

        _unityProvider = services;
    }

    /// <summary>
    /// Gets the service object of the specified type from the Unity container.
    /// </summary>
    /// <param name="serviceType">The type of service object to get.</param>
    /// <returns>
    /// A service object of type <paramref name="serviceType"/>. 
    /// Returns <c>null</c> if the service is not found.
    /// </returns>
    public object GetService(Type serviceType)
    {
        return _unityProvider.Resolve(serviceType);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        _unityProvider.Dispose();
    }
}
