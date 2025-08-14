using System;
using System.Data.Common;

namespace DotNetFrameworkToolkit.Modules.DataAccess;

/// <summary>
/// Represents the base class for database migrations, including methods for executing migration logic
/// and updating the migration tracking table.
/// </summary>
/// <remarks>
/// This implementation of <see cref="IMigration"/> uses SQL Server CE as the database engine.
/// </remarks>
public abstract class SQLServerCEMigration : IMigration
{
    /// <inheritdoc/>
    public abstract uint Number { get; }

    /// <summary>
    /// A brief, human-readable description of the migration's purpose or changes.
    /// </summary>
    public abstract string Description { get; }

    /// <inheritdoc/>
    public void Run(DbConnection dbConnection)
    {
        if (dbConnection == null) { throw new ArgumentNullException("dbConnection"); }

        // Check if this migration number already exists.
        using (DbCommand checkCmd = dbConnection.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(*) FROM Migration WHERE Number = @Number";
            DbParameter param = checkCmd.CreateParameter();
            param.ParameterName = "@Number";
            param.Value = (int)this.Number;
            checkCmd.Parameters.Add(param);

            object result = checkCmd.ExecuteScalar();
            int count = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;
            if (count > 0)
            {
                throw new InvalidOperationException($"Migration number {this.Number} has already been applied.");
            } 
        }

        // Begin transaction, execute migration, update table, commit.
        using var transaction = dbConnection.BeginTransaction();
        try
        {
            Execute(dbConnection, transaction);
            UpdateMigrationTable(dbConnection, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Executes the migration logic.
    /// </summary>
    /// <param name="dbConnection">The database connection to use for the migration.</param>
    /// <param name="transaction">The database transaction within which the migration should be executed.</param>
    protected abstract void Execute(DbConnection dbConnection, DbTransaction transaction);

    /// <summary>
    /// Updates the migration tracking table by inserting a record for this applied migration.
    /// </summary>
    /// <param name="dbConnection">The database connection to use for updating the migration table.</param>
    /// <param name="transaction">The database transaction within which the update should be executed.</param>
    /// <remarks>
    /// This method records the migration's number and description in the <c>Migration</c> table.
    /// </remarks>
    /// <exception cref="System.Data.Common.DbException">
    /// Thrown if the <c>Migration</c> table does not exist or the insert operation fails.
    /// </exception>
    protected void UpdateMigrationTable(DbConnection dbConnection, DbTransaction transaction)
    {
        if (dbConnection == null) { throw new ArgumentNullException("dbConnection"); }
        if (transaction == null) { throw new ArgumentNullException("transaction"); }

        using DbCommand insertCmd = dbConnection.CreateCommand();
        insertCmd.Transaction = transaction;
        insertCmd.CommandText = @"
                INSERT INTO Migration (Number, Description)
                VALUES (@Number, @Description);
            ";

        DbParameter numberParam = insertCmd.CreateParameter();
        numberParam.ParameterName = "@Number";
        numberParam.Value = (int)this.Number;
        insertCmd.Parameters.Add(numberParam);

        DbParameter descParam = insertCmd.CreateParameter();
        descParam.ParameterName = "@Description";
        descParam.Value = this.Description;
        insertCmd.Parameters.Add(descParam);

        insertCmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Returns the SQL script required to create the <c>Migration</c> tracking table in a SQL Server CE database.
    /// </summary>
    public static string GetMigrationTableCreationScript()
    {
        return @"
            CREATE TABLE Migration (
                Id int IDENTITY(1,1) PRIMARY KEY,
                CreatedAt datetime NOT NULL DEFAULT GETDATE(),
                Number int NOT NULL,
                Description ntext NOT NULL
            );
        ";
    }
}
