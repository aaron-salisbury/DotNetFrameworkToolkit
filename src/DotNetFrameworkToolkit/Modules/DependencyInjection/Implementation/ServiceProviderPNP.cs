using Microsoft.Practices.Unity;
using System;

namespace DotNetFrameworkToolkit.Modules.DependencyInjection;

/// <summary>
/// An <see cref="IServiceProvider"/> implementation that uses a Unity container to resolve services.
/// </summary>
/// <remarks>
/// This implementation uses the Patterns & Practices Enterprise Library.
/// </remarks>
public class ServiceProviderPNP : IServiceProvider
{
    private readonly IUnityContainer _unityProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceProviderPNP"/> class and registers itself as an <see cref="IServiceProvider"/> in the Unity container.
    /// </summary>
    /// <param name="services">The Unity container to use for service resolution.</param>
    public ServiceProviderPNP(IUnityContainer services)
    {
        services.RegisterInstance<IServiceProvider>(this);
        //TODO: Register a IServiceScopeFactory that creates a IServiceScope, which has this IServiceProvider and is disposable.
        //      Which once dispose is called, any scoped services that have been resolved will be disposed.

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
}
