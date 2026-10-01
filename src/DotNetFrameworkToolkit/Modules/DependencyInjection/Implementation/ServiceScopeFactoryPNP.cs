using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;

namespace DotNetFrameworkToolkit.Modules.DependencyInjection;

/// <inheritdoc/>
/// <remarks>
/// This implementation uses the Patterns & Practices Enterprise Library.
/// </remarks>
public class ServiceScopeFactoryPNP : IServiceScopeFactory
{
    private readonly IUnityContainer _unityProvider;
    private readonly List<ServiceDescriptor> _scopedServiceDescriptors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceScopeFactoryPNP"/> class.
    /// </summary>
    /// <param name="unityProvider">The root Unity container used to create scoped child containers.</param>
    /// <param name="scopedServiceDescriptors">The scoped service registrations to apply within each new scope.</param>
    public ServiceScopeFactoryPNP(IUnityContainer unityProvider, IEnumerable<ServiceDescriptor> scopedServiceDescriptors)
    {
        if (unityProvider is null)
        {
            throw new ArgumentNullException(nameof(unityProvider));
        }

        if (scopedServiceDescriptors is null)
        {
            throw new ArgumentNullException(nameof(scopedServiceDescriptors));
        }

        _unityProvider = unityProvider;
        _scopedServiceDescriptors = [.. scopedServiceDescriptors];
    }

    /// <inheritdoc/>
    public IServiceScope CreateScope()
    {
        IUnityContainer scopedContainer = _unityProvider.CreateChildContainer();
        RegisterScopedServices(scopedContainer);

        ServiceProviderPNP scopedProvider = new(scopedContainer, _scopedServiceDescriptors);

        return new ServiceScopePNP(scopedProvider);
    }

    private void RegisterScopedServices(IUnityContainer scopedContainer)
    {
        foreach (ServiceDescriptor scopedDescriptor in _scopedServiceDescriptors)
        {
            if (scopedDescriptor.ImplementationInstance != null)
            {
                scopedContainer.RegisterInstance(scopedDescriptor.ServiceType, scopedDescriptor.ImplementationInstance);
            }
            else
            {
                scopedContainer.RegisterType(scopedDescriptor.ServiceType, scopedDescriptor.ImplementationType, new ContainerControlledLifetimeManager());
            }
        }
    }
}
