# Dependency injection ownership and shutdown

The registration-aware `ServiceCollectionPNP` adapter and raw Unity wrapping have different ownership rules. Both use the pinned Unity 1.2 package.

| Construction path | Owner and shutdown |
| --- | --- |
| `ServiceCollectionPNP.BuildServiceProvider()` | The caller owns the returned `ServiceProviderPNP` and disposes it. Root shutdown also closes outstanding child scopes. |
| `IServiceScopeFactory` resolved from a provider | A non-owning factory sharing the root lifetime. Disposing its concrete `ServiceScopeFactoryPNP` is a no-op; dispose the root provider. |
| `new ServiceScopeFactoryPNP(container, descriptors)` | The caller owns and must dispose the factory. It owns its root provider and supplied container, closes outstanding scopes, and prevents subsequent scope creation. Null argument rejection occurs before ownership transfers; descriptor enumeration/registration failure disposes the transferred container. |
| `factory.CreateScope()` | The caller owns the returned scope. Its provider is a child lifetime of the factory's root. Disposing a child leaves the root usable. |
| `new ServiceScopePNP(provider)` | The wrapper owns exactly the supplied provider. Passing a root means disposing the wrapper shuts down that root and its children; this constructor does not create a child. |
| `new ServiceProviderPNP(container)` | The caller owns the wrapper; the wrapper owns the supplied container. Resolution and service ownership otherwise retain native Unity behavior. Do not modify the container after wrapping it. |
| `Ioc.ConfigureServices(provider)` | Successful configuration transfers ownership of a disposable provider to that `Ioc` instance. Rejected configuration does not take ownership of the rejected provider. |

## Registration-aware lifetimes

| Registration | Lifetime and ownership |
| --- | --- |
| Transient type | Each resolution creates an instance; the resolving provider tracks disposable instances until shutdown. |
| Scoped type | One instance per provider, including the root if resolved there. Its provider owns disposable instances. |
| Singleton type | The root creates and owns one instance, including its created dependencies, even when first requested through a child scope. |
| Supplied singleton instance | The caller retains ownership. Neither child nor root shutdown disposes it. |
| Successfully created dependencies of a failed construction | Remain owned by their resolving provider until shutdown. Failed resolution does not roll them back or dispose them early. Unity construction failures propagate and singleton construction can be retried. |

Owned instances are disposed in reverse creation order, so a successfully created consumer is disposed before its dependencies. Root shutdown closes children before disposing root-owned services. Cleanup continues after errors and aggregates those errors.

Raw-container wrappers keep Unity's native ownership behavior: container-controlled singletons are released with their owner, while native transients are not automatically tracked by this toolkit. Use `ServiceCollectionPNP` when toolkit transient tracking is required.

## Shutdown and callbacks

The first disposal caller closes admission, waits for active resolutions and independently running child shutdowns, and performs cleanup. New resolution and scope creation fail with `ObjectDisposedException` once root shutdown begins. Child shutdown closes only that child.

Repeated or concurrent disposal calls are no-ops. They can return before the first caller finishes, and they do not receive that caller's cleanup errors. Coordinate with the first caller when completion is required. Shutdown stays terminal if cleanup throws; a later `Dispose` does not retry cleanup.

Disposal from an active constructor/resolution is rejected with `InvalidOperationException` without closing that lifetime. A child's independent cleanup holds a root operation, so disposing the parent synchronously from that cleanup is also rejected. Calling `Dispose` again on the already closing provider from its own cleanup is a no-op.

Do not synchronously wait for parent disposal from a constructor or callback, including via another thread: it can wait for the operation currently running that callback. These rules do not promise unrestricted callback reentrancy or arbitrary concurrent raw Unity container mutation.

## Example

```csharp
ServiceCollectionPNP services = [];
services.AddScoped<MyService>();

using ServiceProviderPNP root = (ServiceProviderPNP)services.BuildServiceProvider();
IServiceScopeFactory factory = (IServiceScopeFactory)root.GetService(typeof(IServiceScopeFactory));
using IServiceScope scope = factory.CreateScope();
MyService service = (MyService)scope.ServiceProvider.GetService(typeof(MyService));
```

The .NET Framework 4.8.1 tests exercise these contracts against Unity 1.2 on CLR 4. The [installed-package consumer](../consumer_usage.md) additionally verifies scoped identity and disposal on CLR 2.0 in x86/x64 processes on Windows Server 2022; it is a representative smoke check rather than the full contract suite.
