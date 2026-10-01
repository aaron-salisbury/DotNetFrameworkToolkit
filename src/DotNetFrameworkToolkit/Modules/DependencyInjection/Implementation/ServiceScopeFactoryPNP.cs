using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
namespace DotNetFrameworkToolkit.Modules.DependencyInjection;
/// <summary>Creates independent scopes from a root provider.</summary>
public class ServiceScopeFactoryPNP : IServiceScopeFactory
{
    private readonly ServiceProviderPNP root;
    internal ServiceScopeFactoryPNP(ServiceProviderPNP root) { this.root = root; }
    /// <summary>Creates a factory owning a provider over the supplied container and registrations.
    /// Prefer resolving this factory from a ServiceCollectionPNP-built provider.</summary>
    public ServiceScopeFactoryPNP(IUnityContainer unityProvider, IEnumerable<ServiceDescriptor> scopedServiceDescriptors)
    {
        if (scopedServiceDescriptors == null) throw new ArgumentNullException(nameof(scopedServiceDescriptors));
        root = new ServiceProviderPNP(unityProvider, scopedServiceDescriptors);
    }
    /// <inheritdoc/>
    public IServiceScope CreateScope() => root.CreateScope();
}
