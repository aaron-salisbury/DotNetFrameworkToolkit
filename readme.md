<p align="left">
  <img src="https://raw.githubusercontent.com/aaron-salisbury/DotNetFrameworkToolkit/refs/heads/master/content/logo.png" width="175" alt=".Net Framework Toolkit Logo">
</p>

# .Net Framework Toolkit
Common C# app development and shim code for legacy .Net Framework 2.0 projects.

## Purpose
I occasionally find myself in restricted development scenarios where I target legacy Windows platforms limited to .Net Framework. So I created this library to aid in maintenance and rapid development under those constraints.

## Versioning
This project uses [Semantic Versioning](https://semver.org/).

- **MAJOR** version: Incompatible API changes
- **MINOR** version: Backward-compatible functionality
- **PATCH** version: Backward-compatible bug fixes

## Build Requirements

Contributor guidelines: [development conventions](docs/development_conventions.md).
DI lifetime rules: [ownership and shutdown](docs/dependency_injection.md).
Logging contracts: [lifetimes, scopes, templates, and observers](docs/logging.md).
Results and validation: [public contracts](docs/public_contracts.md).
Stored credentials: [format and migration rules](docs/credentials.md).
Database helpers and native deployment: [SQL Server Compact](docs/sql_server_ce.md).

- The shipped library targets .NET Framework 2.0 / CLR 2.0. The test project targets .NET Framework 4.8.1 and does not ship with the package.
- Use Windows with Visual Studio 2022 MSBuild, the .NET Framework 4.8.1 developer pack and Visual Studio test tools, NuGet CLI on PATH, and the .NET 8 SDK for the Cake.Frosting build executable. The build restores the net20 reference assemblies through NuGet.
- Modern C# syntax does not change the library's runtime target.

Run the tests from the repository root:

```powershell
./build/build.ps1 --target=Test --configuration=Debug
./build/build.ps1 --target=Test --configuration=Release
```

Cake builds the solution before running MSTest through VSTest. Failed tests or zero discovered tests fail the build. TRX results are written to `artifacts/test-results/<configuration>/`; GitHub Actions builds and tests Release only and uploads its results.

The default build also runs tests before publishing binaries and creating the package:

```powershell
./build/build.ps1 --configuration=Release
```

Tests cover managed behavior and the pinned SQL CE provider/native runtime on CLR 4. Actual CLR 2.0 runtime verification remains separate [roadmap](docs/roadmap.md) work.
