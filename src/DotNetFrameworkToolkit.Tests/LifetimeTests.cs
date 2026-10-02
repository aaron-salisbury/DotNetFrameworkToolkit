using DotNetFrameworkToolkit.Core;
using DotNetFrameworkToolkit.Modules.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetFrameworkToolkit.Tests;

[TestClass]
public class LifetimeTests
{
    [TestMethod]
    public void ReentrantDisposalFailsWithoutClosingLifetime()
    {
        OperationLifetime lifetime = new();
        using (lifetime.Enter())
        {
            Assert.ThrowsException<InvalidOperationException>(() => lifetime.Dispose(() => Assert.Fail("Must not dispose.")));
        }
        int disposals = 0;
        lifetime.Dispose(() => disposals++);
        lifetime.Dispose(() => disposals++);
        Assert.AreEqual(1, disposals);
        Assert.ThrowsException<ObjectDisposedException>(() => lifetime.Enter());
    }

    [TestMethod]
    [Timeout(15000)]
    public void DisposalRejectsNewOperationsAndWaitsForOutstandingLease()
    {
        OperationLifetime lifetime = new();
        IDisposable lease = lifetime.Enter();
        int disposals = 0;
        Task shutdown = Task.Run(() => lifetime.Dispose(() => Interlocked.Increment(ref disposals)));
        try
        {
            Assert.IsTrue(SpinWait.SpinUntil(() => IsClosed(lifetime), 5000), "Shutdown never entered closing state.");
            Assert.AreEqual(0, Volatile.Read(ref disposals));
            Assert.IsFalse(shutdown.IsCompleted);
        }
        finally
        {
            lease.Dispose();
            Assert.IsTrue(shutdown.Wait(5000), "Shutdown did not drain.");
        }
        Assert.AreEqual(1, disposals);
    }

    [TestMethod]
    [Timeout(15000)]
    public void IocDisposalWaitsForBlockedResolutionAndDisposesProviderOnce()
    {
        using BlockingProvider provider = new();
        Ioc ioc = new();
        ioc.ConfigureServices(provider);
        Task<object> resolution = Task.Run(() => ioc.GetService(typeof(object)));
        Task shutdown = null;
        try
        {
            Assert.IsTrue(provider.Entered.Wait(5000), "Resolution did not enter provider.");
            shutdown = Task.Run(() => ioc.Dispose());
            Assert.IsTrue(SpinWait.SpinUntil(() => IocIsClosed(ioc), 5000), "Ioc did not close.");
            Assert.AreEqual(0, provider.Disposals);
        }
        finally
        {
            provider.Release.Set();
            Assert.IsTrue(resolution.Wait(5000), "Resolution did not finish.");
            if (shutdown != null)
            {
                Assert.IsTrue(shutdown.Wait(5000), "Shutdown did not finish.");
            }
            ioc.Dispose();
        }
        Assert.AreSame(provider.Value, resolution.Result);
        Assert.AreEqual(1, provider.Disposals);
    }

    private static bool IsClosed(OperationLifetime lifetime)
    {
        try
        {
            using IDisposable probe = lifetime.Enter();
            return false;
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }

    private static bool IocIsClosed(Ioc ioc)
    {
        try
        {
            // This request is handled without blocking by the fixture.
            ioc.GetService(typeof(string));
            return false;
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }

    private sealed class BlockingProvider : IServiceProvider, IDisposable
    {
        public readonly ManualResetEventSlim Entered = new();
        public readonly ManualResetEventSlim Release = new();
        public readonly object Value = new();
        public int Disposals;
        public object GetService(Type type)
        {
            if (type == typeof(string))
            {
                return null;
            }
            Entered.Set();
            if (!Release.Wait(5000))
            {
                throw new TimeoutException("Fixture was not released.");
            }
            return Value;
        }
        public void Dispose()
        {
            Interlocked.Increment(ref Disposals);
        }
    }
}
