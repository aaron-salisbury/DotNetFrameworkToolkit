using DotNetFrameworkToolkit.Core;

namespace DotNetFrameworkToolkit.Modules.DataAccess.Database;

/// <summary>
/// Provides methods to ensure the application's database is created and up-to-date.
/// </summary>
public interface IDatabaseInitializer
{
    /// <summary>
    /// Gets the absolute path to the application's database file, constructed from the application's directory and name.
    /// </summary>
    /// <returns>
    /// A <see cref="ProcessResult{T}"/> whose <c>Value</c> property contains the full path to the application's database file.
    /// </returns>
    ProcessResult<string> GetDBPath();

    /// <summary>
    /// Ensures the application's database exists and is current by creating it if necessary
    /// and applying any pending migrations.
    /// </summary>
    /// <returns>
    /// A <see cref="ProcessResult{T}"/> whose <c>Value</c> property is <c>true</c> if the database was initialized; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method should be called during application initialization before configuring DB access services.
    /// </remarks>
    ProcessResult<bool> InitializeDatabase();
}
