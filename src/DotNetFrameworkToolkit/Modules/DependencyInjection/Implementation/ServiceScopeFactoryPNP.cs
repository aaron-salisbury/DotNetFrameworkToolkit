using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Utility;
using System;
using System.Collections.Generic;

namespace DotNetFrameworkToolkit.Modules.DependencyInjection;

/// <inheritdoc/>
public class ServiceScopeFactoryPNP : IServiceScopeFactory, IDisposable
{
    private readonly ServiceProviderPNP _root;
    private readonly bool _ownsRoot;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceScopeFactoryPNP"/> class from an existing root provider.
    /// </summary>
    /// <param name="root">The root service provider.</param>
    /// <remarks>
    /// The provider creates and registers this factory so scopes share its root lifetime.
    /// Consumers should resolve IServiceScopeFactory from the provider.
    /// </remarks>
    internal ServiceScopeFactoryPNP(ServiceProviderPNP root)
    {
        Guard.ArgumentNotNull(root, nameof(root));

        _root = root;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceScopeFactoryPNP"/> class,
    /// owning a provider over the supplied container and registrations.
    /// </summary>
    /// <param name="unityProvider">The Unity container used to resolve services.</param>
    /// <param name="scopedServiceDescriptors">The scoped service descriptors used to create the root provider.</param>
    /// <remarks>
    /// Prefer resolving this factory from a ServiceCollectionPNP-built provider.
    /// This constructor transfers container ownership to the factory. Dispose the factory to
    /// close all its scopes and release the container. Supplied descriptor instances remain caller-owned.
    /// If descriptor enumeration or registration fails, the container is disposed before the error propagates.
    /// </remarks>
    public ServiceScopeFactoryPNP(IUnityContainer unityProvider, IEnumerable<ServiceDescriptor> scopedServiceDescriptors)
    {
        Guard.ArgumentNotNull(scopedServiceDescriptors, nameof(scopedServiceDescriptors));

        _root = new ServiceProviderPNP(unityProvider, scopedServiceDescriptors);
        _ownsRoot = true;
    }

    /// <inheritdoc/>
    public IServiceScope CreateScope()
    {
        return _root.CreateScope();
    }

    /// <summary>
    /// Disposes the root provider owned by a directly constructed factory.
    /// </summary>
    /// <remarks>
    /// Disposal of a factory resolved from a provider is a no-op: that provider owns the lifetime.
    /// For an owning factory, the root provider's shutdown and reentrancy rules apply.
    /// </remarks>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the owned provider when disposing managed resources.
    /// </summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && _ownsRoot)
        {
            _root.Dispose();
        }
    }
}
