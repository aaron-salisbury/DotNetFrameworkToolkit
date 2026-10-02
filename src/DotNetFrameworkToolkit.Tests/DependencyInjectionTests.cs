using DotNetFrameworkToolkit.Modules.DependencyInjection;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToolkitAggregateException = DotNetFrameworkToolkit.Core.AggregateException;

namespace DotNetFrameworkToolkit.Tests;

[TestClass]
public class DependencyInjectionTests
{
    [TestMethod]
    public void MissingRegistrationReturnsNullButRegisteredConstructionFailurePropagates()
    {
        ServiceCollectionPNP services = new();
        services.AddTransient<Broken>();
        using ServiceProviderPNP provider = (ServiceProviderPNP)services.BuildServiceProvider();
        Assert.IsNull(provider.GetService(typeof(DisposableService)));
        Assert.ThrowsException<ResolutionFailedException>(() => provider.GetService(typeof(Broken)));
    }

    [TestMethod]
    public void TransientScopedAndSingletonHaveDistinctLifetimes()
    {
        ServiceCollectionPNP services = new();
        services.AddTransient<Transient>();
        services.AddScoped<Scoped>();
        services.AddSingleton<Singleton>();
        using ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        IServiceScopeFactory factory = (IServiceScopeFactory)root.GetService(typeof(IServiceScopeFactory));
        using IServiceScope first = factory.CreateScope();
        using IServiceScope second = factory.CreateScope();
        Assert.AreNotSame(first.ServiceProvider.GetService(typeof(Transient)), first.ServiceProvider.GetService(typeof(Transient)));
        Assert.AreSame(first.ServiceProvider.GetService(typeof(Scoped)), first.ServiceProvider.GetService(typeof(Scoped)));
        Assert.AreNotSame(first.ServiceProvider.GetService(typeof(Scoped)), second.ServiceProvider.GetService(typeof(Scoped)));
        Assert.AreSame(first.ServiceProvider.GetService(typeof(Singleton)), second.ServiceProvider.GetService(typeof(Singleton)));
        Assert.AreSame(root.GetService(typeof(Singleton)), first.ServiceProvider.GetService(typeof(Singleton)));
    }

    [TestMethod]
    public void ScopeOwnsResolvedTransientsAndDisposesOnlyOnce()
    {
        ServiceCollectionPNP services = new();
        services.AddTransient<DisposableService>();
        using ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        IServiceScope scope = ((IServiceScopeFactory)root.GetService(typeof(IServiceScopeFactory))).CreateScope();
        DisposableService value = (DisposableService)scope.ServiceProvider.GetService(typeof(DisposableService));
        scope.Dispose();
        scope.Dispose();
        Assert.AreEqual(1, value.Disposals);
        Assert.ThrowsException<ObjectDisposedException>(() => scope.ServiceProvider.GetService(typeof(DisposableService)));
    }

    [TestMethod]
    public void OwnedConsumerIsDisposedBeforeItsDependency()
    {
        ServiceCollectionPNP services = new();
        services.AddTransient<DisposableService>();
        services.AddTransient<DisposableConsumer>();
        ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        DisposableConsumer consumer = (DisposableConsumer)root.GetService(typeof(DisposableConsumer));
        root.Dispose();
        Assert.AreEqual(0, consumer.DependencyDisposalsAtShutdown);
        Assert.AreEqual(1, consumer.Disposals);
        Assert.AreEqual(1, consumer.Dependency.Disposals);
    }

    [TestMethod]
    public void SuppliedSingletonIsCallerOwned()
    {
        DisposableService supplied = new();
        ServiceCollectionPNP services = new();
        services.AddInstance(typeof(DisposableService), supplied, ServiceLifetime.Singleton);
        using (ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider())
        {
            using IServiceScope scope = ((IServiceScopeFactory)root.GetService(typeof(IServiceScopeFactory))).CreateScope();
            Assert.AreSame(supplied, scope.ServiceProvider.GetService(typeof(DisposableService)));
        }
        Assert.AreEqual(0, supplied.Disposals);
        supplied.Dispose();
    }

    [TestMethod]
    public void SingletonDependenciesBelongToRootEvenWhenFirstResolvedInScope()
    {
        ServiceCollectionPNP services = new();
        services.AddTransient<DisposableService>();
        services.AddSingleton<SingletonConsumer>();
        ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        try
        {
            IServiceScope scope = ((IServiceScopeFactory)root.GetService(typeof(IServiceScopeFactory))).CreateScope();
            SingletonConsumer consumer = (SingletonConsumer)scope.ServiceProvider.GetService(typeof(SingletonConsumer));
            scope.Dispose();
            Assert.AreEqual(0, consumer.Dependency.Disposals);
            root.Dispose();
            Assert.AreEqual(1, consumer.Dependency.Disposals);
        }
        finally
        {
            root.Dispose();
        }
    }

    [TestMethod]
    public void DisposingRootClosesRemainingScopes()
    {
        ServiceCollectionPNP services = new();
        services.AddScoped<DisposableService>();
        ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        IServiceScope scope = ((IServiceScopeFactory)root.GetService(typeof(IServiceScopeFactory))).CreateScope();
        DisposableService value = (DisposableService)scope.ServiceProvider.GetService(typeof(DisposableService));
        root.Dispose();
        scope.Dispose();
        Assert.AreEqual(1, value.Disposals);
        Assert.ThrowsException<ObjectDisposedException>(() => scope.ServiceProvider.GetService(typeof(DisposableService)));
    }

