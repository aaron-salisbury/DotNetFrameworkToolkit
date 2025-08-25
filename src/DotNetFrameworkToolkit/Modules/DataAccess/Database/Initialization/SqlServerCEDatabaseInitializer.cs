using DotNetFrameworkToolkit.Core;
using DotNetFrameworkToolkit.Modules.DataAccess.FileSystem;
using DotNetFrameworkToolkit.Modules.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.IO;
using System.Reflection;

namespace DotNetFrameworkToolkit.Modules.DataAccess.Database;

/// <summary>
/// Provides methods to ensure the application's database is created and up-to-date.
/// </summary>
/// <remarks>
/// This implementation uses SQL Server CE as the database engine.
/// </remarks>
public class SqlServerCEDatabaseInitializer : IDatabaseInitializer
{
    #region Old Code I still Might Need
    //public void ExecuteQueries(IEnumerable<string> sqlQueries)
    //{
    //    try
    //    {
    //        using (SqlCeConnection sqlConnection = new(_connectionString))
    //        {
    //            sqlConnection.Open();

    //            using (SqlCeCommand sqlCommand = new())
    //            {
    //                sqlCommand.Connection = sqlConnection;

    //                foreach (string sqlQuery in sqlQueries)
    //                {
    //                    sqlCommand.CommandText = sqlQuery;
    //                    sqlCommand.ExecuteNonQuery();
    //                }
    //            }

    //            sqlConnection.Close();
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Failed to execute SQL query.");
    //    }
    //}
    #endregion

    private const string DB_FILE_EXTENSION = ".sdf";

    private static readonly object _initLock = new();

    private readonly ILogger _logger;
    private readonly IFileSystemAccess _fileSystemAccess;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerCEDatabaseInitializer"/> class with the specified logger.
    /// </summary>
    /// <param name="logger">The logger used to record informational messages, warnings, and errors related to database initialization.</param>
    /// <param name="fileSystemAccess">The utility for interacting with the operating system's files and directories.</param>
    public SqlServerCEDatabaseInitializer(ILogger logger, IFileSystemAccess fileSystemAccess)
    {
        _logger = logger;
        _fileSystemAccess = fileSystemAccess;
    }

    /// <inheritdoc/>
    public ProcessResult<string> GetDBPath()
    {
        //TODO: Have to test connection string and that this connection works, and not just in debug. All the examples I see use |DataDirectory|, like the following:
        //      https://github.dev/zzzprojects/EntityFramework-Classic/blob/4953e0478771ec1b065503a12a8bbef132569492/test/Shared/FunctionalTests/SqlServerCompact/CodePlex2197.cs#L83#L120
        //string dbPath = DefaultDbPath();
        //string connectionString = $"Data Source=\"{dbPath}\";";

        ProcessResult<string> appDirectoryResult = _fileSystemAccess.GetAppDirectoryPath();
        if (!appDirectoryResult.IsSuccessful)
        {
            LoggerExtensions.LogError(_logger, "Failed to retrieve path to the application's database file.");
            return appDirectoryResult;
        }

        try
        {
            string appFolderPath = appDirectoryResult.Value;
            string appName = new DirectoryInfo(appFolderPath).Name;
            string dbPath = Path.Combine(appFolderPath, $"{appName}.{DB_FILE_EXTENSION}"); ;
            return ProcessResult<string>.Success(dbPath);
        }
        catch (Exception ex)
        {
            LoggerExtensions.LogError(_logger, ex, "Failed to retrieve path to the application's database file.");
            return ProcessResult<string>.Failure(ex);
        }
    }

    /// <inheritdoc/>
    public ProcessResult<bool> InitializeDatabase()
    {
        lock (_initLock)
        {
            ProcessResult<string> dbPathResult = GetDBPath();
            if (!dbPathResult.IsSuccessful)
            {
                ProcessResult<bool>.LogAndForwardException("Failed to initialize database.", dbPathResult.Error, _logger);
            }

            try
            {
                string dbPath = dbPathResult.Value;

                if (File.Exists(dbPath))
                {
                    UpdateExistingDB(dbPath);
                }
                else
                {
                    CreateNewDB(dbPath);
                }

                return ProcessResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ProcessResult<bool>.LogAndForwardException("Failed to initialize database.", ex, _logger);
            }
        }
    }

    private static void CreateNewDB(string dbPath)
    {
        string connectionString = $"Data Source=\"{dbPath}\";";

        using (SqlCeEngine engine = new(connectionString))
        {
            engine.CreateDatabase();
        }

        using SqlCeConnection connection = new(connectionString);
        connection.Open();

        string migrationTableCreationScript = SQLServerCEMigration.GetMigrationTableCreationScript();

        using SqlCeCommand command = new(migrationTableCreationScript, connection);
        command.ExecuteNonQuery();

        RunMigrations(connection, 0);
    }

    private static void UpdateExistingDB(string dbPath)
    {
        using SqlCeConnection connection = new($"Data Source={dbPath}");
        connection.Open();

        uint lastMigrationNumber = 0;
        const string getMaxNumberSql = "SELECT MAX(Number) FROM Migration;";
        using (SqlCeCommand getMaxNumberCmd = new(getMaxNumberSql, connection))
        {
            object result = getMaxNumberCmd.ExecuteScalar();
            if (result != DBNull.Value && result != null)
            {
                lastMigrationNumber = Convert.ToUInt32(result);
            }
        }

        RunMigrations(connection, ++lastMigrationNumber);
    }

    private static void RunMigrations(DbConnection connection, uint startingNumber)
    {
        SortedList<uint, IMigration> migrations = [];

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type assemblyType in assembly.GetTypes())
            {
                if (!assemblyType.IsAbstract
                    && !assemblyType.IsInterface
                    && typeof(IMigration).IsAssignableFrom(assemblyType)
                    && Activator.CreateInstance(assemblyType) is IMigration migration)
                {
                    if (migration.Number >= startingNumber)
                    {
                        migrations.Add(migration.Number, migration);
                    }
                }
            }
        }

        foreach (IMigration migration in migrations.Values)
        {
            migration.Run(connection);
        }
    }
}
