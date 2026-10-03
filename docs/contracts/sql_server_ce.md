# SQL Server Compact helpers

`SqlServerCeDatabase` in `DotNetFrameworkToolkit.Modules.DataAccess` provides three operations:

| Operation | Contract |
| --- | --- |
| `BuildConnectionString(databasePath)` | Encodes an absolute file path with `SqlCeConnectionStringBuilder`. Normalizes dot segments; rejects relative, drive-relative, current-drive-rooted, and `DataDirectory` token paths. Performs no file operations and prescribes no filename extension. |
| `CreateDatabase(connectionString)` | Creates an empty SQL CE database. Requires an existing parent directory and an absolute data source. Does not overwrite an existing database, create tables, apply migrations, or delete files on failure. Creation errors propagate. |
| `OpenConnection(connectionString)` | Opens an existing database. Returns a connection the caller must dispose. Does not create missing databases. Disposes the connection if opening fails, preserving the original error; aggregates opening and cleanup errors if both fail. |

The toolkit project references Microsoft's [`SqlServerCompact` **4.0.8482.1**](https://www.nuget.org/packages/SqlServerCompact/4.0.8482.1). Applications choose directories, names, schemas, entities, keys, migration strategy, concurrency coordination, passwords, and other provider options. No global initialization lock, migration-history table, assembly scanning, or entity base contract is supplied.

## Basic use

```csharp
// The application chooses and creates this directory.
string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyApplication");
Directory.CreateDirectory(directory);
string path = Path.Combine(directory, "prototype.sdf");
string connectionString = SqlServerCeDatabase.BuildConnectionString(path);

// Call only when the application intends to create a new database.
SqlServerCeDatabase.CreateDatabase(connectionString);

using (SqlCeConnection connection = SqlServerCeDatabase.OpenConnection(connectionString))
{
    // The application creates its own schema and uses ordinary ADO.NET commands and transactions.
}
```

Use `SqlCeConnectionStringBuilder` to add options before passing its connection string to creation/opening. For example, an application can set its own password; the helper does not select or log passwords. The same options must be supplied when reopening the database.

Opening a connection is deliberately separate from creating a database. Applications that create on first use must decide how to coordinate competing creators and how to handle an existing, invalid, or incomplete file. A file-existence check alone is not a cross-process creation protocol. Helpers do not automatically repair or replace a database.

Commands, readers, connections, and transactions follow the provider's ownership and threading contracts. Prefer short-lived connections in `using` statements. Use command parameters for values; the toolkit does not introduce a query language or transaction wrapper.

## Deployment and compatibility

Microsoft's pinned package contains the managed provider at `lib/System.Data.SqlServerCe.dll` and matching native engines under `NativeBinaries/x86` and `NativeBinaries/amd64`, including their `Microsoft.VC90.CRT` subfolders.

The library's post-build commands copy the native assets into its output. The test project's MSBuild target copies them into the test output, and its provider verification compares the loaded managed DLL with `lib/System.Data.SqlServerCe.dll` in the restored Microsoft package.

Consumers must ensure that the matching native engine for their process architecture and the accompanying Visual C++ runtime files reach the application's executable output, or arrange an appropriate runtime installation. Copying assets into a library's output does not by itself guarantee deployment into a consuming application's output. Microsoft documents [private deployment in application folders](https://learn.microsoft.com/en-us/aspnet/web-forms/overview/older-versions-getting-started/deployment-to-a-hosting-provider/deployment-to-a-hosting-provider-deploying-sql-server-compact-databases-2-of-12). Review the runtime's redistribution license when packaging an application.

| Evidence | What it establishes |
| --- | --- |
| Toolkit builds against net20 reference assemblies and has CLR metadata `v2.0.50727` | The library's declared compilation/runtime target remains Framework 2.0. |
| Inspected Microsoft package provider metadata | The provider has CLR metadata `v2.0.50727` and references `mscorlib`, `System`, `System.Data`, and `System.Transactions` version 2.0.0.0. This is not native deployment or OS verification. |
| Windows Release tests on Framework 4.8.1 / CLR 4 | The exact pinned managed provider and matching private native engine can create, query, transact, encrypt, reopen, and release databases in the test environment. Tests verify provider bytes/location and loaded native engine location/version to prevent a global installation from silently substituting another version. |
| Installed package consumer on Windows Server 2022 with the Framework 3.5 feature enabled / CLR 2.0 | The x86 and x64 consumers create a database, define a table, insert parameterized values, read data and dispose resources using the installed package. The smoke check also rejects global native-engine substitution. See [consumer verification and deployment limits](../consumer_usage.md). |
| Framework 2.0-only machine | **Not yet runtime-verified.** Microsoft lists Framework 3.5 SP1 or 4 for SQL CE 4.0 managed development and lists specific supported Windows versions in its [runtime requirements](https://www.microsoft.com/en-us/download/details.aspx?id=30709). Package contents and framework metadata do not establish compatibility with every Framework 2.0-era Windows environment. The verified Windows Server 2022 CLR 2.0 host is a separate deployment environment. |

Removing these APIs does not change existing `.sdf` files. Any existing `Migration` table remains in those files; the new helpers neither read nor alter it. Consumers must deliberately retain or replace their previous migration bookkeeping. No automatic file-format upgrade or data migration is performed.
