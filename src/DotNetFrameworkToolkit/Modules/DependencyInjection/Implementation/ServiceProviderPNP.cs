using DotNetFrameworkToolkit.Core;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Utility;
using System;
using System.Collections.Generic;

namespace DotNetFrameworkToolkit.Modules.DependencyInjection;

/// <summary>
/// A Unity-backed provider.
/// </summary>
/// <remarks>
/// For providers constructed from service descriptors:
/// <list type="bullet">
/// <item>Container-created disposables belong to their resolving lifetime</item>
/// <item>Supplied singleton instances remain owned by the caller</item>
/// </list>
/// Successfully created dependencies of a failed resolution remain owned by the resolving
/// provider until shutdown; resolution failure does not roll back their lifetime.
/// </remarks>
public class ServiceProviderPNP : IServiceProvider, IDisposable
{
    private readonly IUnityContainer container;
    private readonly ServiceProviderPNP root;
    private readonly List<ServiceDescriptor> descriptors;
    private readonly OperationLifetime lifetime = new();
    private readonly List<IDisposable> owned = new();
    private readonly List<ServiceProviderPNP> children = new();
    private readonly object sync = new();
    private readonly bool externalContainer;
    private readonly ServiceScopeFactoryPNP scopeFactory;

    /// <summary>
    /// Wraps an externally configured Unity container, retaining native Unity auto-construction
    /// and ownership semantics. For registration-aware null results and tracked transients, use ServiceCollectionPNP.
    /// </summary>
    /// <remarks>
    /// This provider owns the supplied container; do not modify it after wrapping.
    /// Native Unity transients are not tracked by this wrapper. Dispose them according to
    /// the container's native ownership contract, or use ServiceCollectionPNP for tracked transients.
    /// </remarks>
    /// <param name="services">The Unity container to use for service resolution.</param>
    public ServiceProviderPNP(IUnityContainer services) : this(services, new List<ServiceDescriptor>(), null, true)
    {
    }
    internal ServiceProviderPNP(IUnityContainer services, IEnumerable<ServiceDescriptor> servicesToRegister) : this(services, servicesToRegister, null, false)
    {
    }
    private ServiceProviderPNP(IUnityContainer services, IEnumerable<ServiceDescriptor> servicesToRegister, ServiceProviderPNP root, bool external)
    {
        Guard.ArgumentNotNull(services, nameof(services));

        container = services;
        this.root = root ?? this;
        externalContainer = external;
        descriptors = [];

        try
        {
            foreach (ServiceDescriptor descriptor in servicesToRegister)
            {
                descriptors.Add(ServiceCollectionPNP.Copy(descriptor));
            }

            scopeFactory = new ServiceScopeFactoryPNP(this.root);

            foreach (ServiceDescriptor descriptor in descriptors)
            {
                if (descriptor.ImplementationInstance != null)
                {
                    container.RegisterInstance(descriptor.ServiceType, descriptor.ImplementationInstance, new ExternallyControlledLifetimeManager());
                }
                else if (root != null && descriptor.Lifetime == ServiceLifetime.Singleton)
                {
                    container.RegisterType(descriptor.ServiceType, descriptor.ImplementationType, new RootLifetime(root, descriptor.ServiceType));
                }
                else
                {
                    container.RegisterType(
                    descriptor.ServiceType,
                    descriptor.ImplementationType,
                    descriptor.Lifetime == ServiceLifetime.Transient
                        ? (LifetimeManager)new OwnedTransient(this)
                        : new OwnedSingleton(this));
                }
            }

            container.RegisterInstance<IServiceProvider>(this, new ExternallyControlledLifetimeManager());
            container.RegisterInstance<IServiceScopeFactory>(scopeFactory, new ExternallyControlledLifetimeManager());
        }
        catch (Exception constructionError)
        {
            try
            {
                container.Dispose();
            }
            catch (Exception cleanupError)
            {
                throw new Core.AggregateException("Provider construction and container cleanup failed.", constructionError, cleanupError);
            }

            throw;
        }
    }

    /// <summary>
    /// Gets the service object of the specified type from the Unity container.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Registered construction failures propagate</item>
    /// <item>Raw-container wrappers retain native Unity resolution</item>
    /// </list>
    /// </remarks>
    /// <param name="serviceType">The type of service object to get.</param>
    /// <returns>
    /// A service object of type <paramref name="serviceType"/>.
    /// For collection-built providers, returns <c>null</c> for an unregistered service
    /// </returns>
    public object GetService(Type serviceType)
    {
        Guard.ArgumentNotNull(serviceType, nameof(serviceType));

