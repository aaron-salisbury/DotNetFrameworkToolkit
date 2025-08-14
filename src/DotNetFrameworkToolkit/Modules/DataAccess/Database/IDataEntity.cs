using System;

namespace DotNetFrameworkToolkit.Modules.DataAccess;

/// <summary>
/// Defines the base contract for all data entities that are mapped to database tables.
/// </summary>
public interface IDataEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    uint? Id { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity row was created.
    /// </summary>
    DateTime? CreatedAt { get; set; }
}