    [TestMethod]
    public void DisposalContinuesAfterAnOwnedInstanceThrows()
    {
        ServiceCollectionPNP services = new();
        services.AddTransient<DisposableService>();
        services.AddTransient<ThrowingDisposable>();
        ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        DisposableService first = (DisposableService)root.GetService(typeof(DisposableService));
        root.GetService(typeof(ThrowingDisposable));
        ToolkitAggregateException failure = Assert.ThrowsException<ToolkitAggregateException>(() => root.Dispose());
        Assert.AreEqual(1, first.Disposals);
        Assert.IsTrue(failure.Flatten().InnerExceptions.Any(error => error.Message == "dispose failed"));
    }

    [TestMethod]
    public void BuiltProviderAndDescriptorsAreIndependentSnapshots()
    {
        ServiceDescriptor input = new() { ServiceType = typeof(IDisposable), ImplementationType = typeof(DisposableService), Lifetime = ServiceLifetime.Scoped };
        ServiceCollectionPNP services = new();
        services.Add(input);
        input.ImplementationType = typeof(ThrowingDisposable);
        using ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        services[0].ImplementationType = typeof(ThrowingDisposable);
        services.Clear();
        using IServiceScope scope = ((IServiceScopeFactory)root.GetService(typeof(IServiceScopeFactory))).CreateScope();
        Assert.IsInstanceOfType(scope.ServiceProvider.GetService(typeof(IDisposable)), typeof(DisposableService));
    }

    [TestMethod]
    public void EnumerationSnapshotSurvivesCollectionMutation()
    {
        ServiceCollectionPNP services = new();
        services.AddTransient<Transient>();
        IEnumerator<ServiceDescriptor> iterator = services.GetEnumerator();
        services.Clear();
        Assert.IsTrue(iterator.MoveNext());
        Assert.AreEqual(typeof(Transient), iterator.Current.ServiceType);
        iterator.Dispose();
    }

    [TestMethod]
    public void UnsupportedInstanceAndGenericLifetimesAreRejected()
    {
        ServiceCollectionPNP services = new();
        Assert.ThrowsException<ArgumentException>(() => services.AddInstance(typeof(DisposableService), new DisposableService(), ServiceLifetime.Scoped));
        Assert.ThrowsException<NotSupportedException>(() => services.Add(typeof(List<>), typeof(List<>), ServiceLifetime.Transient));
    }

    [TestMethod]
    [Timeout(15000)]
    public void ConcurrentSingletonResolutionReturnsOneInstance()
    {
        ServiceCollectionPNP services = new();
        services.AddSingleton<Singleton>();
        using ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        object[] values = new object[50];
        Parallel.For(0, values.Length, i => values[i] = root.GetService(typeof(Singleton)));
        Assert.IsTrue(values.All(value => ReferenceEquals(values[0], value)));
    }

    [TestMethod]
    public void RawUnityWrapperRetainsConcreteAutoConstruction()
    {
        using ServiceProviderPNP root = new(new UnityContainer());
        Assert.IsInstanceOfType(root.GetService(typeof(Transient)), typeof(Transient));
    }

    [TestMethod]
    public void IocConfigurationIsSingleUseAndDisposalIsTerminal()
    {
        using Ioc ioc = new();
        Assert.ThrowsException<InvalidOperationException>(() => ioc.GetService<object>());
        ServiceCollectionPNP services = new();
        ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        ioc.ConfigureServices(root);
        Assert.IsNull(ioc.GetService<Transient>());
        Assert.ThrowsException<InvalidOperationException>(() => ioc.GetRequiredService<Transient>());
        Assert.ThrowsException<InvalidOperationException>(() => ioc.ConfigureServices(root));
        ioc.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => ioc.GetService<Transient>());
        Assert.ThrowsException<ObjectDisposedException>(() => ioc.ConfigureServices(root));
    }

    public class Transient
    {
    }
    public class Scoped
    {
    }
    public class Singleton
    {
    }
    public class DisposableService : IDisposable
    {
        public int Disposals { get; private set; }
        public void Dispose()
        {
            Disposals++;
        }
    }
    public class DisposableConsumer : IDisposable
    {
        public DisposableService Dependency { get; }
        public int Disposals { get; private set; }
        public int DependencyDisposalsAtShutdown { get; private set; } = -1;

        public DisposableConsumer(DisposableService dependency)
        {
            Dependency = dependency;
        }

        public void Dispose()
        {
            DependencyDisposalsAtShutdown = Dependency.Disposals;
            Disposals++;
        }
    }

    public class SingletonConsumer
    {
        public DisposableService Dependency { get; }
        public SingletonConsumer(DisposableService dependency)
        {
            Dependency = dependency;
        }
    }
    public class Broken
    {
        public Broken()
        {
            throw new ApplicationException("construction failed");
        }
    }
    public class ThrowingDisposable : IDisposable
    {
        public void Dispose()
        {
            throw new ApplicationException("dispose failed");
        }
    }
}
