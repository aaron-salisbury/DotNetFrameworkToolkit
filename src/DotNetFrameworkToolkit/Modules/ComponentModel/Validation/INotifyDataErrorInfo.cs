using System;
using System.Collections;

namespace DotNetFrameworkToolkit.Modules.ComponentModel.Validation;

/// <summary>
/// Defines members for an object that provides error information for data-bound properties.
/// </summary>
public interface INotifyDataErrorInfo
{
    /// <summary>
    /// Gets a value indicating whether the object has validation errors.
    /// </summary>
    bool HasErrors { get; }

    /// <summary>
    /// Occurs when the validation errors have changed for a property or for the entire object.
    /// </summary>
    event EventHandler<DataErrorsChangedEventArgs> ErrorsChangedCore;

    /// <summary>
    /// Gets the validation errors for a specified property or for the entire object.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve validation errors for; or <c>null</c> or <see cref="string.Empty"/> to retrieve errors for the entire object.</param>
    /// <returns>An <see cref="IEnumerable"/> of errors for the specified property or for the entire object.</returns>
    IEnumerable GetErrors(string propertyName);
}
