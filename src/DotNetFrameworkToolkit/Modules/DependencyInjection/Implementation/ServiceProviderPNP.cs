using Microsoft.Practices.Unity;
using DotNetFrameworkToolkit.Core;
using System;
using System.Collections.Generic;

namespace DotNetFrameworkToolkit.Modules.DependencyInjection;

/// <summary>A Unity-backed provider. Container-created disposables belong to their resolving lifetime;
/// supplied singleton instances remain owned by the caller.</summary>
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

    /// <summary>Wraps an externally configured Unity container. This provider owns the container.</summary>
    public ServiceProviderPNP(IUnityContainer services) : this(services, new List<ServiceDescriptor>(), null, true) { }
    internal ServiceProviderPNP(IUnityContainer services, IEnumerable<ServiceDescriptor> servicesToRegister)
        : this(services, servicesToRegister, null, false) { }
    private ServiceProviderPNP(IUnityContainer services, IEnumerable<ServiceDescriptor> servicesToRegister, ServiceProviderPNP root, bool external)
    {
        container = services ?? throw new ArgumentNullException(nameof(services));
        this.root = root ?? this; externalContainer = external;
        descriptors = new List<ServiceDescriptor>();
        foreach (ServiceDescriptor descriptor in servicesToRegister) descriptors.Add(ServiceCollectionPNP.Copy(descriptor));
        scopeFactory = new ServiceScopeFactoryPNP(this.root);
        try
        {
            foreach (ServiceDescriptor descriptor in descriptors)
            {
                if (descriptor.ImplementationInstance != null)
                    container.RegisterInstance(descriptor.ServiceType, descriptor.ImplementationInstance, new ExternallyControlledLifetimeManager());
                else if (root != null && descriptor.Lifetime == ServiceLifetime.Singleton)
                    container.RegisterType(descriptor.ServiceType, descriptor.ImplementationType, new RootLifetime(root, descriptor.ServiceType));
                else
                    container.RegisterType(descriptor.ServiceType, descriptor.ImplementationType,
                        descriptor.Lifetime == ServiceLifetime.Transient ? (LifetimeManager)new OwnedTransient(this) : new OwnedSingleton(this));
            }
            container.RegisterInstance<IServiceProvider>(this, new ExternallyControlledLifetimeManager());
            container.RegisterInstance<IServiceScopeFactory>(scopeFactory, new ExternallyControlledLifetimeManager());
        }
        catch { container.Dispose(); throw; }
    }
    /// <summary>Returns null for an unregistered service; registered construction failures propagate.</summary>
    public object GetService(Type serviceType)
    {
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
        using (root.lifetime.Enter())
        using (ReferenceEquals(root, this) ? null : lifetime.Enter())
        {
            if (serviceType != typeof(IServiceProvider) && serviceType != typeof(IServiceScopeFactory) && !externalContainer && !Registered(serviceType)) return null;
            return container.Resolve(serviceType);
        }
    }
    private bool Registered(Type type)
    {
        foreach (ServiceDescriptor d in descriptors)
            if (d.ServiceType == type || (type.IsGenericType && d.ServiceType.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == d.ServiceType)) return true;
        return false;
    }
    internal IServiceScope CreateScope()
    {
        using (root.lifetime.Enter())
        {
            ServiceProviderPNP child = new(root.container.CreateChildContainer(), root.descriptors, root, externalContainer);
            lock (root.sync) root.children.Add(child);
            return new ServiceScopePNP(child);
        }
    }
    private void Track(object value)
    {
        if (value is not IDisposable disposable) return;
        lock (sync)
        {
            foreach (IDisposable existing in owned) if (ReferenceEquals(existing, disposable)) return;
            owned.Add(disposable);
        }
    }
    /// <summary>Waits for active resolutions, then disposes owned instances in reverse creation order.
    /// Do not dispose from a constructor or synchronously wait for disposal from an active resolution.</summary>
    public void Dispose()
    {
        lifetime.Dispose(() =>
        {
            List<Exception> errors = new();
            ServiceProviderPNP[] scopes;
            lock (sync) scopes = children.ToArray();
            foreach (ServiceProviderPNP child in scopes) try { child.Dispose(); } catch (Exception e) { errors.Add(e); }
            IDisposable[] instances;
            lock (sync) { instances = owned.ToArray(); owned.Clear(); children.Clear(); }
            for (int i = instances.Length - 1; i >= 0; --i) try { instances[i].Dispose(); } catch (Exception e) { errors.Add(e); }
            try { container.Dispose(); } catch (Exception e) { errors.Add(e); }
            if (!ReferenceEquals(root, this)) lock (root.sync) root.children.Remove(this);
            if (errors.Count != 0) throw new Core.AggregateException(errors);
        });
    }
    private sealed class OwnedTransient : LifetimeManager
    {
        private readonly ServiceProviderPNP owner;
        public OwnedTransient(ServiceProviderPNP owner) { this.owner = owner; }
        public override object GetValue() => null;
        public override void SetValue(object value) => owner.Track(value);
        public override void RemoveValue() { }
    }
    private sealed class OwnedSingleton : SynchronizedLifetimeManager
    {
        private readonly ServiceProviderPNP owner;
        private object value;
        public OwnedSingleton(ServiceProviderPNP owner) { this.owner = owner; }
        protected override object SynchronizedGetValue() => value;
        protected override void SynchronizedSetValue(object newValue) { value = newValue; owner.Track(newValue); }
    }
    private sealed class RootLifetime : LifetimeManager
    {
        private readonly ServiceProviderPNP root;
        private readonly Type type;
        public RootLifetime(ServiceProviderPNP root, Type type) { this.root = root; this.type = type; }
        public override object GetValue() => root.GetService(type);
        public override void SetValue(object value) { }
        public override void RemoveValue() { }
    }
}
