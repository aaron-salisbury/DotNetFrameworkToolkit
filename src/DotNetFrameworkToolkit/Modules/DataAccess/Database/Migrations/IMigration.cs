using System;
using System.Data.Common;

namespace DotNetFrameworkToolkit.Modules.DataAccess;

/// <summary>
/// Defines the contract for a database migration, including methods for executing migration logic
/// and updating the migration tracking table.
/// </summary>
public interface IMigration
{
    /// <summary>
    /// The unique migration number.
    /// </summary>
    uint Number { get; }

    /// <summary>
    /// Executes the migration and updates the migration tracking table.
    /// </summary>
    /// <param name="dbConnection">The database connection to use for the migration.</param>
    /// <exception cref="DbException">
    /// Thrown if the <c>Migration</c> table does not exist or does not contain a <c>Number</c> column.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a migration with the same number already exists in the <c>Migration</c> table.
    /// </exception>
    void Run(DbConnection dbConnection);
}
