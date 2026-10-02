using DotNetFrameworkToolkit.Modules.DependencyInjection;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToolkitAggregateException = DotNetFrameworkToolkit.Core.AggregateException;

namespace DotNetFrameworkToolkit.Tests;

[TestClass]
public class DependencyInjectionLifetimeTests
{
    [TestMethod]
    public void DirectFactoryDisposesItsContainerAndAllOutstandingScopes()
    {
        DisposalCounter nativeSingleton = new();
        UnityContainer container = new();
        ServiceDescriptor descriptor = new()
        {
            ServiceType = typeof(DisposalCounter),
            ImplementationType = typeof(DisposalCounter),
            Lifetime = ServiceLifetime.Scoped
        };
        // Use a different registration for the native singleton retained by the container.
        container.RegisterInstance<IDisposable>(nativeSingleton);
        using ServiceScopeFactoryPNP factory = new(container, [descriptor]);
        descriptor.ImplementationType = typeof(ThrowingCounter);
        IServiceScope first = factory.CreateScope();
        IServiceScope second = factory.CreateScope();
        DisposalCounter firstValue = (DisposalCounter)first.ServiceProvider.GetService(typeof(DisposalCounter));
        DisposalCounter secondValue = (DisposalCounter)second.ServiceProvider.GetService(typeof(DisposalCounter));
        Assert.AreNotSame(firstValue, secondValue);
        first.Dispose();
        Assert.AreEqual(1, firstValue.Disposals);
        Assert.AreEqual(0, secondValue.Disposals);
        factory.Dispose();
        factory.Dispose();
        second.Dispose();
        Assert.AreEqual(1, secondValue.Disposals);
        Assert.AreEqual(1, nativeSingleton.Disposals);
        Assert.ThrowsException<ObjectDisposedException>(() => factory.CreateScope());
        Assert.ThrowsException<ObjectDisposedException>(() => second.ServiceProvider.GetService(typeof(DisposalCounter)));
    }

    [TestMethod]
    public void DirectFactoryRetainsCallerOwnershipOfSuppliedDescriptorInstances()
    {
        DisposalCounter supplied = new();
        ServiceDescriptor descriptor = new()
        {
            ServiceType = typeof(DisposalCounter),
            ImplementationInstance = supplied,
            Lifetime = ServiceLifetime.Singleton
        };
        using (ServiceScopeFactoryPNP factory = new(new UnityContainer(), [descriptor]))
        {
            using IServiceScope scope = factory.CreateScope();
            Assert.AreSame(supplied, scope.ServiceProvider.GetService(typeof(DisposalCounter)));
        }
        Assert.AreEqual(0, supplied.Disposals);
        supplied.Dispose();
    }

    [TestMethod]
    public void ResolvedFactoryDoesNotOwnOrCloseItsProvider()
    {
        ServiceCollectionPNP services = [];
        services.AddScoped<DisposalCounter>();
        using ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        ServiceScopeFactoryPNP factory = (ServiceScopeFactoryPNP)root.GetService(typeof(IServiceScopeFactory));
        factory.Dispose();
        using IServiceScope scope = factory.CreateScope();
        DisposalCounter value = (DisposalCounter)scope.ServiceProvider.GetService(typeof(DisposalCounter));
        Assert.AreEqual(0, value.Disposals);
        root.Dispose();
        Assert.AreEqual(1, value.Disposals);
        Assert.ThrowsException<ObjectDisposedException>(() => factory.CreateScope());
    }

    [TestMethod]
    public void NullDescriptorArgumentLeavesTheContainerCallerOwned()
    {
        DisposalCounter supplied = new();
        using UnityContainer container = new();
        container.RegisterInstance<IDisposable>(supplied);
        Assert.ThrowsException<ArgumentNullException>(() => new ServiceScopeFactoryPNP(container, null));
        Assert.AreEqual(0, supplied.Disposals);
        Assert.AreSame(supplied, container.Resolve<IDisposable>());
        container.Dispose();
        Assert.AreEqual(1, supplied.Disposals);
    }

