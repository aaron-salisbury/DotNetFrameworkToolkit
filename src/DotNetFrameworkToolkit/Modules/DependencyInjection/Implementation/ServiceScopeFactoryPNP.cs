using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Utility;
using System.Collections.Generic;

namespace DotNetFrameworkToolkit.Modules.DependencyInjection;

/// <inheritdoc/>
public class ServiceScopeFactoryPNP : IServiceScopeFactory
{
    private readonly ServiceProviderPNP _root;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceScopeFactoryPNP"/> class from an existing root provider.
    /// </summary>
    /// <param name="root">The root service provider.</param>
    internal ServiceScopeFactoryPNP(ServiceProviderPNP root) { this._root = root; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceScopeFactoryPNP"/> class,
    /// owning a provider over the supplied container and registrations.
    /// </summary>
    /// <param name="unityProvider">The Unity container used to resolve services.</param>
    /// <param name="scopedServiceDescriptors">The scoped service descriptors used to create the root provider.</param>
    /// <remarks>
    /// Prefer resolving this factory from a ServiceCollectionPNP-built provider.
    /// </remarks>
    public ServiceScopeFactoryPNP(IUnityContainer unityProvider, IEnumerable<ServiceDescriptor> scopedServiceDescriptors)
    {
        Guard.ArgumentNotNull(scopedServiceDescriptors, nameof(scopedServiceDescriptors));

        _root = new ServiceProviderPNP(unityProvider, scopedServiceDescriptors);
    }

    /// <inheritdoc/>
    public IServiceScope CreateScope() => _root.CreateScope();
}