        using (root.lifetime.Enter())
        {
            using (ReferenceEquals(root, this) ? null : lifetime.Enter())
            {
                if (serviceType != typeof(IServiceProvider) && serviceType != typeof(IServiceScopeFactory) && !externalContainer && !Registered(serviceType))
                {
                    return null;
                }

                return container.Resolve(serviceType);
            }
        }
    }

    private bool Registered(Type type)
    {
        foreach (ServiceDescriptor d in descriptors)
        {
            if (d.ServiceType == type || (type.IsGenericType && d.ServiceType.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == d.ServiceType))
            {
                return true;
            }
        }

        return false;
    }

    internal IServiceScope CreateScope()
    {
        using (root.lifetime.Enter())
        {
            ServiceProviderPNP child = new(root.container.CreateChildContainer(), root.descriptors, root, externalContainer);
            lock (root.sync)
            {
                root.children.Add(child);
            }
            return new ServiceScopePNP(child);
        }
    }

    private void Track(object value)
    {
        if (value is not IDisposable disposable)
        {
            return;
        }

        lock (sync)
        {
            foreach (IDisposable existing in owned)
            {
                if (ReferenceEquals(existing, disposable))
                {
                    return;
                }
            }

            owned.Add(disposable);
        }
    }

    /// <summary>
    /// Waits for active resolutions, then disposes owned instances in reverse creation order.
    /// </summary>
    /// <remarks>
    /// Do not dispose from a constructor or synchronously wait for disposal from an active resolution.
    /// The first disposal caller closes admission, drains resolutions, and performs cleanup.
    /// Repeated or concurrent calls are no-ops and may return before that caller finishes.
    /// Only the cleanup caller receives aggregated disposal errors; shutdown remains terminal after failure.
    /// Disposing a parent from a child's independent cleanup is rejected because the child holds a parent operation.
    /// </remarks>
    public void Dispose()
    {
        if (ReferenceEquals(root, this))
        {
            DisposeCore();
            return;
        }

        IDisposable rootOperation;
        try
        {
            rootOperation = root.lifetime.Enter();
        }
        catch (ObjectDisposedException)
        {
            // Root shutdown owns disposal of every remaining child.
            return;
        }

        using (rootOperation)
        {
            DisposeCore();
        }
    }

    private void DisposeCore()
    {
        lifetime.Dispose(() =>
        {
            List<Exception> errors = new();
            ServiceProviderPNP[] scopes;
            lock (sync)
            {
                scopes = children.ToArray();
            }

            foreach (ServiceProviderPNP child in scopes)
            {
                try
                {
                    child.DisposeCore();
                }
                catch (Exception e)
                {
                    errors.Add(e);
                }
            }

            IDisposable[] instances;
            lock (sync)
            {
                instances = owned.ToArray();
                owned.Clear();
                children.Clear();
            }

            for (int i = instances.Length - 1; i >= 0; --i)
            {
                try
                {
                    instances[i].Dispose();
                }
                catch (Exception e)
                {
                    errors.Add(e);
                }
            }

            try
            {
                container.Dispose();
            }
            catch (Exception e)
            {
                errors.Add(e);
            }

            if (!ReferenceEquals(root, this))
            {
                lock (root.sync)
                {
                    root.children.Remove(this);
                }
            }

            if (errors.Count != 0)
            {
                throw new Core.AggregateException(errors);
            }
        });
    }

    private sealed class OwnedTransient : LifetimeManager
    {
        private readonly ServiceProviderPNP owner;
        public OwnedTransient(ServiceProviderPNP owner)
        {
            this.owner = owner;
        }
        public override object GetValue()
        {
            return null;
        }
        public override void SetValue(object value)
        {
            owner.Track(value);
        }
        public override void RemoveValue()
        {
        }
    }

    private sealed class OwnedSingleton : SynchronizedLifetimeManager
    {
        private readonly ServiceProviderPNP owner;
        private object value;
        public OwnedSingleton(ServiceProviderPNP owner)
        {
            this.owner = owner;
        }
        protected override object SynchronizedGetValue()
        {
            return value;
        }
        protected override void SynchronizedSetValue(object newValue)
        {
            value = newValue;
            owner.Track(newValue);
        }
    }

    private sealed class RootLifetime : LifetimeManager
    {
        private readonly ServiceProviderPNP root;
        private readonly Type type;
        public RootLifetime(ServiceProviderPNP root, Type type)
        {
            this.root = root;
            this.type = type;
        }
        public override object GetValue()
        {
            return root.GetService(type);
        }
        public override void SetValue(object value)
        {
        }
        public override void RemoveValue()
        {
        }
    }
}
