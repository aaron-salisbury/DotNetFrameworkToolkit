# Review fixes and compatibility notes

The library still targets .NET Framework 2.0 and uses a modern C# compiler.
Unit tests are deferred; this branch contains build verification only.

## Results and expected errors

`ProcessResult<T>` is for exceptions deliberately captured at an application boundary.
`Value` on a failed result throws `InvalidOperationException` with the original exception
as its inner exception, preserving the original stack. Prefer `TryGet(out value)` or
check `IsSuccessful`. The explicit cast to `T` was removed to avoid conflicting boolean
conversions; replace it with `Value` or `TryGet`. A successful `false` value still means
that the operation succeeded.

`ProcessResult<T, TError>` models expected failures using an enum. Reserve the enum's
zero/default value for success and create failures with `Failure(error)`. Unexpected
exceptions should normally propagate. Logging failure reporting retains the original
exception even if the logger itself fails.

## DI ownership and shutdown

Providers built by `ServiceCollectionPNP` snapshot registrations and return null for
unregistered top-level requests. Registered construction failures propagate.
Collection indexers/enumerators return descriptor copies; assign an edited descriptor
back through the indexer to change a registration before building a provider.

Container-created disposable transients and scoped services are owned by their
resolving scope; singletons and their dependencies are constructed and owned by the
root. Supplied instances must be singletons and remain caller-owned. Scopes created
from another scope's factory are independent children of the root. Disposal attempts
all owned disposables in reverse creation order and reports accumulated failures.

The Unity 1.2 adapter accepts closed service types. Register required closed generic
types explicitly; open generic registrations are rejected rather than silently giving
them incorrect lifetime ownership. Wrapping a preconfigured Unity container with the
public `ServiceProviderPNP(IUnityContainer)` constructor retains Unity's native
resolution/ownership behavior, including concrete auto-construction; use the service
collection adapter for the modern contract. Do not mutate a wrapped container.

Disposal drains active operations. Do not call disposal from a constructor, formatter,
logging callback, or other active operation, or synchronously wait for another thread
to dispose that lifetime. Same-thread disposal during an operation fails immediately.
`Ioc` disposal is terminal; create a new `Ioc` for another lifecycle.

## Logging and validation

Named templates retain their properties, numeric templates retain standard format
suffixes, and text output includes exception diagnostics. Log scopes enrich entries,
are isolated per thread, and tolerate out-of-order disposal. They are synchronous
thread-owned scopes: create/dispose on the same thread and establish a separate scope
on worker threads. No async scope propagation is promised on .NET 2.0.

The in-memory sink returns read-only snapshots and trims retention atomically,
including when the limit is lowered. Zero means unlimited retention. Event callbacks
run outside its buffer lock; failing observers cannot abort the logged operation.
Observers must remain nonblocking. Validation objects belong to the UI thread; use
`SetEntityLevelErrors` in derived validators to get entity-error notifications.

## Credentials

`UserAuthenticator` is public and snapshots configuration. New credentials use UTF-8
and .NET 2.0's PBKDF2-HMAC-SHA1 implementation with random salts. Persist **all five**
credential fields: `FormatVersion`, `AlgorithmName`, `LoginSalt`, `LoginHash`, and
`LoginWorkFactor`. Dropping version/algorithm fields would misclassify new records.

Version zero still verifies historical ASCII/repeated-salted-hash records using the
configured legacy algorithm. That format retains its historical Unicode collisions;
after successful verification, use `NeedsUpgrade` and persist freshly created
credentials. Do not rewrite existing hashes or label them as version one. Calibrate
work factors for deployment hardware; the compatibility default is not a security
recommendation. Stored iterations are bounded by `MaxVerificationWorkFactor`.

## Files and database

Missing-file deletion now returns `Success(false)`. Writes use a temporary file in the
same directory and publish after all lines are written, preserving an existing file
if enumeration or writing fails. Filenames must be simple names rather than paths.
Timestamps now include the hour and use invariant formatting.

Database corrections include the single-dot `.sdf` filename, failed-path early return,
quoted connection strings, applying migration zero to empty history, checked migration
number conversions, and preserving the original exception if rollback fails.

TODOs remain beside the relevant code for interrupted initialization recovery,
process-wide lock/callback deadlocks, explicit migration discovery and validation,
cross-process migration coordination, and a unique migration-number constraint with
an upgrade path. These require a database design decision and were intentionally
not redesigned ahead of the expected database subsystem removal.

## Building

On Windows, install .NET SDK 8, Visual Studio 2022 MSBuild, and NuGet 6.14.0 on PATH:

```powershell
./build/build.ps1 --target="Compile Projects" --configuration=Release
```

Cake restores the legacy solution's `packages.config` packages into `src/packages`,
including the official net20 reference assemblies, and builds the solution using
Visual Studio MSBuild. Image generation and package creation are separate from the
compile target. Formatting is an explicit verification target and failures propagate.
NuGet packaging selects the exact configuration's output rather than recursive DLL
wildcards that can accidentally include Debug or intermediate assemblies.

GitHub Actions builds Debug and Release on Windows, verifies CLR 2.0 assembly metadata,
and uploads the outputs. This verifies compilation, not behavior or SQL CE integration.