    [TestMethod]
    public void InvalidDescriptorDisposesTheTransferredContainer()
    {
        DisposalCounter nativeSingleton = new();
        UnityContainer container = new();
        container.RegisterInstance(nativeSingleton);
        Assert.ThrowsException<ArgumentException>(() => new ServiceScopeFactoryPNP(container, [new ServiceDescriptor()]));
        Assert.AreEqual(1, nativeSingleton.Disposals);
    }

    [TestMethod]
    public void FailedDescriptorEnumerationDisposesTheTransferredContainer()
    {
        DisposalCounter nativeSingleton = new();
        UnityContainer container = new();
        container.RegisterInstance(nativeSingleton);
        ApplicationException original = new("enumeration failed");
        ApplicationException failure = Assert.ThrowsException<ApplicationException>(() => new ServiceScopeFactoryPNP(container, ThrowingDescriptors(original)));
        Assert.AreSame(original, failure);
        Assert.AreEqual(1, nativeSingleton.Disposals);
    }

    [TestMethod]
    public void ConstructionAndCleanupErrorsAreBothPreserved()
    {
        ThrowingCounter nativeSingleton = new();
        UnityContainer container = new();
        container.RegisterInstance(nativeSingleton);
        ApplicationException original = new("enumeration failed");
        ToolkitAggregateException failure = Assert.ThrowsException<ToolkitAggregateException>(() => new ServiceScopeFactoryPNP(container, ThrowingDescriptors(original)));
        Assert.IsTrue(failure.Flatten().InnerExceptions.Any(error => ReferenceEquals(error, original)));
        Assert.IsTrue(failure.Flatten().InnerExceptions.Any(error => error.Message == "cleanup failed"));
        Assert.AreEqual(1, nativeSingleton.Disposals);
    }

