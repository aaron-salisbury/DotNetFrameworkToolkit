using System.Collections.Generic;

namespace DotNetFrameworkToolkit.Modules.Logging;

/// <summary>
/// Represents the state of a logger, including support for a single named property or a collection of property values.
/// This class is used to encapsulate contextual information that can be attached to log entries.
/// </summary>
public class LoggerState
{
    /// <summary>
    /// Gets or sets the name of a single property associated with the logger state.
    /// </summary>
    public string SinglePropertyName { get; set; }

    /// <summary>
    /// Gets or sets the value of the single property associated with the logger state.
    /// </summary>
    public object SinglePropertyValue { get; set; }

    /// <summary>
    /// Gets or sets a dictionary containing multiple property names and their corresponding values for the logger state.
    /// </summary>
    public Dictionary<string, object> PropertyValuesByNames { get; set; }

    /// <summary>
    /// Gets a value indicating whether the logger state represents a single property (both name and value are set).
    /// </summary>
    public bool IsSingleProperty => SinglePropertyName != null && SinglePropertyValue != null;
}
