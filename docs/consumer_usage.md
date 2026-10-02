# Consumer usage and compatibility

The [consumer sample](../samples/ConsumerSmoke/Program.cs) is an executable set of examples. Its [project](../samples/ConsumerSmoke/ConsumerSmoke.csproj) targets .NET Framework 2.0 and references installed package assemblies, with no project reference to the toolkit. Modern Visual Studio MSBuild compiles it against the restored net20 reference assemblies. Its configuration requests only CLR 2.0; it also checks the running CLR and process architecture before exercising the APIs.

## Run the package consumer

On Windows, install the build prerequisites listed in the [README](../readme.md) and enable .NET Framework 3.5, which provides CLR 2.0. For Windows Server, Microsoft's [feature installation guidance](https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/enable-net-framework-35-by-using-windows-powershell) describes `Install-WindowsFeature NET-Framework-Core`. The GitHub workflow enables this feature if needed and fails if installation requires a restart.

From the repository root:

```powershell
./build/build.ps1 --target="Verify Consumer" --configuration=Release
```

This is also the default Release target. It first runs the library tests, image checks and package verification. Consumer verification then creates an empty `artifacts/consumer` directory, builds a local feed from this build's package and restored dependencies, installs through NuGet CLI, and checks the installed package's hash. The reference-assemblies package is installed separately as a build dependency. Both x86 and x64 consumers compile, receive private native SQL CE assets, and run with explicit CLR 2.0 and architecture assertions. The deployed toolkit DLL must match the installed package DLL. SQL CE execution checks that the native query engine is loaded from the matching private architecture directory.

`artifacts/consumer/x86.log`, `x64.log` and `validation.json` record the runtime, architecture, operating system, package hash and results. CI uploads these as `consumer-Release`. Missing dependencies, compile errors, incorrect binaries, runtime mismatches and failed operations fail Cake.

## Examples and migration decisions

| Sample method | Usage | Migration guidance |
| --- | --- | --- |
| `DependencyInjection` | Register a scoped service, resolve through `IServiceScopeFactory`, and dispose scopes and the root. Assert identity within a scope and independent instances across scopes. | Prefer `ServiceCollectionPNP` when disposable transient/scoped ownership is needed. Supplied singleton instances remain caller-owned. Keep application registration policy outside the toolkit; review the [ownership contract](contracts/dependency_injection.md) when replacing raw Unity wrapping. |
| `ResultsAndValidation` | Use explicit success/failure factories and `TryGet`. Supply application-owned validation messages and update observable errors through protected setters. | Reserve the enum's zero value for success. A successful false/null value is still a successful operation. Replace failure-constructor usage with `Failure(error)`. Expected failures belong in enum results; unexpected exceptions normally propagate. The [public contracts](contracts/public_contracts.md) describe copies and notifications. |
| `Logging` | Create a bounded memory sink, dispose the logger, and wrap an operation in a scope before writing a structured message. | Scopes are synchronous thread-local context; dispose on the creating thread in reverse nesting order. Sink observers should handle their own exceptions. Review the [logging contract](contracts/logging.md) before relying on mutable event objects, observer propagation or shutdown callbacks. |
| `Credentials` | Persist salt, hash and work factor, restore all three, and verify a Unicode password. | Store each credential's work factor, not just the current new-user configuration. Base64 encodes bytes; it is not encryption. Select work factors for the actual deployment environment; the sample's value is for a smoke check. Previous ASCII-derived non-ASCII credentials require an application-owned reset/migration policy; the toolkit does not silently retry the old encoding. |
| `Database` | Create a database at an explicit absolute path, open/dispose a connection, create an application-defined table and use parameters for values. | Move entity types, primary-key policy, mapping, schema versions and migrations into the consuming application. Replace the removed entity/repository/migration abstractions with application-owned persistence code using these low-level helpers or direct ADO.NET. Existing `.sdf` data is not automatically converted; decide how to handle any old migration bookkeeping. See [SQL CE deployment](contracts/sql_server_ce.md). |

For an existing consumer, install `AaronSalisbury.DotNetFrameworkToolkit` with the required version through NuGet. It selects `lib/net20`; deploy its resolved managed dependencies and the pinned SQL CE provider's matching native assets. Traditional projects need explicit assembly references/HintPaths and native copy rules; the sample illustrates both. A package install alone does not establish that native assets reached the executable directory. Copy `NativeBinaries/x86` and `NativeBinaries/amd64`, including their private CRT subdirectories, next to the executable when using private deployment.

## Names and runtime boundaries

| Concern | Consumer responsibility |
| --- | --- |
| Modern compiler versus legacy runtime | Modern C# syntax is compiled against net20 APIs. Build tools use .NET 10; unit tests use net481/CLR 4. Neither changes the consumer's net20 target or proves CLR 2.0 execution. |
| `Func<T1,T2,TResult>` | The logging namespace defines its own delegate. On .NET 3.5 or later, fully qualify `DotNetFrameworkToolkit.Modules.Logging.Func<...>` when a delegate declaration would be ambiguous with `System.Func<...>`. The delegate types are distinct. |
| `AggregateException` | Qualify `DotNetFrameworkToolkit.Core.AggregateException` when referencing it alongside .NET 4 or later's `System.AggregateException`. |
| Observable validation shims | `DotNetFrameworkToolkit.Modules.ComponentModel.Validation.INotifyDataErrorInfo` and `DataErrorsChangedEventArgs` are toolkit types, distinct from later `System.ComponentModel` types. Alias or qualify names; newer UI controls do not automatically treat the shim as their framework interface. |
| SQL CE | Managed metadata targets CLR 2.0, but deployment also needs matching native binaries/CRT and a supported process/OS. Consult Microsoft's [SQL CE 4 SP1 requirements](https://www.microsoft.com/en-us/download/details.aspx?id=30709); Framework 3.5 SP1 or 4 is listed for managed development. |
| Verification environment | The CI consumer runs on Windows Server 2022 with the .NET Framework 3.5 feature enabled, using CLR 2.0 in x86 and x64 processes. This is not a Framework 2.0-only machine. Historical Windows platforms and Framework 2.0-only installations remain unverified, particularly SQL CE native compatibility. |

The smoke examples exercise representative paths. They complement the library's broader CLR 4 test suite and do not establish every API, UI integration, operating system or application-specific migration strategy.