    [TestMethod]
    [Timeout(30000)]
    public void FailedSingletonConstructionCanBeRetriedAndRetainsDependenciesUntilShutdown()
    {
        using Coordination control = new();
        ServiceCollectionPNP services = [];
        services.AddSingleton(control);
        services.AddTransient<RecordedDependency>();
        services.AddSingleton<RetryingSingleton>();
        using ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        Assert.ThrowsException<ResolutionFailedException>(() => root.GetService(typeof(RetryingSingleton)));
        Task<object> retry = Task.Run(() => root.GetService(typeof(RetryingSingleton)));
        Assert.IsTrue(retry.Wait(10000), "Unity did not release the failed singleton's construction lock.");
        Assert.AreSame(retry.Result, root.GetService(typeof(RetryingSingleton)));
        Assert.AreEqual(2, control.Dependencies.Count);
        Assert.IsTrue(control.Dependencies.All(value => value.Disposals == 0));
        root.Dispose();
        Assert.IsTrue(control.Dependencies.All(value => value.Disposals == 1));
        Assert.AreEqual(1, ((RetryingSingleton)retry.Result).Disposals);
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    [Timeout(30000)]
    public void ShutdownDrainsActualUnityResolutionAndRejectsNewWork(bool disposeRoot)
    {
        using Coordination control = new();
        ServiceCollectionPNP services = [];
        services.AddSingleton(control);
        services.AddScoped<BlockingService>();
        using ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        using IServiceScope scope = ((IServiceScopeFactory)root.GetService(typeof(IServiceScopeFactory))).CreateScope();
        IServiceProvider target = scope.ServiceProvider;
        Task<object> resolution = Task.Run(() => target.GetService(typeof(BlockingService)));
        Task shutdown = null;
        try
        {
            Assert.IsTrue(control.Entered.Wait(10000), "Constructor did not enter.");
            shutdown = Task.Run(() =>
            {
                if (disposeRoot)
                {
                    root.Dispose();
                }
                else
                {
                    scope.Dispose();
                }
            });
            Assert.IsTrue(SpinWait.SpinUntil(() => IsClosed(target), 10000), "Shutdown did not close admission.");
            Assert.IsFalse(shutdown.IsCompleted);
            Assert.AreEqual(0, Volatile.Read(ref control.ServiceDisposals));
        }
        finally
        {
            control.Release.Set();
            Assert.IsTrue(resolution.Wait(10000), "Resolution did not drain.");
            if (shutdown != null)
            {
                Assert.IsTrue(shutdown.Wait(10000), "Shutdown did not finish.");
            }
        }
        Assert.IsInstanceOfType(resolution.Result, typeof(BlockingService));
        Assert.AreEqual(1, control.ServiceDisposals);
        Assert.ThrowsException<ObjectDisposedException>(() => target.GetService(typeof(BlockingService)));
        if (!disposeRoot)
        {
            Assert.AreSame(control, root.GetService(typeof(Coordination)));
        }
    }

    [TestMethod]
    [Timeout(30000)]
    public void ConcurrentDisposeReturnsWithoutWaitingForTheCleanupCaller()
    {
        using Coordination control = new();
        ServiceCollectionPNP services = [];
        services.AddSingleton(control);
        services.AddSingleton<BlockingDisposable>();
        using ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        root.GetService(typeof(BlockingDisposable));
        Task first = Task.Run(() => root.Dispose());
        try
        {
            Assert.IsTrue(control.Entered.Wait(10000), "Cleanup did not enter.");
            Task second = Task.Run(() => root.Dispose());
            Assert.IsTrue(second.Wait(10000), "Repeated disposal must not wait on callbacks.");
            Assert.IsFalse(first.IsCompleted);
            Assert.ThrowsException<ObjectDisposedException>(() => root.GetService(typeof(Coordination)));
        }
        finally
        {
            control.Release.Set();
            Assert.IsTrue(first.Wait(10000), "Cleanup did not finish.");
        }
        Assert.AreEqual(1, control.ServiceDisposals);
    }

    [TestMethod]
    [Timeout(30000)]
    public void RootShutdownWaitsForIndependentChildCleanupWithoutDisposingTwice()
    {
        using Coordination control = new();
        ServiceCollectionPNP services = [];
        services.AddSingleton(control);
        services.AddScoped<BlockingDisposable>();
        using ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        using IServiceScope scope = ((IServiceScopeFactory)root.GetService(typeof(IServiceScopeFactory))).CreateScope();
        scope.ServiceProvider.GetService(typeof(BlockingDisposable));
        Task childShutdown = Task.Run(() => scope.Dispose());
        Task rootShutdown = null;
        try
        {
            Assert.IsTrue(control.Entered.Wait(10000), "Child cleanup did not enter.");
            rootShutdown = Task.Run(() => root.Dispose());
            Assert.IsTrue(SpinWait.SpinUntil(() => IsClosed(root), 10000), "Root did not close admission.");
            Assert.IsFalse(rootShutdown.IsCompleted);
            scope.Dispose();
            Assert.AreEqual(0, Volatile.Read(ref control.ServiceDisposals));
        }
        finally
        {
            control.Release.Set();
            Assert.IsTrue(childShutdown.Wait(10000), "Child cleanup did not finish.");
            if (rootShutdown != null)
            {
                Assert.IsTrue(rootShutdown.Wait(10000), "Root cleanup did not finish.");
            }
        }
        Assert.AreEqual(1, control.ServiceDisposals);
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ConstructorDisposalIsRejectedWithoutClosingTheProvider(bool resolveInScope)
    {
        using Coordination control = new();
        ServiceCollectionPNP services = [];
        services.AddSingleton(control);
        services.AddTransient<ReentrantConstructor>();
        using ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        using IServiceScope scope = ((IServiceScopeFactory)root.GetService(typeof(IServiceScopeFactory))).CreateScope();
        IServiceProvider target = resolveInScope ? scope.ServiceProvider : root;
        target.GetService(typeof(ReentrantConstructor));
        Assert.IsInstanceOfType(control.CallbackError, typeof(InvalidOperationException));
        Assert.AreSame(control, target.GetService(typeof(Coordination)));
    }

    [TestMethod]
    public void ReentrantDisposeOnTheClosingProviderIsANoOp()
    {
        using Coordination control = new();
        ServiceCollectionPNP services = [];
        services.AddSingleton(control);
        services.AddSingleton<CallbackDisposable>();
        using ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        control.Callback = () => root.Dispose();
        root.GetService(typeof(CallbackDisposable));
        root.Dispose();
        Assert.AreEqual(1, control.ServiceDisposals);
    }

    [TestMethod]
    public void ChildCleanupCannotSynchronouslyDisposeItsActiveParent()
    {
        using Coordination control = new();
        ServiceCollectionPNP services = [];
        services.AddSingleton(control);
        services.AddScoped<CallbackDisposable>();
        using ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        using IServiceScope scope = ((IServiceScopeFactory)root.GetService(typeof(IServiceScopeFactory))).CreateScope();
        control.Callback = () => root.Dispose();
        scope.ServiceProvider.GetService(typeof(CallbackDisposable));
        ToolkitAggregateException failure = Assert.ThrowsException<ToolkitAggregateException>(() => scope.Dispose());
        Assert.IsInstanceOfType(failure.Flatten().InnerExceptions.Single(), typeof(InvalidOperationException));
        Assert.AreSame(control, root.GetService(typeof(Coordination)));
        Assert.AreEqual(1, control.ServiceDisposals);
        scope.Dispose();
    }

    [TestMethod]
    public void RootAggregatesChildAndRootErrorsAndShutdownRemainsTerminal()
    {
        ServiceCollectionPNP services = [];
        services.AddTransient<ThrowingCounter>();
        using ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        using IServiceScope scope = ((IServiceScopeFactory)root.GetService(typeof(IServiceScopeFactory))).CreateScope();
        ThrowingCounter rootValue = (ThrowingCounter)root.GetService(typeof(ThrowingCounter));
        ThrowingCounter childValue = (ThrowingCounter)scope.ServiceProvider.GetService(typeof(ThrowingCounter));
        ToolkitAggregateException failure = Assert.ThrowsException<ToolkitAggregateException>(() => root.Dispose());
        Assert.AreEqual(2, failure.Flatten().InnerExceptions.Count);
        Assert.AreEqual(1, rootValue.Disposals);
        Assert.AreEqual(1, childValue.Disposals);
        root.Dispose();
        scope.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => root.GetService(typeof(ThrowingCounter)));
    }

    [TestMethod]
    public void RawWrapperKeepsNativeSingletonOwnershipAndDoesNotTrackUnityTransients()
    {
        DisposalCounter nativeSingleton = new();
        UnityContainer container = new();
        container.RegisterInstance<IDisposable>(nativeSingleton);
        using ServiceProviderPNP root = new(container);
        using IServiceScope scope = ((IServiceScopeFactory)root.GetService(typeof(IServiceScopeFactory))).CreateScope();
        DisposalCounter transient = (DisposalCounter)scope.ServiceProvider.GetService(typeof(DisposalCounter));
        Assert.AreSame(nativeSingleton, scope.ServiceProvider.GetService(typeof(IDisposable)));
        scope.Dispose();
        Assert.AreEqual(0, nativeSingleton.Disposals);
        Assert.AreEqual(0, transient.Disposals);
        root.Dispose();
        Assert.AreEqual(1, nativeSingleton.Disposals);
        Assert.AreEqual(0, transient.Disposals);
        transient.Dispose();
    }

    [TestMethod]
    public void IocOwnsOnlyTheProviderWhoseConfigurationSucceeded()
    {
        ServiceCollectionPNP services = [];
        services.AddSingleton<DisposalCounter>();
        using ServiceProviderPNP accepted = (ServiceProviderPNP)services.BuildServiceProvider();
        using ServiceProviderPNP rejected = (ServiceProviderPNP)services.BuildServiceProvider();
        DisposalCounter acceptedValue = (DisposalCounter)accepted.GetService(typeof(DisposalCounter));
        DisposalCounter rejectedValue = (DisposalCounter)rejected.GetService(typeof(DisposalCounter));
        using Ioc ioc = new();
        ioc.ConfigureServices(accepted);
        Assert.ThrowsException<InvalidOperationException>(() => ioc.ConfigureServices(rejected));
        ioc.Dispose();
        Assert.AreEqual(1, acceptedValue.Disposals);
        Assert.AreEqual(0, rejectedValue.Disposals);
        Assert.AreSame(rejectedValue, rejected.GetService(typeof(DisposalCounter)));
        Assert.ThrowsException<ObjectDisposedException>(() => ioc.ConfigureServices(rejected));
        Assert.AreEqual(0, rejectedValue.Disposals);
    }

    [TestMethod]
    public void ExplicitScopeWrapperOwnsTheSuppliedRootProvider()
    {
        ServiceCollectionPNP services = [];
        services.AddSingleton<DisposalCounter>();
        using ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
        DisposalCounter value = (DisposalCounter)root.GetService(typeof(DisposalCounter));
        using ServiceScopePNP wrapper = new(root);
        Assert.AreSame(root, wrapper.ServiceProvider);
        wrapper.Dispose();
        Assert.AreEqual(1, value.Disposals);
        Assert.ThrowsException<ObjectDisposedException>(() => root.GetService(typeof(DisposalCounter)));
    }

    private static IEnumerable<ServiceDescriptor> ThrowingDescriptors(Exception error)
    {
        yield return new ServiceDescriptor
        {
            ServiceType = typeof(DisposalCounter),
            ImplementationType = typeof(DisposalCounter),
            Lifetime = ServiceLifetime.Transient
        };
        throw error;
    }

    private static bool IsClosed(IServiceProvider provider)
    {
        try
        {
            provider.GetService(typeof(string));
            return false;
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }

    public class DisposalCounter : IDisposable
    {
        public int Disposals { get; private set; }

        public virtual void Dispose()
        {
            Disposals++;
        }
    }

    public class ThrowingCounter : DisposalCounter
    {
        public override void Dispose()
        {
            base.Dispose();
            throw new ApplicationException("cleanup failed");
        }
    }

    public sealed class Coordination : IDisposable
    {
        public readonly ManualResetEventSlim Entered = new();
        public readonly ManualResetEventSlim Release = new();
        public readonly List<RecordedDependency> Dependencies = [];
        public int Attempts;
        public int ServiceDisposals;
        public Exception CallbackError;
        public Action Callback;

        public void Dispose()
        {
            Entered.Dispose();
            Release.Dispose();
        }
    }

    public class RecordedDependency : DisposalCounter
    {
        public RecordedDependency(Coordination control)
        {
            control.Dependencies.Add(this);
        }
    }

    public class RetryingSingleton : DisposalCounter
    {
        public RetryingSingleton(Coordination control, RecordedDependency dependency)
        {
            if (Interlocked.Increment(ref control.Attempts) == 1)
            {
                throw new ApplicationException("first construction failed");
            }
        }
    }

    public class BlockingService : IDisposable
    {
        private readonly Coordination _control;

        public BlockingService(Coordination control)
        {
            _control = control;
            control.Entered.Set();
            if (!control.Release.Wait(10000))
            {
                throw new TimeoutException("Constructor was not released.");
            }
        }

        public void Dispose()
        {
            Interlocked.Increment(ref _control.ServiceDisposals);
        }
    }

    public class BlockingDisposable : IDisposable
    {
        private readonly Coordination _control;

        public BlockingDisposable(Coordination control)
        {
            _control = control;
        }

        public void Dispose()
        {
            _control.Entered.Set();
            if (!_control.Release.Wait(10000))
            {
                throw new TimeoutException("Cleanup was not released.");
            }
            Interlocked.Increment(ref _control.ServiceDisposals);
        }
    }

    public class ReentrantConstructor
    {
        public ReentrantConstructor(IServiceProvider provider, Coordination control)
        {
            try
            {
                ((IDisposable)provider).Dispose();
            }
            catch (Exception error)
            {
                control.CallbackError = error;
            }
        }
    }

    public class CallbackDisposable : IDisposable
    {
        private readonly Coordination _control;

        public CallbackDisposable(Coordination control)
        {
            _control = control;
        }

        public void Dispose()
        {
            Interlocked.Increment(ref _control.ServiceDisposals);
            _control.Callback();
        }
    }
}
