using Microsoft.Practices.Unity.Utility;
using System;
using System.Data.SqlServerCe;
using System.IO;

namespace DotNetFrameworkToolkit.Modules.DataAccess;

/// <summary>
/// Provides explicit-path creation and connection helpers for SQL Server Compact.
/// </summary>
/// <remarks>
/// Applications own database locations, directories, schemas, migrations, and connection options.
/// Native SQL CE binaries matching the managed provider and process architecture must be deployed.
/// No method creates tables, scans assemblies, or applies migrations.
/// </remarks>
public static class SqlServerCeDatabase
{
    /// <summary>
    /// Builds a connection string with a safely encoded, absolute database path.
    /// </summary>
    /// <param name="databasePath">An absolute file path. No extension is prescribed.</param>
    /// <returns>A SQL CE connection string with the normalized path as its data source.</returns>
    /// <remarks>
    /// Relative, drive-relative, and current-drive-rooted paths are rejected. This method performs
    /// no file operations. Use <see cref="SqlCeConnectionStringBuilder"/> to set additional provider options.
    /// </remarks>
    public static string BuildConnectionString(string databasePath)
    {
        return new SqlCeConnectionStringBuilder
        {
            DataSource = ValidateDatabasePath(databasePath)
        }.ConnectionString;
    }

    /// <summary>
    /// Creates a new, empty database using the specified connection string.
    /// </summary>
    /// <param name="connectionString">A SQL CE connection string containing an absolute data source path.</param>
    /// <remarks>
    /// The parent directory must already exist. An existing database is not overwritten.
    /// Schema creation and coordination between creators belong to the application.
    /// Provider errors propagate; this method does not delete files on failure.
    /// </remarks>
    public static void CreateDatabase(string connectionString)
    {
        using SqlCeEngine engine = new(ValidateConnectionString(connectionString));
        engine.CreateDatabase();
    }

    /// <summary>
    /// Opens an existing database and returns a connection owned by the caller.
    /// </summary>
    /// <param name="connectionString">A SQL CE connection string containing an absolute data source path.</param>
    /// <returns>An open connection that the caller must dispose.</returns>
    /// <remarks>
    /// Does not create a missing database. A connection whose Open fails is disposed before
    /// the error propagates. If cleanup also fails, both errors are preserved in an aggregate.
    /// Connections and transactions retain the provider's threading and ownership rules.
    /// </remarks>
    public static SqlCeConnection OpenConnection(string connectionString)
    {
        SqlCeConnection connection = new(ValidateConnectionString(connectionString));
        try
        {
            connection.Open();
            return connection;
        }
        catch (Exception openError)
        {
            try
            {
                connection.Dispose();
            }
            catch (Exception cleanupError)
            {
                throw new Core.AggregateException("Opening the database and disposing the connection failed.", openError, cleanupError);
            }
            throw;
        }
    }

    private static string ValidateConnectionString(string connectionString)
    {
        Guard.ArgumentNotNull(connectionString, nameof(connectionString));

        if (connectionString.Trim().Length == 0)
        {
            throw new ArgumentException("A connection string is required.", nameof(connectionString));
        }

        SqlCeConnectionStringBuilder builder = new(connectionString);
        builder.DataSource = ValidateDatabasePath(builder.DataSource);
        return builder.ConnectionString;
    }

    private static string ValidateDatabasePath(string databasePath)
    {
        Guard.ArgumentNotNull(databasePath, nameof(databasePath));

        if (databasePath.Trim().Length == 0)
        {
            throw new ArgumentException("An absolute database file path is required.", nameof(databasePath));
        }

        string root = Path.GetPathRoot(databasePath);
        if (string.IsNullOrEmpty(root) || root == "\\" || root == "/" || (root.Length == 2 && root[1] == ':'))
        {
            throw new ArgumentException("An absolute database file path is required.", nameof(databasePath));
        }

        return Path.GetFullPath(databasePath);
    }
}
